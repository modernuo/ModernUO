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
}
