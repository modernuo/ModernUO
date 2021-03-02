using System;
using System.Reflection;

namespace Server
{
    public static class Types
    {
        public static readonly Type OfInt = typeof(int);
        public static readonly Type OfObject = typeof(object);
        public static readonly Type OfBool = typeof(bool);
        public static readonly Type OfChar = typeof(char);
        public static readonly Type OfString = typeof(string);

        public static readonly Type OfSerial = typeof(Serial);
        public static readonly Type OfTimeSpan = typeof(TimeSpan);
        public static readonly Type OfPoint3D = typeof(Point3D);
        public static readonly Type OfPoint2D = typeof(Point2D);
        public static readonly Type OfEnum = typeof(Enum);
        public static readonly Type OfType = typeof(Type);

        public static readonly Type OfCPA = typeof(CommandPropertyAttribute);
        public static readonly Type OfText = typeof(TextDefinition);
        public static readonly Type OfParsable = typeof(ParsableAttribute);
        public static readonly Type OfMobile = typeof(Mobile);
        public static readonly Type OfItem = typeof(Item);
        public static readonly Type OfCustomEnum = typeof(CustomEnumAttribute);
        public static readonly Type OfPoison = typeof(Poison);
        public static readonly Type OfMap = typeof(Map);
        public static readonly Type OfSkills = typeof(Skills);
        public static readonly Type OfPropertyObject = typeof(PropertyObjectAttribute);
        public static readonly Type OfNoSort = typeof(NoSortAttribute);
        public static readonly Type OfEntity = typeof(IEntity);
        public static readonly Type OfConstructible = typeof(ConstructibleAttribute);


        public static readonly Type[] DecimalTypes =
        {
            typeof(float),
            typeof(double)
        };

        public static readonly Type[] NumericTypes =
        {
            typeof(byte),
            typeof(short),
            OfInt,
            typeof(long),
            typeof(sbyte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong)
        };

        private static readonly Type[] SignedNumerics =
        {
            typeof(long),
            typeof(int),
            typeof(short),
            typeof(sbyte)
        };

        private static readonly Type[] UnsignedNumerics =
        {
            typeof(ulong),
            typeof(uint),
            typeof(ushort),
            typeof(byte)
        };


        public static readonly Type[] ParseTypes = { typeof(string) };

        public static bool IsSerial(Type t) => t == OfSerial;

        public static bool IsType(Type t) => t == OfType;

        public static bool IsChar(Type t) => t == OfChar;

        public static bool IsString(Type t) => t == OfString;

        public static bool IsText(Type t) => t == OfText;

        public static bool IsEnum(Type t) => t.IsEnum;

        public static bool IsParsable(Type t) => t == OfTimeSpan || t.IsDefined(OfParsable, false);

        public static bool IsNumeric(Type t) => Array.IndexOf(NumericTypes, t) >= 0;

        public static bool IsEntity(Type t) => OfEntity.IsAssignableFrom(t);

        public static bool IsConstructible(ConstructorInfo ctor, AccessLevel accessLevel)
        {
            var attrs = ctor.GetCustomAttributes(OfConstructible, false);

            return attrs.Length != 0 && accessLevel >= ((ConstructibleAttribute)attrs[0]).AccessLevel;
        }

        public static bool IsSignedNumeric(Type type)
        {
            for (var i = 0; i < SignedNumerics.Length; ++i)
            {
                if (type == SignedNumerics[i])
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsUnsignedNumeric(Type type)
        {
            for (var i = 0; i < UnsignedNumerics.Length; ++i)
            {
                if (type == UnsignedNumerics[i])
                {
                    return true;
                }
            }

            return false;
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
    }
}
