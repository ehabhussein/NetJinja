namespace NetJinja.Tests;

public class FilterTests
{
    [Fact]
    public void Upper_ConvertsToUppercase()
    {
        Assert.Equal("HELLO", Jinja.Render("{{ 'hello' | upper }}"));
    }

    [Fact]
    public void Lower_ConvertsToLowercase()
    {
        Assert.Equal("hello", Jinja.Render("{{ 'HELLO' | lower }}"));
    }

    [Fact]
    public void Capitalize_CapitalizesFirst()
    {
        Assert.Equal("Hello world", Jinja.Render("{{ 'hello world' | capitalize }}"));
    }

    [Fact]
    public void Title_TitleCases()
    {
        Assert.Equal("Hello World", Jinja.Render("{{ 'hello world' | title }}"));
    }

    [Fact]
    public void Trim_RemovesWhitespace()
    {
        Assert.Equal("hello", Jinja.Render("{{ '  hello  ' | trim }}"));
    }

    [Fact]
    public void Replace_SubstitutesText()
    {
        Assert.Equal("hi world", Jinja.Render("{{ 'hello world' | replace('hello', 'hi') }}"));
    }

    [Fact]
    public void Replace_WithCount_LimitsReplacements()
    {
        Assert.Equal("b-a-a", Jinja.Render("{{ 'a-a-a' | replace('a', 'b', 1) }}"));
    }

    [Fact]
    public void Truncate_ShortensText()
    {
        Assert.Equal("Hello...", Jinja.Render("{{ 'Hello World' | truncate(8) }}"));
    }

    [Fact]
    public void Truncate_PreservesWordBoundary()
    {
        Assert.Equal("Hello...", Jinja.Render("{{ 'Hello World' | truncate(10) }}"));
    }

    [Fact]
    public void Truncate_KillWords_CutsExactly()
    {
        Assert.Equal("Hello...", Jinja.Render("{{ 'Hello World' | truncate(8, true) }}"));
    }

    [Fact]
    public void Length_ReturnsLength()
    {
        Assert.Equal("5", Jinja.Render("{{ 'hello' | length }}"));
        Assert.Equal("3", Jinja.Render("{{ [1, 2, 3] | length }}"));
    }

    [Fact]
    public void First_ReturnsFirstElement()
    {
        Assert.Equal("a", Jinja.Render("{{ ['a', 'b', 'c'] | first }}"));
        Assert.Equal("h", Jinja.Render("{{ 'hello' | first }}"));
    }

    [Fact]
    public void Last_ReturnsLastElement()
    {
        Assert.Equal("c", Jinja.Render("{{ ['a', 'b', 'c'] | last }}"));
        Assert.Equal("o", Jinja.Render("{{ 'hello' | last }}"));
    }

    [Fact]
    public void Join_JoinsElements()
    {
        Assert.Equal("a, b, c", Jinja.Render("{{ ['a', 'b', 'c'] | join(', ') }}"));
    }

    [Fact]
    public void Join_WithAttribute_JoinsPropertyValues()
    {
        var result = Jinja.Render(
            "{{ items | join(', ', attribute='name') }}",
            new { items = new[] { new { name = "a" }, new { name = "b" } } });
        Assert.Equal("a, b", result);
    }

    [Fact]
    public void Sort_SortsElements()
    {
        Assert.Equal("1-2-3", Jinja.Render("{{ [3, 1, 2] | sort | join('-') }}"));
    }

    [Fact]
    public void Sort_Reverse_SortsDescending()
    {
        Assert.Equal("3-2-1", Jinja.Render("{{ [1, 2, 3] | sort(reverse=true) | join('-') }}"));
    }

    [Fact]
    public void Reverse_ReversesSequence()
    {
        Assert.Equal("c-b-a", Jinja.Render("{{ ['a', 'b', 'c'] | reverse | join('-') }}"));
        Assert.Equal("olleh", Jinja.Render("{{ 'hello' | reverse }}"));
    }

    [Fact]
    public void Unique_RemovesDuplicates()
    {
        Assert.Equal("1-2-3", Jinja.Render("{{ [1, 2, 2, 3, 1] | unique | join('-') }}"));
    }

    [Fact]
    public void Map_ExtractsAttribute()
    {
        var result = Jinja.Render(
            "{{ items | map(attribute='name') | join(',') }}",
            new { items = new[] { new { name = "a" }, new { name = "b" } } });
        Assert.Equal("a,b", result);
    }

    [Fact]
    public void Select_FiltersTruthy()
    {
        Assert.Equal("1-2", Jinja.Render("{{ [0, 1, '', 2] | select | join('-') }}"));
    }

    [Fact]
    public void Reject_FiltersFalsy()
    {
        // Reject keeps falsy values: 0 and ''
        Assert.Equal("0-", Jinja.Render("{{ [0, 1, '', 2] | reject | join('-') }}"));
    }

    [Fact]
    public void Batch_GroupsElements()
    {
        var result = Jinja.Render("{% for batch in items | batch(2) %}[{{ batch | join(',') }}]{% endfor %}",
            new { items = new[] { 1, 2, 3, 4, 5 } });
        Assert.Equal("[1,2][3,4][5]", result);
    }

    [Fact]
    public void Batch_WithFillValue_PadsLastBatch()
    {
        var result = Jinja.Render("{% for batch in items | batch(2, 'x') %}[{{ batch | join(',') }}]{% endfor %}",
            new { items = new[] { 1, 2, 3 } });
        Assert.Equal("[1,2][3,x]", result);
    }

    [Fact]
    public void Abs_ReturnsAbsoluteValue()
    {
        // Using variable to avoid precedence issues with unary minus and pipe
        Assert.Equal("5", Jinja.Render("{{ x | abs }}", new { x = -5 }));
    }

    [Fact]
    public void Round_RoundsNumber()
    {
        Assert.Equal("3", Jinja.Render("{{ 2.7 | round }}"));
        Assert.Equal("2.35", Jinja.Render("{{ 2.345 | round(2) }}"));
    }

    [Fact]
    public void Round_WithMethod_RoundsCorrectly()
    {
        Assert.Equal("3", Jinja.Render("{{ 2.1 | round(method='ceil') }}"));
        Assert.Equal("2", Jinja.Render("{{ 2.9 | round(method='floor') }}"));
    }

    [Fact]
    public void Int_ConvertsToInteger()
    {
        Assert.Equal("42", Jinja.Render("{{ 42.7 | int }}"));
    }

    [Fact]
    public void Float_ConvertsToFloat()
    {
        Assert.Equal("42", Jinja.Render("{{ 42 | float }}"));
    }

    [Fact]
    public void Sum_SumsElements()
    {
        Assert.Equal("6", Jinja.Render("{{ [1, 2, 3] | sum }}"));
    }

    [Fact]
    public void Sum_WithStart_AddsStartValue()
    {
        Assert.Equal("16", Jinja.Render("{{ [1, 2, 3] | sum(start=10) }}"));
    }

    [Fact]
    public void Min_ReturnsMinimum()
    {
        Assert.Equal("1", Jinja.Render("{{ [3, 1, 2] | min }}"));
    }

    [Fact]
    public void Max_ReturnsMaximum()
    {
        Assert.Equal("3", Jinja.Render("{{ [3, 1, 2] | max }}"));
    }

    [Fact]
    public void Default_ReturnsDefaultWhenUndefined()
    {
        Assert.Equal("default", Jinja.Render("{{ undefined_var | default('default') }}"));
    }

    [Fact]
    public void Default_ReturnsValueWhenDefined()
    {
        Assert.Equal("value", Jinja.Render("{{ x | default('default') }}", new { x = "value" }));
    }

    [Fact]
    public void Default_Boolean_ReturnsDefaultForFalsy()
    {
        Assert.Equal("default", Jinja.Render("{{ '' | default('default', true) }}"));
    }

    [Fact]
    public void Escape_EscapesHtml()
    {
        Assert.Equal("&lt;script&gt;", Jinja.Render("{{ '<script>' | escape }}"));
    }

    [Fact]
    public void Safe_BypassesEscaping()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = true;
        var result = env.FromString("{{ html | safe }}").Render(new { html = "<b>bold</b>" });
        Assert.Equal("<b>bold</b>", result);
    }

    [Fact]
    public void Striptags_RemovesHtmlTags()
    {
        Assert.Equal("Hello World", Jinja.Render("{{ '<p>Hello</p> <b>World</b>' | striptags }}"));
    }

    [Fact]
    public void Urlencode_EncodesUrl()
    {
        Assert.Equal("hello%20world", Jinja.Render("{{ 'hello world' | urlencode }}"));
    }

    [Fact]
    public void Tojson_SerializesToJson()
    {
        var result = Jinja.Render("{{ data | tojson }}", new { data = new { a = 1, b = "text" } });
        Assert.Contains("\"a\":1", result);
        Assert.Contains("\"b\":\"text\"", result);
    }

    [Fact]
    public void Items_ReturnsDictionaryItems()
    {
        var result = Jinja.Render(
            "{% for k, v in data | items %}{{ k }}={{ v }};{% endfor %}",
            new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
        Assert.Contains("a=1", result);
        Assert.Contains("b=2", result);
    }

    [Fact]
    public void Keys_ReturnsDictionaryKeys()
    {
        var result = Jinja.Render(
            "{{ data | keys | join(',') }}",
            new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
        Assert.Contains("a", result);
        Assert.Contains("b", result);
    }

    [Fact]
    public void Values_ReturnsDictionaryValues()
    {
        var result = Jinja.Render(
            "{{ data | values | join(',') }}",
            new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
        Assert.Contains("1", result);
        Assert.Contains("2", result);
    }

    [Fact]
    public void FilesizeFormat_FormatsBytes()
    {
        Assert.Equal("1.0 KB", Jinja.Render("{{ 1000 | filesizeformat }}"));
        Assert.Equal("1.0 MB", Jinja.Render("{{ 1000000 | filesizeformat }}"));
    }

    [Fact]
    public void Center_CentersText()
    {
        Assert.Equal("  ab  ", Jinja.Render("{{ 'ab' | center(6) }}"));
    }

    [Fact]
    public void Indent_IndentsText()
    {
        Assert.Equal("    hello\n    world", Jinja.Render("{{ 'hello\nworld' | indent(4, first=true) }}"));
    }

    [Fact]
    public void GroupBy_GroupsElements()
    {
        var result = Jinja.Render(
            "{% for group in items | groupby('type') %}{{ group.grouper }}:{{ group.list | length }};{% endfor %}",
            new { items = new[] { new { type = "a" }, new { type = "b" }, new { type = "a" } } });
        Assert.Contains("a:2", result);
        Assert.Contains("b:1", result);
    }

    [Fact]
    public void CustomFilter_CanBeRegistered()
    {
        var env = Jinja.CreateEnvironment();
        env.AddFilter("double", (v) => Convert.ToInt32(v) * 2);
        var result = env.FromString("{{ 21 | double }}").Render();
        Assert.Equal("42", result);
    }
}
