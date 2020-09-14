using System;

namespace Server.Items
{
    [Flippable]
    public class RedHangingLantern : BaseLight
    {
        [Constructible]
        public RedHangingLantern() : base(0x24C2)
        {
            Movable = true;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.Circle300;
            Weight = 3.0;
        }

        public RedHangingLantern(Serial serial) : base(serial)
        {
        }

        public override int LitItemID
        {
            get
            {
                if (ItemID == 0x24C2)
                {
                    return 0x24C1;
                }

                return 0x24C3;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                if (ItemID == 0x24C1)
                {
                    return 0x24C2;
                }

                return 0x24C4;
            }
        }

        public void Flip()
        {
            Light = LightType.Circle300;

            ItemID = ItemID switch
            {
                0x24C2 => 0x24C4,
                0x24C1 => 0x24C3,
                0x24C4 => 0x24C2,
                0x24C3 => 0x24C1,
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
