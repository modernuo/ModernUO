namespace Server.Engines.PartySystem
{
    public class PartyMemberInfo
    {
        public PartyMemberInfo(Mobile m)
        {
            Mobile = m;
            CanLoot = !Core.ML;
        }

        public Mobile Mobile { get; }

        public bool CanLoot { get; set; }
    }
}
