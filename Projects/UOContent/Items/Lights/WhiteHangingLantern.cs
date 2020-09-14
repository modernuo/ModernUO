using System;

namespace Server.Items
{
    [Flippable]
    public class WhiteHangingLantern : BaseLight
    {
        [Constructible]
        public WhiteHangingLantern() : base(0x24C6)
        {
            Movable = true;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.Circle300;
            Weight = 3.0;
        }

        public WhiteHangingLantern(Serial serial) : base(serial)
        {
        }

        public override int LitItemID
        {
            get
            {
                if (ItemID == 0x24C6)
                {
                    return 0x24C5;
                }

                return 0x24C7;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                if (ItemID == 0x24C5)
                {
                    return 0x24C6;
                }

                return 0x24C8;
            }
        }

        public void Flip()
        {
            Light = LightType.Circle300;

            ItemID = ItemID switch
            {
                0x24C6 => 0x24C8,
                0x24C5 => 0x24C7,
                0x24C8 => 0x24C6,
                0x24C7 => 0x24C5,
                _      => ItemID
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
