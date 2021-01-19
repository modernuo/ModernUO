using System;
using System.Runtime.CompilerServices;

namespace Server
{
    public readonly struct Serial : IComparable<Serial>, IComparable<uint>, IEquatable<Serial>
    {
        public static readonly Serial MinusOne = new(0xFFFFFFFF);
        public static readonly Serial Zero = new(0);

        private Serial(uint serial) => Value = serial;

        public uint Value { get; }

        public bool IsMobile => Value > 0 && Value < World.ItemOffset;

        public bool IsItem => Value >= World.ItemOffset && Value < World.MaxItemSerial;

        public bool IsValid => Value > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(Serial other) => Value.CompareTo(other.Value);

        public int CompareTo(uint other) => Value.CompareTo(other);

        public override bool Equals(object obj)
        {
            if (obj is Serial serial)
            {
                return this == serial;
            }

            if (obj is uint u)
            {
                return Value == u;
            }

            return false;
        }

        public static bool operator ==(Serial l, Serial r) => l.Value == r.Value;

        public static bool operator !=(Serial l, Serial r) => l.Value != r.Value;

        public static bool operator >(Serial l, Serial r) => l.Value > r.Value;

        public static bool operator <(Serial l, Serial r) => l.Value < r.Value;

        public static bool operator >=(Serial l, Serial r) => l.Value >= r.Value;

        public static bool operator <=(Serial l, Serial r) => l.Value <= r.Value;

        public override string ToString() => $"0x{Value:X8}";

        public static implicit operator uint(Serial a) => a.Value;

        public static implicit operator Serial(uint a) => new(a);

        public bool Equals(Serial other) => Value == other.Value;

        public int ToInt32() => (int)Value;
    }
}
