using System;

namespace Server.Items
{
    [Flippable]
    public class ShojiLantern : BaseLight
    {
        [Constructible]
        public ShojiLantern() : base(0x24BC)
        {
            Movable = true;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.Circle150;
            Weight = 3.0;
        }

        public ShojiLantern(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => 0x24BB;
        public override int UnlitItemID => 0x24BC;

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
