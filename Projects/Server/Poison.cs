using System;
using System.Collections.Generic;

namespace Server
{
    [Parsable]
    public abstract class Poison
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

        public static Poison Parse(string value) =>
            (int.TryParse(value, out var plevel) ? GetPoison(plevel) : null) ?? GetPoison(value);

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

        public static Poison GetPoison(string name)
        {
            for (var i = 0; i < Poisons.Count; ++i)
            {
                var p = Poisons[i];

                if (Utility.InsensitiveCompare(p.Name, name) == 0)
                {
                    return p;
                }
            }

            return null;
        }
    }
}
