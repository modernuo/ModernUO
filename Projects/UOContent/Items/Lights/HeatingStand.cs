using System;

namespace Server.Items
{
    public class HeatingStand : BaseLight
    {
        [Constructible]
        public HeatingStand() : base(0x1849)
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
            Light = LightType.Empty;
            Weight = 1.0;
        }

        public HeatingStand(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => 0x184A;
        public override int UnlitItemID => 0x1849;

        public override void Ignite()
        {
            base.Ignite();

            if (ItemID == LitItemID)
            {
                Light = LightType.Circle150;
            }
            else if (ItemID == UnlitItemID)
            {
                Light = LightType.Empty;
            }
        }

        public override void Douse()
        {
            base.Douse();

            if (ItemID == LitItemID)
            {
                Light = LightType.Circle150;
            }
            else if (ItemID == UnlitItemID)
            {
                Light = LightType.Empty;
            }
        }

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
