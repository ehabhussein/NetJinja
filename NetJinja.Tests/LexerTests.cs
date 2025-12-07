using NetJinja.Lexing;

namespace NetJinja.Tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_PlainText_ReturnsTextToken()
    {
        var lexer = new Lexer("Hello, World!");
        var tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("Hello, World!", tokens[0].Value);
        Assert.Equal(TokenType.Eof, tokens[1].Type);
    }

    [Fact]
    public void Tokenize_SimpleVariable_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("{{ name }}");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenType.VariableStart, tokens[0].Type);
        Assert.Equal(TokenType.Name, tokens[1].Type);
        Assert.Equal("name", tokens[1].Value);
        Assert.Equal(TokenType.VariableEnd, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_VariableWithFilter_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("{{ name | upper }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.Pipe);
        Assert.Contains(tokens, t => t.Type == TokenType.Name && t.Value == "upper");
    }

    [Fact]
    public void Tokenize_StringLiteral_ParsesCorrectly()
    {
        var lexer = new Lexer("{{ \"hello world\" }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.String && t.Value == "hello world");
    }

    [Fact]
    public void Tokenize_IntegerLiteral_ParsesCorrectly()
    {
        var lexer = new Lexer("{{ 42 }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.Integer && t.Value == "42");
    }

    [Fact]
    public void Tokenize_FloatLiteral_ParsesCorrectly()
    {
        var lexer = new Lexer("{{ 3.14 }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.Float && t.Value == "3.14");
    }

    [Fact]
    public void Tokenize_BlockStatement_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("{% if condition %}{% endif %}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.BlockStart);
        Assert.Contains(tokens, t => t.Type == TokenType.If);
        Assert.Contains(tokens, t => t.Type == TokenType.BlockEnd);
        Assert.Contains(tokens, t => t.Type == TokenType.Endif);
    }

    [Fact]
    public void Tokenize_Comment_IsSkipped()
    {
        var lexer = new Lexer("Hello{# comment #}World");
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal("Hello", tokens[0].Value);
        Assert.Equal("World", tokens[1].Value);
        Assert.Equal(TokenType.Eof, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_Operators_ParsesCorrectly()
    {
        var lexer = new Lexer("{{ a + b - c * d / e }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.Plus);
        Assert.Contains(tokens, t => t.Type == TokenType.Minus);
        Assert.Contains(tokens, t => t.Type == TokenType.Multiply);
        Assert.Contains(tokens, t => t.Type == TokenType.Divide);
    }

    [Fact]
    public void Tokenize_ComparisonOperators_ParsesCorrectly()
    {
        var lexer = new Lexer("{{ a == b != c < d <= e > f >= g }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.Equal);
        Assert.Contains(tokens, t => t.Type == TokenType.NotEqual);
        Assert.Contains(tokens, t => t.Type == TokenType.LessThan);
        Assert.Contains(tokens, t => t.Type == TokenType.LessThanOrEqual);
        Assert.Contains(tokens, t => t.Type == TokenType.GreaterThan);
        Assert.Contains(tokens, t => t.Type == TokenType.GreaterThanOrEqual);
    }

    [Fact]
    public void Tokenize_BooleanKeywords_ParsesCorrectly()
    {
        var lexer = new Lexer("{{ true and false or not none }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.True);
        Assert.Contains(tokens, t => t.Type == TokenType.And);
        Assert.Contains(tokens, t => t.Type == TokenType.False);
        Assert.Contains(tokens, t => t.Type == TokenType.Or);
        Assert.Contains(tokens, t => t.Type == TokenType.Not);
        Assert.Contains(tokens, t => t.Type == TokenType.None);
    }

    [Fact]
    public void Tokenize_ForLoop_ParsesCorrectly()
    {
        var lexer = new Lexer("{% for item in items %}{% endfor %}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.For);
        Assert.Contains(tokens, t => t.Type == TokenType.In);
        Assert.Contains(tokens, t => t.Type == TokenType.Endfor);
    }

    [Fact]
    public void Tokenize_EscapedString_HandlesEscapes()
    {
        var lexer = new Lexer("{{ \"hello\\nworld\" }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.String && t.Value == "hello\nworld");
    }

    [Fact]
    public void Tokenize_ScientificNotation_ParsesAsFloat()
    {
        var lexer = new Lexer("{{ 1.5e10 }}");
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.Float && t.Value == "1.5e10");
    }

    [Fact]
    public void Tokenize_CustomDelimiters_WorksCorrectly()
    {
        var options = new LexerOptions
        {
            VariableStart = "${",
            VariableEnd = "}",
            BlockStart = "<%",
            BlockEnd = "%>"
        };
        var lexer = new Lexer("<% if x %>${ name }<% endif %>", options);
        var tokens = lexer.Tokenize();

        Assert.Contains(tokens, t => t.Type == TokenType.BlockStart);
        Assert.Contains(tokens, t => t.Type == TokenType.VariableStart);
    }
}
