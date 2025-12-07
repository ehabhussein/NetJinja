using NetJinja.Runtime;

namespace NetJinja.Tests;

public class RenderTests
{
    private readonly JinjaEnvironment _env = new();

    [Fact]
    public void Render_PlainText_ReturnsUnchanged()
    {
        var result = Jinja.Render("Hello, World!");
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Render_SimpleVariable_SubstitutesValue()
    {
        var result = Jinja.Render("Hello, {{ name }}!", new { name = "Alice" });
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void Render_ObjectProperty_AccessesProperty()
    {
        var result = Jinja.Render("{{ user.name }}", new
        {
            user = new { name = "Bob" }
        });
        Assert.Equal("Bob", result);
    }

    [Fact]
    public void Render_DictionaryKey_AccessesValue()
    {
        var result = Jinja.Render("{{ data.key }}", new Dictionary<string, object?>
        {
            ["data"] = new Dictionary<string, object?> { ["key"] = "value" }
        });
        Assert.Equal("value", result);
    }

    [Fact]
    public void Render_ListIndex_AccessesElement()
    {
        var result = Jinja.Render("{{ items[1] }}", new { items = new[] { "a", "b", "c" } });
        Assert.Equal("b", result);
    }

    [Fact]
    public void Render_NegativeIndex_CountsFromEnd()
    {
        var result = Jinja.Render("{{ items[-1] }}", new { items = new[] { "a", "b", "c" } });
        Assert.Equal("c", result);
    }

    [Fact]
    public void Render_Arithmetic_EvaluatesCorrectly()
    {
        Assert.Equal("7", Jinja.Render("{{ 3 + 4 }}"));
        Assert.Equal("6", Jinja.Render("{{ 10 - 4 }}"));
        Assert.Equal("12", Jinja.Render("{{ 3 * 4 }}"));
        Assert.Equal("2.5", Jinja.Render("{{ 5 / 2 }}"));
        Assert.Equal("2", Jinja.Render("{{ 5 // 2 }}"));
        Assert.Equal("1", Jinja.Render("{{ 5 % 2 }}"));
        Assert.Equal("8", Jinja.Render("{{ 2 ** 3 }}"));
    }

    [Fact]
    public void Render_Comparison_EvaluatesCorrectly()
    {
        Assert.Equal("True", Jinja.Render("{{ 1 == 1 }}"));
        Assert.Equal("True", Jinja.Render("{{ 1 != 2 }}"));
        Assert.Equal("True", Jinja.Render("{{ 1 < 2 }}"));
        Assert.Equal("True", Jinja.Render("{{ 2 > 1 }}"));
        Assert.Equal("True", Jinja.Render("{{ 1 <= 1 }}"));
        Assert.Equal("True", Jinja.Render("{{ 2 >= 2 }}"));
    }

    [Fact]
    public void Render_Logical_EvaluatesCorrectly()
    {
        Assert.Equal("True", Jinja.Render("{{ true and true }}"));
        Assert.Equal("False", Jinja.Render("{{ true and false }}"));
        Assert.Equal("True", Jinja.Render("{{ true or false }}"));
        Assert.Equal("False", Jinja.Render("{{ not true }}"));
    }

    [Fact]
    public void Render_StringConcatenation_Works()
    {
        var result = Jinja.Render("{{ 'hello' ~ ' ' ~ 'world' }}");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Render_StringRepetition_Works()
    {
        var result = Jinja.Render("{{ 'ab' * 3 }}");
        Assert.Equal("ababab", result);
    }

    [Fact]
    public void Render_ConditionalExpression_Works()
    {
        Assert.Equal("yes", Jinja.Render("{{ 'yes' if true else 'no' }}"));
        Assert.Equal("no", Jinja.Render("{{ 'yes' if false else 'no' }}"));
    }

    [Fact]
    public void Render_InOperator_ChecksMembership()
    {
        Assert.Equal("True", Jinja.Render("{{ 2 in [1, 2, 3] }}"));
        Assert.Equal("False", Jinja.Render("{{ 4 in [1, 2, 3] }}"));
        Assert.Equal("True", Jinja.Render("{{ 'a' in 'abc' }}"));
    }

    [Fact]
    public void Render_NotInOperator_ChecksNonMembership()
    {
        Assert.Equal("True", Jinja.Render("{{ 4 not in [1, 2, 3] }}"));
        Assert.Equal("False", Jinja.Render("{{ 2 not in [1, 2, 3] }}"));
    }

    [Fact]
    public void Render_ListLiteral_CreatesLIst()
    {
        var result = Jinja.Render("{{ [1, 2, 3] | join('-') }}");
        Assert.Equal("1-2-3", result);
    }

    [Fact]
    public void Render_DictLiteral_CreatesDict()
    {
        var result = Jinja.Render("{{ {'a': 1, 'b': 2} | length }}");
        Assert.Equal("2", result);
    }
}
