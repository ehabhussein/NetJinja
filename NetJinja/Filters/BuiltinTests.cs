using System.Collections;
using NetJinja.Runtime;

namespace NetJinja.Filters;

/// <summary>
/// Built-in Jinja tests for "is" expressions.
/// </summary>
public static class BuiltinTests
{
    public static void Register(JinjaEnvironment env)
    {
        // Type tests
        env.Tests["defined"] = (v, a, c) => v != null && v != c.Environment.UndefinedValue;
        env.Tests["undefined"] = (v, a, c) => v == null || v == c.Environment.UndefinedValue;
        env.Tests["none"] = (v, a, c) => v == null;
        env.Tests["boolean"] = (v, a, c) => v is bool;
        env.Tests["integer"] = (v, a, c) => v is int or long or short or byte or sbyte or uint or ulong or ushort;
        env.Tests["float"] = (v, a, c) => v is float or double or decimal;
        env.Tests["number"] = (v, a, c) => IsNumeric(v);
        env.Tests["string"] = (v, a, c) => v is string;
        env.Tests["mapping"] = (v, a, c) => v is IDictionary;
        env.Tests["iterable"] = (v, a, c) => v is IEnumerable;
        env.Tests["sequence"] = (v, a, c) => v is IList or Array;
        env.Tests["callable"] = (v, a, c) => v is Delegate;

        // Comparison tests
        env.Tests["eq"] = (v, a, c) => AreEqual(v, a.Length > 0 ? a[0] : null);
        env.Tests["equalto"] = env.Tests["eq"];
        env.Tests["=="] = env.Tests["eq"];
        env.Tests["ne"] = (v, a, c) => !AreEqual(v, a.Length > 0 ? a[0] : null);
        env.Tests["!="] = env.Tests["ne"];
        env.Tests["lt"] = (v, a, c) => Compare(v, a.Length > 0 ? a[0] : null) < 0;
        env.Tests["lessthan"] = env.Tests["lt"];
        env.Tests["<"] = env.Tests["lt"];
        env.Tests["le"] = (v, a, c) => Compare(v, a.Length > 0 ? a[0] : null) <= 0;
        env.Tests["<="] = env.Tests["le"];
        env.Tests["gt"] = (v, a, c) => Compare(v, a.Length > 0 ? a[0] : null) > 0;
        env.Tests["greaterthan"] = env.Tests["gt"];
        env.Tests[">"] = env.Tests["gt"];
        env.Tests["ge"] = (v, a, c) => Compare(v, a.Length > 0 ? a[0] : null) >= 0;
        env.Tests[">="] = env.Tests["ge"];

        // Value tests
        env.Tests["true"] = (v, a, c) => v is true;
        env.Tests["false"] = (v, a, c) => v is false;
        env.Tests["sameas"] = (v, a, c) => ReferenceEquals(v, a.Length > 0 ? a[0] : null);

        // Numeric tests
        env.Tests["odd"] = (v, a, c) => ToLong(v) % 2 != 0;
        env.Tests["even"] = (v, a, c) => ToLong(v) % 2 == 0;
        env.Tests["divisibleby"] = (v, a, c) =>
        {
            if (a.Length == 0) return false;
            var divisor = ToLong(a[0]);
            return divisor != 0 && ToLong(v) % divisor == 0;
        };

        // String tests
        env.Tests["lower"] = (v, a, c) => v is string s && s == s.ToLowerInvariant();
        env.Tests["upper"] = (v, a, c) => v is string s && s == s.ToUpperInvariant();

        // Collection tests
        env.Tests["empty"] = Empty;
        env.Tests["in"] = (v, a, c) =>
        {
            if (a.Length == 0) return false;
            return Contains(a[0], v);
        };
    }

    private static bool Empty(object? value, object?[] args, RenderContext ctx)
    {
        return value switch
        {
            null => true,
            string s => s.Length == 0,
            ICollection c => c.Count == 0,
            IEnumerable e => !e.Cast<object>().Any(),
            _ => false
        };
    }

    private static bool IsNumeric(object? value)
    {
        return value is byte or sbyte or short or ushort or int or uint
            or long or ulong or float or double or decimal;
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return left == null && right == null;

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

        return 0;
    }

    private static long ToLong(object? value)
    {
        return value switch
        {
            null => 0,
            int i => i,
            long l => l,
            double d => (long)d,
            float f => (long)f,
            string s => long.TryParse(s, out var r) ? r : 0,
            _ => Convert.ToInt64(value)
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
}
