namespace NetJinja.Tests;

public class MacroTests
{
    [Fact]
    public void Macro_CanBeDefined()
    {
        var result = Jinja.Render("{% macro greet() %}Hello{% endmacro %}{{ greet() }}");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Macro_WithParameters_AcceptsArguments()
    {
        var result = Jinja.Render("{% macro greet(name) %}Hello, {{ name }}!{% endmacro %}{{ greet('World') }}");
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Macro_WithDefaultParameters_UsesDefaults()
    {
        var result = Jinja.Render("{% macro greet(name='World') %}Hello, {{ name }}!{% endmacro %}{{ greet() }}");
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Macro_WithDefaultParameters_OverridesDefaults()
    {
        var result = Jinja.Render("{% macro greet(name='World') %}Hello, {{ name }}!{% endmacro %}{{ greet('Alice') }}");
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void Macro_WithKeywordArguments_Works()
    {
        var result = Jinja.Render("{% macro greet(name='World') %}Hello, {{ name }}!{% endmacro %}{{ greet(name='Bob') }}");
        Assert.Equal("Hello, Bob!", result);
    }

    [Fact]
    public void Macro_MultipleParameters_WorksCorrectly()
    {
        var result = Jinja.Render(@"
{% macro input(name, value='', type='text') %}
<input type=""{{ type }}"" name=""{{ name }}"" value=""{{ value }}"">
{% endmacro %}
{{ input('username') }}
{{ input('password', type='password') }}
");
        Assert.Contains("type=\"text\"", result);
        Assert.Contains("name=\"username\"", result);
        Assert.Contains("type=\"password\"", result);
        Assert.Contains("name=\"password\"", result);
    }

    [Fact]
    public void Macro_NestedCalls_Work()
    {
        var result = Jinja.Render(@"
{% macro outer() %}[{{ inner() }}]{% endmacro %}
{% macro inner() %}INNER{% endmacro %}
{{ outer() }}
");
        Assert.Contains("[INNER]", result);
    }

    [Fact]
    public void Macro_WithLoops_Works()
    {
        var result = Jinja.Render(@"
{% macro list_items(items) %}
{% for item in items %}{{ item }},{% endfor %}
{% endmacro %}
{{ list_items([1, 2, 3]) }}
");
        Assert.Contains("1,2,3,", result);
    }

    [Fact]
    public void Macro_OutputIsHtmlSafe()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = true;
        var result = env.FromString(@"
{% macro test() %}<b>bold</b>{% endmacro %}
{{ test() }}
").Render();
        Assert.Contains("<b>bold</b>", result);
    }
}
