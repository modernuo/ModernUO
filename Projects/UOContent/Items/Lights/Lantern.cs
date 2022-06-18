using System;

namespace Server.Items
{
    public class Lantern : BaseEquipableLight
    {
        [Constructible]
        public Lantern() : base(0xA25)
        {
            Duration = Burnout ? TimeSpan.FromMinutes(20) : TimeSpan.Zero;

            Burning = false;
            Light = LightType.Circle300;
            Weight = 2.0;
        }

        public Lantern(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => ItemID is 0xA15 or 0xA17 ? ItemID : 0xA22;

        public override int UnlitItemID => ItemID == 0xA18 ? ItemID : 0xA25;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class LanternOfSouls : Lantern
    {
        [Constructible]
        public LanternOfSouls() => Hue = 0x482;

        public LanternOfSouls(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061618; // Lantern of Souls

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
