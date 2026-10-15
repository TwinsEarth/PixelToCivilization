using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace UnityEngine
{
    /// <summary>
    /// 反射 JSON 序列化器（v10.3.1 由空 stub 升级为真实实现）。
    /// 语义对齐 Unity JsonUtility：序列化 public 字段（及 [SerializeField] 私有字段），
    /// 不序列化属性；支持基本类型、枚举（数字）、嵌套 [Serializable]、T[]、List&lt;T&gt;、
    /// Unity 向量/颜色等。不支持 Dictionary（与 Unity 一致）。
    /// </summary>
    public sealed class JsonUtility
    {
        public static string ToJson(object obj) => ToJson(obj, false);

        public static string ToJson(object obj, bool prettyPrint)
        {
            var sb = new StringBuilder(256);
            var w = new JWriter(sb, prettyPrint);
            w.WriteValue(obj, 0);
            return sb.ToString();
        }

        public static T FromJson<T>(string json)
        {
            object v = FromJson(json, typeof(T));
            return v == null ? default(T) : (T)v;
        }

        public static object FromJson(string json, Type t)
        {
            if (string.IsNullOrEmpty(json)) return JCommon.CreateDefault(t);
            object tree;
            try { tree = new JParser(json).Parse(); }
            catch { return JCommon.CreateDefault(t); }
            Type ut = Nullable.GetUnderlyingType(t);
            if (ut != null) t = ut;
            return JBuilder.Build(tree, t);
        }

        public static void FromJsonOverwrite(string json, object target)
        {
            if (target == null || string.IsNullOrEmpty(json)) return;
            object tree;
            try { tree = new JParser(json).Parse(); }
            catch { return; }
            if (tree is IDictionary) JBuilder.BuildInto(tree, target);
        }
    }

    // ============================================================
    // 共享工具
    // ============================================================
    internal static class JCommon
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool IsIntegerType(Type t)
        {
            return t == typeof(byte) || t == typeof(sbyte) ||
                   t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(int) || t == typeof(uint) ||
                   t == typeof(long) || t == typeof(ulong);
        }

        public static bool IsFloatType(Type t)
        {
            return t == typeof(float) || t == typeof(double) || t == typeof(decimal);
        }

        /// <summary>列出可序列化字段：public（除非 [NonSerialized]）或带 [SerializeField] 的私有。</summary>
        public static FieldInfo[] GetSerializableFields(Type t)
        {
            var list = new List<FieldInfo>();
            foreach (var f in t.GetFields(FieldFlags))
            {
                if (f.IsNotSerialized) continue;
                if (f.IsPublic)
                {
                    list.Add(f);
                }
                else
                {
                    foreach (var a in f.GetCustomAttributes(false))
                        if (a.GetType().FullName == "UnityEngine.SerializeField")
                        { list.Add(f); break; }
                }
            }
            return list.ToArray();
        }

        public static object CreateDefault(Type t)
        {
            if (t.IsValueType) return Activator.CreateInstance(t);
            return null;
        }

        public static object CreateInstance(Type t)
        {
            if (t.IsValueType) return Activator.CreateInstance(t);
            try { return Activator.CreateInstance(t); }
            catch { return FormatterServices.GetUninitializedObject(t); }
        }

        /// <summary>若为 List&lt;T&gt;，返回元素类型。</summary>
        public static bool TryGetListElementType(Type t, out Type element)
        {
            element = null;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                element = t.GetGenericArguments()[0];
                return true;
            }
            return false;
        }
    }

    // ============================================================
    // 序列化
    // ============================================================
    internal sealed class JWriter
    {
        private readonly StringBuilder _sb;
        private readonly bool _pretty;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public JWriter(StringBuilder sb, bool pretty) { _sb = sb; _pretty = pretty; }

        private void Indent(int depth)
        {
            if (!_pretty) return;
            for (int i = 0; i < depth; i++) _sb.Append("  ");
        }

        private void NewLine() { if (_pretty) _sb.Append('\n'); }

        public void WriteValue(object value, int depth)
        {
            if (value == null) { _sb.Append("null"); return; }
            Type t = value.GetType();

            if (t == typeof(string)) { WriteString((string)value); return; }
            if (t == typeof(char)) { WriteString(((char)value).ToString()); return; }
            if (t == typeof(bool)) { _sb.Append(((bool)value) ? "true" : "false"); return; }
            if (t.IsEnum) { WriteIntegerNumber(value); return; }
            if (JCommon.IsIntegerType(t)) { WriteIntegerNumber(value); return; }
            if (JCommon.IsFloatType(t)) { WriteFloat(value); return; }
            if (value is IEnumerable) { WriteArray((IEnumerable)value, depth); return; }
            WriteObject(value, t, depth);
        }

        /// <summary>
        /// 输出整数/枚举（无符号类型用 ToUInt64，避免 ulong 值 &gt; long.MaxValue 时
        /// Convert.ToInt64 抛 OverflowException；对齐 Unity JsonUtility 对无符号字段的处理）。
        /// </summary>
        private void WriteIntegerNumber(object value)
        {
            Type t = value.GetType();
            Type check = t.IsEnum ? Enum.GetUnderlyingType(t) : t;
            bool unsigned =
                check == typeof(ulong) || check == typeof(uint) ||
                check == typeof(ushort) || check == typeof(byte);
            if (unsigned)
                _sb.Append(Convert.ToUInt64(value).ToString(Inv));
            else
                _sb.Append(Convert.ToInt64(value).ToString(Inv));
        }

        private void WriteFloat(object value)
        {
            float f = Convert.ToSingle(value, Inv);
            if (float.IsNaN(f) || float.IsInfinity(f)) { _sb.Append('0'); return; }
            string s;
            if (value is double) s = ((double)value).ToString("R", Inv);
            else if (value is float) s = f.ToString("R", Inv);
            else s = f.ToString("R", Inv);
            _sb.Append(s);
        }

        private void WriteObject(object value, Type t, int depth)
        {
            FieldInfo[] fields = JCommon.GetSerializableFields(t);
            _sb.Append('{');
            bool any = false;
            for (int i = 0; i < fields.Length; i++)
            {
                object fv = fields[i].GetValue(value);
                if (any) _sb.Append(',');
                NewLine();
                Indent(depth + 1);
                WriteString(fields[i].Name);
                _sb.Append(_pretty ? ": " : ":");
                WriteValue(fv, depth + 1);
                any = true;
            }
            if (any) { NewLine(); Indent(depth); }
            _sb.Append('}');
        }

        private void WriteArray(IEnumerable items, int depth)
        {
            _sb.Append('[');
            bool any = false;
            foreach (var item in items)
            {
                if (any) _sb.Append(',');
                NewLine();
                Indent(depth + 1);
                WriteValue(item, depth + 1);
                any = true;
            }
            if (any) { NewLine(); Indent(depth); }
            _sb.Append(']');
        }

        private void WriteString(string s)
        {
            _sb.Append('"');
            if (s != null)
            {
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': _sb.Append("\\\""); break;
                        case '\\': _sb.Append("\\\\"); break;
                        case '\n': _sb.Append("\\n"); break;
                        case '\r': _sb.Append("\\r"); break;
                        case '\t': _sb.Append("\\t"); break;
                        case '\b': _sb.Append("\\b"); break;
                        case '\f': _sb.Append("\\f"); break;
                        default:
                            if (c < 0x20)
                                _sb.Append("\\u").Append(((int)c).ToString("x4", Inv));
                            else _sb.Append(c);
                            break;
                    }
                }
            }
            _sb.Append('"');
        }
    }

    // ============================================================
    // JSON 解析（generic tree）
    // ============================================================
    internal sealed class JParser
    {
        private readonly string _s;
        private int _i;

        public JParser(string s) { _s = s; _i = 0; }

        public object Parse()
        {
            SkipWs();
            object v = ParseValue();
            SkipWs();
            return v;
        }

        private void SkipWs()
        {
            while (_i < _s.Length)
            {
                char c = _s[_i];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                else break;
            }
        }

        private object ParseValue()
        {
            SkipWs();
            if (_i >= _s.Length) return null;
            char c = _s[_i];
            if (c == '{') return ParseObject();
            if (c == '[') return ParseArray();
            if (c == '"') return ParseString();
            if (c == 't' || c == 'f') return ParseBool();
            if (c == 'n') { ParseLiteral("null"); return null; }
            return ParseNumber();
        }

        private void ParseLiteral(string lit)
        {
            for (int k = 0; k < lit.Length; k++, _i++)
            {
                if (_i >= _s.Length || _s[_i] != lit[k])
                    throw new FormatException("JSON 字面量不匹配");
            }
        }

        private bool ParseBool()
        {
            if (_s[_i] == 't') { ParseLiteral("true"); return true; }
            ParseLiteral("false"); return false;
        }

        private Dictionary<string, object> ParseObject()
        {
            var d = new Dictionary<string, object>();
            _i++; // {
            SkipWs();
            if (_i < _s.Length && _s[_i] == '}') { _i++; return d; }
            while (true)
            {
                SkipWs();
                string key = ParseString();
                SkipWs();
                if (_i >= _s.Length || _s[_i] != ':') throw new FormatException("JSON 缺少 ':'");
                _i++;
                object val = ParseValue();
                d[key] = val;
                SkipWs();
                if (_i >= _s.Length) throw new FormatException("JSON 对象未闭合");
                char c = _s[_i];
                if (c == ',') { _i++; continue; }
                if (c == '}') { _i++; break; }
                throw new FormatException("JSON 对象分隔符错误");
            }
            return d;
        }

        private List<object> ParseArray()
        {
            var list = new List<object>();
            _i++; // [
            SkipWs();
            if (_i < _s.Length && _s[_i] == ']') { _i++; return list; }
            while (true)
            {
                object val = ParseValue();
                list.Add(val);
                SkipWs();
                if (_i >= _s.Length) throw new FormatException("JSON 数组未闭合");
                char c = _s[_i];
                if (c == ',') { _i++; continue; }
                if (c == ']') { _i++; break; }
                throw new FormatException("JSON 数组分隔符错误");
            }
            return list;
        }

        private string ParseString()
        {
            if (_i >= _s.Length || _s[_i] != '"') throw new FormatException("JSON 字符串未开始");
            _i++;
            var sb = new StringBuilder();
            while (_i < _s.Length)
            {
                char c = _s[_i++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (_i >= _s.Length) throw new FormatException("JSON 转义未结束");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                        {
                            if (_i + 4 > _s.Length) throw new FormatException("JSON \\u 长度不足");
                            string hex = _s.Substring(_i, 4);
                            _i += 4;
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            break;
                        }
                        default: sb.Append(e); break;
                    }
                }
                else sb.Append(c);
            }
            throw new FormatException("JSON 字符串未闭合");
        }

        private double ParseNumber()
        {
            int start = _i;
            while (_i < _s.Length)
            {
                char c = _s[_i];
                if (c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E' ||
                    (c >= '0' && c <= '9'))
                    _i++;
                else break;
            }
            string num = _s.Substring(start, _i - start);
            if (!double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                throw new FormatException("JSON 数字格式错误: " + num);
            return d;
        }
    }

    // ============================================================
    // generic tree → typed object
    // ============================================================
    internal static class JBuilder
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static object Build(object node, Type target)
        {
            Type ut = Nullable.GetUnderlyingType(target);
            if (ut != null)
            {
                if (node == null) return null;
                target = ut;
            }

            if (target == typeof(string)) return node as string;
            if (target == typeof(char))
            {
                string s = node as string;
                return (s != null && s.Length > 0) ? s[0] : default(char);
            }
            if (target == typeof(bool))
            {
                if (node is bool b) return b;
                return ToNumber(node) != 0;
            }
            if (target.IsEnum) return BuildEnum(node, target);
            if (JCommon.IsIntegerType(target) || JCommon.IsFloatType(target))
            {
                double n = ToNumber(node);
                if (target == typeof(float)) return (float)n;
                if (target == typeof(double)) return n;
                if (target == typeof(decimal)) return (decimal)n;
                return Convert.ChangeType(n, target, Inv);
            }
            if (target.IsArray) return BuildArray(node, target);
            if (JCommon.TryGetListElementType(target, out Type elem))
                return BuildList(node, elem);
            if (node is IDictionary) return BuildObject(node, target);
            return node;
        }

        private static object BuildEnum(object node, Type target)
        {
            if (node is string s)
            {
                try { return Enum.Parse(target, s, true); }
                catch { return Activator.CreateInstance(target); }
            }
            long n = Convert.ToInt64(ToNumber(node), Inv);
            return Enum.ToObject(target, n);
        }

        private static object BuildObject(object node, Type target)
        {
            object instance = JCommon.CreateInstance(target);
            FillObject((IDictionary)node, instance);
            return instance;
        }

        public static void BuildInto(object node, object existing)
        {
            if (node is IDictionary) FillObject((IDictionary)node, existing);
        }

        private static void FillObject(IDictionary dict, object instance)
        {
            Type t = instance.GetType();
            FieldInfo[] fields = JCommon.GetSerializableFields(t);
            foreach (var f in fields)
            {
                if (dict.Contains(f.Name))
                {
                    object child = Build(dict[f.Name], f.FieldType);
                    f.SetValue(instance, child);
                }
            }
        }

        private static object BuildArray(object node, Type target)
        {
            Type elem = target.GetElementType();
            IList src = node as IList;
            int n = src != null ? src.Count : 0;
            Array arr = Array.CreateInstance(elem, n);
            for (int i = 0; i < n; i++)
                arr.SetValue(Build(src[i], elem), i);
            return arr;
        }

        private static object BuildList(object node, Type elem)
        {
            IList src = node as IList;
            Type gl = typeof(List<>).MakeGenericType(elem);
            IList result = (IList)Activator.CreateInstance(gl);
            if (src != null)
                foreach (var item in src)
                    result.Add(Build(item, elem));
            return result;
        }

        private static double ToNumber(object node)
        {
            if (node == null) return 0;
            if (node is double d) return d;
            if (node is bool b) return b ? 1 : 0;
            if (node is string s)
            {
                if (double.TryParse(s, NumberStyles.Any, Inv, out double p)) return p;
                return 0;
            }
            try { return Convert.ToDouble(node, Inv); }
            catch { return 0; }
        }
    }
}
