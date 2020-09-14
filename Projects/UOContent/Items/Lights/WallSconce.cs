using System;

namespace Server.Items
{
    [Flippable]
    public class WallSconce : BaseLight
    {
        [Constructible]
        public WallSconce() : base(0x9FB)
        {
            Movable = false;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.WestBig;
            Weight = 3.0;
        }

        public WallSconce(Serial serial) : base(serial)
        {
        }

        public override int LitItemID
        {
            get
            {
                if (ItemID == 0x9FB)
                {
                    return 0x9FD;
                }

                return 0xA02;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                if (ItemID == 0x9FD)
                {
                    return 0x9FB;
                }

                return 0xA00;
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
                0x9FB => 0xA00,
                0x9FD => 0xA02,
                0xA00 => 0x9FB,
                0xA02 => 0x9FD,
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
