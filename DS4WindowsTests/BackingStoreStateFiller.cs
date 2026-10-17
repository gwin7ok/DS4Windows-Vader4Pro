using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W1: <c>BackingStore</c> の公開の値を、既定値と異なる決定的な値に変えるテスト用ヘルパー
    /// （「全項目が既定値」では保存・読込の取りこぼしを検出できないため）。
    /// bool は反転、整数・実数は小さく加算、列挙型は次の値、文字列は接尾辞を付ける。スロットごとの配列は指定スロットの要素だけを変える。
    /// 他のインスタンスと共有されている配列（静的な既定値）は変えない。値の検証・丸めは保存・読込の側が行い、その結果をゴールデンが固定する。
    /// </summary>
    internal static class BackingStoreStateFiller
    {
        private static readonly HashSet<string> ExcludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "m_Profile", "m_Actions", "m_linkedProfiles", "m_controllerConfigs", "ProcessInspector", "ActiveOutDevType",
            "lastVersionChecked", "lastVersionCheckedNum", "useLang", "fakeExeFileName", "loggedInvalidActions",
            "actions", "linkedProfiles",
        };

        // パス・ファイル・言語・バージョン・アドレスなど、値の形式に意味がある文字列は変えない
        private static readonly Regex KeepStringNames = new Regex("path|file|exe|lang|version|address|culture|dir|url|uri", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void Fill(object root, int slot)
        {
            object reference = null;
            try { reference = Activator.CreateInstance(root.GetType()); } catch { }
            FillObject(root, reference, slot, 0);
        }

        private static bool IsScalar(Type t) => t.IsPrimitive || t.IsEnum || t == typeof(string);

        private static void FillObject(object target, object reference, int slot, int depth)
        {
            Type t = target.GetType();
            foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ExcludedNames.Contains(f.Name) || f.IsInitOnly || f.IsLiteral) continue;
                try { FillMember(target, reference, f.Name, f.FieldType, () => f.GetValue(target), v => f.SetValue(target, v),
                    () => reference == null ? null : f.GetValue(reference), slot, depth); } catch { }
            }
            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ExcludedNames.Contains(p.Name) || !p.CanRead || p.GetIndexParameters().Length != 0) continue;
                bool settable = p.CanWrite && p.GetSetMethod() != null;
                try { FillMember(target, reference, p.Name, p.PropertyType, () => p.GetValue(target), settable ? (Action<object>)(v => p.SetValue(target, v)) : null,
                    () => reference == null ? null : p.GetValue(reference), slot, depth); } catch { }
            }
        }

        private static void FillMember(object owner, object ownerReference, string name, Type type, Func<object> get, Action<object> set,
            Func<object> getReference, int slot, int depth)
        {
            object current = get();
            if (IsScalar(type))
            {
                if (set == null) return;
                object next = Perturb(current, name);
                if (next != null && !Equals(next, current)) set(next);
                return;
            }

            if (current is Array arr && arr.Rank == 1 && arr.Length > 0)
            {
                object other = getReference();
                if (other != null && ReferenceEquals(other, arr)) return; // 静的な既定値などの共有された配列は変えない
                int idx = arr.Length > slot ? slot : 0;
                Type elem = type.GetElementType();
                if (IsScalar(elem))
                {
                    object next = Perturb(arr.GetValue(idx), name);
                    if (next != null) arr.SetValue(next, idx);
                }
                else if (elem.IsValueType)
                {
                    object boxed = arr.GetValue(idx);
                    if (boxed != null && depth < 3) { FillObject(boxed, null, slot, depth + 1); arr.SetValue(boxed, idx); }
                }
                else
                {
                    object element = arr.GetValue(idx);
                    if (element != null && depth < 3 && IsOwnType(element.GetType())) FillObject(element, null, slot, depth + 1);
                }
                return;
            }

            if (current != null && !type.IsValueType && depth < 2 && IsOwnType(current.GetType()) && !(current is System.Collections.IEnumerable))
            {
                object other = getReference();
                if (other != null && ReferenceEquals(other, current)) return;
                FillObject(current, null, slot, depth + 1);
            }
        }

        private static bool IsOwnType(Type t) => (t.Namespace ?? string.Empty).StartsWith("DS4Windows", StringComparison.Ordinal)
                                                  && (t.Namespace != "DS4Windows.DI") && (t.Namespace != "DS4Windows.Services");

        private static object Perturb(object v, string name)
        {
            switch (v)
            {
                case bool b: return !b;
                case int i: return i >= 0 && i < 1000 ? i + 1 : i;
                case byte by: return by < 255 ? (byte)(by + 1) : by;
                case short s: return s >= 0 && s < 1000 ? (short)(s + 1) : s;
                case uint u: return u < 1000 ? u + 1 : u;
                case double d: return Math.Abs(d) < 1000.0 ? Math.Round(d + 0.25, 2) : d;
                case float f: return Math.Abs(f) < 1000f ? (float)Math.Round(f + 0.25f, 2) : f;
                case Enum e:
                    {
                        Array values = Enum.GetValues(e.GetType());
                        int at = Array.IndexOf(values, e);
                        return at >= 0 ? values.GetValue((at + 1) % values.Length) : e;
                    }
                case string str:
                    if (KeepStringNames.IsMatch(name)) return str;
                    return str + "_g";
                default: return null;
            }
        }
    }
}
