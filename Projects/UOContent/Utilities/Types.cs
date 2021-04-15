using System;

namespace Server
{
    public static class Types
    {
        public static readonly Type OfByte = typeof(byte);
        public static readonly Type OfSByte = typeof(sbyte);
        public static readonly Type OfShort = typeof(short);
        public static readonly Type OfUShort = typeof(ushort);
        public static readonly Type OfInt = typeof(int);
        public static readonly Type OfUInt = typeof(uint);
        public static readonly Type OfLong = typeof(long);
        public static readonly Type OfULong = typeof(ulong);
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

        public static readonly string[] BoolNames = { "True", "False" };
        public static readonly object[] BoolValues = { true, false };

        public static readonly string[] PoisonNames = { "None", "Lesser", "Regular", "Greater", "Deadly", "Lethal" };
        public static readonly object[] PoisonValues =
            { null, Poison.Lesser, Poison.Regular, Poison.Greater, Poison.Deadly, Poison.Lethal };

        public static readonly Type[] DecimalTypes =
        {
            typeof(float),
            typeof(double)
        };

        public static readonly Type[] NumericTypes =
        {
            OfByte,
            OfShort,
            OfInt,
            OfLong,
            OfSByte,
            OfUShort,
            OfUInt,
            OfULong
        };

        private static readonly Type[] SignedNumerics =
        {
            OfLong,
            OfInt,
            OfShort,
            OfSByte
        };

        private static readonly Type[] UnsignedNumerics =
        {
            OfULong,
            OfUInt,
            OfUShort,
            OfByte
        };


        public static readonly Type[] ParseTypes = { OfString };

        public static bool IsSerial(Type t) => t == OfSerial;

        public static bool IsType(Type t) => t == OfType;

        public static bool IsChar(Type t) => t == OfChar;

        public static bool IsString(Type t) => t == OfString;

        public static bool IsText(Type t) => t == OfText;

        public static bool IsEnum(Type t) => t.IsEnum;

        public static bool IsParsable(Type t) => t == OfTimeSpan || t.IsDefined(OfParsable, false);

        public static bool IsNumeric(Type t) => Array.IndexOf(NumericTypes, t) >= 0;

        public static bool IsSignedNumeric(Type t) => Array.IndexOf(SignedNumerics, t) >= 0;

        public static bool IsUnsignedNumeric(Type t) => Array.IndexOf(UnsignedNumerics, t) >= 0;

        public static bool IsEntity(Type t) => OfEntity.IsAssignableFrom(t);
    }
}
