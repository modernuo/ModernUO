using System;
using System.Collections.Concurrent;
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
        // Legacy RunUO signature: a static Parse(string) that predates IParsable<T> (e.g. Faction, Town).
        public static readonly Type[] ParseStringSingleParamTypes = { typeof(string) };

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

        // Thread-safe: parse metadata is read from parallel callers (e.g. the Advanced Search workers),
        // not just the single-threaded command path.
        private static readonly ConcurrentDictionary<Type, bool> _isParsable = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsType(Type type, Type check) => check.IsAssignableFrom(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChar(Type t) => IsType(t, OfChar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(Type t) => IsType(t, OfString);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsText(Type t) => IsType(t, OfText);

        public static bool IsParsable(Type t) =>
            _isParsable.GetOrAdd(t, static type =>
            {
                foreach (var x in type.GetInterfaces())
                {
                    if (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IParsable<>))
                    {
                        return true;
                    }
                }

                return false;
            });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDecimal(Type t) => Array.IndexOf(DecimalTypes, t) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(Type t) => Array.IndexOf(NumericTypes, t) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntity(Type t) => OfEntity.IsAssignableFrom(t);

        private static readonly ConcurrentDictionary<Type, MethodInfo> _parseMethods = new();

        // A static string Parse method: the modern IParsable<T> Parse(string, IFormatProvider), or a
        // legacy RunUO Parse(string). Cached per type; null if the type has neither. (Span-based Parse
        // can't be reflection-invoked — a ReadOnlySpan can't be boxed into the args array — so the
        // string overloads are what we bind to.)
        public static MethodInfo GetParseMethod(Type t) =>
            _parseMethods.GetOrAdd(
                t,
                static type => type.GetMethod("Parse", ParseStringParamTypes)
                               ?? type.GetMethod("Parse", ParseStringSingleParamTypes)
            );

        public static object Parse(Type t, string value)
        {
            var method = GetParseMethod(t);
            if (method == null)
            {
                return null;
            }

            // Fresh args array per call — a shared static array would race across concurrent callers.
            // Arg shape depends on which overload we bound to (IParsable 2-arg vs legacy 1-arg).
            var args = method.GetParameters().Length == 2 ? new object[] { value, null } : new object[] { value };
            return method.Invoke(null, args);
        }

        // Parses directly into the concrete numeric type via INumber<T>.TryParse (the Type-dispatched
        // equivalent of a generic TryParse<T>). Returns the boxed value; false if the text doesn't fit
        // the type's range/format so the caller can fall through.
        private static bool TryParseNumeric(Type type, ReadOnlySpan<char> span, NumberStyles style, out object result)
        {
            if (type == OfInt && int.TryParse(span, style, null, out var i))
            {
                result = i;
                return true;
            }
            if (type == OfUInt && uint.TryParse(span, style, null, out var ui))
            {
                result = ui;
                return true;
            }
            if (type == OfLong && long.TryParse(span, style, null, out var l))
            {
                result = l;
                return true;
            }
            if (type == OfULong && ulong.TryParse(span, style, null, out var ul))
            {
                result = ul;
                return true;
            }
            if (type == OfShort && short.TryParse(span, style, null, out var s))
            {
                result = s;
                return true;
            }
            if (type == OfUShort && ushort.TryParse(span, style, null, out var us))
            {
                result = us;
                return true;
            }
            if (type == OfByte && byte.TryParse(span, style, null, out var b))
            {
                result = b;
                return true;
            }
            if (type == OfSByte && sbyte.TryParse(span, style, null, out var sb))
            {
                result = sb;
                return true;
            }

            result = null;
            return false;
        }

        // Do not use this in "Parse" methods, it may cause a stack overflow
        public static string TryParse(Type type, string value, out object constructed)
        {
            constructed = null;
            var isSerial = IsType(type, OfSerial);
            var isEntity = IsType(type, OfEntity);

            if (isSerial || isEntity) // mutate into int32
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

            if (IsType(type, OfBool))
            {
                if (bool.TryParse(value, out var parsed))
                {
                    constructed = parsed;
                    return null;
                }

                return "Not a valid boolean string.";
            }

            if (IsNumeric(type))
            {
                var span = value.AsSpan();
                var style = NumberStyles.Integer;
                if (span.StartsWithOrdinal("0x"))
                {
                    span = span[2..];
                    style = NumberStyles.HexNumber;
                }

                if (isEntity || isSerial)
                {
                    // Serial/entity properties were mutated to int above; a Serial is a uint, so parse
                    // the full 32-bit range as ulong and resolve.
                    if (ulong.TryParse(span, style, null, out var num))
                    {
                        constructed = isEntity ? World.FindEntity((Serial)num) : (Serial)num;
                        return null;
                    }
                }
                else if (TryParseNumeric(type, span, style, out constructed))
                {
                    // Parse the string directly into the target type via INumber<T>.TryParse — no
                    // Convert.ChangeType, and (unlike parse-as-ulong) signed and per-type ranges are honored.
                    return null;
                }

                // On parse failure, fall through to the Parse-method / Convert.ChangeType fallbacks below.
            }

            // IParsable<T> (Parse(string, IFormatProvider)) or a legacy RunUO Parse(string). Gating on
            // the discovered method rather than the IParsable interface keeps pre-IParsable types
            // (Faction, Town, ...) parseable for backwards compatibility.
            if (GetParseMethod(type) != null)
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
                return null;
            }
            catch
            {
                return "That is not properly formatted.";
            }
        }
    }
}
