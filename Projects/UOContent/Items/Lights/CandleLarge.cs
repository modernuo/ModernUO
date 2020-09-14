using System;

namespace Server.Items
{
    public class CandleLarge : BaseLight
    {
        [Constructible]
        public CandleLarge() : base(0xA26)
        {
            if (Burnout)
            {
                Duration = TimeSpan.FromMinutes(25);
            }
            else
            {
                Duration = TimeSpan.Zero;
            }

            Burning = false;
            Light = LightType.Circle150;
            Weight = 2.0;
        }

        public CandleLarge(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => 0xB1A;
        public override int UnlitItemID => 0xA26;

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
