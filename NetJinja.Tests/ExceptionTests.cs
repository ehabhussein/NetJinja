using NetJinja.Exceptions;
using NetJinja.Runtime;

namespace NetJinja.Tests;

public class ExceptionTests
{
    [Fact]
    public void UndefinedVariable_StrictMode_Throws()
    {
        var env = Jinja.CreateEnvironment();
        env.StrictUndefined = true;

        var ex = Assert.Throws<UndefinedVariableException>(() =>
            env.FromString("{{ undefined_var }}").Render());

        Assert.Equal("undefined_var", ex.VariableName);
    }

    [Fact]
    public void UndefinedVariable_NonStrictMode_ReturnsEmpty()
    {
        var env = Jinja.CreateEnvironment();
        env.StrictUndefined = false;

        var result = env.FromString("{{ undefined_var }}").Render();
        Assert.Equal("", result);
    }

    [Fact]
    public void UndefinedFilter_Throws()
    {
        var ex = Assert.Throws<UndefinedFilterException>(() =>
            Jinja.Render("{{ 'test' | nonexistent_filter }}"));

        Assert.Equal("nonexistent_filter", ex.FilterName);
    }

    [Fact]
    public void TemplateNotFound_Throws()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>());

        Assert.Throws<TemplateNotFoundException>(() =>
            env.GetTemplate("missing.html"));
    }

    [Fact]
    public void LexerException_IncludesLocation()
    {
        var ex = Assert.Throws<LexerException>(() =>
            Jinja.Render("{{ 'unclosed string }}"));

        Assert.True(ex.Line > 0);
    }

    [Fact]
    public void ParserException_OnInvalidSyntax()
    {
        var ex = Assert.Throws<ParserException>(() =>
            Jinja.Render("{% if %}{% endif %}"));

        Assert.True(ex.Line > 0);
    }

    [Fact]
    public void DivisionByZero_Throws()
    {
        Assert.Throws<RenderException>(() =>
            Jinja.Render("{{ 1 / 0 }}"));
    }

    [Fact]
    public void UnclosedBlock_Throws()
    {
        Assert.Throws<LexerException>(() =>
            Jinja.Render("{{ name "));
    }

    [Fact]
    public void MismatchedEndblock_Throws()
    {
        Assert.Throws<ParserException>(() =>
            Jinja.Render("{% block test %}{% endblock other %}"));
    }
}
