namespace Server.Factions
{
    public class TownMonolith : BaseMonolith
    {
        public TownMonolith(Town town = null) : base(town)
        {
        }

        public TownMonolith(Serial serial) : base(serial)
        {
        }

        public override int DefaultLabelNumber => 1041403; // A Faction Town Sigil Monolith

        public override void OnTownChanged()
        {
            AssignName(Town?.Definition.TownMonolithName);
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
