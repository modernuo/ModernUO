using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

public abstract class Poison : ISpanParsable<Poison>
{
    /*public abstract TimeSpan Interval{ get; }
      public abstract TimeSpan Duration{ get; }*/
    public abstract string Name { get; }
    public abstract int Level { get; }

    public static Poison Lesser => GetPoison("Lesser");
    public static Poison Regular => GetPoison("Regular");
    public static Poison Greater => GetPoison("Greater");
    public static Poison Deadly => GetPoison("Deadly");
    public static Poison Lethal => GetPoison("Lethal");

    public static List<Poison> Poisons { get; } = new();

    public abstract Timer ConstructTimer(Mobile m);
    /*public abstract void OnDamage( Mobile m, ref object state );*/

    public override string ToString() => Name;

    public static void Register(Poison reg)
    {
        var regName = reg.Name.ToLower();

        for (var i = 0; i < Poisons.Count; i++)
        {
            if (reg.Level == Poisons[i].Level)
            {
                throw new Exception("A poison with that level already exists.");
            }

            if (regName == Poisons[i].Name.ToLower())
            {
                throw new Exception("A poison with that name already exists.");
            }
        }

        Poisons.Add(reg);
    }

    public static Poison GetPoison(int level)
    {
        for (var i = 0; i < Poisons.Count; ++i)
        {
            var p = Poisons[i];

            if (p.Level == level)
            {
                return p;
            }
        }

        return null;
    }

    public static Poison GetPoison(ReadOnlySpan<char> name)
    {
        for (var i = 0; i < Poisons.Count; ++i)
        {
            var p = Poisons[i];

            if (name.InsensitiveEquals(p.Name))
            {
                return p;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Poison Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Poison result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Poison Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        if (int.TryParse(s, provider, out var pLevel))
        {
            var result = GetPoison(pLevel);
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
        if (int.TryParse(s, provider, out var pLevel))
        {
            result = GetPoison(pLevel);
            if (result != null)
            {
                return true;
            }
        }

        result = GetPoison(s.Trim());
        return result != null;
    }
}
