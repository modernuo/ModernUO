using System;

namespace Server.Network
{
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
                17 + (info.Crafter?.RawName?.Length ?? 0) +
                (info.Unidentified ? 4 : 0) + attrs.Length * 6
            );

            Stream.Write((short)0x10);
            Stream.Write(item.Serial);

            Stream.Write(info.Number);

            var name = info.Crafter?.RawName?.Trim() ?? "";

            if (name.Length > 0)
            {
                Stream.Write(-3);

                var length = name.Length;
                Stream.Write((ushort)length);
                Stream.WriteAsciiFixed(name, length);
            }

            if (info.Unidentified)
            {
                Stream.Write(-4);
            }

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
                {
                    hue = parent.SolidHueOverride;
                }
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
