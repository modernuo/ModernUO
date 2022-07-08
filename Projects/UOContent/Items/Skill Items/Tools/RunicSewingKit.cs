using Server.Engines.Craft;

namespace Server.Items
{
    public class RunicSewingKit : BaseRunicTool
    {
        [Constructible]
        public RunicSewingKit(CraftResource resource) : base(resource, 0xF9D)
        {
            Weight = 2.0;
            Hue = CraftResources.GetHue(resource);
        }

        [Constructible]
        public RunicSewingKit(CraftResource resource, int uses) : base(resource, uses, 0xF9D)
        {
            Weight = 2.0;
            Hue = CraftResources.GetHue(resource);
        }

        public RunicSewingKit(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefTailoring.CraftSystem;

        public override void AddNameProperty(IPropertyList list)
        {
            if (CraftResources.IsStandard(Resource))
            {
                list.Add(1061119, " "); // ~1_LEATHER_TYPE~ runic sewing kit
                return;
            }

            var num = CraftResources.GetLocalizationNumber(Resource);

            if (num > 0)
            {
                // ~1_LEATHER_TYPE~ runic sewing kit
                list.Add(1061119, $"#{num}");
            }
            else
            {
                // ~1_LEATHER_TYPE~ runic sewing kit
                list.Add(1061119, CraftResources.GetName(Resource));
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            var v = " ";

            if (!CraftResources.IsStandard(Resource))
            {
                var num = CraftResources.GetLocalizationNumber(Resource);

                if (num > 0)
                {
                    v = $"#{num}";
                }
                else
                {
                    v = CraftResources.GetName(Resource);
                }
            }

            LabelTo(from, 1061119, v); // ~1_LEATHER_TYPE~ runic sewing kit
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

            if (ItemID is 0x13E4 or 0x13E3)
            {
                ItemID = 0xF9D;
            }
        }
    }
}
