using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using NetJinja.Ast;
using NetJinja.Exceptions;
using NetJinja.Runtime;

namespace NetJinja.Compilation;

/// <summary>
/// High-performance template renderer that executes the AST.
/// </summary>
public sealed class Renderer
{
    private readonly RenderContext _context;
    private readonly StringBuilder _output;
    private bool _autoEscape;
    private readonly Dictionary<string, BlockStatement> _blocks = new();
    private readonly Dictionary<string, MacroStatement> _macros = new();
    private readonly Stack<Dictionary<string, BlockStatement>> _blockStack = new();

    public Renderer(RenderContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _output = new StringBuilder(4096);
        _autoEscape = context.Environment.AutoEscape;
    }

    /// <summary>
    /// Renders a template AST to string.
    /// </summary>
    public string Render(TemplateNode template)
    {
        RenderStatements(template.Body);
        return _output.ToString();
    }

    /// <summary>
    /// Renders with block overrides for template inheritance.
    /// </summary>
    public string Render(TemplateNode template, Dictionary<string, BlockStatement>? blockOverrides)
    {
        if (blockOverrides != null)
        {
            _blockStack.Push(blockOverrides);
        }

        // First pass: collect blocks and check for extends
        CollectBlocks(template.Body);

        RenderStatements(template.Body);
        return _output.ToString();
    }

    private void CollectBlocks(IReadOnlyList<Statement> statements)
    {
        foreach (var stmt in statements)
        {
            if (stmt is BlockStatement block)
            {
                _blocks[block.Name] = block;
            }
        }
    }

    private void RenderStatements(IReadOnlyList<Statement> statements)
    {
        foreach (var stmt in statements)
        {
            RenderStatement(stmt);
        }
    }

    private void RenderStatement(Statement stmt)
    {
        switch (stmt)
        {
            case TextStatement text:
                _output.Append(text.Text);
                break;

            case OutputStatement output:
                RenderOutput(output);
                break;

            case IfStatement ifStmt:
                RenderIf(ifStmt);
                break;

            case ForStatement forStmt:
                RenderFor(forStmt);
                break;

            case BlockStatement block:
                RenderBlock(block);
                break;

            case SetStatement set:
                RenderSet(set);
                break;

            case MacroStatement macro:
                _macros[macro.Name] = macro;
                break;

            case WithStatement with:
                RenderWith(with);
                break;

            case AutoescapeStatement autoescape:
                RenderAutoescape(autoescape);
                break;

            case IncludeStatement include:
                RenderInclude(include);
                break;

            case ExtendsStatement:
                // Handled at template level
                break;

            case ContinueStatement:
                throw new LoopControlException(LoopControl.Continue);

            case BreakStatement:
                throw new LoopControlException(LoopControl.Break);

            default:
                throw new RenderException($"Unknown statement type: {stmt.GetType().Name}", stmt.Line, stmt.Column);
        }
    }

    private void RenderOutput(OutputStatement output)
    {
        var value = Evaluate(output.Expression);
        var str = ConvertToString(value);

        if (_autoEscape && value is not HtmlString)
        {
            str = HttpUtility.HtmlEncode(str);
        }

        _output.Append(str);
    }

    private void RenderIf(IfStatement stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            RenderStatements(stmt.ThenBody);
            return;
        }

        foreach (var (condition, body) in stmt.ElifBranches)
        {
            if (IsTruthy(Evaluate(condition)))
            {
                RenderStatements(body);
                return;
            }
        }

        if (stmt.ElseBody != null)
        {
            RenderStatements(stmt.ElseBody);
        }
    }

    private void RenderFor(ForStatement stmt)
    {
        var iterable = Evaluate(stmt.Iterable);
        var items = GetEnumerable(iterable);

        // Materialize for length calculation
        var itemList = items.Cast<object?>().ToList();

        // Apply filter if present
        if (stmt.Filter != null)
        {
            var filtered = new List<object?>();
            foreach (var item in itemList)
            {
                SetLoopVariables(stmt.TargetNames, item);
                if (IsTruthy(Evaluate(stmt.Filter)))
                {
                    filtered.Add(item);
                }
            }
            itemList = filtered;
        }

        if (itemList.Count == 0)
        {
            if (stmt.ElseBody != null)
            {
                RenderStatements(stmt.ElseBody);
            }
            return;
        }

        var loopContext = new LoopContext(itemList.Count);
        if (_context.CurrentLoop != null)
        {
            loopContext.Parent = _context.CurrentLoop;
            loopContext.Depth = _context.CurrentLoop.Depth + 1;
        }

        _context.PushLoop(loopContext);

        try
        {
            using (_context.PushScope())
            {
                foreach (var item in itemList)
                {
                    SetLoopVariables(stmt.TargetNames, item);

                    try
                    {
                        RenderStatements(stmt.Body);
                    }
                    catch (LoopControlException ex) when (ex.Control == LoopControl.Continue)
                    {
                        // Continue to next iteration
                    }
                    catch (LoopControlException ex) when (ex.Control == LoopControl.Break)
                    {
                        break;
                    }

                    loopContext.Advance();
                }
            }
        }
        finally
        {
            _context.PopLoop();
        }
    }

    private void SetLoopVariables(IReadOnlyList<string> names, object? value)
    {
        if (names.Count == 1)
        {
            _context.Set(names[0], value);
        }
        else
        {
            // Tuple unpacking
            var items = GetEnumerable(value).Cast<object?>().ToList();
            for (int i = 0; i < names.Count && i < items.Count; i++)
            {
                _context.Set(names[i], items[i]);
            }
        }
    }

    private void RenderBlock(BlockStatement block)
    {
        // Check for override in parent templates
        BlockStatement? effectiveBlock = block;

        foreach (var overrides in _blockStack)
        {
            if (overrides.TryGetValue(block.Name, out var overrideBlock))
            {
                effectiveBlock = overrideBlock;
                break;
            }
        }

        if (block.Scoped)
        {
            using (_context.PushScope())
            {
                RenderStatements(effectiveBlock.Body);
            }
        }
        else
        {
            RenderStatements(effectiveBlock.Body);
        }
    }

    private void RenderSet(SetStatement stmt)
    {
        if (stmt.Value != null)
        {
            var value = Evaluate(stmt.Value);
            if (stmt.Names.Count == 1)
            {
                _context.Set(stmt.Names[0], value);
            }
            else
            {
                // Tuple unpacking
                var items = GetEnumerable(value).Cast<object?>().ToList();
                for (int i = 0; i < stmt.Names.Count && i < items.Count; i++)
                {
                    _context.Set(stmt.Names[i], items[i]);
                }
            }
        }
        else if (stmt.Body != null)
        {
            // Block form: capture output
            var savedOutput = _output.ToString();
            _output.Clear();
            RenderStatements(stmt.Body);
            var captured = _output.ToString();
            _output.Clear();
            _output.Append(savedOutput);

            _context.Set(stmt.Names[0], captured);
        }
    }

    private void RenderWith(WithStatement stmt)
    {
        using (_context.PushScope())
        {
            foreach (var (name, expr) in stmt.Assignments)
            {
                _context.Set(name, Evaluate(expr));
            }
            RenderStatements(stmt.Body);
        }
    }

    private void RenderAutoescape(AutoescapeStatement stmt)
    {
        var savedAutoEscape = _autoEscape;
        _autoEscape = stmt.Enabled;
        try
        {
            RenderStatements(stmt.Body);
        }
        finally
        {
            _autoEscape = savedAutoEscape;
        }
    }

    private void RenderInclude(IncludeStatement stmt)
    {
        var templateName = ConvertToString(Evaluate(stmt.Template));
        try
        {
            var template = _context.Environment.GetTemplate(templateName);
            var includeRenderer = stmt.WithContext
                ? new Renderer(_context)
                : new Renderer(new RenderContext(_context.Environment));

            var result = includeRenderer.Render(template.CompiledTemplate.Ast);
            _output.Append(result);
        }
        catch (TemplateNotFoundException) when (stmt.IgnoreMissing)
        {
            // Ignore missing template
        }
    }

    #region Expression Evaluation

    private object? Evaluate(Expression expr)
    {
        return expr switch
        {
            LiteralExpression lit => lit.Value,
            NameExpression name => EvaluateName(name),
            GetAttrExpression getAttr => EvaluateGetAttr(getAttr),
            GetItemExpression getItem => EvaluateGetItem(getItem),
            CallExpression call => EvaluateCall(call),
            FilterExpression filter => EvaluateFilter(filter),
            TestExpression test => EvaluateTest(test),
            BinaryExpression binary => EvaluateBinary(binary),
            UnaryExpression unary => EvaluateUnary(unary),
            ConditionalExpression cond => EvaluateConditional(cond),
            ListExpression list => EvaluateList(list),
            DictExpression dict => EvaluateDict(dict),
            TupleExpression tuple => EvaluateTuple(tuple),
            ConcatExpression concat => EvaluateConcat(concat),
            CompareExpression compare => EvaluateCompare(compare),
            _ => throw new RenderException($"Unknown expression type: {expr.GetType().Name}", expr.Line, expr.Column)
        };
    }

    private object? EvaluateName(NameExpression expr)
    {
        return _context.Get(expr.Name);
    }

    private object? EvaluateGetAttr(GetAttrExpression expr)
    {
        var obj = Evaluate(expr.Object);
        return GetAttribute(obj, expr.Attribute);
    }

    private object? EvaluateGetItem(GetItemExpression expr)
    {
        var obj = Evaluate(expr.Object);
        var key = Evaluate(expr.Key);
        return GetItem(obj, key);
    }

    private object? EvaluateCall(CallExpression expr)
    {
        var callee = Evaluate(expr.Callee);

        // Check if it's a macro
        if (expr.Callee is NameExpression name && _macros.TryGetValue(name.Name, out var macro))
        {
            return CallMacro(macro, expr.Arguments, expr.KeywordArguments);
        }

        // Regular callable
        if (callee is Delegate del)
        {
            var args = expr.Arguments.Select(Evaluate).ToArray();
            return del.DynamicInvoke(args);
        }

        if (callee is Func<object?[], object?> func)
        {
            var args = expr.Arguments.Select(Evaluate).ToArray();
            return func(args);
        }

        throw new RenderException($"'{callee}' is not callable", expr.Line, expr.Column);
    }

    private object? CallMacro(MacroStatement macro, IReadOnlyList<Expression> args, IReadOnlyDictionary<string, Expression> kwargs)
    {
        using (_context.PushScope())
        {
            // Bind parameters
            for (int i = 0; i < macro.Parameters.Count; i++)
            {
                var (paramName, defaultValue) = macro.Parameters[i];
                object? value;

                if (kwargs.TryGetValue(paramName, out var kwExpr))
                {
                    value = Evaluate(kwExpr);
                }
                else if (i < args.Count)
                {
                    value = Evaluate(args[i]);
                }
                else if (defaultValue != null)
                {
                    value = Evaluate(defaultValue);
                }
                else
                {
                    value = null;
                }

                _context.Set(paramName, value);
            }

            // Render macro body
            var savedOutput = _output.ToString();
            _output.Clear();
            RenderStatements(macro.Body);
            var result = _output.ToString();
            _output.Clear();
            _output.Append(savedOutput);

            return new HtmlString(result);
        }
    }

    private object? EvaluateFilter(FilterExpression expr)
    {
        var value = Evaluate(expr.Value);
        var args = expr.Arguments.Select(Evaluate).ToArray();
        var kwargs = expr.KeywordArguments.ToDictionary(kvp => kvp.Key, kvp => Evaluate(kvp.Value));

        if (!_context.Environment.Filters.TryGetValue(expr.FilterName, out var filter))
        {
            throw new UndefinedFilterException(expr.FilterName, expr.Line, expr.Column);
        }

        return filter(value, args, kwargs, _context);
    }

    private bool EvaluateTest(TestExpression expr)
    {
        var value = Evaluate(expr.Value);
        var args = expr.Arguments.Select(Evaluate).ToArray();

        if (!_context.Environment.Tests.TryGetValue(expr.TestName, out var test))
        {
            throw new RenderException($"Undefined test: '{expr.TestName}'", expr.Line, expr.Column);
        }

        var result = test(value, args, _context);
        return expr.Negated ? !result : result;
    }

    private object? EvaluateBinary(BinaryExpression expr)
    {
        // Short-circuit evaluation for logical operators
        if (expr.Operator == BinaryOperator.And)
        {
            var left = Evaluate(expr.Left);
            if (!IsTruthy(left)) return left;
            return Evaluate(expr.Right);
        }

        if (expr.Operator == BinaryOperator.Or)
        {
            var left = Evaluate(expr.Left);
            if (IsTruthy(left)) return left;
            return Evaluate(expr.Right);
        }

        var leftVal = Evaluate(expr.Left);
        var rightVal = Evaluate(expr.Right);

        return expr.Operator switch
        {
            BinaryOperator.Add => Add(leftVal, rightVal),
            BinaryOperator.Subtract => Subtract(leftVal, rightVal),
            BinaryOperator.Multiply => Multiply(leftVal, rightVal),
            BinaryOperator.Divide => Divide(leftVal, rightVal),
            BinaryOperator.FloorDivide => FloorDivide(leftVal, rightVal),
            BinaryOperator.Modulo => Modulo(leftVal, rightVal),
            BinaryOperator.Power => Power(leftVal, rightVal),
            BinaryOperator.In => Contains(rightVal, leftVal),
            BinaryOperator.NotIn => !Contains(rightVal, leftVal),
            _ => throw new RenderException($"Unknown operator: {expr.Operator}", expr.Line, expr.Column)
        };
    }

    private object? EvaluateUnary(UnaryExpression expr)
    {
        var operand = Evaluate(expr.Operand);

        return expr.Operator switch
        {
            UnaryOperator.Not => !IsTruthy(operand),
            UnaryOperator.Negative => Negate(operand),
            UnaryOperator.Positive => operand,
            _ => throw new RenderException($"Unknown operator: {expr.Operator}", expr.Line, expr.Column)
        };
    }

    private object? EvaluateConditional(ConditionalExpression expr)
    {
        var condition = Evaluate(expr.Condition);
        return IsTruthy(condition) ? Evaluate(expr.TrueExpr) : Evaluate(expr.FalseExpr);
    }

    private List<object?> EvaluateList(ListExpression expr)
    {
        return expr.Items.Select(Evaluate).ToList();
    }

    private Dictionary<object, object?> EvaluateDict(DictExpression expr)
    {
        var result = new Dictionary<object, object?>();
        foreach (var (keyExpr, valueExpr) in expr.Items)
        {
            var key = Evaluate(keyExpr) ?? throw new RenderException("Dictionary key cannot be null", expr.Line, expr.Column);
            result[key] = Evaluate(valueExpr);
        }
        return result;
    }

    private object?[] EvaluateTuple(TupleExpression expr)
    {
        return expr.Items.Select(Evaluate).ToArray();
    }

    private string EvaluateConcat(ConcatExpression expr)
    {
        var left = ConvertToString(Evaluate(expr.Left));
        var right = ConvertToString(Evaluate(expr.Right));
        return left + right;
    }

    private bool EvaluateCompare(CompareExpression expr)
    {
        var left = Evaluate(expr.Left);

        foreach (var (op, rightExpr) in expr.Comparisons)
        {
            var right = Evaluate(rightExpr);

            bool result = op switch
            {
                CompareOperator.Equal => AreEqual(left, right),
                CompareOperator.NotEqual => !AreEqual(left, right),
                CompareOperator.LessThan => Compare(left, right) < 0,
                CompareOperator.LessThanOrEqual => Compare(left, right) <= 0,
                CompareOperator.GreaterThan => Compare(left, right) > 0,
                CompareOperator.GreaterThanOrEqual => Compare(left, right) >= 0,
                _ => throw new RenderException($"Unknown comparison operator: {op}")
            };

            if (!result) return false;
            left = right;
        }

        return true;
    }

    #endregion

    #region Helper Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            float f => f != 0,
            string s => s.Length > 0,
            ICollection c => c.Count > 0,
            IEnumerable e => e.Cast<object>().Any(),
            _ => true
        };
    }

    private static string ConvertToString(object? value)
    {
        return value switch
        {
            null => "",
            bool b => b ? "True" : "False",
            HtmlString hs => hs.Value,
            _ => value.ToString() ?? ""
        };
    }

    private static IEnumerable GetEnumerable(object? value)
    {
        return value switch
        {
            null => Array.Empty<object>(),
            string s => s.ToCharArray().Cast<object>(),
            IEnumerable e => e,
            _ => throw new RenderException($"Cannot iterate over {value.GetType().Name}")
        };
    }

    private static object? GetAttribute(object? obj, string name)
    {
        if (obj == null) return null;

        var type = obj.GetType();

        // Try property
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null) return prop.GetValue(obj);

        // Try field
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null) return field.GetValue(obj);

        // Try method
        var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, Type.EmptyTypes);
        if (method != null) return method.Invoke(obj, null);

        // Try indexer (dictionary-like)
        if (obj is IDictionary dict && dict.Contains(name))
        {
            return dict[name];
        }

        // Try dynamic object
        if (obj is IDynamicMetaObjectProvider dynamic)
        {
            return ((dynamic)obj)[name];
        }

        return null;
    }

    private static object? GetItem(object? obj, object? key)
    {
        if (obj == null) return null;

        // Convert numeric key to int if needed
        int? intKey = key switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            _ => null
        };

        // String indexing
        if (obj is string s && intKey.HasValue)
        {
            var index = intKey.Value;
            if (index < 0) index = s.Length + index;
            return index >= 0 && index < s.Length ? s[index].ToString() : null;
        }

        // List/array indexing
        if (obj is IList list && intKey.HasValue)
        {
            var index = intKey.Value;
            if (index < 0) index = list.Count + index;
            return index >= 0 && index < list.Count ? list[index] : null;
        }

        // Dictionary access
        if (obj is IDictionary dict)
        {
            return dict.Contains(key!) ? dict[key!] : null;
        }

        // Generic dictionary
        var type = obj.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var tryGetValue = type.GetMethod("TryGetValue");
            var args = new object?[] { key, null };
            if ((bool)tryGetValue!.Invoke(obj, args)!)
            {
                return args[1];
            }
        }

        return null;
    }

    private static object? Add(object? left, object? right)
    {
        return (left, right) switch
        {
            (int a, int b) => a + b,
            (long a, long b) => a + b,
            (double a, double b) => a + b,
            (string a, string b) => a + b,
            (IList a, IList b) => a.Cast<object?>().Concat(b.Cast<object?>()).ToList(),
            _ when left != null && right != null =>
                Convert.ToDouble(left) + Convert.ToDouble(right),
            _ => throw new RenderException($"Cannot add {left?.GetType().Name} and {right?.GetType().Name}")
        };
    }

    private static object? Subtract(object? left, object? right)
    {
        return (left, right) switch
        {
            (int a, int b) => a - b,
            (long a, long b) => a - b,
            (double a, double b) => a - b,
            _ when left != null && right != null =>
                Convert.ToDouble(left) - Convert.ToDouble(right),
            _ => throw new RenderException($"Cannot subtract {right?.GetType().Name} from {left?.GetType().Name}")
        };
    }

    private static object? Multiply(object? left, object? right)
    {
        // String repetition
        if (left is string s && right is int or long)
        {
            var count = Convert.ToInt32(right);
            return string.Concat(Enumerable.Repeat(s, count));
        }
        if (right is string s2 && left is int or long)
        {
            var count = Convert.ToInt32(left);
            return string.Concat(Enumerable.Repeat(s2, count));
        }

        return (left, right) switch
        {
            (int a, int b) => a * b,
            (long a, long b) => a * b,
            (double a, double b) => a * b,
            _ when left != null && right != null =>
                Convert.ToDouble(left) * Convert.ToDouble(right),
            _ => throw new RenderException($"Cannot multiply {left?.GetType().Name} and {right?.GetType().Name}")
        };
    }

    private static object? Divide(object? left, object? right)
    {
        var r = Convert.ToDouble(right);
        if (r == 0) throw new RenderException("Division by zero");
        return Convert.ToDouble(left) / r;
    }

    private static object? FloorDivide(object? left, object? right)
    {
        var r = Convert.ToDouble(right);
        if (r == 0) throw new RenderException("Division by zero");
        return Math.Floor(Convert.ToDouble(left) / r);
    }

    private static object? Modulo(object? left, object? right)
    {
        return (left, right) switch
        {
            (int a, int b) => a % b,
            (long a, long b) => a % b,
            _ => Convert.ToDouble(left) % Convert.ToDouble(right)
        };
    }

    private static object? Power(object? left, object? right)
    {
        return Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right));
    }

    private static object? Negate(object? value)
    {
        return value switch
        {
            int i => -i,
            long l => -l,
            double d => -d,
            float f => -f,
            _ => -Convert.ToDouble(value)
        };
    }

    private static bool Contains(object? container, object? item)
    {
        if (container is string s && item != null)
        {
            return s.Contains(item.ToString()!);
        }

        if (container is IDictionary dict)
        {
            return dict.Contains(item!);
        }

        if (container is IEnumerable enumerable)
        {
            foreach (var element in enumerable)
            {
                if (AreEqual(element, item)) return true;
            }
        }

        return false;
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return left == null && right == null;

        // Handle numeric comparisons
        if (IsNumeric(left) && IsNumeric(right))
        {
            return Convert.ToDouble(left) == Convert.ToDouble(right);
        }

        return left.Equals(right);
    }

    private static int Compare(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        if (IsNumeric(left) && IsNumeric(right))
        {
            return Convert.ToDouble(left).CompareTo(Convert.ToDouble(right));
        }

        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        throw new RenderException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}");
    }

    private static bool IsNumeric(object? value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    #endregion
}

/// <summary>
/// Represents pre-escaped HTML content.
/// </summary>
public sealed class HtmlString
{
    public string Value { get; }

    public HtmlString(string value) => Value = value;

    public override string ToString() => Value;
}

internal enum LoopControl { Continue, Break }

internal sealed class LoopControlException : Exception
{
    public LoopControl Control { get; }
    public LoopControlException(LoopControl control) => Control = control;
}
