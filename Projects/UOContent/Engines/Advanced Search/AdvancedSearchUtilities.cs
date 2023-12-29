using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Server.Engines.AdvancedSearch;

public static class AdvancedSearchUtilities
{
    private static readonly SearchValues<char> _operators = SearchValues.Create(['=', '!', '>', '<', '~']);

    public static ReadOnlySpan<char> FindOperatorIndex(ReadOnlySpan<char> expression, out int index)
    {
        index = expression.IndexOfAny(_operators);
        if (index == -1)
        {
            return ReadOnlySpan<char>.Empty;
        }

        // We are at the end
        if (index + 1 == expression.Length)
        {
            return expression.Slice(index, 1);
        }

        // Look for double character
        // <=, >=, ~<, ~>, ~~, ~=, ~!
        var op = expression[index];
        var next = expression[index + 1];
        if (next is '=' && op is '=' or '<' or '>' or '~' or '!' || op is '~' && next is '<' or '>' or '~' or '!')
        {
            return expression.Slice(index, 2);
        }

        return expression.Slice(index, 1);
    }

    public static bool CompareValues(Type propertyType, object propertyValue, ReadOnlySpan<char> valuePart, ReadOnlySpan<char> operatorSpan)
    {
        // TODO: Add support for implicit conversion types like Serial -> uint

        if (propertyType == typeof(long))
        {
            var parsedValue = ParseValue<long>(valuePart);
            return CompareNumeric((long)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(ulong))
        {
            var parsedValue = ParseValue<ulong>(valuePart);
            return CompareNumeric((ulong)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(int))
        {
            var parsedValue = ParseValue<int>(valuePart);
            return CompareNumeric((int)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(uint))
        {
            var parsedValue = ParseValue<uint>(valuePart);
            return CompareNumeric((uint)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(short))
        {
            var parsedValue = ParseValue<short>(valuePart);
            return CompareNumeric((short)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(ushort))
        {
            var parsedValue = ParseValue<ushort>(valuePart);
            return CompareNumeric((ushort)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(sbyte))
        {
            var parsedValue = ParseValue<sbyte>(valuePart);
            return CompareNumeric((sbyte)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(byte))
        {
            var parsedValue = ParseValue<byte>(valuePart);
            return CompareNumeric((byte)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(float))
        {
            var parsedValue = ParseValue<float>(valuePart);
            return Compare((float)propertyValue!, parsedValue, valuePart, operatorSpan);
        }
        if (propertyType == typeof(double))
        {
            var parsedValue = ParseValue<double>(valuePart);
            return Compare((double)propertyValue!, parsedValue, valuePart, operatorSpan);
        }
        if (propertyType == typeof(string))
        {
            var parsedValue = ParseValue<string>(valuePart);
            return Compare((string)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(TimeSpan))
        {
            var parsedValue = ParseValue<TimeSpan>(valuePart);
            return Compare((TimeSpan)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(DateTime))
        {
            var parsedValue = ParseValue<DateTime>(valuePart);
            return Compare((DateTime)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(bool))
        {
            var parsedValue = ParseValue<bool>(valuePart);
            return Compare((bool)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType.IsEnum)
        {
            var valueEnum = Enum.Parse(propertyType, valuePart, false);

            return GetEnumSize(propertyType) switch
            {
                1 => CompareNumeric((byte)propertyValue!, (byte)valueEnum, operatorSpan),
                2 => CompareNumeric((short)propertyValue!, (short)valueEnum, operatorSpan),
                4 => CompareNumeric((int)propertyValue!, (int)valueEnum, operatorSpan),
                8 => CompareNumeric((long)propertyValue!, (long)valueEnum, operatorSpan),
            };
        }
        if (!propertyType.IsValueType)
        {
            var parsedValue = ParseValue<object>(valuePart);
            return CompareReference(propertyValue!, parsedValue, operatorSpan);
        }

        return false;
    }

    public static bool CompareNumeric<T>(T propertyValue, T parsedValue, ReadOnlySpan<char> operatorSpan) where T : INumber<T> =>
        operatorSpan switch
        {
            "=" or "==" => propertyValue == parsedValue,
            "!" or "!=" => propertyValue != parsedValue,
            ">"         => propertyValue > parsedValue,
            "<"         => propertyValue < parsedValue,
            ">="        => propertyValue >= parsedValue,
            "<="        => propertyValue <= parsedValue,
            _           => false
        };

    public static bool Compare(
        double propertyValue,
        double parsedValue,
        ReadOnlySpan<char> originalValue,
        ReadOnlySpan<char> operatorSpan
    )
    {
        double epsilon = CalculateEpsilon(originalValue);

        return operatorSpan switch
        {
            "=" or "==" => Math.Abs(propertyValue - parsedValue) < epsilon,
            "!" or "!=" => Math.Abs(propertyValue - parsedValue) >= epsilon,
            ">"         => propertyValue > parsedValue + epsilon,
            "<"         => propertyValue < parsedValue - epsilon,
            ">="        => propertyValue >= parsedValue - epsilon,
            "<="        => propertyValue <= parsedValue + epsilon,
            _           => throw new ArgumentException("Invalid operator")
        };
    }

    public static double CalculateEpsilon(ReadOnlySpan<char> value)
    {
        int decimalPlace = value.IndexOf('.');

        if (decimalPlace == -1)
        {
            // No decimal point, so use a default small epsilon
            return 1E-10;
        }

        // Convert decimal places to a negative power of 10
        return (value.Length - decimalPlace - 1) switch
        {
            < 10 => 1E-10,
            10   => 1E-11,
            11   => 1E-12,
            12   => 1E-13,
            13   => 1E-14,
            14   => 1E-15,
            _    => 1E-16
        };
    }

    public static bool Compare(string propertyValue, string parsedValue, ReadOnlySpan<char> operatorSpan) =>
        operatorSpan switch
        {
            "=" or "==" => propertyValue.EqualsOrdinal(parsedValue),
            "!" or "!=" => !propertyValue.EqualsOrdinal(parsedValue),
            ">"         => propertyValue.StartsWithOrdinal(parsedValue),
            "<"         => propertyValue.EndsWithOrdinal(parsedValue),
            "~"         => propertyValue.Contains(parsedValue),
            "~<"        => propertyValue.InsensitiveEndsWith(parsedValue),
            "~>"        => propertyValue.InsensitiveStartsWith(parsedValue),
            "~~"        => propertyValue.InsensitiveContains(parsedValue),
            "~="        => propertyValue.InsensitiveEquals(parsedValue),
            "~!"        => !propertyValue.InsensitiveEquals(parsedValue),
            _           => false
        };

    public static bool Compare(TimeSpan propertyValue, TimeSpan parsedValue, ReadOnlySpan<char> operatorSpan) =>
        operatorSpan switch
        {
            "=" or "==" => propertyValue == parsedValue,
            "!" or "!=" => propertyValue != parsedValue,
            ">"         => propertyValue > parsedValue,
            "<"         => propertyValue < parsedValue,
            ">="        => propertyValue >= parsedValue,
            "<="        => propertyValue <= parsedValue,
            _           => false
        };

    public static bool Compare(DateTime propertyValue, DateTime parsedValue, ReadOnlySpan<char> operatorSpan) =>
        operatorSpan switch
        {
            "=" or "==" => propertyValue == parsedValue,
            "!" or "!=" => propertyValue != parsedValue,
            ">"         => propertyValue > parsedValue,
            "<"         => propertyValue < parsedValue,
            ">="        => propertyValue >= parsedValue,
            "<="        => propertyValue <= parsedValue,
            _           => false
        };

    public static bool Compare(bool propertyValue, bool parsedValue, ReadOnlySpan<char> operatorSpan) =>
        operatorSpan switch
        {
            "=" or "==" => propertyValue == parsedValue,
            "!" or "!=" => propertyValue != parsedValue,
            _           => false
        };

    public static bool CompareReference<T>(T propertyValue, T parsedValue, ReadOnlySpan<char> operatorSpan) =>
        operatorSpan switch
        {
            "=" or "==" => propertyValue.Equals(parsedValue),
            "!" or "!=" => !propertyValue.Equals(parsedValue),
            ">"         => Comparer<T>.Default.Compare(propertyValue, parsedValue) > 0,
            "<"         => Comparer<T>.Default.Compare(propertyValue, parsedValue) < 0,
            ">="        => Comparer<T>.Default.Compare(propertyValue, parsedValue) >= 0,
            "<="        => Comparer<T>.Default.Compare(propertyValue, parsedValue) <= 0,
            _           => false
        };

    public static T ParseValue<T>(ReadOnlySpan<char> valuePart)
    {
        // Special handling for boolean and hexadecimal values
        if (typeof(T) == typeof(bool))
        {
            string val = valuePart.ToString().ToLower();
            if (val is "true" or "1" or "enabled" or "on")
            {
                return (T)(object)true;
            }

            if (val is "false" or "0" or "disabled" or "off")
            {
                return (T)(object)false;
            }
        }

        if (typeof(T) == typeof(long))
        {
            return ParseNumericValue<long, T>(valuePart);
        }

        if (typeof(T) == typeof(ulong))
        {
            return ParseNumericValue<ulong, T>(valuePart);
        }

        if (typeof(T) == typeof(int))
        {
            return ParseNumericValue<int, T>(valuePart);
        }

        if (typeof(T) == typeof(uint))
        {
            return ParseNumericValue<uint, T>(valuePart);
        }

        if (typeof(T) == typeof(short))
        {
            return ParseNumericValue<short, T>(valuePart);
        }

        if (typeof(T) == typeof(ushort))
        {
            return ParseNumericValue<ushort, T>(valuePart);
        }

        if (typeof(T) == typeof(sbyte))
        {
            return ParseNumericValue<sbyte, T>(valuePart);
        }

        if (typeof(T) == typeof(byte))
        {
            return ParseNumericValue<byte, T>(valuePart);
        }

        if (typeof(T) == typeof(float))
        {
            return ParseNumericValue<float, T>(valuePart);
        }

        if (typeof(T) == typeof(double))
        {
            return ParseNumericValue<double, T>(valuePart);
        }

        // Default parsing for other types
        return (T)Convert.ChangeType(valuePart.ToString(), typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static R ParseNumericValue<T, R>(ReadOnlySpan<char> valuePart) where T : INumber<T> =>
        valuePart.StartsWith("0x")
            ? (R)(object)T.Parse(valuePart[2..], NumberStyles.HexNumber, null)
            : (R)(object)T.Parse(valuePart, null);

    private static int GetEnumSize(Type enumType) =>
        Type.GetTypeCode(Enum.GetUnderlyingType(enumType)) switch
        {
            TypeCode.Byte or TypeCode.SByte   => sizeof(byte),
            TypeCode.Int16 or TypeCode.UInt16 => sizeof(ushort),
            TypeCode.Int32 or TypeCode.UInt32 => sizeof(uint),
            TypeCode.Int64 or TypeCode.UInt64 => sizeof(ulong),
            _                                 => 4
        };
}
