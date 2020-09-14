using System;
using Server.Mobiles;

namespace Server.Items
{
    public class Torch : BaseEquipableLight
    {
        [Constructible]
        public Torch() : base(0xF6B)
        {
            if (Burnout)
            {
                Duration = TimeSpan.FromMinutes(30);
            }
            else
            {
                Duration = TimeSpan.Zero;
            }

            Burning = false;
            Light = LightType.Circle300;
            Weight = 1.0;
        }

        public Torch(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => 0xA12;
        public override int UnlitItemID => 0xF6B;

        public override int LitSound => 0x54;
        public override int UnlitSound => 0x4BB;

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile mobile && Burning)
            {
                MeerMage.StopEffect(mobile, true);
            }
        }

        public override void Ignite()
        {
            base.Ignite();

            if (Parent is Mobile mobile && Burning)
            {
                MeerMage.StopEffect(mobile, true);
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

            if (Weight == 2.0)
            {
                Weight = 1.0;
            }
        }
    }
}
