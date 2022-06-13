using System;

namespace Server.Items
{
    public class CrateForSledge : TransientItem
    {
        [Constructible]
        public CrateForSledge() : base(0x1FFF, TimeSpan.FromHours(1)) => LootType = LootType.Blessed;

        public CrateForSledge(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074520; // Crate for Sledge

        public override bool Nontransferable => true;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);
            AddQuestItemProperty(list);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
