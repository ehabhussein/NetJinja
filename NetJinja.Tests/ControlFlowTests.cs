namespace NetJinja.Tests;

public class ControlFlowTests
{
    [Fact]
    public void If_TrueCondition_RendersBody()
    {
        var result = Jinja.Render("{% if true %}yes{% endif %}");
        Assert.Equal("yes", result);
    }

    [Fact]
    public void If_FalseCondition_SkipsBody()
    {
        var result = Jinja.Render("{% if false %}yes{% endif %}");
        Assert.Equal("", result);
    }

    [Fact]
    public void If_Else_RendersElseBranch()
    {
        var result = Jinja.Render("{% if false %}yes{% else %}no{% endif %}");
        Assert.Equal("no", result);
    }

    [Fact]
    public void If_Elif_RendersCorrectBranch()
    {
        var template = "{% if x == 1 %}one{% elif x == 2 %}two{% else %}other{% endif %}";
        Assert.Equal("one", Jinja.Render(template, new { x = 1 }));
        Assert.Equal("two", Jinja.Render(template, new { x = 2 }));
        Assert.Equal("other", Jinja.Render(template, new { x = 3 }));
    }

    [Fact]
    public void If_NestedConditions_WorksCorrectly()
    {
        var result = Jinja.Render("{% if true %}{% if true %}nested{% endif %}{% endif %}");
        Assert.Equal("nested", result);
    }

    [Fact]
    public void If_TruthyValues_EvaluateCorrectly()
    {
        Assert.Equal("yes", Jinja.Render("{% if 1 %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if 'text' %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if [1] %}yes{% endif %}"));
        Assert.Equal("", Jinja.Render("{% if 0 %}yes{% endif %}"));
        Assert.Equal("", Jinja.Render("{% if '' %}yes{% endif %}"));
        Assert.Equal("", Jinja.Render("{% if [] %}yes{% endif %}"));
    }

    [Fact]
    public void For_SimpleLoop_IteratesCorrectly()
    {
        var result = Jinja.Render("{% for i in items %}{{ i }}{% endfor %}", new { items = new[] { 1, 2, 3 } });
        Assert.Equal("123", result);
    }

    [Fact]
    public void For_EmptyIterable_SkipsBody()
    {
        var result = Jinja.Render("{% for i in items %}{{ i }}{% endfor %}", new { items = Array.Empty<int>() });
        Assert.Equal("", result);
    }

    [Fact]
    public void For_ElseBranch_RendersWhenEmpty()
    {
        var result = Jinja.Render("{% for i in items %}{{ i }}{% else %}empty{% endfor %}", new { items = Array.Empty<int>() });
        Assert.Equal("empty", result);
    }

    [Fact]
    public void For_TupleUnpacking_Works()
    {
        var result = Jinja.Render(
            "{% for k, v in items %}{{ k }}={{ v }};{% endfor %}",
            new { items = new[] { new object[] { "a", 1 }, new object[] { "b", 2 } } });
        Assert.Equal("a=1;b=2;", result);
    }

    [Fact]
    public void For_LoopIndex_IsAvailable()
    {
        var result = Jinja.Render("{% for i in items %}{{ loop.index }}{% endfor %}", new { items = new[] { "a", "b", "c" } });
        Assert.Equal("123", result);
    }

    [Fact]
    public void For_LoopIndex0_IsZeroBased()
    {
        var result = Jinja.Render("{% for i in items %}{{ loop.index0 }}{% endfor %}", new { items = new[] { "a", "b", "c" } });
        Assert.Equal("012", result);
    }

    [Fact]
    public void For_LoopFirst_IsTrueOnFirstIteration()
    {
        var result = Jinja.Render("{% for i in items %}{% if loop.first %}F{% endif %}{{ i }}{% endfor %}", new { items = new[] { "a", "b" } });
        Assert.Equal("Fab", result);
    }

    [Fact]
    public void For_LoopLast_IsTrueOnLastIteration()
    {
        var result = Jinja.Render("{% for i in items %}{{ i }}{% if loop.last %}L{% endif %}{% endfor %}", new { items = new[] { "a", "b" } });
        Assert.Equal("abL", result);
    }

    [Fact]
    public void For_LoopLength_ReturnsCount()
    {
        var result = Jinja.Render("{% for i in items %}{{ loop.length }}{% endfor %}", new { items = new[] { 1, 2, 3 } });
        Assert.Equal("333", result);
    }

    [Fact]
    public void For_LoopRevindex_CountsFromEnd()
    {
        var result = Jinja.Render("{% for i in items %}{{ loop.revindex }}{% endfor %}", new { items = new[] { "a", "b", "c" } });
        Assert.Equal("321", result);
    }

    [Fact]
    public void For_NestedLoops_HaveCorrectDepth()
    {
        var result = Jinja.Render(
            "{% for i in a %}{% for j in b %}{{ loop.depth }}{% endfor %}{% endfor %}",
            new { a = new[] { 1 }, b = new[] { 1, 2 } });
        Assert.Equal("22", result);
    }

    [Fact]
    public void For_WithFilter_FiltersItems()
    {
        var result = Jinja.Render(
            "{% for i in items if i > 1 %}{{ i }}{% endfor %}",
            new { items = new[] { 1, 2, 3 } });
        Assert.Equal("23", result);
    }

    [Fact]
    public void For_Break_StopsIteration()
    {
        var result = Jinja.Render(
            "{% for i in items %}{% if i == 2 %}{% break %}{% endif %}{{ i }}{% endfor %}",
            new { items = new[] { 1, 2, 3 } });
        Assert.Equal("1", result);
    }

    [Fact]
    public void For_Continue_SkipsIteration()
    {
        var result = Jinja.Render(
            "{% for i in items %}{% if i == 2 %}{% continue %}{% endif %}{{ i }}{% endfor %}",
            new { items = new[] { 1, 2, 3 } });
        Assert.Equal("13", result);
    }

    [Fact]
    public void For_IteratesOverString()
    {
        var result = Jinja.Render("{% for c in text %}{{ c }},{% endfor %}", new { text = "abc" });
        Assert.Equal("a,b,c,", result);
    }

    [Fact]
    public void For_IteratesOverDictionary()
    {
        var result = Jinja.Render(
            "{% for k in data %}{{ k }}{% endfor %}",
            new { data = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
        Assert.Contains("a", result);
        Assert.Contains("b", result);
    }
}
