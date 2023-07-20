using System.Collections.Generic;

namespace Server;

public partial class Mobile
{
    // Migrating Stabled property to PlayerMobile
    public static Dictionary<Mobile, HashSet<Mobile>> StableMigrations { get; private set; }

    public static void AddToStabledMigration(Mobile m, HashSet<Mobile> stabled)
    {
        if (stabled?.Count > 0)
        {
            StableMigrations ??= new Dictionary<Mobile, HashSet<Mobile>>();
            StableMigrations[m] = stabled;
        }
    }

    // Migrating murders to the murder system
    public static Dictionary<Mobile, int> MurderMigrations { get; private set; }

    public static void AddToMurderMigration(Mobile m, int shortTermMurders)
    {
        if (shortTermMurders > 0)
        {
            MurderMigrations ??= new Dictionary<Mobile, int>();
            MurderMigrations[m] = shortTermMurders;
        }
    }

    // Migrating VirtueInfo to VirtueSystem
    public static Dictionary<Mobile, int[]> VirtueMigrations { get; private set; }

    public static void AddToVirtueMigration(Mobile m, int[] values)
    {
        if (values != null)
        {
            VirtueMigrations ??= new Dictionary<Mobile, int[]>();
            VirtueMigrations[m] = values;
        }
    }
}
