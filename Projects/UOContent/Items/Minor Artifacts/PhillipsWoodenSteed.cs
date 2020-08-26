namespace Server.Items
{
    public class PhillipsWoodenSteed : MonsterStatuette
    {
        [Constructible]
        public PhillipsWoodenSteed() : base(MonsterStatuetteType.PhillipsWoodenSteed) => LootType = LootType.Regular;

        public PhillipsWoodenSteed(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

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
