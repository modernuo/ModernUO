/***************************************************************************
 *                                Serial.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;

namespace Server
{
    public readonly struct Serial : IComparable<Serial>, IComparable<uint>, IEquatable<Serial>
    {
        public static readonly Serial MinusOne = new Serial(0xFFFFFFFF);
        public static readonly Serial Zero = new Serial(0);

        public static Serial LastMobile { get; private set; } = Zero;

        public static Serial LastItem { get; private set; } = 0x40000000;

        public static Serial NewMobile
        {
            get
            {
                while (World.FindMobile(LastMobile += 1) != null)
                {
                }

                return LastMobile;
            }
        }

        public static Serial NewItem
        {
            get
            {
                while (World.FindItem(LastItem = LastItem + 1) != null)
                {
                }

                return LastItem;
            }
        }

        private Serial(uint serial) => Value = serial;

        public uint Value { get; }

        public bool IsMobile => Value > 0 && Value < 0x40000000;

        public bool IsItem => Value >= 0x40000000 && Value < 0x80000000;

        public bool IsValid => Value > 0;

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(Serial other) => Value.CompareTo(other.Value);

        public int CompareTo(uint other) => Value.CompareTo(other);

        public override bool Equals(object obj)
        {
            if (obj is Serial serial) return this == serial;

            if (obj is uint u) return Value == u;

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

        public static implicit operator Serial(uint a) => new Serial(a);

        public bool Equals(Serial other) => Value == other.Value;

        public int ToInt32() => (int)Value;
    }
}
