namespace Server.Factions
{
    public class TownDefinition
    {
        public TownDefinition(
            int sort, int sigilID, string region, string friendlyName, TextDefinition townName,
            TextDefinition townStoneHeader, TextDefinition strongholdMonolithName, TextDefinition townMonolithName,
            TextDefinition townStoneName, TextDefinition sigilName, TextDefinition corruptedSigilName, Point3D monolith,
            Point3D townStone
        )
        {
            Sort = sort;
            SigilID = sigilID;
            Region = region;
            FriendlyName = friendlyName;
            TownName = townName;
            TownStoneHeader = townStoneHeader;
            StrongholdMonolithName = strongholdMonolithName;
            TownMonolithName = townMonolithName;
            TownStoneName = townStoneName;
            SigilName = sigilName;
            CorruptedSigilName = corruptedSigilName;
            Monolith = monolith;
            TownStone = townStone;
        }

        public int Sort { get; }

        public int SigilID { get; }

        public string Region { get; }

        public string FriendlyName { get; }

        public TextDefinition TownName { get; }

        public TextDefinition TownStoneHeader { get; }

        public TextDefinition StrongholdMonolithName { get; }

        public TextDefinition TownMonolithName { get; }

        public TextDefinition TownStoneName { get; }

        public TextDefinition SigilName { get; }

        public TextDefinition CorruptedSigilName { get; }

        public Point3D Monolith { get; }

        public Point3D TownStone { get; }
    }
}
