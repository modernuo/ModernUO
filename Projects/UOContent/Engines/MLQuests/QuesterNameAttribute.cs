using System;
using System.Collections.Generic;

namespace Server.Engines.MLQuests
{
    [AttributeUsage(AttributeTargets.Class)]
    public class QuesterNameAttribute : Attribute
    {
        private static readonly Type m_Type = typeof(QuesterNameAttribute);
        private static readonly Dictionary<Type, string> m_Cache = new();

        public QuesterNameAttribute(string questerName) => QuesterName = questerName;

        public string QuesterName { get; }

        public static string GetQuesterNameFor(Type t)
        {
            if (t == null)
            {
                return "";
            }

            if (m_Cache.TryGetValue(t, out var result))
            {
                return result;
            }

            var attributes = t.GetCustomAttributes(m_Type, false);

            return m_Cache[t] = attributes.Length != 0 ? ((QuesterNameAttribute)attributes[0]).QuesterName : t.Name;
        }
    }
}
