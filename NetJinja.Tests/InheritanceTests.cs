using NetJinja.Runtime;

namespace NetJinja.Tests;

public class InheritanceTests
{
    [Fact]
    public void Extends_InheritsFromParent()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["base.html"] = "Header{% block content %}Default{% endblock %}Footer",
            ["child.html"] = "{% extends \"base.html\" %}{% block content %}Child Content{% endblock %}"
        });

        var result = env.GetTemplate("child.html").Render();
        Assert.Equal("HeaderChild ContentFooter", result);
    }

    [Fact]
    public void Block_UsesDefaultWhenNotOverridden()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["base.html"] = "{% block title %}Default Title{% endblock %}",
            ["child.html"] = "{% extends \"base.html\" %}"
        });

        var result = env.GetTemplate("child.html").Render();
        Assert.Equal("Default Title", result);
    }

    [Fact]
    public void Block_CanBeOverridden()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["base.html"] = "{% block title %}Default{% endblock %}",
            ["child.html"] = "{% extends \"base.html\" %}{% block title %}Custom{% endblock %}"
        });

        var result = env.GetTemplate("child.html").Render();
        Assert.Equal("Custom", result);
    }

    [Fact]
    public void MultipleBlocks_AllCanBeOverridden()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["base.html"] = "{% block a %}A{% endblock %}-{% block b %}B{% endblock %}",
            ["child.html"] = "{% extends \"base.html\" %}{% block a %}X{% endblock %}{% block b %}Y{% endblock %}"
        });

        var result = env.GetTemplate("child.html").Render();
        Assert.Equal("X-Y", result);
    }

    [Fact]
    public void MultiLevelInheritance_Works()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["base.html"] = "[{% block content %}Base{% endblock %}]",
            ["middle.html"] = "{% extends \"base.html\" %}{% block content %}Middle{% endblock %}",
            ["child.html"] = "{% extends \"middle.html\" %}{% block content %}Child{% endblock %}"
        });

        var result = env.GetTemplate("child.html").Render();
        Assert.Equal("[Child]", result);
    }

    [Fact]
    public void Include_InsertsTemplate()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["main.html"] = "Before{% include \"partial.html\" %}After",
            ["partial.html"] = "[PARTIAL]"
        });

        var result = env.GetTemplate("main.html").Render();
        Assert.Equal("Before[PARTIAL]After", result);
    }

    [Fact]
    public void Include_WithContext_SharesVariables()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["main.html"] = "{% include \"partial.html\" %}",
            ["partial.html"] = "{{ name }}"
        });

        var result = env.GetTemplate("main.html").Render(new { name = "Alice" });
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void Include_IgnoreMissing_DoesNotThrow()
    {
        var env = Jinja.CreateEnvironment();
        env.Loader = new DictLoader(new Dictionary<string, string>
        {
            ["main.html"] = "Before{% include \"missing.html\" ignore missing %}After"
        });

        var result = env.GetTemplate("main.html").Render();
        Assert.Equal("BeforeAfter", result);
    }

    [Fact]
    public void BlockNamed_ValidatesMatchingName()
    {
        var env = Jinja.CreateEnvironment();
        var template = env.FromString("{% block test %}Content{% endblock test %}");
        var result = template.Render();
        Assert.Equal("Content", result);
    }
}
