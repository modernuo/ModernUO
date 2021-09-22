using System;

namespace Server.Saves
{
    public static class ArchiveSaves
    {
        public static Action<DateTime> Archive { get; set; }
        public static Action<DateTime> Prune { get; set; }
    }
}
