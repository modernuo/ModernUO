using Server.Engines.Craft;

namespace Server.Items
{
    public class RunicDovetailSaw : BaseRunicTool
    {
        [Constructible]
        public RunicDovetailSaw(CraftResource resource) : base(resource, 0x1028)
        {
            Weight = 2.0;
            Hue = CraftResources.GetHue(resource);
        }

        [Constructible]
        public RunicDovetailSaw(CraftResource resource, int uses) : base(resource, uses, 0x1028)
        {
            Weight = 2.0;
            Hue = CraftResources.GetHue(resource);
        }

        public RunicDovetailSaw(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

        public override int LabelNumber
        {
            get
            {
                var index = CraftResources.GetIndex(Resource);

                if (index >= 1 && index <= 6)
                {
                    return 1072633 + index;
                }

                return 1024137; // dovetail saw
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
