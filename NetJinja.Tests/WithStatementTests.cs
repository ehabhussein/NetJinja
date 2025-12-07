namespace NetJinja.Tests;

public class WithStatementTests
{
    [Fact]
    public void With_CreatesLocalScope()
    {
        var result = Jinja.Render(@"
{% with x = 1 %}
{{ x }}
{% endwith %}
");
        Assert.Contains("1", result);
    }

    [Fact]
    public void With_MultipleVariables_AllAvailable()
    {
        var result = Jinja.Render(@"
{% with x = 1, y = 2, z = 3 %}
{{ x + y + z }}
{% endwith %}
");
        Assert.Contains("6", result);
    }

    [Fact]
    public void With_ShadowsOuterVariable()
    {
        var result = Jinja.Render(@"
{% set x = 'outer' %}
{% with x = 'inner' %}{{ x }}{% endwith %}
{{ x }}
", new { });
        Assert.Contains("inner", result);
        Assert.Contains("outer", result);
    }

    [Fact]
    public void With_CanReferenceOuterVariables()
    {
        var result = Jinja.Render(@"
{% with doubled = x * 2 %}{{ doubled }}{% endwith %}
", new { x = 21 });
        Assert.Contains("42", result);
    }

    [Fact]
    public void With_Nested_WorksCorrectly()
    {
        var result = Jinja.Render(@"
{% with a = 1 %}
{% with b = 2 %}
{{ a + b }}
{% endwith %}
{% endwith %}
");
        Assert.Contains("3", result);
    }
}
