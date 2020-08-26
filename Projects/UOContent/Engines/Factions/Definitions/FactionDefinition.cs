namespace Server.Factions
{
    public class FactionDefinition
    {
        public FactionDefinition(
            int sort, int huePrimary, int hueSecondary, int hueJoin, int hueBroadcast, int warHorseBody,
            int warHorseItem, string friendlyName, string keyword, string abbreviation, TextDefinition name,
            TextDefinition propName, TextDefinition header, TextDefinition about, TextDefinition cityControl,
            TextDefinition sigilControl, TextDefinition signupName, TextDefinition factionStoneName,
            TextDefinition ownerLabel, TextDefinition guardIgnore, TextDefinition guardWarn, TextDefinition guardAttack,
            StrongholdDefinition stronghold, RankDefinition[] ranks, GuardDefinition[] guards
        )
        {
            Sort = sort;
            HuePrimary = huePrimary;
            HueSecondary = hueSecondary;
            HueJoin = hueJoin;
            HueBroadcast = hueBroadcast;
            WarHorseBody = warHorseBody;
            WarHorseItem = warHorseItem;
            FriendlyName = friendlyName;
            Keyword = keyword;
            Abbreviation = abbreviation;
            Name = name;
            PropName = propName;
            Header = header;
            About = about;
            CityControl = cityControl;
            SigilControl = sigilControl;
            SignupName = signupName;
            FactionStoneName = factionStoneName;
            OwnerLabel = ownerLabel;
            GuardIgnore = guardIgnore;
            GuardWarn = guardWarn;
            GuardAttack = guardAttack;
            Stronghold = stronghold;
            Ranks = ranks;
            Guards = guards;
        }

        public int Sort { get; }

        public int HuePrimary { get; }

        public int HueSecondary { get; }

        public int HueJoin { get; }

        public int HueBroadcast { get; }

        public int WarHorseBody { get; }

        public int WarHorseItem { get; }

        public string FriendlyName { get; }

        public string Keyword { get; }

        public string Abbreviation { get; }

        public TextDefinition Name { get; }

        public TextDefinition PropName { get; }

        public TextDefinition Header { get; }

        public TextDefinition About { get; }

        public TextDefinition CityControl { get; }

        public TextDefinition SigilControl { get; }

        public TextDefinition SignupName { get; }

        public TextDefinition FactionStoneName { get; }

        public TextDefinition OwnerLabel { get; }

        public TextDefinition GuardIgnore { get; }

        public TextDefinition GuardWarn { get; }

        public TextDefinition GuardAttack { get; }

        public StrongholdDefinition Stronghold { get; }

        public RankDefinition[] Ranks { get; }

        public GuardDefinition[] Guards { get; }
    }
}
