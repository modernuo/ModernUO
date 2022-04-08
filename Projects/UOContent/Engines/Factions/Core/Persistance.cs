using Server.Logging;

namespace Server.Factions
{
    public static class FactionPersistance
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(FactionPersistance));

        public static void Configure()
        {
            GenericPersistence.Register("Factions", Serialize, Deserialize);
        }

        public static void Serialize(IGenericWriter writer)
        {
            logger.Information("Saving Factions");

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

        public static void Deserialize(IGenericReader reader)
        {
            logger.Information("Loading Factions");

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

        private enum PersistedType
        {
            Terminator,
            Faction,
            Town
        }
    }
}
