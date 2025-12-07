using NetJinja.Runtime;

namespace NetJinja.Tests;

/// <summary>
/// Tests to verify reported issues from external testing.
/// </summary>
public class ReportedIssuesTests
{
    [Fact]
    public void Wordcount_Filter_ShouldCountWords()
    {
        // Reported: wordcount filter not implemented
        var result = Jinja.Render("{{ text | wordcount }}", new { text = "Hello world foo bar" });
        Assert.Equal("4", result);
    }

    [Fact]
    public void LoopCycle_ShouldAlternateValues()
    {
        // Reported: loop.cycle() returns empty string, not callable
        var template = "{% for i in items %}{{ loop.cycle('odd', 'even') }}{% endfor %}";
        var result = Jinja.Render(template, new { items = new[] { 1, 2, 3, 4 } });
        Assert.Equal("oddevenoddeven", result);
    }

    [Fact]
    public void Super_ShouldCallParentBlock()
    {
        // Reported: super() returns empty string, not callable
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["base"] = "{% block content %}BASE{% endblock %}"
        });

        var child = env.FromString("{% extends 'base' %}{% block content %}{{ super() }}CHILD{% endblock %}");
        var result = child.Render();

        Assert.Equal("BASECHILD", result);
    }

    [Fact]
    public void TrimBlocks_ShouldRemoveNewlineAfterBlockTag()
    {
        // Reported: TrimBlocks not trimming newline after block tags
        var env = Jinja.CreateEnvironment();
        env.TrimBlocks = true;

        var template = env.FromString("{% if true %}\nHello{% endif %}");
        var result = template.Render();

        // With TrimBlocks, the newline after %} should be removed
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void LstripBlocks_ShouldStripLeadingWhitespace()
    {
        // Reported: LstripBlocks not stripping leading whitespace
        var env = Jinja.CreateEnvironment();
        env.LstripBlocks = true;

        var template = env.FromString("    {% if true %}Hello{% endif %}");
        var result = template.Render();

        // With LstripBlocks, leading whitespace before {% should be removed
        Assert.Equal("Hello", result);
    }
}
