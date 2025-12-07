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
    private readonly bool _trimBlocks;
    private readonly bool _lstripBlocks;

    // Flag to trim leading whitespace from next text token (for -%})
    private bool _trimNextText;
    // Flag to trim only the first newline from next text token (for TrimBlocks)
    private bool _trimFirstNewline;

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
        _trimBlocks = options.TrimBlocks;
        _lstripBlocks = options.LstripBlocks;
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
                // LstripBlocks: strip leading whitespace from the start of the line to the block tag
                if (_lstripBlocks && tokens.Count > 0 && tokens[^1].Type == TokenType.Text)
                {
                    var prevTextToken = tokens[^1];
                    var strippedValue = StripTrailingLineWhitespace(prevTextToken.Value);
                    if (strippedValue != prevTextToken.Value)
                    {
                        tokens[^1] = prevTextToken with { Value = strippedValue };
                    }
                }

                tokens.Add(blockStart);

                // Check for raw block - needs special handling
                if (TryParseRawBlock(tokens))
                {
                    continue;
                }

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

        // If previous tag ended with -, skip all leading whitespace
        if (_trimNextText)
        {
            _trimNextText = false;
            _trimFirstNewline = false; // -%} takes precedence
            while (_position < _length && char.IsWhiteSpace(Current))
            {
                Advance();
            }
            startPos = _position;
            startLine = _line;
            startColumn = _column;
        }
        // If TrimBlocks is active, skip only the first newline
        else if (_trimFirstNewline)
        {
            _trimFirstNewline = false;
            if (_position < _length && Current == '\n')
            {
                Advance();
                startPos = _position;
                startLine = _line;
                startColumn = _column;
            }
            else if (_position < _length && Current == '\r')
            {
                Advance();
                if (_position < _length && Current == '\n')
                {
                    Advance();
                }
                startPos = _position;
                startLine = _line;
                startColumn = _column;
            }
        }

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
            // Trim trailing whitespace from the text token before the block start
            // tokens[^1] is the BlockStart/VariableStart, so we need [^2] for the text
            if (tokens.Count >= 2 && tokens[^2].Type == TokenType.Text)
            {
                var textToken = tokens[^2];
                tokens[^2] = textToken with { Value = textToken.Value.TrimEnd() };
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
                // Trim whitespace version - set flag to trim leading whitespace from next text
                Advance(); // skip -
                if (LookaheadMatch(endDelimiter))
                {
                    var line = _line;
                    var col = _column;
                    _position += endDelimiter.Length;
                    UpdatePosition(endDelimiter);
                    tokens.Add(new Token(endTokenType, "-" + endDelimiter, line, col));
                    _trimNextText = true; // Trim leading whitespace from next text token
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
                // Set TrimBlocks flag for block tags (not variable tags)
                if (_trimBlocks && endTokenType == TokenType.BlockEnd)
                {
                    _trimFirstNewline = true;
                }
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

    /// <summary>
    /// Strips trailing whitespace from the last line if it contains only spaces/tabs.
    /// Used for LstripBlocks support.
    /// </summary>
    private static string StripTrailingLineWhitespace(string text)
    {
        if (text.Length == 0) return text;

        // Find the last newline
        var lastNewlineIndex = text.LastIndexOf('\n');

        // Check if everything after the last newline (or from start if no newline) is only spaces/tabs
        var startOfLine = lastNewlineIndex + 1;
        for (int i = startOfLine; i < text.Length; i++)
        {
            var c = text[i];
            if (c != ' ' && c != '\t')
            {
                // Non-whitespace character found, don't strip
                return text;
            }
        }

        // Everything after the last newline is whitespace, strip it
        return text.Substring(0, startOfLine);
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

    /// <summary>
    /// Tries to parse a raw block. Returns true if this was a raw block.
    /// Raw blocks preserve content exactly as-is until {% endraw %}.
    /// </summary>
    private bool TryParseRawBlock(List<Token> tokens)
    {
        // Save position in case this isn't a raw block
        var savedPos = _position;
        var savedLine = _line;
        var savedColumn = _column;

        // Handle optional whitespace control dash
        if (_position < _length && Current == '-')
        {
            Advance();
        }

        SkipWhitespace();

        // Check if this is "raw"
        if (!LookaheadMatch("raw"))
        {
            // Not a raw block, restore position
            _position = savedPos;
            _line = savedLine;
            _column = savedColumn;
            return false;
        }

        // Check it's actually the keyword "raw" and not "rawdata" or similar
        var afterRaw = _position + 3;
        if (afterRaw < _length && (char.IsLetterOrDigit(_source[afterRaw]) || _source[afterRaw] == '_'))
        {
            _position = savedPos;
            _line = savedLine;
            _column = savedColumn;
            return false;
        }

        // It's a raw block! Consume "raw"
        var rawLine = _line;
        var rawColumn = _column;
        _position += 3;
        _column += 3;

        tokens.Add(new Token(TokenType.Raw, "raw", rawLine, rawColumn));

        SkipWhitespace();

        // Handle optional whitespace control dash before %}
        if (_position < _length && Current == '-' && LookaheadMatch("-" + _blockEnd))
        {
            Advance();
        }

        // Consume %}
        if (!LookaheadMatch(_blockEnd))
        {
            throw new LexerException($"Expected '{_blockEnd}' after 'raw'", _line, _column);
        }
        var endLine = _line;
        var endColumn = _column;
        _position += _blockEnd.Length;
        UpdatePosition(_blockEnd);
        tokens.Add(new Token(TokenType.BlockEnd, _blockEnd, endLine, endColumn));

        // Now read raw content until {% endraw %}
        var rawContentStart = _position;
        var rawContentLine = _line;
        var rawContentColumn = _column;
        var endrawPattern = _blockStart + " endraw " + _blockEnd;
        var endrawPatternTight = _blockStart + "endraw" + _blockEnd;
        var endrawPatternDash = _blockStart + "- endraw " + _blockEnd;
        var endrawPatternDashEnd = _blockStart + " endraw -" + _blockEnd;

        while (_position < _length)
        {
            if (LookaheadMatch(_blockStart))
            {
                // Check for various endraw patterns
                var remaining = _source.AsSpan(_position);

                // Try to match {% endraw %} with various whitespace combinations
                var endrawStart = _position;
                var tempPos = _position + _blockStart.Length;

                // Skip optional -
                if (tempPos < _length && _source[tempPos] == '-') tempPos++;

                // Skip whitespace
                while (tempPos < _length && char.IsWhiteSpace(_source[tempPos])) tempPos++;

                // Check for "endraw"
                if (tempPos + 6 <= _length && _source.AsSpan(tempPos, 6).SequenceEqual("endraw".AsSpan()))
                {
                    tempPos += 6;

                    // Skip whitespace
                    while (tempPos < _length && char.IsWhiteSpace(_source[tempPos])) tempPos++;

                    // Skip optional -
                    if (tempPos < _length && _source[tempPos] == '-') tempPos++;

                    // Check for %}
                    if (tempPos + _blockEnd.Length <= _length &&
                        _source.AsSpan(tempPos, _blockEnd.Length).SequenceEqual(_blockEnd.AsSpan()))
                    {
                        // Found endraw! Capture the raw content
                        var rawContent = _source.Substring(rawContentStart, endrawStart - rawContentStart);
                        if (rawContent.Length > 0)
                        {
                            tokens.Add(new Token(TokenType.Text, rawContent, rawContentLine, rawContentColumn));
                        }

                        // Add {% endraw %} tokens
                        tokens.Add(new Token(TokenType.BlockStart, _blockStart, _line, _column));
                        tokens.Add(new Token(TokenType.Endraw, "endraw", _line, _column));
                        tokens.Add(new Token(TokenType.BlockEnd, _blockEnd, _line, _column));

                        // Move position past {% endraw %}
                        _position = tempPos + _blockEnd.Length;
                        // Update line/column tracking
                        for (int i = endrawStart; i < _position && i < _length; i++)
                        {
                            if (_source[i] == '\n')
                            {
                                _line++;
                                _column = 1;
                            }
                            else
                            {
                                _column++;
                            }
                        }

                        return true;
                    }
                }
            }

            Advance();
        }

        throw new LexerException("Unclosed raw block, expected '{% endraw %}'", rawContentLine, rawContentColumn);
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

    /// <summary>
    /// Remove the first newline after a block tag.
    /// </summary>
    public bool TrimBlocks { get; init; } = false;

    /// <summary>
    /// Strip leading whitespace and tabs from the start of a line to a block tag.
    /// </summary>
    public bool LstripBlocks { get; init; } = false;

    public static LexerOptions Default { get; } = new();
}
