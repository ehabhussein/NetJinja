using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NetJinja;
using NetJinja.Runtime;

BenchmarkRunner.Run<TemplateBenchmarks>();

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class TemplateBenchmarks
{
    private JinjaEnvironment _env = null!;
    private Template _simpleTemplate = null!;
    private Template _variableTemplate = null!;
    private Template _loopTemplate = null!;
    private Template _filterTemplate = null!;
    private Template _conditionalTemplate = null!;
    private Template _complexTemplate = null!;

    private readonly object _simpleModel = new { name = "World" };
    private readonly object _loopModel = new { items = Enumerable.Range(1, 100).ToArray() };
    private readonly object _complexModel = new
    {
        title = "Products",
        products = Enumerable.Range(1, 50).Select(i => new
        {
            id = i,
            name = $"Product {i}",
            price = i * 9.99,
            inStock = i % 3 != 0,
            tags = new[] { "tag1", "tag2", "tag3" }
        }).ToArray()
    };

    [GlobalSetup]
    public void Setup()
    {
        _env = Jinja.CreateEnvironment();

        _simpleTemplate = _env.FromString("Hello!");
        _variableTemplate = _env.FromString("Hello, {{ name }}!");
        _loopTemplate = _env.FromString("{% for i in items %}{{ i }}{% endfor %}");
        _filterTemplate = _env.FromString("{{ name | upper | reverse }}");
        _conditionalTemplate = _env.FromString("{% if name %}Hello, {{ name }}!{% else %}Hello!{% endif %}");
        _complexTemplate = _env.FromString(@"
<html>
<head><title>{{ title }}</title></head>
<body>
<h1>{{ title | upper }}</h1>
<ul>
{% for product in products %}
<li>
    <strong>{{ product.name }}</strong> - ${{ product.price | round(2) }}
    {% if product.inStock %}<span class=""available"">In Stock</span>{% else %}<span class=""unavailable"">Out of Stock</span>{% endif %}
    <div>Tags: {% for tag in product.tags %}{{ tag }}{% if not loop.last %}, {% endif %}{% endfor %}</div>
</li>
{% endfor %}
</ul>
</body>
</html>");
    }

    [Benchmark(Description = "Static text only")]
    public string StaticText() => _simpleTemplate.Render();

    [Benchmark(Description = "Single variable")]
    public string SingleVariable() => _variableTemplate.Render(_simpleModel);

    [Benchmark(Description = "Loop 100 items")]
    public string Loop100Items() => _loopTemplate.Render(_loopModel);

    [Benchmark(Description = "Filter chain")]
    public string FilterChain() => _filterTemplate.Render(_simpleModel);

    [Benchmark(Description = "Conditional")]
    public string Conditional() => _conditionalTemplate.Render(_simpleModel);

    [Benchmark(Description = "Complex template (50 products)")]
    public string ComplexTemplate() => _complexTemplate.Render(_complexModel);

    [Benchmark(Description = "Parse + Render (no cache)")]
    public string ParseAndRender() => Jinja.Render("Hello, {{ name }}!", _simpleModel);
}
