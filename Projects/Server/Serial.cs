/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Serial.cs                                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

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

        public bool IsMobile
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value > 0 && Value < World.ItemOffset;
        }

        public bool IsItem
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value >= World.ItemOffset && Value < World.MaxItemSerial;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Serial other) => Value.CompareTo(other.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(uint other) => Value.CompareTo(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) =>
            obj switch
            {
                Serial serial => this == serial,
                uint u        => Value == u,
                _             => false
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Serial l, Serial r) => l.Value == r.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Serial l, Serial r) => l.Value != r.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Serial l, Serial r) => l.Value > r.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Serial l, Serial r) => l.Value < r.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Serial l, Serial r) => l.Value >= r.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Serial l, Serial r) => l.Value <= r.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"0x{Value:X8}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(Serial a) => a.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Serial(uint a) => new(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Serial other) => Value == other.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToInt32() => (int)Value;
    }
}
