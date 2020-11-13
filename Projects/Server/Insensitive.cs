using System;
using System.Collections.Generic;

namespace Server
{
    public static class Insensitive
    {
        public static IComparer<string> Comparer { get; } = StringComparer.OrdinalIgnoreCase;

        public static int Compare(string a, string b) => Comparer.Compare(a, b);

        public static bool Equals(string a, string b) =>
            a == null && b == null || a != null && b != null && a.Length == b.Length && Comparer.Compare(a, b) == 0;

        public static bool StartsWith(string a, string b) =>
            a != null && b != null && a.Length >= b.Length && Comparer.Compare(a.Substring(0, b.Length), b) == 0;

        public static bool EndsWith(string a, string b) =>
            a != null && b != null && a.Length >= b.Length && Comparer.Compare(a.Substring(a.Length - b.Length), b) == 0;

        public static bool Contains(string a, string b) =>
            a != null && b != null && a.Length >= b.Length && a.Contains(b, StringComparison.Ordinal);
    }
}
