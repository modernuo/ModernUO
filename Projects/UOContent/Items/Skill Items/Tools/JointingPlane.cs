using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x1030, 0x1031)]
    public class JointingPlane : BaseTool
    {
        [Constructible]
        public JointingPlane() : base(0x1030) => Weight = 2.0;

        [Constructible]
        public JointingPlane(int uses) : base(uses, 0x1030) => Weight = 2.0;

        public JointingPlane(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Weight == 1.0)
            {
                Weight = 2.0;
            }
        }
    }
}
