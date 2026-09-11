using System;
using System.Buffers;

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
}
