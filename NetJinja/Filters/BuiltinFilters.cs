using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using NetJinja.Compilation;
using NetJinja.Runtime;

namespace NetJinja.Filters;

/// <summary>
/// Built-in Jinja filters.
/// </summary>
public static class BuiltinFilters
{
    public static void Register(JinjaEnvironment env)
    {
        // String filters
        env.Filters["upper"] = (v, a, k, c) => ToString(v).ToUpperInvariant();
        env.Filters["lower"] = (v, a, k, c) => ToString(v).ToLowerInvariant();
        env.Filters["capitalize"] = Capitalize;
        env.Filters["title"] = Title;
        env.Filters["trim"] = (v, a, k, c) => ToString(v).Trim();
        env.Filters["striptags"] = StripTags;
        env.Filters["safe"] = (v, a, k, c) => new HtmlString(ToString(v));
        env.Filters["escape"] = (v, a, k, c) => HttpUtility.HtmlEncode(ToString(v));
        env.Filters["e"] = env.Filters["escape"];
        env.Filters["urlencode"] = (v, a, k, c) => Uri.EscapeDataString(ToString(v));
        env.Filters["replace"] = Replace;
        env.Filters["truncate"] = Truncate;
        env.Filters["wordwrap"] = WordWrap;
        env.Filters["center"] = Center;
        env.Filters["ljust"] = Ljust;
        env.Filters["rjust"] = Rjust;
        env.Filters["indent"] = Indent;
        env.Filters["format"] = Format;

        // Collection filters
        env.Filters["length"] = Length;
        env.Filters["count"] = Length;
        env.Filters["first"] = First;
        env.Filters["last"] = Last;
        env.Filters["join"] = Join;
        env.Filters["sort"] = Sort;
        env.Filters["reverse"] = Reverse;
        env.Filters["list"] = ToList;
        env.Filters["unique"] = Unique;
        env.Filters["map"] = Map;
        env.Filters["select"] = Select;
        env.Filters["reject"] = Reject;
        env.Filters["selectattr"] = SelectAttr;
        env.Filters["rejectattr"] = RejectAttr;
        env.Filters["groupby"] = GroupBy;
        env.Filters["batch"] = Batch;
        env.Filters["slice"] = Slice;

        // Numeric filters
        env.Filters["abs"] = (v, a, k, c) => Math.Abs(ToDouble(v));
        env.Filters["round"] = Round;
        env.Filters["int"] = (v, a, k, c) => (long)ToDouble(v);
        env.Filters["float"] = (v, a, k, c) => ToDouble(v);
        env.Filters["sum"] = Sum;
        env.Filters["min"] = Min;
        env.Filters["max"] = Max;
        env.Filters["random"] = Random;

        // Type conversion
        env.Filters["string"] = (v, a, k, c) => ToString(v);
        env.Filters["bool"] = (v, a, k, c) => IsTruthy(v);

        // Dictionary/Object filters
        env.Filters["items"] = Items;
        env.Filters["keys"] = Keys;
        env.Filters["values"] = Values;
        env.Filters["attr"] = Attr;

        // Serialization
        env.Filters["tojson"] = ToJson;
        env.Filters["pprint"] = PrettyPrint;

        // Default value
        env.Filters["default"] = Default;
        env.Filters["d"] = Default;

        // XML/HTML
        env.Filters["xmlattr"] = XmlAttr;
        env.Filters["filesizeformat"] = FileSizeFormat;

        // Regex
        env.Filters["regex_replace"] = RegexReplace;
        env.Filters["regex_search"] = RegexSearch;
    }

    #region String Filters

    private static object? Capitalize(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        if (s.Length == 0) return s;
        return char.ToUpper(s[0]) + s[1..].ToLower();
    }

    private static object? Title(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ToString(value).ToLower());
    }

    private static object? StripTags(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        return Regex.Replace(s, "<[^>]+>", "");
    }

    private static object? Replace(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var old = args.Length > 0 ? ToString(args[0]) : "";
        var replacement = args.Length > 1 ? ToString(args[1]) : "";
        var count = args.Length > 2 ? (int)ToDouble(args[2]) : -1;

        if (count < 0)
        {
            return s.Replace(old, replacement);
        }

        var result = new StringBuilder(s);
        for (int i = 0; i < count; i++)
        {
            var idx = result.ToString().IndexOf(old, StringComparison.Ordinal);
            if (idx < 0) break;
            result.Remove(idx, old.Length);
            result.Insert(idx, replacement);
        }
        return result.ToString();
    }

    private static object? Truncate(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var length = args.Length > 0 ? (int)ToDouble(args[0]) : 255;
        var killwords = args.Length > 1 && IsTruthy(args[1]);
        var end = args.Length > 2 ? ToString(args[2]) : "...";

        if (s.Length <= length) return s;

        var cutoff = length - end.Length;
        if (cutoff < 0) return end;

        if (killwords)
        {
            return s[..cutoff] + end;
        }

        // Find word boundary
        var lastSpace = s.LastIndexOf(' ', Math.Min(cutoff, s.Length - 1));
        if (lastSpace < 1) lastSpace = cutoff;
        return s[..lastSpace].TrimEnd() + end;
    }

    private static object? WordWrap(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var width = args.Length > 0 ? (int)ToDouble(args[0]) : 79;
        var breakLongWords = kwargs.TryGetValue("break_long_words", out var blw) ? IsTruthy(blw) : true;
        var wrapString = kwargs.TryGetValue("wrapstring", out var ws) ? ToString(ws) : "\n";

        var words = s.Split(' ');
        var lines = new List<string>();
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 > width && current.Length > 0)
            {
                lines.Add(current.ToString().TrimEnd());
                current.Clear();
            }

            if (word.Length > width && breakLongWords)
            {
                for (int i = 0; i < word.Length; i += width)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString().TrimEnd());
                        current.Clear();
                    }
                    current.Append(word.AsSpan(i, Math.Min(width, word.Length - i)));
                }
            }
            else
            {
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString().TrimEnd());
        }

        return string.Join(wrapString, lines);
    }

    private static object? Center(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var width = args.Length > 0 ? (int)ToDouble(args[0]) : 80;
        if (s.Length >= width) return s;

        var padding = width - s.Length;
        var left = padding / 2;
        var right = padding - left;
        return new string(' ', left) + s + new string(' ', right);
    }

    private static object? Ljust(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var width = args.Length > 0 ? (int)ToDouble(args[0]) : 80;
        return s.PadRight(width);
    }

    private static object? Rjust(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var width = args.Length > 0 ? (int)ToDouble(args[0]) : 80;
        return s.PadLeft(width);
    }

    private static object? Indent(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var width = args.Length > 0 ? (int)ToDouble(args[0]) : 4;
        var firstLine = kwargs.TryGetValue("first", out var fl) ? IsTruthy(fl) : false;
        var blankLines = kwargs.TryGetValue("blank", out var bl) ? IsTruthy(bl) : false;

        var indent = new string(' ', width);
        var lines = s.Split('\n');
        var result = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var shouldIndent = (i > 0 || firstLine) && (line.Length > 0 || blankLines);

            if (shouldIndent) result.Append(indent);
            result.Append(line);
            if (i < lines.Length - 1) result.Append('\n');
        }

        return result.ToString();
    }

    private static object? Format(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);

        // Simple positional replacement
        for (int i = 0; i < args.Length; i++)
        {
            s = s.Replace($"{{{i}}}", ToString(args[i]));
        }

        // Keyword replacement
        foreach (var kvp in kwargs)
        {
            s = s.Replace($"{{{kvp.Key}}}", ToString(kvp.Value));
        }

        return s;
    }

    #endregion

    #region Collection Filters

    private static object? Length(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        return value switch
        {
            string s => s.Length,
            ICollection c => c.Count,
            IEnumerable e => e.Cast<object>().Count(),
            _ => 0
        };
    }

    private static object? First(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        return value switch
        {
            string s => s.Length > 0 ? s[0].ToString() : null,
            IEnumerable e => e.Cast<object?>().FirstOrDefault(),
            _ => null
        };
    }

    private static object? Last(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        return value switch
        {
            string s => s.Length > 0 ? s[^1].ToString() : null,
            IList l => l.Count > 0 ? l[^1] : null,
            IEnumerable e => e.Cast<object?>().LastOrDefault(),
            _ => null
        };
    }

    private static object? Join(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var separator = args.Length > 0 ? ToString(args[0]) : "";
        var attr = kwargs.TryGetValue("attribute", out var a) ? ToString(a) : null;

        if (value is not IEnumerable enumerable) return ToString(value);

        var items = enumerable.Cast<object?>();

        if (attr != null)
        {
            items = items.Select(item => GetAttribute(item, attr));
        }

        return string.Join(separator, items.Select(ToString));
    }

    private static object? Sort(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;

        var items = enumerable.Cast<object?>().ToList();
        var reverse = kwargs.TryGetValue("reverse", out var r) && IsTruthy(r);
        var attr = kwargs.TryGetValue("attribute", out var a) ? ToString(a) : null;

        if (attr != null)
        {
            items = items.OrderBy(x => GetAttribute(x, attr)).ToList();
        }
        else
        {
            items = items.OrderBy(x => x).ToList();
        }

        if (reverse) items.Reverse();
        return items;
    }

    private static object? Reverse(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        return value switch
        {
            string s => new string(s.Reverse().ToArray()),
            IList l => l.Cast<object?>().Reverse().ToList(),
            IEnumerable e => e.Cast<object?>().Reverse().ToList(),
            _ => value
        };
    }

    private static object? ToList(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        return value switch
        {
            string s => s.Select(c => c.ToString()).ToList(),
            IEnumerable e => e.Cast<object?>().ToList(),
            _ => new List<object?> { value }
        };
    }

    private static object? Unique(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;

        var attr = kwargs.TryGetValue("attribute", out var a) ? ToString(a) : null;

        if (attr != null)
        {
            var seen = new HashSet<object?>();
            return enumerable.Cast<object?>()
                .Where(x => seen.Add(GetAttribute(x, attr)))
                .ToList();
        }

        return enumerable.Cast<object?>().Distinct().ToList();
    }

    private static object? Map(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;

        var attr = args.Length > 0 ? ToString(args[0]) : null;
        if (attr == null && kwargs.TryGetValue("attribute", out var a))
        {
            attr = ToString(a);
        }

        if (attr != null)
        {
            return enumerable.Cast<object?>().Select(x => GetAttribute(x, attr)).ToList();
        }

        return value;
    }

    private static object? Select(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        return enumerable.Cast<object?>().Where(x => IsTruthy(x)).ToList();
    }

    private static object? Reject(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        return enumerable.Cast<object?>().Where(x => !IsTruthy(x)).ToList();
    }

    private static object? SelectAttr(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var attr = args.Length > 0 ? ToString(args[0]) : "";
        return enumerable.Cast<object?>().Where(x => IsTruthy(GetAttribute(x, attr))).ToList();
    }

    private static object? RejectAttr(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var attr = args.Length > 0 ? ToString(args[0]) : "";
        return enumerable.Cast<object?>().Where(x => !IsTruthy(GetAttribute(x, attr))).ToList();
    }

    private static object? GroupBy(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var attr = args.Length > 0 ? ToString(args[0]) : "";

        return enumerable.Cast<object?>()
            .GroupBy(x => GetAttribute(x, attr))
            .Select(g => new Dictionary<string, object?>
            {
                ["grouper"] = g.Key,
                ["list"] = g.ToList()
            })
            .ToList();
    }

    private static object? Batch(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var size = args.Length > 0 ? (int)ToDouble(args[0]) : 1;
        var fill = args.Length > 1 ? args[1] : null;

        var items = enumerable.Cast<object?>().ToList();
        var batches = new List<List<object?>>();

        for (int i = 0; i < items.Count; i += size)
        {
            var batch = items.Skip(i).Take(size).ToList();
            if (fill != null && batch.Count < size)
            {
                while (batch.Count < size)
                {
                    batch.Add(fill);
                }
            }
            batches.Add(batch);
        }

        return batches;
    }

    private static object? Slice(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var slices = args.Length > 0 ? (int)ToDouble(args[0]) : 1;
        var fill = args.Length > 1 ? args[1] : null;

        var items = enumerable.Cast<object?>().ToList();
        var result = new List<List<object?>>();
        var perSlice = (int)Math.Ceiling((double)items.Count / slices);

        for (int i = 0; i < slices; i++)
        {
            var slice = items.Skip(i * perSlice).Take(perSlice).ToList();
            if (fill != null && slice.Count < perSlice)
            {
                while (slice.Count < perSlice)
                {
                    slice.Add(fill);
                }
            }
            result.Add(slice);
        }

        return result;
    }

    #endregion

    #region Numeric Filters

    private static object? Round(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var precision = args.Length > 0 ? (int)ToDouble(args[0]) : 0;
        var method = kwargs.TryGetValue("method", out var m) ? ToString(m) : "common";

        var num = ToDouble(value);

        return method switch
        {
            "ceil" => Math.Ceiling(num * Math.Pow(10, precision)) / Math.Pow(10, precision),
            "floor" => Math.Floor(num * Math.Pow(10, precision)) / Math.Pow(10, precision),
            _ => Math.Round(num, precision, MidpointRounding.AwayFromZero)
        };
    }

    private static object? Sum(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return ToDouble(value);
        var attr = kwargs.TryGetValue("attribute", out var a) ? ToString(a) : null;
        var start = kwargs.TryGetValue("start", out var s) ? ToDouble(s) : 0;

        var items = enumerable.Cast<object?>();
        if (attr != null)
        {
            items = items.Select(x => GetAttribute(x, attr));
        }

        return items.Sum(x => ToDouble(x)) + start;
    }

    private static object? Min(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var attr = kwargs.TryGetValue("attribute", out var a) ? ToString(a) : null;

        var items = enumerable.Cast<object?>();
        if (attr != null)
        {
            return items.MinBy(x => GetAttribute(x, attr));
        }
        return items.Min();
    }

    private static object? Max(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var attr = kwargs.TryGetValue("attribute", out var a) ? ToString(a) : null;

        var items = enumerable.Cast<object?>();
        if (attr != null)
        {
            return items.MaxBy(x => GetAttribute(x, attr));
        }
        return items.Max();
    }

    private static readonly System.Random _random = new();

    private static object? Random(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IEnumerable enumerable) return value;
        var items = enumerable.Cast<object?>().ToList();
        if (items.Count == 0) return null;
        return items[_random.Next(items.Count)];
    }

    #endregion

    #region Dictionary/Object Filters

    private static object? Items(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is IDictionary dict)
        {
            return dict.Keys.Cast<object>()
                .Select(k => new object?[] { k, dict[k] })
                .ToList();
        }
        return new List<object?>();
    }

    private static object? Keys(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is IDictionary dict)
        {
            return dict.Keys.Cast<object>().ToList();
        }
        return new List<object>();
    }

    private static object? Values(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is IDictionary dict)
        {
            return dict.Values.Cast<object?>().ToList();
        }
        return new List<object?>();
    }

    private static object? Attr(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var name = args.Length > 0 ? ToString(args[0]) : "";
        return GetAttribute(value, name);
    }

    #endregion

    #region Serialization Filters

    private static object? ToJson(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var indent = kwargs.TryGetValue("indent", out var i) ? (int)ToDouble(i) : 0;

        var options = new JsonSerializerOptions
        {
            WriteIndented = indent > 0
        };

        return JsonSerializer.Serialize(value, options);
    }

    private static object? PrettyPrint(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(value, options);
    }

    #endregion

    #region Other Filters

    private static object? Default(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var defaultValue = args.Length > 0 ? args[0] : "";
        var boolean = args.Length > 1 && IsTruthy(args[1]);

        if (boolean)
        {
            return IsTruthy(value) ? value : defaultValue;
        }

        return value ?? defaultValue;
    }

    private static object? XmlAttr(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        if (value is not IDictionary dict) return "";

        var sb = new StringBuilder();
        foreach (DictionaryEntry kvp in dict)
        {
            if (!IsTruthy(kvp.Value)) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(HttpUtility.HtmlEncode(ToString(kvp.Key)));
            sb.Append("=\"");
            sb.Append(HttpUtility.HtmlEncode(ToString(kvp.Value)));
            sb.Append('"');
        }
        return new HtmlString(sb.ToString());
    }

    private static readonly string[] SizeUnits = ["Bytes", "KB", "MB", "GB", "TB", "PB"];

    private static object? FileSizeFormat(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var binary = kwargs.TryGetValue("binary", out var b) && IsTruthy(b);
        var bytes = ToDouble(value);
        var divisor = binary ? 1024.0 : 1000.0;

        int i = 0;
        while (bytes >= divisor && i < SizeUnits.Length - 1)
        {
            bytes /= divisor;
            i++;
        }

        return $"{bytes:F1} {SizeUnits[i]}";
    }

    private static object? RegexReplace(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var pattern = args.Length > 0 ? ToString(args[0]) : "";
        var replacement = args.Length > 1 ? ToString(args[1]) : "";
        var count = args.Length > 2 ? (int)ToDouble(args[2]) : -1;

        if (count < 0)
        {
            return Regex.Replace(s, pattern, replacement);
        }

        return Regex.Replace(s, pattern, m => count-- > 0 ? replacement : m.Value);
    }

    private static object? RegexSearch(object? value, object?[] args, IDictionary<string, object?> kwargs, RenderContext ctx)
    {
        var s = ToString(value);
        var pattern = args.Length > 0 ? ToString(args[0]) : "";
        var match = Regex.Match(s, pattern);
        return match.Success ? match.Value : null;
    }

    #endregion

    #region Helper Methods

    private static string ToString(object? value)
    {
        return value switch
        {
            null => "",
            bool b => b ? "True" : "False",
            HtmlString hs => hs.Value,
            _ => value.ToString() ?? ""
        };
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            null => 0,
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            decimal m => (double)m,
            string s => double.TryParse(s, out var r) ? r : 0,
            _ => Convert.ToDouble(value)
        };
    }

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

    private static object? GetAttribute(object? obj, string name)
    {
        if (obj == null) return null;

        var type = obj.GetType();
        var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (prop != null) return prop.GetValue(obj);

        var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (field != null) return field.GetValue(obj);

        if (obj is IDictionary dict && dict.Contains(name))
        {
            return dict[name];
        }

        return null;
    }

    #endregion
}
