using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W1: <c>BackingStore</c> の公開状態（フィールドとプロパティ）を、決定的な文字列にするテスト用ヘルパー。
    /// 読込後の状態をゴールデンと照合するために使う。メンバーは名前の順、配列は添字の順、辞書・集合は並べ替えて出力する。
    /// サービス・デリゲート・XML ノードなど、状態ではない型は出力しない。揺れる値（日時）は出力しない。
    /// </summary>
    internal static class BackingStoreStateDumper
    {
        private const int MaxDepth = 7;

        // 読込・保存の対象ではない（パス、環境依存、サービス）メンバー
        private static readonly HashSet<string> ExcludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "m_Profile", "m_Actions", "m_linkedProfiles", "m_controllerConfigs", "ProcessInspector", "loggedInvalidActions",
        };

        public static string Dump(object root)
        {
            var sb = new StringBuilder();
            Visit(sb, "$", root, 0, new List<object>());
            return sb.ToString();
        }

        /// <summary>基準の出力に無い行（+）と、基準にあって無くなった行（-）だけを返す。</summary>
        public static string Diff(string baseline, string current)
        {
            var baseLines = baseline.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToList();
            var curLines = current.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToList();
            var baseSet = new HashSet<string>(baseLines, StringComparer.Ordinal);
            var curSet = new HashSet<string>(curLines, StringComparer.Ordinal);
            var sb = new StringBuilder();
            foreach (string l in baseLines) if (!curSet.Contains(l)) sb.Append("- ").Append(l).Append('\n');
            foreach (string l in curLines) if (!baseSet.Contains(l)) sb.Append("+ ").Append(l).Append('\n');
            return sb.ToString();
        }

        private static bool IsScalar(Type t)
        {
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime)
                   || t == typeof(TimeSpan) || t == typeof(Guid) || t == typeof(DateTimeOffset);
        }

        private static string Format(object v)
        {
            switch (v)
            {
                case string s: return "\"" + s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case DateTime _: return "<DateTime>";
                case DateTimeOffset _: return "<DateTimeOffset>";
                case TimeSpan ts: return ts.ToString("c", CultureInfo.InvariantCulture);
                case bool b: return b ? "true" : "false";
                case Enum e: return e.GetType().Name + "." + e;
                case IFormattable fm: return fm.ToString(null, CultureInfo.InvariantCulture);
                default: return v.ToString();
            }
        }

        private static bool IsSkipped(Type t)
        {
            if (typeof(Delegate).IsAssignableFrom(t) || typeof(Type).IsAssignableFrom(t) || typeof(MemberInfo).IsAssignableFrom(t)
                || typeof(Assembly).IsAssignableFrom(t) || typeof(System.Threading.WaitHandle).IsAssignableFrom(t)
                || typeof(System.Threading.Tasks.Task).IsAssignableFrom(t) || typeof(System.Threading.Thread).IsAssignableFrom(t)
                || typeof(System.IO.Stream).IsAssignableFrom(t) || typeof(System.Xml.XmlNode).IsAssignableFrom(t)
                || t == typeof(object))
                return true;
            string ns = t.Namespace ?? string.Empty;
            if (ns == "DS4Windows.DI" || ns == "DS4Windows.Services" || ns.StartsWith("Microsoft.", StringComparison.Ordinal)
                || ns.StartsWith("System.Windows", StringComparison.Ordinal) || ns.StartsWith("System.Threading", StringComparison.Ordinal)
                || ns.StartsWith("System.Reflection", StringComparison.Ordinal) || ns.StartsWith("System.Text.RegularExpressions", StringComparison.Ordinal))
                return true;
            if (t.Name == "ControlService" || t.Name == "DS4Device" || t.Name == "OutputDevice")
                return true;
            if (t.GetInterfaces().Any(i => i.Namespace == "DS4Windows.DI")) return true;
            return false;
        }

        private static void Line(StringBuilder sb, string path, string value)
        {
            sb.Append(path).Append(" = ").Append(value).Append('\n');
        }

        private static void Visit(StringBuilder sb, string path, object value, int depth, List<object> stack)
        {
            if (value == null) { Line(sb, path, "null"); return; }
            Type t = value.GetType();
            if (IsScalar(t)) { Line(sb, path, Format(value)); return; }
            if (t == typeof(System.Drawing.Color))
            {
                var c = (System.Drawing.Color)value;
                Line(sb, path, $"Color({c.A},{c.R},{c.G},{c.B})");
                return;
            }
            if (IsSkipped(t)) return;
            if (depth > MaxDepth) { Line(sb, path, "<depth>"); return; }
            if (!t.IsValueType)
            {
                if (stack.Any(o => ReferenceEquals(o, value))) { Line(sb, path, "<cycle>"); return; }
                stack.Add(value);
            }

            try
            {
                if (value is Array arr)
                {
                    if (arr.Rank == 1)
                    {
                        Line(sb, path + ".Length", arr.Length.ToString(CultureInfo.InvariantCulture));
                        for (int i = 0; i < arr.Length; i++)
                            Visit(sb, path + "[" + i + "]", arr.GetValue(i), depth + 1, stack);
                    }
                    else
                    {
                        int n = 0;
                        foreach (object item in arr) Visit(sb, path + "[#" + (n++) + "]", item, depth + 1, stack);
                    }
                }
                else if (value is IDictionary dict)
                {
                    var keys = dict.Keys.Cast<object>().OrderBy(k => Format(k), StringComparer.Ordinal).ToList();
                    Line(sb, path + ".Count", keys.Count.ToString(CultureInfo.InvariantCulture));
                    foreach (object k in keys)
                        Visit(sb, path + "[" + Format(k) + "]", dict[k], depth + 1, stack);
                }
                else if (value is IEnumerable en)
                {
                    var items = en.Cast<object>().ToList();
                    bool unordered = t.Name.StartsWith("HashSet", StringComparison.Ordinal) || t.Name.Contains("Set");
                    if (unordered && items.All(o => o == null || IsScalar(o.GetType())))
                        items = items.OrderBy(o => o == null ? string.Empty : Format(o), StringComparer.Ordinal).ToList();
                    Line(sb, path + ".Count", items.Count.ToString(CultureInfo.InvariantCulture));
                    for (int i = 0; i < items.Count; i++)
                        Visit(sb, path + "[" + i + "]", items[i], depth + 1, stack);
                }
                else
                {
                    foreach (MemberInfo m in Members(t))
                    {
                        object v;
                        try { v = m is FieldInfo f ? f.GetValue(value) : ((PropertyInfo)m).GetValue(value); }
                        catch (Exception ex) { Line(sb, path + "." + m.Name, "<throws:" + (ex.InnerException ?? ex).GetType().Name + ">"); continue; }
                        Visit(sb, path + "." + m.Name, v, depth + 1, stack);
                    }
                }
            }
            finally
            {
                if (!t.IsValueType) stack.RemoveAt(stack.Count - 1);
            }
        }

        private static IEnumerable<MemberInfo> Members(Type t)
        {
            var list = new List<MemberInfo>();
            foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!ExcludedNames.Contains(f.Name)) list.Add(f);
            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (p.CanRead && p.GetIndexParameters().Length == 0 && !ExcludedNames.Contains(p.Name)) list.Add(p);
            return list.OrderBy(m => m.Name, StringComparer.Ordinal);
        }
    }
}
