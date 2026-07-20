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
            return TryParseValue<long>(valuePart, out var parsedValue) &&
                   CompareNumeric((long)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(ulong))
        {
            return TryParseValue<ulong>(valuePart, out var parsedValue) &&
                   CompareNumeric((ulong)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(int))
        {
            return TryParseValue<int>(valuePart, out var parsedValue) &&
                   CompareNumeric((int)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(uint))
        {
            return TryParseValue<uint>(valuePart, out var parsedValue) &&
                   CompareNumeric((uint)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(short))
        {
            return TryParseValue<short>(valuePart, out var parsedValue) &&
                   CompareNumeric((short)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(ushort))
        {
            return TryParseValue<ushort>(valuePart, out var parsedValue) &&
                   CompareNumeric((ushort)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(sbyte))
        {
            return TryParseValue<sbyte>(valuePart, out var parsedValue) &&
                   CompareNumeric((sbyte)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(byte))
        {
            return TryParseValue<byte>(valuePart, out var parsedValue) &&
                   CompareNumeric((byte)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(float))
        {
            return TryParseValue<float>(valuePart, out var parsedValue) &&
                   Compare((float)propertyValue!, parsedValue, valuePart, operatorSpan);
        }
        if (propertyType == typeof(double))
        {
            return TryParseValue<double>(valuePart, out var parsedValue) &&
                   Compare((double)propertyValue!, parsedValue, valuePart, operatorSpan);
        }
        if (propertyType == typeof(string))
        {
            return TryParseValue<string>(valuePart, out var parsedValue) &&
                   Compare((string)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(TimeSpan))
        {
            return TryParseValue<TimeSpan>(valuePart, out var parsedValue) &&
                   Compare((TimeSpan)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(DateTime))
        {
            return TryParseValue<DateTime>(valuePart, out var parsedValue) &&
                   Compare((DateTime)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType == typeof(bool))
        {
            return TryParseValue<bool>(valuePart, out var parsedValue) &&
                   Compare((bool)propertyValue!, parsedValue, operatorSpan);
        }
        if (propertyType.IsEnum)
        {
            if (!Enum.TryParse(propertyType, valuePart.ToString(), true, out var valueEnum) || valueEnum == null)
            {
                return false;
            }

            return GetEnumSize(propertyType) switch
            {
                1 => CompareNumeric((byte)propertyValue!, (byte)valueEnum, operatorSpan),
                2 => CompareNumeric((short)propertyValue!, (short)valueEnum, operatorSpan),
                4 => CompareNumeric((int)propertyValue!, (int)valueEnum, operatorSpan),
                8 => CompareNumeric((long)propertyValue!, (long)valueEnum, operatorSpan),
                _ => false
            };
        }
        if (!propertyType.IsValueType)
        {
            return TryParseValue<object>(valuePart, out var parsedValue) &&
                   CompareReference(propertyValue!, parsedValue, operatorSpan);
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
        var epsilon = CalculateEpsilon(originalValue);

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
        var decimalPlace = value.IndexOf('.');

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

    public static bool CompareReference<T>(T propertyValue, T parsedValue, ReadOnlySpan<char> operatorSpan)
    {
        switch (operatorSpan)
        {
            case "=":
            case "==": return Equals(propertyValue, parsedValue);
            case "!":
            case "!=": return !Equals(propertyValue, parsedValue);
        }

        if (propertyValue is IComparable cmp && parsedValue != null)
        {
            try
            {
                var c = cmp.CompareTo(parsedValue);
                return operatorSpan switch
                {
                    ">"  => c > 0,
                    "<"  => c < 0,
                    ">=" => c >= 0,
                    "<=" => c <= 0,
                    _    => false
                };
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    internal static bool TryParseValue<T>(ReadOnlySpan<char> valuePart, out T value)
    {
        // Special handling for boolean and hexadecimal values
        if (typeof(T) == typeof(bool))
        {
            var val = valuePart.ToString().ToLower();
            if (val is "true" or "1" or "enabled" or "on")
            {
                value = (T)(object)true;
                return true;
            }

            if (val is "false" or "0" or "disabled" or "off")
            {
                value = (T)(object)false;
                return true;
            }

            value = default;
            return false;
        }

        if (typeof(T) == typeof(long))
        {
            return TryParseNumericValue<long, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(ulong))
        {
            return TryParseNumericValue<ulong, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(int))
        {
            return TryParseNumericValue<int, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(uint))
        {
            return TryParseNumericValue<uint, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(short))
        {
            return TryParseNumericValue<short, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(ushort))
        {
            return TryParseNumericValue<ushort, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(sbyte))
        {
            return TryParseNumericValue<sbyte, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(byte))
        {
            return TryParseNumericValue<byte, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(float))
        {
            return TryParseNumericValue<float, T>(valuePart, out value);
        }

        if (typeof(T) == typeof(double))
        {
            return TryParseNumericValue<double, T>(valuePart, out value);
        }

        // Default parsing for other types
        try
        {
            value = (T)Convert.ChangeType(valuePart.ToString(), typeof(T));
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseNumericValue<T, R>(ReadOnlySpan<char> valuePart, out R value) where T : INumber<T>
    {
        var ok = valuePart.StartsWith("0x")
            ? T.TryParse(valuePart[2..], NumberStyles.HexNumber, null, out var parsed)
            : T.TryParse(valuePart, null, out parsed);

        if (ok)
        {
            value = (R)(object)parsed;
            return true;
        }

        value = default;
        return false;
    }

    // OR ('|') binds looser than AND ('@'); split on the outermost OR first, then AND.
    internal static bool EvaluateBoolean(ReadOnlySpan<char> expr, Func<string, bool> evalLeaf)
    {
        var orIndex = expr.IndexOf('|');
        if (orIndex != -1)
        {
            return EvaluateBoolean(expr[..orIndex], evalLeaf) || EvaluateBoolean(expr[(orIndex + 1)..], evalLeaf);
        }

        var andIndex = expr.IndexOf('@');
        if (andIndex != -1)
        {
            return EvaluateBoolean(expr[..andIndex], evalLeaf) && EvaluateBoolean(expr[(andIndex + 1)..], evalLeaf);
        }

        return evalLeaf(expr.Trim().ToString());
    }

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
