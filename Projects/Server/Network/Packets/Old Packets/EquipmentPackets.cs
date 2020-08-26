/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: EquipmentPackets.cs - Created: 2020/05/07 - Updated: 2020/05/07 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server.Network
{
    public class EquipInfoAttribute
    {
        public EquipInfoAttribute(int number, int charges = -1)
        {
            Number = number;
            Charges = charges;
        }

        public int Number { get; }

        public int Charges { get; }
    }

    public class EquipmentInfo
    {
        public EquipmentInfo(int number, Mobile crafter, bool unidentified, EquipInfoAttribute[] attributes)
        {
            Number = number;
            Crafter = crafter;
            Unidentified = unidentified;
            Attributes = attributes;
        }

        public int Number { get; }

        public Mobile Crafter { get; }

        public bool Unidentified { get; }

        public EquipInfoAttribute[] Attributes { get; }
    }

    public sealed class DisplayEquipmentInfo : Packet
    {
        public DisplayEquipmentInfo(Item item, EquipmentInfo info) : base(0xBF)
        {
            var attrs = info.Attributes;

            EnsureCapacity(
                17 + (info.Crafter?.Name?.Length ?? 0) +
                (info.Unidentified ? 4 : 0) + attrs.Length * 6
            );

            Stream.Write((short)0x10);
            Stream.Write(item.Serial);

            Stream.Write(info.Number);

            if (info.Crafter != null)
            {
                var name = info.Crafter.Name;

                Stream.Write(-3);

                if (name == null)
                {
                    Stream.Write((ushort)0);
                }
                else
                {
                    var length = name.Length;
                    Stream.Write((ushort)length);
                    Stream.WriteAsciiFixed(name, length);
                }
            }

            if (info.Unidentified) Stream.Write(-4);

            for (var i = 0; i < attrs.Length; ++i)
            {
                Stream.Write(attrs[i].Number);
                Stream.Write((short)attrs[i].Charges);
            }

            Stream.Write(-1);
        }
    }

    public sealed class EquipUpdate : Packet
    {
        public EquipUpdate(Item item) : base(0x2E, 15)
        {
            Serial parentSerial;

            var parent = item.Parent as Mobile;
            var hue = item.Hue;

            if (parent != null)
            {
                parentSerial = parent.Serial;

                if (parent.SolidHueOverride >= 0)
                    hue = parent.SolidHueOverride;
            }
            else
            {
                Console.WriteLine("Warning: EquipUpdate on item with !(parent is Mobile)");
                parentSerial = Serial.Zero;
            }

            Stream.Write(item.Serial);
            Stream.Write((short)item.ItemID);
            Stream.Write((byte)0);
            Stream.Write((byte)item.Layer);
            Stream.Write(parentSerial);
            Stream.Write((short)hue);
        }
    }
}
