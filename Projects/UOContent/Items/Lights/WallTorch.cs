using System;

namespace Server.Items
{
    [Flippable]
    public class WallTorch : BaseLight
    {
        [Constructible]
        public WallTorch() : base(0xA05)
        {
            Movable = false;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.WestBig;
            Weight = 3.0;
        }

        public WallTorch(Serial serial) : base(serial)
        {
        }

        public override int LitItemID
        {
            get
            {
                if (ItemID == 0xA05)
                {
                    return 0xA07;
                }

                return 0xA0C;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                if (ItemID == 0xA07)
                {
                    return 0xA05;
                }

                return 0xA0A;
            }
        }

        public void Flip()
        {
            if (Light == LightType.WestBig)
            {
                Light = LightType.NorthBig;
            }
            else if (Light == LightType.NorthBig)
            {
                Light = LightType.WestBig;
            }

            ItemID = ItemID switch
            {
                0xA05 => 0xA0A,
                0xA07 => 0xA0C,
                0xA0A => 0xA05,
                0xA0C => 0xA07,
                _     => ItemID
            };
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
