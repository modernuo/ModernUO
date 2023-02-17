using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Server
{
    public static class Types
    {
        public static readonly Type[] ParseStringParamTypes = { typeof(string), typeof(IFormatProvider) };
        public static readonly Type[] ParseStringNumericParamTypes = { typeof(string), typeof(NumberStyles) };
        private static object[] _parseParams = { null, null };

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

        private static Dictionary<Type, bool> _isParsable;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsType(Type type, Type check) => check.IsAssignableFrom(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChar(Type t) => IsType(t, OfChar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(Type t) => IsType(t, OfString);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsText(Type t) => IsType(t, OfText);

        public static bool IsParsable(Type t)
        {
            _isParsable ??= new();
            if (_isParsable.TryGetValue(t, out var isParsable))
            {
                return isParsable;
            }

            foreach (var x in t.GetInterfaces())
            {
                if (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IParsable<>))
                {
                    isParsable = true;
                    break;
                }
            }

            return _isParsable[t] = isParsable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDecimal(Type t) => Array.IndexOf(DecimalTypes, t) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(Type t) => Array.IndexOf(NumericTypes, t) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntity(Type t) => OfEntity.IsAssignableFrom(t);

        private static Dictionary<Type, MethodInfo> _parseMethods;

        public static object Parse(Type t, string value)
        {
            _parseMethods ??= new();
            if (!_parseMethods.TryGetValue(t, out var method))
            {
                _parseMethods[t] = method = t.GetMethod("Parse", ParseStringParamTypes);
            }

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
                    if (ulong.TryParse(value.AsSpan(2), NumberStyles.HexNumber, null, out var num))
                    {
                        constructed = Convert.ChangeType(num, type);
                    }
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
