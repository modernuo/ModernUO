using System;
using System.Reflection;
using static Server.Types;

namespace Server
{
    public static class Attributes
    {
        public static bool IsConstructible(ConstructorInfo ctor, AccessLevel accessLevel)
        {
            var attrs = ctor.GetCustomAttributes(OfConstructible, false);

            return attrs.Length != 0 && accessLevel >= ((ConstructibleAttribute)attrs[0]).AccessLevel;
        }

        public static CommandPropertyAttribute GetCPA(PropertyInfo p)
        {
            var attrs = p.GetCustomAttributes(OfCPA, false);

            if (attrs.Length == 0)
            {
                return null;
            }

            return attrs[0] as CommandPropertyAttribute;
        }
        public static bool HasAttribute(Type type, Type check, bool inherit) =>
            type.GetCustomAttributes(check, inherit).Length > 0;
    }
}
