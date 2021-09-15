using System;
using System.Runtime.CompilerServices;

namespace Server
{
    public static class Types
    {
        private static readonly Type[] _parseStringParamTypes = { typeof(string) };
        private static readonly object[] _parseParams = new object[1];

        public static readonly Type OfByte = typeof(byte);
        public static readonly Type OfSByte = typeof(sbyte);
        public static readonly Type OfShort = typeof(short);
        public static readonly Type OfUShort = typeof(ushort);
        public static readonly Type OfInt = typeof(int);
        public static readonly Type OfUInt = typeof(uint);
        public static readonly Type OfLong = typeof(long);
        public static readonly Type OfULong = typeof(ulong);
        public static readonly Type OfFloat = typeof(float);
        public static readonly Type OfDouble = typeof(double);
        public static readonly Type OfDecimal = typeof(decimal);
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
        public static readonly Type OfGuid = typeof(Guid);

        public static readonly string[] BoolNames = { "True", "False" };
        public static readonly object[] BoolValues = { true, false };

        public static readonly string[] PoisonNames = { "None", "Lesser", "Regular", "Greater", "Deadly", "Lethal" };
        public static readonly object[] PoisonValues =
            { null, Poison.Lesser, Poison.Regular, Poison.Greater, Poison.Deadly, Poison.Lethal };

        public static readonly Type[] DecimalTypes =
        {
            OfFloat,
            OfDouble,
            OfDecimal
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsType(Type type, Type check) => check.IsAssignableFrom(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChar(Type t) => IsType(t, OfChar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(Type t) => IsType(t, OfString);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsText(Type t) => IsType(t, OfText);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsParsable(Type t) =>
            IsChar(t) || IsType(t, OfGuid) ||
            IsType(t, OfTimeSpan) || IsNumeric(t) || IsDecimal(t) || t.IsDefined(OfParsable, false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDecimal(Type t) => Array.IndexOf(DecimalTypes, t) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(Type t) => Array.IndexOf(NumericTypes, t) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntity(Type t) => OfEntity.IsAssignableFrom(t);

        public static object Parse(Type t, string value)
        {
            var method = t.GetMethod("Parse", _parseStringParamTypes);
            _parseParams[0] = value;
            return method?.Invoke(null, _parseParams);
        }

        // Do not use this in "Parse" methods, it may cause a stack overflow
        public static string TryParse(Type type, string value, out object constructed)
        {
            constructed = null;
            var isSerial = IsType(type, OfSerial);

            if (isSerial) // mutate into int32
            {
                type = OfInt;
            }

            if (value == "(-null-)" && !type.IsValueType)
            {
                value = null;
            }

            if (IsType(type, OfEnum))
            {
                try
                {
                    constructed = Enum.Parse(type, value ?? "", true);
                    return null;
                }
                catch
                {
                    return "That is not a valid enumeration member.";
                }
            }

            if (IsType(type, OfType))
            {
                try
                {
                    constructed = AssemblyHandler.FindTypeByName(value);

                    return constructed == null ? "No type with that name was found." : null;
                }
                catch
                {
                    return "No type with that name was found.";
                }
            }

            if (value == null)
            {
                constructed = null;
                return null;
            }

            if (IsType(type, OfString))
            {
                constructed = value;
                return null;
            }

            if (value.StartsWithOrdinal("0x") && IsNumeric(type))
            {
                try
                {
                    constructed = Convert.ChangeType(Convert.ToUInt64(value[2..], 16), type);
                    return null;
                }
                catch
                {
                    return "That is not properly formatted.";
                }
            }

            if (IsParsable(type))
            {
                try
                {
                    constructed = Parse(type, value);
                    return null;
                }
                catch
                {
                    return "That is not properly formatted.";
                }
            }

            try
            {
                constructed = Convert.ChangeType(value, type);
                if (isSerial) // mutate back
                {
                    constructed = (Serial)(constructed ?? Serial.MinusOne);
                }

                return null;
            }
            catch
            {
                return "That is not properly formatted.";
            }
        }
    }
}
