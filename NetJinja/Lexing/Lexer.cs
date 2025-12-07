using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using NetJinja.Exceptions;

namespace NetJinja.Lexing;

/// <summary>
/// High-performance lexer for Jinja templates.
/// Converts template source into a stream of tokens.
/// </summary>
public sealed class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private readonly int _length;

    // Delimiters (configurable)
    private readonly string _variableStart;
    private readonly string _variableEnd;
    private readonly string _blockStart;
    private readonly string _blockEnd;
    private readonly string _commentStart;
    private readonly string _commentEnd;

    // Keyword lookup for fast matching
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.Ordinal)
    {
        ["if"] = TokenType.If,
        ["elif"] = TokenType.Elif,
        ["else"] = TokenType.Else,
        ["endif"] = TokenType.Endif,
        ["for"] = TokenType.For,
        ["endfor"] = TokenType.Endfor,
        ["in"] = TokenType.In,
        ["not"] = TokenType.Not,
        ["and"] = TokenType.And,
        ["or"] = TokenType.Or,
        ["is"] = TokenType.Is,
        ["block"] = TokenType.Block,
        ["endblock"] = TokenType.Endblock,
        ["extends"] = TokenType.Extends,
        ["include"] = TokenType.Include,
        ["import"] = TokenType.Import,
        ["from"] = TokenType.From,
        ["as"] = TokenType.As,
        ["macro"] = TokenType.Macro,
        ["endmacro"] = TokenType.Endmacro,
        ["call"] = TokenType.Call,
        ["endcall"] = TokenType.Endcall,
        ["set"] = TokenType.Set,
        ["endset"] = TokenType.Endset,
        ["with"] = TokenType.With,
        ["endwith"] = TokenType.Endwith,
        ["autoescape"] = TokenType.Autoescape,
        ["endautoescape"] = TokenType.Endautoescape,
        ["raw"] = TokenType.Raw,
        ["endraw"] = TokenType.Endraw,
        ["true"] = TokenType.True,
        ["True"] = TokenType.True,
        ["false"] = TokenType.False,
        ["False"] = TokenType.False,
        ["none"] = TokenType.None,
        ["None"] = TokenType.None,
        ["recursive"] = TokenType.Recursive,
        ["continue"] = TokenType.Continue,
        ["break"] = TokenType.Break,
    };

    public Lexer(string source, LexerOptions? options = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _length = source.Length;
        options ??= LexerOptions.Default;

        _variableStart = options.VariableStart;
        _variableEnd = options.VariableEnd;
        _blockStart = options.BlockStart;
        _blockEnd = options.BlockEnd;
        _commentStart = options.CommentStart;
        _commentEnd = options.CommentEnd;
    }

    /// <summary>
    /// Tokenizes the entire template source.
    /// </summary>
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>(256);

        while (_position < _length)
        {
            // Check for Jinja delimiters
            if (TryMatchDelimiter(_commentStart, TokenType.CommentStart, out _))
            {
                SkipComment();
                continue;
            }

            if (TryMatchDelimiter(_variableStart, TokenType.VariableStart, out var varStart))
            {
                tokens.Add(varStart);
                TokenizeExpression(tokens, _variableEnd, TokenType.VariableEnd);
                continue;
            }

            if (TryMatchDelimiter(_blockStart, TokenType.BlockStart, out var blockStart))
            {
                tokens.Add(blockStart);
                TokenizeExpression(tokens, _blockEnd, TokenType.BlockEnd);
                continue;
            }

            // Read raw text until next delimiter
            var textToken = ReadText();
            if (textToken.Value.Length > 0)
            {
                tokens.Add(textToken);
            }
        }

        tokens.Add(new Token(TokenType.Eof, "", _line, _column));
        return tokens;
    }

    private Token ReadText()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;

        while (_position < _length)
        {
            // Check for any delimiter
            if (LookaheadMatch(_variableStart) ||
                LookaheadMatch(_blockStart) ||
                LookaheadMatch(_commentStart))
            {
                break;
            }

            Advance();
        }

        var value = _source.Substring(startPos, _position - startPos);
        return new Token(TokenType.Text, value, startLine, startColumn);
    }

    private void TokenizeExpression(List<Token> tokens, string endDelimiter, TokenType endTokenType)
    {
        // Handle whitespace trimming (-) at start
        bool trimStart = false;
        if (_position < _length && Current == '-')
        {
            trimStart = true;
            Advance();
            // Trim trailing whitespace from previous text token
            if (tokens.Count > 0 && tokens[^1].Type == TokenType.Text)
            {
                var lastToken = tokens[^1];
                tokens[^1] = lastToken with { Value = lastToken.Value.TrimEnd() };
            }
        }

        SkipWhitespace();

        while (_position < _length)
        {
            SkipWhitespace();

            // Check if we've reached the end after skipping whitespace
            if (_position >= _length)
            {
                break;
            }

            // Check for end delimiter with optional whitespace trimming
            if (Current == '-' && LookaheadMatch("-" + endDelimiter))
            {
                // Trim whitespace version
                Advance(); // skip -
                if (LookaheadMatch(endDelimiter))
                {
                    _position += endDelimiter.Length;
                    UpdatePosition(endDelimiter);
                    tokens.Add(new Token(endTokenType, "-" + endDelimiter, _line, _column));
                    return;
                }
                _position--; // backtrack if not actually end
            }

            if (LookaheadMatch(endDelimiter))
            {
                var line = _line;
                var col = _column;
                _position += endDelimiter.Length;
                UpdatePosition(endDelimiter);
                tokens.Add(new Token(endTokenType, endDelimiter, line, col));
                return;
            }

            var token = ReadExpressionToken();
            if (token.Type != TokenType.Whitespace)
            {
                tokens.Add(token);
            }
        }

        throw new LexerException($"Unclosed expression, expected '{endDelimiter}'", _line, _column);
    }

    private Token ReadExpressionToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Current;

        // String literals
        if (c == '"' || c == '\'')
        {
            return ReadString(c);
        }

        // Numbers
        if (char.IsDigit(c))
        {
            return ReadNumber();
        }

        // Identifiers and keywords
        if (char.IsLetter(c) || c == '_')
        {
            return ReadIdentifier();
        }

        // Operators and punctuation
        return ReadOperator();
    }

    private Token ReadString(char quote)
    {
        var startLine = _line;
        var startColumn = _column;
        var sb = new StringBuilder();

        Advance(); // skip opening quote

        while (_position < _length && Current != quote)
        {
            if (Current == '\\' && _position + 1 < _length)
            {
                Advance();
                sb.Append(Current switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => Current
                });
            }
            else
            {
                sb.Append(Current);
            }
            Advance();
        }

        if (_position >= _length)
        {
            throw new LexerException("Unterminated string literal", startLine, startColumn);
        }

        Advance(); // skip closing quote
        return new Token(TokenType.String, sb.ToString(), startLine, startColumn);
    }

    private Token ReadNumber()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;
        var isFloat = false;

        while (_position < _length && (char.IsDigit(Current) || Current == '.'))
        {
            if (Current == '.')
            {
                if (isFloat) break; // second dot, stop
                if (_position + 1 < _length && !char.IsDigit(_source[_position + 1]))
                {
                    break; // dot not followed by digit
                }
                isFloat = true;
            }
            Advance();
        }

        // Handle scientific notation
        if (_position < _length && (Current == 'e' || Current == 'E'))
        {
            isFloat = true;
            Advance();
            if (_position < _length && (Current == '+' || Current == '-'))
            {
                Advance();
            }
            while (_position < _length && char.IsDigit(Current))
            {
                Advance();
            }
        }

        var value = _source.Substring(startPos, _position - startPos);
        return new Token(isFloat ? TokenType.Float : TokenType.Integer, value, startLine, startColumn);
    }

    private Token ReadIdentifier()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;

        while (_position < _length && (char.IsLetterOrDigit(Current) || Current == '_'))
        {
            Advance();
        }

        var value = _source.Substring(startPos, _position - startPos);
        var type = Keywords.TryGetValue(value, out var keywordType) ? keywordType : TokenType.Name;

        return new Token(type, value, startLine, startColumn);
    }

    private Token ReadOperator()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Current;

        // Two-character operators
        if (_position + 1 < _length)
        {
            var twoChar = _source.Substring(_position, 2);
            var twoCharType = twoChar switch
            {
                "==" => TokenType.Equal,
                "!=" => TokenType.NotEqual,
                "<=" => TokenType.LessThanOrEqual,
                ">=" => TokenType.GreaterThanOrEqual,
                "//" => TokenType.FloorDivide,
                "**" => TokenType.Power,
                _ => (TokenType?)null
            };

            if (twoCharType.HasValue)
            {
                Advance();
                Advance();
                return new Token(twoCharType.Value, twoChar, startLine, startColumn);
            }
        }

        // Single-character operators
        Advance();
        var (type, value) = c switch
        {
            '|' => (TokenType.Pipe, "|"),
            '.' => (TokenType.Dot, "."),
            ',' => (TokenType.Comma, ","),
            ':' => (TokenType.Colon, ":"),
            '~' => (TokenType.Tilde, "~"),
            '(' => (TokenType.LeftParen, "("),
            ')' => (TokenType.RightParen, ")"),
            '[' => (TokenType.LeftBracket, "["),
            ']' => (TokenType.RightBracket, "]"),
            '{' => (TokenType.LeftBrace, "{"),
            '}' => (TokenType.RightBrace, "}"),
            '=' => (TokenType.Assign, "="),
            '<' => (TokenType.LessThan, "<"),
            '>' => (TokenType.GreaterThan, ">"),
            '+' => (TokenType.Plus, "+"),
            '-' => (TokenType.Minus, "-"),
            '*' => (TokenType.Multiply, "*"),
            '/' => (TokenType.Divide, "/"),
            '%' => (TokenType.Modulo, "%"),
            _ => throw new LexerException($"Unexpected character: '{c}'", startLine, startColumn)
        };

        return new Token(type, value, startLine, startColumn);
    }

    private void SkipComment()
    {
        while (_position < _length)
        {
            if (LookaheadMatch(_commentEnd))
            {
                _position += _commentEnd.Length;
                UpdatePosition(_commentEnd);
                return;
            }
            Advance();
        }
        throw new LexerException($"Unclosed comment, expected '{_commentEnd}'", _line, _column);
    }

    private void SkipWhitespace()
    {
        while (_position < _length && char.IsWhiteSpace(Current))
        {
            Advance();
        }
    }

    private bool TryMatchDelimiter(string delimiter, TokenType type, out Token token)
    {
        if (LookaheadMatch(delimiter))
        {
            var line = _line;
            var col = _column;
            _position += delimiter.Length;
            UpdatePosition(delimiter);
            token = new Token(type, delimiter, line, col);
            return true;
        }
        token = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool LookaheadMatch(string s)
    {
        if (_position + s.Length > _length) return false;
        return _source.AsSpan(_position, s.Length).SequenceEqual(s.AsSpan());
    }

    private char Current => _source[_position];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance()
    {
        if (_position < _length)
        {
            if (_source[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }

    private void UpdatePosition(string s)
    {
        foreach (var c in s)
        {
            if (c == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
        }
    }
}

/// <summary>
/// Options for configuring the lexer delimiters.
/// </summary>
public sealed class LexerOptions
{
    public string VariableStart { get; init; } = "{{";
    public string VariableEnd { get; init; } = "}}";
    public string BlockStart { get; init; } = "{%";
    public string BlockEnd { get; init; } = "%}";
    public string CommentStart { get; init; } = "{#";
    public string CommentEnd { get; init; } = "#}";

    public static LexerOptions Default { get; } = new();
}
