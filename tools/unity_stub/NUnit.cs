using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SetUpAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestFixtureAttribute : Attribute { }

    /// <summary>断言失败异常（反射测试运行器据此判定用例失败）。</summary>
    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    /// <summary>
    /// 断言实现。v10.3.1：由空 stub 改为真实检查（失败抛 AssertionException），
    /// 使 Roslyn 反射测试运行器可真实执行 EditMode 断言；语义与 NUnit 对齐。
    /// </summary>
    public static class Assert
    {
        private static bool IsNumeric(object o)
        {
            return o is sbyte || o is byte || o is short || o is ushort ||
                   o is int || o is uint || o is long || o is ulong ||
                   o is float || o is double || o is decimal ||
                   o is Enum;
        }

        private static bool ValuesEqual(object expected, object actual)
        {
            if (object.Equals(expected, actual)) return true;
            if (expected == null || actual == null) return false;
            // 数值/枚举跨类型（如 int 与 float）按 double 比较
            if (IsNumeric(expected) && IsNumeric(actual))
            {
                double d = Math.Abs(Convert.ToDouble(expected) - Convert.ToDouble(actual));
                if (d < 1e-9) return true;
                // 浮点相对误差（大数值）
                double mag = Math.Max(Math.Abs(Convert.ToDouble(expected)),
                                      Math.Abs(Convert.ToDouble(actual)));
                return mag > 0 && d / mag < 1e-6;
            }
            return false;
        }

        private static string Msg(string userMessage, string detail)
        {
            if (!string.IsNullOrEmpty(userMessage))
                return detail + "  |  " + userMessage;
            return detail;
        }

        public static void AreEqual(object expected, object actual)
        {
            if (!ValuesEqual(expected, actual))
                throw new AssertionException(
                    $"AreEqual 期望: <{expected ?? "null"}>，实际: <{actual ?? "null"}>");
        }

        public static void AreEqual(object expected, object actual, string message)
        {
            if (!ValuesEqual(expected, actual))
                throw new AssertionException(Msg(message,
                    $"AreEqual 期望: <{expected ?? "null"}>，实际: <{actual ?? "null"}>"));
        }

        // 浮点数容差重载（float 隐式提升为 double）。
        public static void AreEqual(double expected, double actual, double delta)
        {
            if (!DoublesEqual(expected, actual, delta))
                throw new AssertionException(
                    $"AreEqual 期望: <{expected}>，实际: <{actual}>（容差 {delta}）");
        }

        public static void AreEqual(double expected, double actual, double delta, string message)
        {
            if (!DoublesEqual(expected, actual, delta))
                throw new AssertionException(Msg(message,
                    $"AreEqual 期望: <{expected}>，实际: <{actual}>（容差 {delta}）"));
        }

        private static bool DoublesEqual(double expected, double actual, double delta)
        {
            if (double.IsNaN(expected) && double.IsNaN(actual)) return true;
            if (double.IsInfinity(expected) || double.IsInfinity(actual))
                return expected == actual;
            return System.Math.Abs(expected - actual) <= System.Math.Abs(delta);
        }

        public static void AreSame(object expected, object actual)
        {
            if (!object.ReferenceEquals(expected, actual))
                throw new AssertionException(
                    $"AreSame 期望同一引用，实际不是（expected={(expected ?? "null")}, actual={(actual ?? "null")}）");
        }

        public static void IsTrue(bool condition)
        {
            if (!condition) throw new AssertionException("IsTrue 期望: true，实际: false");
        }

        public static void IsTrue(bool condition, string message)
        {
            if (!condition) throw new AssertionException(Msg(message, "IsTrue 期望: true，实际: false"));
        }

        public static void IsFalse(bool condition)
        {
            if (condition) throw new AssertionException("IsFalse 期望: false，实际: true");
        }

        public static void IsFalse(bool condition, string message)
        {
            if (condition) throw new AssertionException(Msg(message, "IsFalse 期望: false，实际: true"));
        }

        public static void IsNull(object value)
        {
            if (value != null) throw new AssertionException($"IsNull 期望: null，实际: <{value}>");
        }

        public static void IsNull(object value, string message)
        {
            if (value != null) throw new AssertionException(Msg(message, $"IsNull 期望: null，实际: <{value}>"));
        }

        public static void IsNotNull(object value)
        {
            if (value == null) throw new AssertionException("IsNotNull 期望: 非 null，实际: null");
        }

        public static void IsNotNull(object value, string message)
        {
            if (value == null) throw new AssertionException(Msg(message, "IsNotNull 期望: 非 null，实际: null"));
        }

        private static double ToDouble(IComparable c)
        {
            if (c is Enum) return Convert.ToDouble(c);
            if (c is sbyte || c is byte || c is short || c is ushort ||
                c is int || c is uint || c is long || c is ulong ||
                c is float || c is double || c is decimal)
                return Convert.ToDouble(c);
            throw new ArgumentException($"Less/Greater 仅支持数值类型，实际: {c?.GetType()}");
        }

        public static void Less(IComparable actual, IComparable expected)
        {
            if (!(ToDouble(actual) < ToDouble(expected)))
                throw new AssertionException($"Less 期望: {actual} < {expected}");
        }

        public static void Less(IComparable actual, IComparable expected, string message)
        {
            if (!(ToDouble(actual) < ToDouble(expected)))
                throw new AssertionException(Msg(message, $"Less 期望: {actual} < {expected}"));
        }

        public static void Greater(IComparable actual, IComparable expected)
        {
            if (!(ToDouble(actual) > ToDouble(expected)))
                throw new AssertionException($"Greater 期望: {actual} > {expected}");
        }

        public static void Greater(IComparable actual, IComparable expected, string message)
        {
            if (!(ToDouble(actual) > ToDouble(expected)))
                throw new AssertionException(Msg(message, $"Greater 期望: {actual} > {expected}"));
        }

        public static void GreaterOrEqual(IComparable actual, IComparable expected)
        {
            if (!(ToDouble(actual) >= ToDouble(expected)))
                throw new AssertionException($"GreaterOrEqual 期望: {actual} >= {expected}");
        }

        public static void GreaterOrEqual(IComparable actual, IComparable expected, string message)
        {
            if (!(ToDouble(actual) >= ToDouble(expected)))
                throw new AssertionException(Msg(message, $"GreaterOrEqual 期望: {actual} >= {expected}"));
        }
    }

    /// <summary>集合断言实现。</summary>
    public static class CollectionAssert
    {
        private static int CountOf(object collection)
        {
            if (collection is ICollection c) return c.Count;
            if (collection is IEnumerable e)
            {
                int n = 0;
                foreach (var item in e) n++;
                return n;
            }
            throw new ArgumentException("参数不是集合");
        }

        public static void IsNotEmpty(ICollection collection)
        {
            if (collection == null || collection.Count == 0)
                throw new AssertionException("CollectionAssert.IsNotEmpty 期望非空集合，实际为空");
        }

        public static void IsNotEmpty(IEnumerable collection)
        {
            if (collection == null || CountOf(collection) == 0)
                throw new AssertionException("CollectionAssert.IsNotEmpty 期望非空集合，实际为空");
        }

        public static void IsEmpty(ICollection collection)
        {
            if (collection != null && collection.Count > 0)
                throw new AssertionException($"CollectionAssert.IsEmpty 期望空集合，实际含 {collection.Count} 项");
        }
    }
}
