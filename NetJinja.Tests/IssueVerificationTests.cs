namespace NetJinja.Tests;

/// <summary>
/// Tests to verify reported issues.
/// </summary>
public class IssueVerificationTests
{
    [Fact]
    public void Issue1_RawBlock_ShouldPreserveSpaces()
    {
        // Claim: Raw block stripped spaces ({{ this will not be processed }} → {{thiswillnotbeprocessed}})
        var template = "{% raw %}{{ this will not be processed }}{% endraw %}";
        var result = Jinja.Render(template);

        // Raw should preserve the content exactly as-is, including spaces
        Assert.Equal("{{ this will not be processed }}", result);
    }

    [Fact]
    public void Issue2_WhitespaceControl_ShouldRemoveNewlines()
    {
        // Claim: Whitespace control ({%-) added extra newlines instead of removing them
        // {%- removes whitespace BEFORE the tag (trim left)
        // -%} removes whitespace AFTER the tag (trim right)

        // Simple test: {%- should trim preceding whitespace
        var simple = "Hello\n{%- if true %}World{% endif %}";
        var simpleResult = Jinja.Render(simple);
        Assert.Equal("HelloWorld", simpleResult);
    }

    [Fact]
    public void Issue2b_WhitespaceControl_TrailingDash()
    {
        // Test -%} which removes whitespace AFTER the tag
        var template = "Hello{% if true -%}\n\nWorld{% endif %}";
        var result = Jinja.Render(template);

        Console.WriteLine($"Result: [{result.Replace("\n", "\\n").Replace("\r", "\\r")}]");

        // -%} should remove whitespace after, so the \n\n after if should be removed
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void Issue3_Dictionary_NoNullabilityWarning()
    {
        // Claim: Nullability warning with Dictionary<string, object>
        // Fixed by using Dictionary<string, object?> which matches the API signature
        var data = new Dictionary<string, object?>
        {
            ["name"] = "John",
            ["age"] = 30,
            ["items"] = new[] { "a", "b", "c" }
        };

        var result = Jinja.Render("{{ name }} is {{ age }}", data);
        Assert.Equal("John is 30", result);
    }

    [Fact]
    public void Issue3b_Dictionary_WithNullValue()
    {
        // Test dictionary with null value
        var data = new Dictionary<string, object?>
        {
            ["name"] = "John",
            ["value"] = null
        };

        var result = Jinja.Render("{{ name }}: {{ value | default('none') }}", data);
        Assert.Equal("John: none", result);
    }
}
