using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

public enum PoisonFamily { Standard, Darkglow, Parasitic }

public abstract class Poison : ISpanParsable<Poison>
{
    public static List<Poison> Poisons { get; } = [];
    public static Dictionary<string, Poison> PoisonsByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Poison(int index) => Index = index;

    public int Index { get; }
    public abstract string Name { get; }
    public abstract int Level { get; }
    public abstract PoisonFamily Family { get; }

    public abstract Timer ConstructTimer(Mobile m);

    public override string ToString() => Name;

    public static void Register(Poison reg)
    {
        var regName = reg.Name;

        for (var i = 0; i < Poisons.Count; i++)
        {
            var poison = Poisons[i];
            if (reg.Index == poison.Index)
            {
                throw new Exception("A poison with that index already exists.");
            }

            if (GetPoison(regName) != null)
            {
                throw new Exception("A poison with that name already exists.");
            }
        }

        Poisons.Add(reg);
        PoisonsByName.Add(regName, reg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison GetPoisonByIndex(int index) => index >= 0 && index < Poisons.Count ? Poisons[index] : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison IncreaseLevel(Poison oldPoison) =>
        oldPoison == null ? null : GetPoisonByIndex(oldPoison.Index + 1) ?? oldPoison;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison GetPoison(ReadOnlySpan<char> name) =>
        PoisonsByName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out var poison) ? poison : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Poison result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Poison Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        if (int.TryParse(s, provider, out var index))
        {
            var result = GetPoisonByIndex(index);
            if (result != null)
            {
                return result;
            }
        }

        var poison = GetPoison(s.Trim());
        if (poison == null)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        return poison;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Poison result)
    {
        if (int.TryParse(s, provider, out var index))
        {
            result = GetPoisonByIndex(index);
            if (result != null)
            {
                return true;
            }
        }

        result = GetPoison(s.Trim());
        return result != null;
    }
}
