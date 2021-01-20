using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Server
{
    public enum BodyType : byte
    {
        Empty,
        Monster,
        Sea,
        Animal,
        Human,
        Equipment
    }

    public readonly struct Body : IEquatable<object>, IEquatable<Body>, IEquatable<int>
    {
        private static readonly BodyType[] m_Types = Array.Empty<BodyType>();

        static Body()
        {
            if (File.Exists("Data/bodyTable.cfg"))
            {
                using var ip = new StreamReader("Data/bodyTable.cfg");
                m_Types = new BodyType[0x1000];

                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    if (line.Length == 0 || line.StartsWithOrdinal("#"))
                    {
                        continue;
                    }

                    var split = line.Split('\t');

                    if (int.TryParse(split[0], out var bodyID) && Enum.TryParse(split[1], true, out BodyType type) &&
                        bodyID >= 0 &&
                        bodyID < m_Types.Length)
                    {
                        m_Types[bodyID] = type;
                    }
                    else
                    {
                        Console.WriteLine("Warning: Invalid bodyTable entry:");
                        Console.WriteLine(line);
                    }
                }
            }
            else
            {
                Console.WriteLine("Warning: Data/bodyTable.cfg does not exist");
            }
        }

        public Body(int bodyID) => BodyID = bodyID;

        public BodyType Type => BodyID >= 0 && BodyID < m_Types.Length ? m_Types[BodyID] : BodyType.Empty;

        public bool IsHuman => BodyID >= 0
                               && BodyID < m_Types.Length
                               && m_Types[BodyID] == BodyType.Human
                               && BodyID != 402
                               && BodyID != 403
                               && BodyID != 607
                               && BodyID != 608
                               && BodyID != 694
                               && BodyID != 695
                               && BodyID != 970;

        public bool IsGargoyle => BodyID == 666
                                  || BodyID == 667
                                  || BodyID == 694
                                  || BodyID == 695;

        public bool IsMale => BodyID == 183
                              || BodyID == 185
                              || BodyID == 400
                              || BodyID == 402
                              || BodyID == 605
                              || BodyID == 607
                              || BodyID == 666
                              || BodyID == 694
                              || BodyID == 750;

        public bool IsFemale => BodyID == 184
                                || BodyID == 186
                                || BodyID == 401
                                || BodyID == 403
                                || BodyID == 606
                                || BodyID == 608
                                || BodyID == 667
                                || BodyID == 695
                                || BodyID == 751;

        public bool IsGhost => BodyID == 402
                               || BodyID == 403
                               || BodyID == 607
                               || BodyID == 608
                               || BodyID == 694
                               || BodyID == 695
                               || BodyID == 970;

        public bool IsMonster => BodyID >= 0
                                 && BodyID < m_Types.Length
                                 && m_Types[BodyID] == BodyType.Monster;

        public bool IsAnimal => BodyID >= 0
                                && BodyID < m_Types.Length
                                && m_Types[BodyID] == BodyType.Animal;

        public bool IsEmpty => BodyID >= 0
                               && BodyID < m_Types.Length
                               && m_Types[BodyID] == BodyType.Empty;

        public bool IsSea => BodyID >= 0
                             && BodyID < m_Types.Length
                             && m_Types[BodyID] == BodyType.Sea;

        public bool IsEquipment => BodyID >= 0
                                   && BodyID < m_Types.Length
                                   && m_Types[BodyID] == BodyType.Equipment;

        public int BodyID { get; }

        public static implicit operator int(Body a) => a.BodyID;

        public static implicit operator Body(int a) => new(a);

        public override string ToString() => $"0x{BodyID:X}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => BodyID.GetHashCode();

        public override bool Equals(object o) => o is Body b && b.BodyID == BodyID;

        public bool Equals(Body b) => b.BodyID == BodyID;

        public bool Equals(int number) => number == BodyID;

        public static bool operator ==(Body l, Body r) => l.BodyID == r.BodyID;

        public static bool operator !=(Body l, Body r) => l.BodyID != r.BodyID;

        public static bool operator >(Body l, Body r) => l.BodyID > r.BodyID;

        public static bool operator >=(Body l, Body r) => l.BodyID >= r.BodyID;

        public static bool operator <(Body l, Body r) => l.BodyID < r.BodyID;

        public static bool operator <=(Body l, Body r) => l.BodyID <= r.BodyID;
    }
}
