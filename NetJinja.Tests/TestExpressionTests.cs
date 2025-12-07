namespace NetJinja.Tests;

public class TestExpressionTests
{
    [Fact]
    public void Defined_IsTrueForDefinedVariable()
    {
        Assert.Equal("yes", Jinja.Render("{% if x is defined %}yes{% endif %}", new { x = 1 }));
    }

    [Fact]
    public void Defined_IsFalseForUndefinedVariable()
    {
        Assert.Equal("no", Jinja.Render("{% if x is defined %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Undefined_IsTrueForUndefinedVariable()
    {
        Assert.Equal("yes", Jinja.Render("{% if x is undefined %}yes{% endif %}"));
    }

    [Fact]
    public void None_IsTrueForNull()
    {
        Assert.Equal("yes", Jinja.Render("{% if x is none %}yes{% endif %}", new Dictionary<string, object?> { ["x"] = null }));
    }

    [Fact]
    public void Boolean_IsTrueForBooleans()
    {
        Assert.Equal("yes", Jinja.Render("{% if true is boolean %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 1 is boolean %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Integer_IsTrueForIntegers()
    {
        Assert.Equal("yes", Jinja.Render("{% if 42 is integer %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 3.14 is integer %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Float_IsTrueForFloats()
    {
        Assert.Equal("yes", Jinja.Render("{% if 3.14 is float %}yes{% endif %}"));
    }

    [Fact]
    public void Number_IsTrueForNumbers()
    {
        Assert.Equal("yes", Jinja.Render("{% if 42 is number %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if 3.14 is number %}yes{% endif %}"));
    }

    [Fact]
    public void String_IsTrueForStrings()
    {
        Assert.Equal("yes", Jinja.Render("{% if 'hello' is string %}yes{% endif %}"));
    }

    [Fact]
    public void Mapping_IsTrueForDictionaries()
    {
        Assert.Equal("yes", Jinja.Render("{% if data is mapping %}yes{% endif %}",
            new { data = new Dictionary<string, int>() }));
    }

    [Fact]
    public void Iterable_IsTrueForIterables()
    {
        Assert.Equal("yes", Jinja.Render("{% if items is iterable %}yes{% endif %}",
            new { items = new[] { 1, 2, 3 } }));
        Assert.Equal("yes", Jinja.Render("{% if 'text' is iterable %}yes{% endif %}"));
    }

    [Fact]
    public void Sequence_IsTrueForLists()
    {
        Assert.Equal("yes", Jinja.Render("{% if items is sequence %}yes{% endif %}",
            new { items = new[] { 1, 2, 3 } }));
    }

    [Fact]
    public void Odd_IsTrueForOddNumbers()
    {
        Assert.Equal("yes", Jinja.Render("{% if 3 is odd %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 4 is odd %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Even_IsTrueForEvenNumbers()
    {
        Assert.Equal("yes", Jinja.Render("{% if 4 is even %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 3 is even %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Divisibleby_IsTrueWhenDivisible()
    {
        Assert.Equal("yes", Jinja.Render("{% if 9 is divisibleby(3) %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 10 is divisibleby(3) %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Lower_IsTrueForLowercase()
    {
        Assert.Equal("yes", Jinja.Render("{% if 'hello' is lower %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 'Hello' is lower %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Upper_IsTrueForUppercase()
    {
        Assert.Equal("yes", Jinja.Render("{% if 'HELLO' is upper %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 'Hello' is upper %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Empty_IsTrueForEmptyCollections()
    {
        Assert.Equal("yes", Jinja.Render("{% if '' is empty %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if items is empty %}yes{% endif %}", new { items = Array.Empty<int>() }));
        Assert.Equal("no", Jinja.Render("{% if 'text' is empty %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void Eq_ComparesEquality()
    {
        Assert.Equal("yes", Jinja.Render("{% if 1 is eq(1) %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if 1 is equalto(1) %}yes{% endif %}"));
    }

    [Fact]
    public void Ne_ComparesInequality()
    {
        Assert.Equal("yes", Jinja.Render("{% if 1 is ne(2) %}yes{% endif %}"));
    }

    [Fact]
    public void Lt_ComparesLessThan()
    {
        Assert.Equal("yes", Jinja.Render("{% if 1 is lt(2) %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if 1 is lessthan(2) %}yes{% endif %}"));
    }

    [Fact]
    public void Gt_ComparesGreaterThan()
    {
        Assert.Equal("yes", Jinja.Render("{% if 2 is gt(1) %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if 2 is greaterthan(1) %}yes{% endif %}"));
    }

    [Fact]
    public void In_ChecksContainment()
    {
        Assert.Equal("yes", Jinja.Render("{% if 2 is in([1, 2, 3]) %}yes{% endif %}"));
    }

    [Fact]
    public void TrueTest_IsTrueOnlyForTrue()
    {
        Assert.Equal("yes", Jinja.Render("{% if true is true %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 1 is true %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void FalseTest_IsTrueOnlyForFalse()
    {
        Assert.Equal("yes", Jinja.Render("{% if false is false %}yes{% endif %}"));
        Assert.Equal("no", Jinja.Render("{% if 0 is false %}yes{% else %}no{% endif %}"));
    }

    [Fact]
    public void IsNot_NegatesTest()
    {
        Assert.Equal("yes", Jinja.Render("{% if 2 is not odd %}yes{% endif %}"));
        Assert.Equal("yes", Jinja.Render("{% if 4 is not in([1, 2, 3]) %}yes{% endif %}"));
    }
}
