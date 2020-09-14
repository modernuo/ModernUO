using System;

namespace Server.Items
{
    public class Candle : BaseEquipableLight
    {
        [Constructible]
        public Candle() : base(0xA28)
        {
            if (Burnout)
            {
                Duration = TimeSpan.FromMinutes(20);
            }
            else
            {
                Duration = TimeSpan.Zero;
            }

            Burning = false;
            Light = LightType.Circle150;
            Weight = 1.0;
        }

        public Candle(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => 0xA0F;
        public override int UnlitItemID => 0xA28;

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
