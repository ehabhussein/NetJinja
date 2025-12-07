namespace NetJinja.Tests;

public class SetStatementTests
{
    [Fact]
    public void Set_AssignsVariable()
    {
        var result = Jinja.Render("{% set x = 42 %}{{ x }}");
        Assert.Equal("42", result);
    }

    [Fact]
    public void Set_AssignsExpression()
    {
        var result = Jinja.Render("{% set x = 1 + 2 + 3 %}{{ x }}");
        Assert.Equal("6", result);
    }

    [Fact]
    public void Set_AssignsString()
    {
        var result = Jinja.Render("{% set greeting = 'Hello' %}{{ greeting }}");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Set_AssignsList()
    {
        var result = Jinja.Render("{% set items = [1, 2, 3] %}{{ items | join('-') }}");
        Assert.Equal("1-2-3", result);
    }

    [Fact]
    public void Set_AssignsDict()
    {
        var result = Jinja.Render("{% set data = {'a': 1} %}{{ data.a }}");
        Assert.Equal("1", result);
    }

    [Fact]
    public void Set_TupleUnpacking_Works()
    {
        var result = Jinja.Render("{% set a, b = [1, 2] %}{{ a }}-{{ b }}");
        Assert.Equal("1-2", result);
    }

    [Fact]
    public void Set_BlockForm_CapturesOutput()
    {
        var result = Jinja.Render(@"
{% set content %}
Hello World
{% endset %}
[{{ content | trim }}]
");
        Assert.Contains("[Hello World]", result);
    }

    [Fact]
    public void Set_CanBeOverwritten()
    {
        var result = Jinja.Render("{% set x = 1 %}{% set x = 2 %}{{ x }}");
        Assert.Equal("2", result);
    }

    [Fact]
    public void Set_ScopedInWith()
    {
        var result = Jinja.Render(@"
{% set x = 'outer' %}
{% with %}
    {% set x = 'inner' %}
    {{ x }}
{% endwith %}
{{ x }}
");
        Assert.Contains("inner", result);
        Assert.Contains("outer", result);
    }
}
