using System.Collections.Concurrent;
using NetJinja.Filters;
using NetJinja.Lexing;

namespace NetJinja.Runtime;

/// <summary>
/// Jinja environment configuration and shared state.
/// Contains filters, globals, and template loading configuration.
/// </summary>
public sealed class JinjaEnvironment
{
    private readonly ConcurrentDictionary<string, CompiledTemplate> _templateCache = new();
    private readonly Dictionary<string, FilterDelegate> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TestDelegate> _tests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _globals = new(StringComparer.Ordinal);

    /// <summary>
    /// Template loader for resolving template names to sources.
    /// </summary>
    public ITemplateLoader? Loader { get; set; }

    /// <summary>
    /// Global variables available to all templates.
    /// </summary>
    public IDictionary<string, object?> Globals => _globals;

    /// <summary>
    /// Registered filters.
    /// </summary>
    public IDictionary<string, FilterDelegate> Filters => _filters;

    /// <summary>
    /// Registered tests (for "is" expressions).
    /// </summary>
    public IDictionary<string, TestDelegate> Tests => _tests;

    /// <summary>
    /// Whether to throw on undefined variables.
    /// </summary>
    public bool StrictUndefined { get; set; } = false;

    /// <summary>
    /// Value returned for undefined variables when StrictUndefined is false.
    /// </summary>
    public object? UndefinedValue { get; set; } = null;

    /// <summary>
    /// Whether to auto-escape HTML by default.
    /// </summary>
    public bool AutoEscape { get; set; } = false;

    /// <summary>
    /// Lexer options for custom delimiters.
    /// </summary>
    public LexerOptions LexerOptions { get; set; } = LexerOptions.Default;

    /// <summary>
    /// Whether to trim blocks (remove first newline after block tags).
    /// </summary>
    public bool TrimBlocks { get; set; } = false;

    /// <summary>
    /// Whether to strip leading spaces/tabs from start of line to block.
    /// </summary>
    public bool LstripBlocks { get; set; } = false;

    /// <summary>
    /// Whether to preserve trailing newline when loading templates.
    /// </summary>
    public bool KeepTrailingNewline { get; set; } = true;

    /// <summary>
    /// Creates a new Jinja environment with default settings.
    /// </summary>
    public JinjaEnvironment()
    {
        // Register built-in filters
        BuiltinFilters.Register(this);
        BuiltinTests.Register(this);
    }

    /// <summary>
    /// Creates a template from source string.
    /// </summary>
    public Template FromString(string source, string? name = null)
    {
        return new Template(source, this, name);
    }

    /// <summary>
    /// Gets a template by name using the configured loader.
    /// </summary>
    public Template GetTemplate(string name)
    {
        if (Loader == null)
        {
            throw new InvalidOperationException("No template loader configured");
        }

        // Check cache first
        if (_templateCache.TryGetValue(name, out var cached))
        {
            return new Template(cached, this, name);
        }

        var source = Loader.GetSource(name);
        if (source == null)
        {
            throw new NetJinja.Exceptions.TemplateNotFoundException(name);
        }

        var template = new Template(source, this, name);
        return template;
    }

    /// <summary>
    /// Clears the template cache.
    /// </summary>
    public void ClearCache() => _templateCache.Clear();

    /// <summary>
    /// Adds a compiled template to the cache.
    /// </summary>
    internal void CacheTemplate(string name, CompiledTemplate template)
    {
        _templateCache[name] = template;
    }

    /// <summary>
    /// Tries to get a cached template.
    /// </summary>
    internal bool TryGetCached(string name, out CompiledTemplate? template)
    {
        return _templateCache.TryGetValue(name, out template);
    }

    /// <summary>
    /// Registers a custom filter.
    /// </summary>
    public JinjaEnvironment AddFilter(string name, FilterDelegate filter)
    {
        _filters[name] = filter;
        return this;
    }

    /// <summary>
    /// Registers a custom filter with a simpler signature.
    /// </summary>
    public JinjaEnvironment AddFilter(string name, Func<object?, object?> filter)
    {
        _filters[name] = (value, args, kwargs, ctx) => filter(value);
        return this;
    }

    /// <summary>
    /// Registers a custom test.
    /// </summary>
    public JinjaEnvironment AddTest(string name, TestDelegate test)
    {
        _tests[name] = test;
        return this;
    }

    /// <summary>
    /// Adds a global variable.
    /// </summary>
    public JinjaEnvironment AddGlobal(string name, object? value)
    {
        _globals[name] = value;
        return this;
    }
}

/// <summary>
/// Delegate for filter functions.
/// </summary>
public delegate object? FilterDelegate(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext context);

/// <summary>
/// Delegate for test functions.
/// </summary>
public delegate bool TestDelegate(object? value, object?[] args, RenderContext context);

/// <summary>
/// Interface for template loaders.
/// </summary>
public interface ITemplateLoader
{
    /// <summary>
    /// Gets the source for a template by name.
    /// </summary>
    string? GetSource(string name);

    /// <summary>
    /// Checks if a template exists.
    /// </summary>
    bool Exists(string name);
}

/// <summary>
/// Loads templates from the file system.
/// </summary>
public sealed class FileSystemLoader : ITemplateLoader
{
    private readonly string[] _searchPaths;

    public FileSystemLoader(params string[] searchPaths)
    {
        _searchPaths = searchPaths.Length > 0 ? searchPaths : ["."];
    }

    public string? GetSource(string name)
    {
        foreach (var basePath in _searchPaths)
        {
            var fullPath = Path.Combine(basePath, name);
            if (File.Exists(fullPath))
            {
                return File.ReadAllText(fullPath);
            }
        }
        return null;
    }

    public bool Exists(string name)
    {
        foreach (var basePath in _searchPaths)
        {
            if (File.Exists(Path.Combine(basePath, name)))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Loads templates from a dictionary.
/// </summary>
public sealed class DictLoader : ITemplateLoader
{
    private readonly Dictionary<string, string> _templates;

    public DictLoader(Dictionary<string, string> templates)
    {
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    public string? GetSource(string name)
    {
        return _templates.TryGetValue(name, out var source) ? source : null;
    }

    public bool Exists(string name) => _templates.ContainsKey(name);
}

/// <summary>
/// Compiled template representation.
/// </summary>
public sealed class CompiledTemplate
{
    public NetJinja.Ast.TemplateNode Ast { get; }
    public string? ExtendsTemplate { get; set; }
    public Dictionary<string, NetJinja.Ast.BlockStatement> Blocks { get; } = new();

    public CompiledTemplate(NetJinja.Ast.TemplateNode ast)
    {
        Ast = ast;
    }
}
