namespace Server.Factions
{
    public class FactionPersistance : Item
    {
        public FactionPersistance() : base(1)
        {
            Movable = false;

            if (Instance?.Deleted == true)
            {
                Instance = this;
            }
            else
            {
                base.Delete();
            }
        }

        public FactionPersistance(Serial serial) : base(serial) => Instance = this;

        public static FactionPersistance Instance { get; private set; }

        public override string DefaultName => "Faction Persistance - Internal";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            var factions = Faction.Factions;

            for (var i = 0; i < factions.Count; ++i)
            {
                writer.WriteEncodedInt((int)PersistedType.Faction);
                factions[i].State.Serialize(writer);
            }

            var towns = Town.Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                writer.WriteEncodedInt((int)PersistedType.Town);
                towns[i].State.Serialize(writer);
            }

            writer.WriteEncodedInt((int)PersistedType.Terminator);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        PersistedType type;

                        while ((type = (PersistedType)reader.ReadEncodedInt()) != PersistedType.Terminator)
                        {
                            switch (type)
                            {
                                case PersistedType.Faction:
                                    new FactionState(reader);
                                    break;
                                case PersistedType.Town:
                                    new TownState(reader);
                                    break;
                            }
                        }

                        break;
                    }
            }
        }

        public override void Delete()
        {
        }

        private enum PersistedType
        {
            Terminator,
            Faction,
            Town
        }
    }
}
