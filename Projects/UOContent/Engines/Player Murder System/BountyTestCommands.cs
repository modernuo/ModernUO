using System;
using Server.Commands;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.PlayerMurderSystem;

public static class BountyTestCommands
{
    private static readonly string[] _maleNames =
    {
        "Aldric", "Blackthorn", "Cassius", "Drakken",
        "Edmund Graves", "Falkner", "Grimshaw", "Hadrian",
        "Ivan the Cruel", "Jorvik", "Kael Darkmore", "Lothar Vex",
        "Mordecai", "Nyx", "Osric", "Pike Morrow",
        "Quillan Thorne", "Roderick", "Sven", "Theron"
    };

    private static readonly string[] _femaleNames =
    {
        "Agatha", "Brynhild", "Circe Vex", "Dahlia",
        "Elara", "Freya", "Griselda", "Helena",
        "Isolde", "Jezebel"
    };

    private static readonly int[] _hairStyles =
    {
        0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2046, 0x2047, 0x2048, 0x2049, 0x204A
    };

    public static void Configure()
    {
        CommandSystem.Register("GenBounties", AccessLevel.Administrator, GenBounties_OnCommand);
        CommandSystem.Register("ClearBounties", AccessLevel.Administrator, ClearBounties_OnCommand);
        CommandSystem.Register("SetBounty", AccessLevel.Administrator, SetBounty_OnCommand);
    }

    [Usage("GenBounties [count]")]
    [Description("Generates test PlayerMobiles with random bounties for bounty board testing.")]
    public static void GenBounties_OnCommand(CommandEventArgs e)
    {
        var count = e.Length > 0 ? e.GetInt32(0) : 20;
        count = Math.Clamp(count, 1, 30);
        var from = e.Mobile;

        for (var i = 0; i < count; i++)
        {
            var female = Utility.RandomBool();
            var names = female ? _femaleNames : _maleNames;
            var name = names[Utility.Random(names.Length)];

            var pm = new PlayerMobile
            {
                Name = name,
                Body = female ? 0x191 : 0x190,
                Female = female,
                Hue = Race.Human.RandomSkinHue(),
                HairItemID = _hairStyles[Utility.Random(_hairStyles.Length)],
                HairHue = Race.Human.RandomHairHue()
            };

            pm.MoveToWorld(from.Location, from.Map);

            var kills = Utility.RandomMinMax(5, 50);
            pm.Kills = kills;

            // Create the murder context so AddBounty can find it
            var context = PlayerMurderSystem.GetOrCreateMurderContext(pm);
            context.LastMurderTime = Core.Now;
            var bounty = Utility.RandomMinMax(100, 10000);
            PlayerMurderSystem.AddBounty(pm, bounty);

            from.SendMessage($"  {pm.Name}: {kills} kills, {bounty}gp bounty");
        }

        from.SendMessage($"Generated {count} test bounty targets.");
    }

    [Usage("SetBounty")]
    [Description("Target a player to give them a random kill count (20-50) and bounty (20k-60k).")]
    public static void SetBounty_OnCommand(CommandEventArgs e)
    {
        e.Mobile.BeginTarget(-1, false, TargetFlags.None, SetBounty_OnTarget);
        e.Mobile.SendMessage("Target a player to apply a bounty.");
    }

    private static void SetBounty_OnTarget(Mobile from, object obj)
    {
        if (obj is not PlayerMobile pm)
        {
            from.SendMessage("That is not a player.");
            return;
        }

        var kills = Utility.RandomMinMax(20, 50);
        pm.Kills = kills;

        var context = PlayerMurderSystem.GetOrCreateMurderContext(pm);
        context.LastMurderTime = Core.Now;

        var bounty = Utility.RandomMinMax(20000, 60000);
        PlayerMurderSystem.AddBounty(pm, bounty);

        from.SendMessage($"{pm.Name}: {kills} kills, {bounty}gp bounty applied.");
    }

    [Usage("ClearBounties")]
    [Description("Deletes all test PlayerMobiles created by GenBounties (on Map.Internal with bounties).")]
    public static void ClearBounties_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var removed = 0;

        foreach (var (player, _) in PlayerMurderSystem.GetActiveBounties())
        {
            if (player.Map == Map.Internal && player.Account == null)
            {
                PlayerMurderSystem.ClearBounty(player);
                player.Delete();
                removed++;
            }
        }

        from.SendMessage($"Removed {removed} test bounty targets.");
    }
}
