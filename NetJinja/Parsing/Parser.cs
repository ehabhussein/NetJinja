using System.Globalization;
using System.Runtime.CompilerServices;
using NetJinja.Ast;
using NetJinja.Exceptions;
using NetJinja.Lexing;

namespace NetJinja.Parsing;

/// <summary>
/// Recursive descent parser for Jinja templates.
/// Converts tokens into an Abstract Syntax Tree (AST).
/// </summary>
public sealed class Parser
{
    private readonly List<Token> _tokens;
    private int _position;
    private readonly string? _templateName;

    public Parser(List<Token> tokens, string? templateName = null)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _templateName = templateName;
    }

    /// <summary>
    /// Parses the token stream into a template AST.
    /// </summary>
    public TemplateNode Parse()
    {
        var body = ParseStatements(TokenType.Eof);
        return new TemplateNode(body);
    }

    private List<Statement> ParseStatements(params TokenType[] endKeywords)
    {
        var statements = new List<Statement>();

        while (!IsAtEnd)
        {
            // Check if we've reached an end keyword
            if (Current.Type == TokenType.BlockStart && endKeywords.Length > 0)
            {
                // Peek ahead to see what keyword follows
                var nextToken = Peek(1);
                if (endKeywords.Contains(nextToken.Type))
                {
                    break;
                }
            }

            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        return statements;
    }

    private Statement? ParseStatement()
    {
        var token = Current;

        return token.Type switch
        {
            TokenType.Text => ParseTextStatement(),
            TokenType.VariableStart => ParseOutputStatement(),
            TokenType.BlockStart => ParseBlockStatement(),
            _ => throw Unexpected(token)
        };
    }

    private TextStatement ParseTextStatement()
    {
        var token = Consume(TokenType.Text);
        return new TextStatement(token.Value, token.Line, token.Column);
    }

    private OutputStatement ParseOutputStatement()
    {
        var start = Consume(TokenType.VariableStart);
        var expr = ParseExpression();
        Consume(TokenType.VariableEnd);
        return new OutputStatement(expr, start.Line, start.Column);
    }

    private Statement ParseBlockStatement()
    {
        Consume(TokenType.BlockStart);
        var token = Current;

        return token.Type switch
        {
            TokenType.If => ParseIfStatement(),
            TokenType.For => ParseForStatement(),
            TokenType.Block => ParseBlockDefinition(),
            TokenType.Extends => ParseExtendsStatement(),
            TokenType.Include => ParseIncludeStatement(),
            TokenType.Set => ParseSetStatement(),
            TokenType.Macro => ParseMacroStatement(),
            TokenType.Call => ParseCallStatement(),
            TokenType.With => ParseWithStatement(),
            TokenType.Autoescape => ParseAutoescapeStatement(),
            TokenType.Raw => ParseRawStatement(),
            TokenType.Continue => ParseContinueStatement(),
            TokenType.Break => ParseBreakStatement(),
            _ => throw Unexpected(token, "block keyword")
        };
    }

    #region Control Flow Statements

    private IfStatement ParseIfStatement()
    {
        var start = Consume(TokenType.If);
        var condition = ParseExpression();
        Consume(TokenType.BlockEnd);

        var thenBody = ParseStatements(TokenType.Elif, TokenType.Else, TokenType.Endif);
        var elifBranches = new List<(Expression, IReadOnlyList<Statement>)>();
        List<Statement>? elseBody = null;

        while (Current.Type == TokenType.BlockStart)
        {
            Consume(TokenType.BlockStart);

            if (Check(TokenType.Elif))
            {
                Advance();
                var elifCondition = ParseExpression();
                Consume(TokenType.BlockEnd);
                var elifBody = ParseStatements(TokenType.Elif, TokenType.Else, TokenType.Endif);
                elifBranches.Add((elifCondition, elifBody));
            }
            else if (Check(TokenType.Else))
            {
                Advance();
                Consume(TokenType.BlockEnd);
                elseBody = ParseStatements(TokenType.Endif);
            }
            else if (Check(TokenType.Endif))
            {
                Advance();
                Consume(TokenType.BlockEnd);
                break;
            }
            else
            {
                throw Unexpected(Current, "elif, else, or endif");
            }
        }

        return new IfStatement(condition, thenBody, elifBranches, elseBody, start.Line, start.Column);
    }

    private ForStatement ParseForStatement()
    {
        var start = Consume(TokenType.For);

        // Parse target names (can be tuple: for a, b in items)
        var targets = new List<string>();
        targets.Add(Consume(TokenType.Name).Value);

        while (Check(TokenType.Comma))
        {
            Advance();
            targets.Add(Consume(TokenType.Name).Value);
        }

        Consume(TokenType.In);
        // Use ParseOrExpression to avoid triggering ternary conditional parsing
        var iterable = ParseOrExpression();

        // Optional filter: for x in items if x > 0
        // Use ParseOrExpression to avoid treating it as ternary conditional
        Expression? filter = null;
        if (Check(TokenType.If))
        {
            Advance();
            filter = ParseOrExpression();
        }

        // Optional recursive
        bool recursive = false;
        if (Check(TokenType.Recursive))
        {
            Advance();
            recursive = true;
        }

        Consume(TokenType.BlockEnd);

        var body = ParseStatements(TokenType.Endfor, TokenType.Else);
        List<Statement>? elseBody = null;

        Consume(TokenType.BlockStart);
        if (Check(TokenType.Else))
        {
            Advance();
            Consume(TokenType.BlockEnd);
            elseBody = ParseStatements(TokenType.Endfor);
            Consume(TokenType.BlockStart);
        }

        Consume(TokenType.Endfor);
        Consume(TokenType.BlockEnd);

        return new ForStatement(targets, iterable, body, elseBody, filter, recursive, start.Line, start.Column);
    }

    private BlockStatement ParseBlockDefinition()
    {
        var start = Consume(TokenType.Block);
        var name = Consume(TokenType.Name).Value;

        bool scoped = false;
        if (Check(TokenType.Name) && Current.Value == "scoped")
        {
            Advance();
            scoped = true;
        }

        Consume(TokenType.BlockEnd);

        var body = ParseStatements(TokenType.Endblock);

        Consume(TokenType.BlockStart);
        Consume(TokenType.Endblock);

        // Optional name after endblock
        if (Check(TokenType.Name))
        {
            var endName = Advance().Value;
            if (endName != name)
            {
                throw new ParserException($"Block name mismatch: expected '{name}', got '{endName}'", Current.Line, Current.Column, _templateName);
            }
        }

        Consume(TokenType.BlockEnd);

        return new BlockStatement(name, body, scoped, start.Line, start.Column);
    }

    #endregion

    #region Template Inheritance Statements

    private ExtendsStatement ParseExtendsStatement()
    {
        var start = Consume(TokenType.Extends);
        var template = ParseExpression();
        Consume(TokenType.BlockEnd);
        return new ExtendsStatement(template, start.Line, start.Column);
    }

    private IncludeStatement ParseIncludeStatement()
    {
        var start = Consume(TokenType.Include);
        var template = ParseExpression();

        bool ignoreMissing = false;
        bool withContext = true;

        // Parse optional modifiers
        while (Check(TokenType.Name))
        {
            var modifier = Current.Value;
            if (modifier == "ignore" && Peek(1).Value == "missing")
            {
                Advance();
                Advance();
                ignoreMissing = true;
            }
            else if (modifier == "with" && Peek(1).Value == "context")
            {
                Advance();
                Advance();
                withContext = true;
            }
            else if (modifier == "without" && Peek(1).Value == "context")
            {
                Advance();
                Advance();
                withContext = false;
            }
            else
            {
                break;
            }
        }

        Consume(TokenType.BlockEnd);
        return new IncludeStatement(template, withContext, ignoreMissing, start.Line, start.Column);
    }

    #endregion

    #region Variable Statements

    private SetStatement ParseSetStatement()
    {
        var start = Consume(TokenType.Set);

        var names = new List<string> { Consume(TokenType.Name).Value };
        while (Check(TokenType.Comma))
        {
            Advance();
            names.Add(Consume(TokenType.Name).Value);
        }

        // Check for block form: {% set x %}...{% endset %}
        if (Check(TokenType.BlockEnd))
        {
            Consume(TokenType.BlockEnd);
            var body = ParseStatements(TokenType.Endset);
            Consume(TokenType.BlockStart);
            Consume(TokenType.Endset);
            Consume(TokenType.BlockEnd);
            return new SetStatement(names, null, body, start.Line, start.Column);
        }

        Consume(TokenType.Assign);
        var value = ParseExpression();
        Consume(TokenType.BlockEnd);

        return new SetStatement(names, value, null, start.Line, start.Column);
    }

    #endregion

    #region Macro Statements

    private MacroStatement ParseMacroStatement()
    {
        var start = Consume(TokenType.Macro);
        var name = Consume(TokenType.Name).Value;
        var parameters = ParseMacroParameters();
        Consume(TokenType.BlockEnd);

        var body = ParseStatements(TokenType.Endmacro);

        Consume(TokenType.BlockStart);
        Consume(TokenType.Endmacro);

        // Optional name after endmacro
        if (Check(TokenType.Name))
        {
            Advance();
        }

        Consume(TokenType.BlockEnd);

        return new MacroStatement(name, parameters, body, start.Line, start.Column);
    }

    private CallStatement ParseCallStatement()
    {
        var start = Consume(TokenType.Call);

        // Optional caller arguments
        var parameters = new List<(string, Expression?)>();
        if (Check(TokenType.LeftParen))
        {
            parameters = ParseMacroParameters();
        }

        var call = ParseExpression();
        Consume(TokenType.BlockEnd);

        var body = ParseStatements(TokenType.Endcall);

        Consume(TokenType.BlockStart);
        Consume(TokenType.Endcall);
        Consume(TokenType.BlockEnd);

        return new CallStatement(call, parameters, body, start.Line, start.Column);
    }

    private List<(string Name, Expression? Default)> ParseMacroParameters()
    {
        var parameters = new List<(string, Expression?)>();
        Consume(TokenType.LeftParen);

        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (Check(TokenType.Comma)) Advance();
                var paramName = Consume(TokenType.Name).Value;
                Expression? defaultValue = null;

                if (Check(TokenType.Assign))
                {
                    Advance();
                    defaultValue = ParseExpression();
                }

                parameters.Add((paramName, defaultValue));
            } while (Check(TokenType.Comma));
        }

        Consume(TokenType.RightParen);
        return parameters;
    }

    #endregion

    #region Scope Statements

    private WithStatement ParseWithStatement()
    {
        var start = Consume(TokenType.With);

        var assignments = new List<(string, Expression)>();

        // Parse assignments: with a = 1, b = 2
        if (!Check(TokenType.BlockEnd))
        {
            do
            {
                if (Check(TokenType.Comma)) Advance();
                var name = Consume(TokenType.Name).Value;
                Consume(TokenType.Assign);
                var value = ParseExpression();
                assignments.Add((name, value));
            } while (Check(TokenType.Comma));
        }

        Consume(TokenType.BlockEnd);

        var body = ParseStatements(TokenType.Endwith);

        Consume(TokenType.BlockStart);
        Consume(TokenType.Endwith);
        Consume(TokenType.BlockEnd);

        return new WithStatement(assignments, body, start.Line, start.Column);
    }

    private AutoescapeStatement ParseAutoescapeStatement()
    {
        var start = Consume(TokenType.Autoescape);

        bool enabled = true;
        if (Check(TokenType.True))
        {
            Advance();
            enabled = true;
        }
        else if (Check(TokenType.False))
        {
            Advance();
            enabled = false;
        }
        else if (Check(TokenType.Name))
        {
            var value = Current.Value;
            enabled = value != "false" && value != "off";
            Advance();
        }

        Consume(TokenType.BlockEnd);

        var body = ParseStatements(TokenType.Endautoescape);

        Consume(TokenType.BlockStart);
        Consume(TokenType.Endautoescape);
        Consume(TokenType.BlockEnd);

        return new AutoescapeStatement(enabled, body, start.Line, start.Column);
    }

    private TextStatement ParseRawStatement()
    {
        var start = Consume(TokenType.Raw);
        Consume(TokenType.BlockEnd);

        // Collect all text until {% endraw %}
        var textBuilder = new System.Text.StringBuilder();
        var startLine = Current.Line;
        var startColumn = Current.Column;

        while (!IsAtEnd)
        {
            if (Check(TokenType.BlockStart))
            {
                var savedPos = _position;
                Advance();
                if (Check(TokenType.Endraw))
                {
                    Advance();
                    Consume(TokenType.BlockEnd);
                    break;
                }
                _position = savedPos;
                textBuilder.Append(Current.Value);
                Advance();
            }
            else
            {
                textBuilder.Append(Current.Value);
                Advance();
            }
        }

        return new TextStatement(textBuilder.ToString(), startLine, startColumn);
    }

    #endregion

    #region Loop Control

    private ContinueStatement ParseContinueStatement()
    {
        var token = Consume(TokenType.Continue);
        Consume(TokenType.BlockEnd);
        return new ContinueStatement(token.Line, token.Column);
    }

    private BreakStatement ParseBreakStatement()
    {
        var token = Consume(TokenType.Break);
        Consume(TokenType.BlockEnd);
        return new BreakStatement(token.Line, token.Column);
    }

    #endregion

    #region Expression Parsing

    private Expression ParseExpression() => ParseConditionalExpression();

    private Expression ParseConditionalExpression()
    {
        var expr = ParseOrExpression();

        // expr if condition else expr
        if (Check(TokenType.If))
        {
            Advance();
            var condition = ParseOrExpression();
            Consume(TokenType.Else);
            var falseExpr = ParseConditionalExpression();
            return new ConditionalExpression(expr, condition, falseExpr, expr.Line, expr.Column);
        }

        return expr;
    }

    private Expression ParseOrExpression()
    {
        var left = ParseAndExpression();

        while (Check(TokenType.Or))
        {
            Advance();
            var right = ParseAndExpression();
            left = new BinaryExpression(left, BinaryOperator.Or, right, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParseAndExpression()
    {
        var left = ParseNotExpression();

        while (Check(TokenType.And))
        {
            Advance();
            var right = ParseNotExpression();
            left = new BinaryExpression(left, BinaryOperator.And, right, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParseNotExpression()
    {
        if (Check(TokenType.Not))
        {
            var op = Advance();
            var operand = ParseNotExpression();
            return new UnaryExpression(UnaryOperator.Not, operand, op.Line, op.Column);
        }

        return ParseCompareExpression();
    }

    private Expression ParseCompareExpression()
    {
        var left = ParseInExpression();
        var comparisons = new List<(CompareOperator, Expression)>();

        while (true)
        {
            CompareOperator? op = Current.Type switch
            {
                TokenType.Equal => CompareOperator.Equal,
                TokenType.NotEqual => CompareOperator.NotEqual,
                TokenType.LessThan => CompareOperator.LessThan,
                TokenType.LessThanOrEqual => CompareOperator.LessThanOrEqual,
                TokenType.GreaterThan => CompareOperator.GreaterThan,
                TokenType.GreaterThanOrEqual => CompareOperator.GreaterThanOrEqual,
                _ => null
            };

            if (op == null) break;

            Advance();
            var right = ParseInExpression();
            comparisons.Add((op.Value, right));
        }

        if (comparisons.Count == 0) return left;

        // Always return CompareExpression for comparisons
        return new CompareExpression(left, comparisons, left.Line, left.Column);
    }

    private Expression ParseInExpression()
    {
        var left = ParseConcatExpression();

        bool negated = false;
        if (Check(TokenType.Not))
        {
            Advance();
            negated = true;
        }

        if (Check(TokenType.In))
        {
            Advance();
            var right = ParseConcatExpression();
            var op = negated ? BinaryOperator.NotIn : BinaryOperator.In;
            return new BinaryExpression(left, op, right, left.Line, left.Column);
        }
        else if (negated)
        {
            // Was "not" but not "in", backtrack
            _position--;
        }

        // Handle "is" test
        if (Check(TokenType.Is))
        {
            Advance();
            bool testNegated = false;
            if (Check(TokenType.Not))
            {
                Advance();
                testNegated = true;
            }
            // Test name can be a Name or a keyword like true/false/none/in
            var testToken = Current;
            string testName;
            if (Check(TokenType.Name))
            {
                testName = Advance().Value;
            }
            else if (Check(TokenType.True))
            {
                testName = "true";
                Advance();
            }
            else if (Check(TokenType.False))
            {
                testName = "false";
                Advance();
            }
            else if (Check(TokenType.None))
            {
                testName = "none";
                Advance();
            }
            else if (Check(TokenType.In))
            {
                testName = "in";
                Advance();
            }
            else
            {
                throw Unexpected(testToken, "test name");
            }
            var args = new List<Expression>();

            if (Check(TokenType.LeftParen))
            {
                args = ParseCallArguments().Args;
            }

            return new TestExpression(left, testName, args, testNegated, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParseConcatExpression()
    {
        var left = ParseAdditiveExpression();

        while (Check(TokenType.Tilde))
        {
            Advance();
            var right = ParseAdditiveExpression();
            left = new ConcatExpression(left, right, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParseAdditiveExpression()
    {
        var left = ParseMultiplicativeExpression();

        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            var op = Advance();
            var right = ParseMultiplicativeExpression();
            var binOp = op.Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            left = new BinaryExpression(left, binOp, right, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParseMultiplicativeExpression()
    {
        var left = ParsePowerExpression();

        while (Check(TokenType.Multiply) || Check(TokenType.Divide) ||
               Check(TokenType.FloorDivide) || Check(TokenType.Modulo))
        {
            var op = Advance();
            var right = ParsePowerExpression();
            var binOp = op.Type switch
            {
                TokenType.Multiply => BinaryOperator.Multiply,
                TokenType.Divide => BinaryOperator.Divide,
                TokenType.FloorDivide => BinaryOperator.FloorDivide,
                TokenType.Modulo => BinaryOperator.Modulo,
                _ => throw new InvalidOperationException()
            };
            left = new BinaryExpression(left, binOp, right, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParsePowerExpression()
    {
        var left = ParseUnaryExpression();

        if (Check(TokenType.Power))
        {
            Advance();
            var right = ParsePowerExpression(); // Right associative
            left = new BinaryExpression(left, BinaryOperator.Power, right, left.Line, left.Column);
        }

        return left;
    }

    private Expression ParseUnaryExpression()
    {
        if (Check(TokenType.Minus))
        {
            var op = Advance();
            var operand = ParseUnaryExpression();
            return new UnaryExpression(UnaryOperator.Negative, operand, op.Line, op.Column);
        }

        if (Check(TokenType.Plus))
        {
            var op = Advance();
            var operand = ParseUnaryExpression();
            return new UnaryExpression(UnaryOperator.Positive, operand, op.Line, op.Column);
        }

        return ParsePostfixExpression();
    }

    private Expression ParsePostfixExpression()
    {
        var expr = ParsePrimaryExpression();

        while (true)
        {
            if (Check(TokenType.Dot))
            {
                Advance();
                var attr = Consume(TokenType.Name).Value;
                expr = new GetAttrExpression(expr, attr, expr.Line, expr.Column);
            }
            else if (Check(TokenType.LeftBracket))
            {
                Advance();
                var key = ParseExpression();
                Consume(TokenType.RightBracket);
                expr = new GetItemExpression(expr, key, expr.Line, expr.Column);
            }
            else if (Check(TokenType.LeftParen))
            {
                var (args, kwargs) = ParseCallArguments();
                expr = new CallExpression(expr, args, kwargs, expr.Line, expr.Column);
            }
            else if (Check(TokenType.Pipe))
            {
                Advance();
                var filterName = Consume(TokenType.Name).Value;
                var args = new List<Expression>();
                var kwargs = new Dictionary<string, Expression>();

                if (Check(TokenType.LeftParen))
                {
                    (args, kwargs) = ParseCallArguments();
                }

                expr = new FilterExpression(expr, filterName, args, kwargs, expr.Line, expr.Column);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private (List<Expression> Args, Dictionary<string, Expression> Kwargs) ParseCallArguments()
    {
        Consume(TokenType.LeftParen);
        var args = new List<Expression>();
        var kwargs = new Dictionary<string, Expression>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (Check(TokenType.Comma)) Advance();

                // Check for keyword argument
                if (Check(TokenType.Name) && Peek(1).Type == TokenType.Assign)
                {
                    var name = Advance().Value;
                    Advance(); // consume =
                    var value = ParseExpression();
                    kwargs[name] = value;
                }
                else
                {
                    args.Add(ParseExpression());
                }
            } while (Check(TokenType.Comma));
        }

        Consume(TokenType.RightParen);
        return (args, kwargs);
    }

    private Expression ParsePrimaryExpression()
    {
        var token = Current;

        switch (token.Type)
        {
            case TokenType.Name:
                Advance();
                return new NameExpression(token.Value, token.Line, token.Column);

            case TokenType.String:
                Advance();
                return new LiteralExpression(token.Value, token.Line, token.Column);

            case TokenType.Integer:
                Advance();
                return new LiteralExpression(long.Parse(token.Value, CultureInfo.InvariantCulture), token.Line, token.Column);

            case TokenType.Float:
                Advance();
                return new LiteralExpression(double.Parse(token.Value, CultureInfo.InvariantCulture), token.Line, token.Column);

            case TokenType.True:
                Advance();
                return new LiteralExpression(true, token.Line, token.Column);

            case TokenType.False:
                Advance();
                return new LiteralExpression(false, token.Line, token.Column);

            case TokenType.None:
                Advance();
                return new LiteralExpression(null, token.Line, token.Column);

            case TokenType.LeftParen:
                return ParseTupleOrParenExpr();

            case TokenType.LeftBracket:
                return ParseListLiteral();

            case TokenType.LeftBrace:
                return ParseDictLiteral();

            default:
                throw Unexpected(token, "expression");
        }
    }

    private Expression ParseTupleOrParenExpr()
    {
        var start = Consume(TokenType.LeftParen);
        var items = new List<Expression>();
        bool hasComma = false;

        if (!Check(TokenType.RightParen))
        {
            items.Add(ParseExpression());
            while (Check(TokenType.Comma))
            {
                hasComma = true;
                Advance();
                if (Check(TokenType.RightParen)) break;
                items.Add(ParseExpression());
            }
        }

        Consume(TokenType.RightParen);

        // Single item without comma is just a grouped expression
        if (items.Count == 1 && !hasComma)
        {
            return items[0];
        }

        return new TupleExpression(items, start.Line, start.Column);
    }

    private ListExpression ParseListLiteral()
    {
        var start = Consume(TokenType.LeftBracket);
        var items = new List<Expression>();

        if (!Check(TokenType.RightBracket))
        {
            items.Add(ParseExpression());
            while (Check(TokenType.Comma))
            {
                Advance();
                if (Check(TokenType.RightBracket)) break;
                items.Add(ParseExpression());
            }
        }

        Consume(TokenType.RightBracket);
        return new ListExpression(items, start.Line, start.Column);
    }

    private DictExpression ParseDictLiteral()
    {
        var start = Consume(TokenType.LeftBrace);
        var items = new List<(Expression, Expression)>();

        if (!Check(TokenType.RightBrace))
        {
            var key = ParseExpression();
            Consume(TokenType.Colon);
            var value = ParseExpression();
            items.Add((key, value));

            while (Check(TokenType.Comma))
            {
                Advance();
                if (Check(TokenType.RightBrace)) break;
                key = ParseExpression();
                Consume(TokenType.Colon);
                value = ParseExpression();
                items.Add((key, value));
            }
        }

        Consume(TokenType.RightBrace);
        return new DictExpression(items, start.Line, start.Column);
    }

    #endregion

    #region Helper Methods

    private Token Current => _tokens[_position];

    private bool IsAtEnd => _position >= _tokens.Count || Current.Type == TokenType.Eof;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Check(TokenType type) => !IsAtEnd && Current.Type == type;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token Peek(int offset) =>
        _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[^1];

    private Token Advance()
    {
        var token = Current;
        if (!IsAtEnd) _position++;
        return token;
    }

    private Token Consume(TokenType type)
    {
        if (!Check(type))
        {
            throw Unexpected(Current, type.ToString());
        }
        return Advance();
    }

    private ParserException Unexpected(Token token, string? expected = null)
    {
        var msg = expected != null
            ? $"Unexpected {token.Type} '{token.Value}', expected {expected}"
            : $"Unexpected {token.Type} '{token.Value}'";
        return new ParserException(msg, token.Line, token.Column, _templateName);
    }

    #endregion
}
