using NetJinja.Runtime;

namespace NetJinja.Tests;

public class AutoescapeTests
{
    [Fact]
    public void AutoEscape_Disabled_DoesNotEscapeHtml()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = false;
        var result = env.FromString("{{ html }}").Render(new { html = "<b>bold</b>" });
        Assert.Equal("<b>bold</b>", result);
    }

    [Fact]
    public void AutoEscape_Enabled_EscapesHtml()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = true;
        var result = env.FromString("{{ html }}").Render(new { html = "<b>bold</b>" });
        Assert.Equal("&lt;b&gt;bold&lt;/b&gt;", result);
    }

    [Fact]
    public void AutoEscape_SafeFilter_BypassesEscaping()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = true;
        var result = env.FromString("{{ html | safe }}").Render(new { html = "<b>bold</b>" });
        Assert.Equal("<b>bold</b>", result);
    }

    [Fact]
    public void AutoEscape_EscapeFilter_ExplicitlyEscapes()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = false;
        var result = env.FromString("{{ html | escape }}").Render(new { html = "<b>bold</b>" });
        Assert.Equal("&lt;b&gt;bold&lt;/b&gt;", result);
    }

    [Fact]
    public void AutoescapeBlock_EnablesEscaping()
    {
        var result = Jinja.Render(@"
{% autoescape true %}{{ html }}{% endautoescape %}
", new { html = "<script>" });
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void AutoescapeBlock_DisablesEscaping()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = true;
        var result = env.FromString(@"
{% autoescape false %}{{ html }}{% endautoescape %}
").Render(new { html = "<script>" });
        Assert.Contains("<script>", result);
    }

    [Fact]
    public void AutoescapeBlock_RestoredAfterBlock()
    {
        var env = Jinja.CreateEnvironment();
        env.AutoEscape = true;
        var result = env.FromString(@"
{% autoescape false %}{{ html }}{% endautoescape %}
{{ html }}
").Render(new { html = "<b>" });
        Assert.Contains("<b>", result);
        Assert.Contains("&lt;b&gt;", result);
    }

    [Fact]
    public void Escape_HandlesSpecialCharacters()
    {
        var result = Jinja.Render("{{ text | escape }}", new { text = "< > & \" '" });
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.Contains("&lt;", result);
        Assert.Contains("&gt;", result);
        Assert.Contains("&amp;", result);
    }
}
