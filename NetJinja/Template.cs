using NetJinja.Ast;
using NetJinja.Compilation;
using NetJinja.Lexing;
using NetJinja.Parsing;
using NetJinja.Runtime;

namespace NetJinja;

/// <summary>
/// Represents a compiled Jinja template ready for rendering.
/// </summary>
public sealed class Template
{
    private readonly JinjaEnvironment _environment;
    private readonly string? _name;

    /// <summary>
    /// The compiled template representation.
    /// </summary>
    public CompiledTemplate CompiledTemplate { get; }

    /// <summary>
    /// The template name (if any).
    /// </summary>
    public string? Name => _name;

    /// <summary>
    /// Creates a template from source code.
    /// </summary>
    /// <param name="source">The template source.</param>
    /// <param name="environment">The Jinja environment.</param>
    /// <param name="name">Optional template name for error messages.</param>
    public Template(string source, JinjaEnvironment environment, string? name = null)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _name = name;

        // Check cache first
        if (name != null && environment.TryGetCached(name, out var cached))
        {
            CompiledTemplate = cached!;
            return;
        }

        // Compile the template
        CompiledTemplate = Compile(source, name);

        // Cache if named
        if (name != null)
        {
            environment.CacheTemplate(name, CompiledTemplate);
        }
    }

    /// <summary>
    /// Creates a template from a pre-compiled template.
    /// </summary>
    internal Template(CompiledTemplate compiled, JinjaEnvironment environment, string? name = null)
    {
        _environment = environment;
        _name = name;
        CompiledTemplate = compiled;
    }

    /// <summary>
    /// Compiles template source into a CompiledTemplate.
    /// </summary>
    private CompiledTemplate Compile(string source, string? name)
    {
        var lexer = new Lexer(source, _environment.LexerOptions);
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens, name);
        var ast = parser.Parse();

        var compiled = new CompiledTemplate(ast);

        // Collect blocks and check for extends
        CollectTemplateInfo(ast.Body, compiled);

        return compiled;
    }

    /// <summary>
    /// Collects block definitions and extends statements from the AST.
    /// </summary>
    private void CollectTemplateInfo(IReadOnlyList<Statement> statements, CompiledTemplate compiled)
    {
        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case ExtendsStatement extends:
                    if (extends.Template is LiteralExpression lit && lit.Value is string templateName)
                    {
                        compiled.ExtendsTemplate = templateName;
                    }
                    break;

                case BlockStatement block:
                    compiled.Blocks[block.Name] = block;
                    break;
            }
        }
    }

    /// <summary>
    /// Renders the template with the given variables.
    /// </summary>
    /// <param name="variables">Variables to pass to the template.</param>
    /// <returns>The rendered template string.</returns>
    public string Render(IDictionary<string, object?>? variables = null)
    {
        var context = new RenderContext(_environment, variables);
        return RenderWithContext(context);
    }

    /// <summary>
    /// Renders the template with an anonymous object as variables.
    /// </summary>
    /// <param name="model">Anonymous object with properties as variables.</param>
    /// <returns>The rendered template string.</returns>
    public string Render(object model)
    {
        var variables = ObjectToDictionary(model);
        return Render(variables);
    }

    /// <summary>
    /// Renders the template with the given context.
    /// </summary>
    internal string RenderWithContext(RenderContext context)
    {
        // Handle template inheritance
        if (CompiledTemplate.ExtendsTemplate != null)
        {
            return RenderWithInheritance(context);
        }

        var renderer = new Renderer(context);
        return renderer.Render(CompiledTemplate.Ast);
    }

    /// <summary>
    /// Renders a template that extends another template.
    /// </summary>
    private string RenderWithInheritance(RenderContext context)
    {
        // Get the parent template
        var parentTemplate = _environment.GetTemplate(CompiledTemplate.ExtendsTemplate!);

        // Build block hierarchy
        var blockOverrides = new Dictionary<string, BlockStatement>(CompiledTemplate.Blocks);

        // Recursively resolve inheritance chain
        var currentParent = parentTemplate.CompiledTemplate;
        while (currentParent.ExtendsTemplate != null)
        {
            // Child blocks override parent blocks
            foreach (var block in currentParent.Blocks)
            {
                if (!blockOverrides.ContainsKey(block.Key))
                {
                    blockOverrides[block.Key] = block.Value;
                }
            }

            var grandparent = _environment.GetTemplate(currentParent.ExtendsTemplate);
            currentParent = grandparent.CompiledTemplate;
        }

        // Add root template blocks
        foreach (var block in currentParent.Blocks)
        {
            if (!blockOverrides.ContainsKey(block.Key))
            {
                blockOverrides[block.Key] = block.Value;
            }
        }

        // Render the root parent with block overrides
        var renderer = new Renderer(context);
        return renderer.Render(currentParent.Ast, blockOverrides);
    }

    /// <summary>
    /// Converts an anonymous object to a dictionary.
    /// </summary>
    private static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        var type = obj.GetType();

        foreach (var prop in type.GetProperties())
        {
            dict[prop.Name] = prop.GetValue(obj);
        }

        return dict;
    }

    /// <summary>
    /// Renders the template asynchronously.
    /// </summary>
    public Task<string> RenderAsync(IDictionary<string, object?>? variables = null)
    {
        return Task.FromResult(Render(variables));
    }

    /// <summary>
    /// Renders the template to a TextWriter.
    /// </summary>
    public void Render(TextWriter writer, IDictionary<string, object?>? variables = null)
    {
        var result = Render(variables);
        writer.Write(result);
    }

    /// <summary>
    /// Renders the template to a TextWriter asynchronously.
    /// </summary>
    public async Task RenderAsync(TextWriter writer, IDictionary<string, object?>? variables = null)
    {
        var result = Render(variables);
        await writer.WriteAsync(result);
    }
}

/// <summary>
/// Static helper class for quick template rendering.
/// </summary>
public static class Jinja
{
    private static readonly JinjaEnvironment DefaultEnvironment = new();

    /// <summary>
    /// Renders a template string with the given variables.
    /// </summary>
    public static string Render(string template, IDictionary<string, object?>? variables = null)
    {
        return DefaultEnvironment.FromString(template).Render(variables);
    }

    /// <summary>
    /// Renders a template string with an anonymous object as variables.
    /// </summary>
    public static string Render(string template, object model)
    {
        return DefaultEnvironment.FromString(template).Render(model);
    }

    /// <summary>
    /// Creates a new Jinja environment.
    /// </summary>
    public static JinjaEnvironment CreateEnvironment() => new();

    /// <summary>
    /// Creates a template from source.
    /// </summary>
    public static Template FromString(string source, JinjaEnvironment? environment = null)
    {
        return (environment ?? DefaultEnvironment).FromString(source);
    }
}
