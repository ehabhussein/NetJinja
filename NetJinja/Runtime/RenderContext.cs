using System.Runtime.CompilerServices;
using NetJinja.Ast;

namespace NetJinja.Runtime;

/// <summary>
/// Represents a context scope that can be pushed/popped during rendering.
/// </summary>
internal sealed class Scope
{
    private readonly Dictionary<string, object?> _variables = new(StringComparer.Ordinal);
    public Scope? Parent { get; }

    public Scope(Scope? parent = null) => Parent = parent;

    public void Set(string name, object? value) => _variables[name] = value;

    public bool TryGet(string name, out object? value)
    {
        if (_variables.TryGetValue(name, out value)) return true;
        return Parent?.TryGet(name, out value) ?? false;
    }

    public bool ContainsLocal(string name) => _variables.ContainsKey(name);
}

/// <summary>
/// Context for template rendering, holding variables and runtime state.
/// </summary>
public sealed class RenderContext
{
    private Scope _scope;
    private readonly JinjaEnvironment _environment;
    private readonly Stack<LoopContext> _loopStack = new();
    private readonly Stack<BlockStatement> _parentBlockStack = new();

    internal JinjaEnvironment Environment => _environment;
    internal LoopContext? CurrentLoop => _loopStack.Count > 0 ? _loopStack.Peek() : null;
    internal BlockStatement? CurrentParentBlock => _parentBlockStack.Count > 0 ? _parentBlockStack.Peek() : null;

    public RenderContext(JinjaEnvironment environment, IDictionary<string, object?>? variables = null)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _scope = new Scope();

        if (variables != null)
        {
            foreach (var kvp in variables)
            {
                _scope.Set(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Gets a variable value by name.
    /// </summary>
    public object? Get(string name)
    {
        // Check for special loop variable
        if (name == "loop" && _loopStack.Count > 0)
        {
            return _loopStack.Peek();
        }

        if (_scope.TryGet(name, out var value))
        {
            return value;
        }

        // Check globals in environment
        if (_environment.Globals.TryGetValue(name, out value))
        {
            return value;
        }

        if (_environment.StrictUndefined)
        {
            throw new NetJinja.Exceptions.UndefinedVariableException(name);
        }

        return _environment.UndefinedValue;
    }

    /// <summary>
    /// Sets a variable in the current scope.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(string name, object? value) => _scope.Set(name, value);

    /// <summary>
    /// Pushes a new scope for block-scoped variables.
    /// </summary>
    public IDisposable PushScope() => new ScopeGuard(this);

    /// <summary>
    /// Pushes a loop context onto the stack.
    /// </summary>
    internal void PushLoop(LoopContext loop) => _loopStack.Push(loop);

    /// <summary>
    /// Pops the current loop context.
    /// </summary>
    internal void PopLoop() => _loopStack.Pop();

    /// <summary>
    /// Pushes a parent block onto the stack for super() support.
    /// </summary>
    internal void PushParentBlock(BlockStatement block) => _parentBlockStack.Push(block);

    /// <summary>
    /// Pops the current parent block.
    /// </summary>
    internal void PopParentBlock() => _parentBlockStack.Pop();

    private sealed class ScopeGuard : IDisposable
    {
        private readonly RenderContext _context;
        private readonly Scope _previousScope;

        public ScopeGuard(RenderContext context)
        {
            _context = context;
            _previousScope = context._scope;
            context._scope = new Scope(context._scope);
        }

        public void Dispose() => _context._scope = _previousScope;
    }
}

/// <summary>
/// Provides loop iteration metadata (Jinja's loop variable).
/// </summary>
public sealed class LoopContext
{
    private readonly int _length;
    private int _index0;

    public LoopContext(int length) => _length = length;

    public int Index0 => _index0;
    public int Index => _index0 + 1;
    public int RevIndex0 => _length - _index0 - 1;
    public int RevIndex => _length - _index0;
    public bool First => _index0 == 0;
    public bool Last => _index0 == _length - 1;
    public int Length => _length;
    public int Depth { get; internal set; } = 1;
    public int Depth0 => Depth - 1;
    public LoopContext? Parent { get; internal set; }

    /// <summary>
    /// Cycles through values based on current index.
    /// </summary>
    public object? Cycle(params object?[] values)
    {
        if (values.Length == 0) return null;
        return values[_index0 % values.Length];
    }

    /// <summary>
    /// Returns true every n iterations.
    /// </summary>
    public bool Changed(object? value) => true; // Simplified

    internal void Advance() => _index0++;
}
