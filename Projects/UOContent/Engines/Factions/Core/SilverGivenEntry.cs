using System;

namespace Server.Factions
{
    public class SilverGivenEntry
    {
        public static readonly TimeSpan ExpirePeriod = TimeSpan.FromHours(3.0);

        public SilverGivenEntry(Mobile givenTo)
        {
            GivenTo = givenTo;
            TimeOfGift = DateTime.UtcNow;
        }

        public Mobile GivenTo { get; }

        public DateTime TimeOfGift { get; }

        public bool IsExpired => TimeOfGift + ExpirePeriod < DateTime.UtcNow;
    }
}
