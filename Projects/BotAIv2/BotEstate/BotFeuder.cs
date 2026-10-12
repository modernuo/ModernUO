using System.Collections.Generic;
using Server.Logging;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a member of a guild at war somebody of the enemy to fight.
///
/// <para>
/// <b>The call comes first and the sighting second, and that order is the whole of "call your
/// guildmates".</b> A member asks what its guild is already on before it asks what it can see: if the guild
/// has a call standing and the enemy is within <c>BotFeud.Answer</c>, that is who it goes to, wherever it
/// happens to be looking. Only a bot whose guild is on nobody starts a quarrel of its own, and starting one
/// raises the call the others will answer. Without that ordering five members find five different enemies
/// and a war is five duels.
/// </para>
///
/// <para>
/// <b>Only classes that fight.</b> A tailor sent at an armed warrior is a tailor killed for nothing, and
/// the population pays for the resurrection and the lost afternoon. The class's own roll says whether it
/// carries a weapon; that is the same question <c>BotHarrow</c> asks of a company, asked here of one bot.
/// </para>
/// </summary>
public sealed class BotFeuder : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFeuder));

    /// <summary>How far a member looks for somebody of the enemy on its own account.</summary>
    public static int Watch { get; set; } = 20;

    /// <summary>
    /// The least health a bot will start a fight at, as a share of its own maximum.
    ///
    /// A bot that opens a quarrel at a tenth of its hits is a bot handing the enemy a kill and the guild a
    /// resurrection to pay for. Not a veto on defending itself — nothing here touches what happens when
    /// somebody swings first — only on choosing to begin one.
    /// </summary>
    public static double Fit { get; set; } = 0.5;

    /// <summary>Members sent to defend their own ground rather than to a quarrel they happened upon.</summary>
    public static long Defended { get; private set; }

    /// <summary>Bots looked at.</summary>
    public static long Asked { get; private set; }

    /// <summary>Quarrels offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times the bot's guild was at war with nobody.</summary>
    public static long Peaceful { get; private set; }

    /// <summary>Times it was at war and no enemy was within sight or call.</summary>
    public static long Unseen { get; private set; }

    /// <summary>Times the bot was in no condition to start one.</summary>
    public static long Unfit { get; private set; }

    /// <summary>Times the bot's trade was not fighting.</summary>
    public static long Unarmed { get; private set; }

    /// <summary>Members kept out of a fight on somebody else's ground for want of their supplies. See <see cref="BotProvision"/>.</summary>
    public static long Unsupplied { get; private set; }

    /// <summary>Times a guarded region stood between the two, which is most of Britain.</summary>
    public static long Sheltered { get; private set; }

    /// <summary>Members sent out after an enemy nobody had seen. See <c>BotFeud.Sortie</c>.</summary>
    public static long Sallied { get; private set; }

    /// <summary>Looks at a member back from rest on its guild's call, which answers the call from any distance.</summary>
    public static long Summoned { get; private set; }

    /// <summary>Looks at a member whose guild had fewer in its war than the score asks for, which answers from any distance too.</summary>
    public static long Mustered { get; private set; }

    public string Name => "feuder";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotFeud.Running || !BotRegard.Running || !BotRegard.Warring)
        {
            return null;
        }

        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map == null ||
            body.Map == Map.Internal)
        {
            return null;
        }

        if (body.Guild is not Guild ours)
        {
            return null;
        }

        // The Shadow keeps no house, no counter and no hired hands: its home is a fire and a chest far outside every
        // town, and a band with a signboard has stopped being secret. See BotUnderworld.Band.
        if (BotUnderworld.Band(ours))
        {
            return null;
        }

        Asked++;

        // <b>A bandit in sight is answered by whoever sees it, and nobody is called for it.</b> The Shadow is every guild's
        // enemy (Patrick's order of 26.09.2026, BotUnderworld.EnemyOfAll) but never a war: no ledger, no muster, no company
        // of fifteen raised at the seat for one thief. A fighter fit for it that can see one of the band — not one hiding —
        // within its watch, and is not the weaker of the two, sets on it alone. Asked of the band's own roll, five names,
        // before the cheap peace test below, which the band does not change.
        //
        // <b>Only a bandit standing where the fighter can get to it (07.10.2026, loops18).</b> On build 418's first 35 minutes all 19
        // quarrels taken were against two recruits of the band going about honest work - Lyrindel, mounted, carrying a scroll to
        // Nesta, and Ilyrana walking in a company - and none was finished: 15 failed "could not get nearer to Lyrindel at (...)" at a
        // new place each time, 2 "no quarrel any more", Brunhild died in six seconds closing with one in its company. A bandit
        // walking, in a company, or about steadfast work is not offered (not failed: not offered), nor one the reach ledger says
        // cannot be reached from here; each refusal counted by its cause (BanditsLeft).
        if (BotUnderworld.Near(body, Watch) is { } bandit)
        {
            var why = BanditLeft(bot, body, bandit);

            if (why == null)
            {
                Bandits++;
                Offered++;

                return new BotQuarrel(bandit, ours.Name);
            }

            _banditsLeft[why] = _banditsLeft.GetValueOrDefault(why) + 1;

            // A member of a company was never offered anything here (the quarrel went through Alone): unchanged.
            if (bot is IBotSquadMember { Squad: not null })
            {
                return null;
            }
        }

        // The cheapest question first, and it is false for nearly every bot on nearly every beat: this
        // whole proposer costs one guild lookup while the island is at peace.
        if (!AnyWar(ours))
        {
            Peaceful++;

            return null;
        }

        if (body.Hits < body.HitsMax * Fit)
        {
            Unfit++;

            return null;
        }

        var kit = bot.Class?.Kit;

        if (kit == null || kit.Melee.Count == 0 && kit.Ranged.Count == 0)
        {
            Unarmed++;

            return null;
        }

        // <b>Our own ground first, before the guild's call and before anything this bot can see.</b>
        // Patrick's order of 13.09.2026: an enemy on the yard is answered ahead of everything else, from
        // most of the island away and at a higher price. See BotFeud.Watch for how a threat is seen.
        var threat = BotFeud.Threat(ours);

        // <b>A member called back from its rest for this fight answers from wherever it came back.</b> Patrick's order of
        // 24.09.2026 (see BotRest.Calls): the first five The Blade called back at 09:52:29 came back at their own logout
        // tiles, out of reach of the guild's call, and took on cooking, an order and a harrowing. The reach below is how far
        // an ordinary member's attention runs; a called one came for nothing else.
        var summoned = string.Equals(BotRest.CalledBy(body), ours.Name, System.StringComparison.OrdinalIgnoreCase);

        if (summoned)
        {
            Summoned++;
        }

        // <b>And a member its guild draws into the war answers from anywhere above ground.</b> Patrick's order of
        // 24.09.2026: the lower the losing side's score, the more of its bots are drawn in (BotWar.Wanted). Reach was the
        // one thing that kept them out: the side losing fought with two to seven of its fifteen while the rest stood
        // further than BotFeud.Answer from the fight. Who is drawn is chosen once a look by the war (BotWar.Mustered) —
        // the nearest of the free, as many as are wanted — not by each bot for itself. Below ground is left out there.
        var mustered = !summoned && BotWar.Mustered(body);

        if (mustered)
        {
            Mustered++;
        }

        var anywhere = summoned || mustered;

        if (threat != null && threat != body && (anywhere || body.InRange(threat.Location, BotFeud.Defend)))
        {
            if (BotFeud.Quarrel(body, threat))
            {
                BotFeud.Stood();
                Defended++;
                Offered++;

                // As a company, by Patrick's order of 13.09.2026; alone only when no company can be had.
                return BotFeud.Rally(bot, ours, threat, true) ?? Alone(bot, new BotQuarrel(threat, ours.Name, true));
            }

            Sheltered++;
        }

        // <b>Past its own door, nobody goes without its supplies.</b> Patrick's order of 26.09.2026 (BotProvision). The
        // defence of its own ground, above, is answered whatever a bot carries.
        if (!BotProvision.Fit(body, out _))
        {
            Unsupplied++;

            return null;
        }

        // What the guild is already on, before what this bot can see. See the note at the top.
        var called = BotFeud.On(ours);

        if (called != null && called != body && (anywhere || body.InRange(called.Location, BotFeud.Answer)))
        {
            if (!BotFeud.Quarrel(body, called))
            {
                Sheltered++;

                return null;
            }

            BotFeud.Came();
            Offered++;

            return BotFeud.Rally(bot, ours, called, false) ?? Alone(bot, new BotQuarrel(called, ours.Name));
        }

        BotMobile enemy = null;
        var sheltered = false;

        foreach (var near in body.GetMobilesInRange<BotMobile>(Watch))
        {
            if (near == body || near.Deleted || !near.Alive || near.Guild is not Guild theirs || theirs == ours)
            {
                continue;
            }

            // The band is answered above, one bot at a time, and never with the war's call.
            if (theirs.Name == BotUnderworld.GuildName)
            {
                continue;
            }

            if (!BotRegard.AtWar(ours, theirs))
            {
                continue;
            }

            if (!BotFeud.Quarrel(body, near))
            {
                sheltered = true;

                continue;
            }

            enemy = near;

            break;
        }

        // <b>Nobody seen, nobody called, nobody on our ground, and the war has stood a while: go and find
        // them.</b> See BotFeud.Sortie. The call is raised on the enemy found so that whoever is near it
        // answers, and everybody else reaches this line on its own review and rallies to the same company.
        if (enemy == null && !sheltered && BotFeud.Sortie(ours))
        {
            var foe = BotFeud.Foe(ours, body.Location, body.Map);

            if (foe != null && BotFeud.Quarrel(body, foe))
            {
                BotFeud.Call(ours, foe);
                BotFeud.Sortied();
                Sallied++;
                Offered++;

                return BotFeud.Rally(bot, ours, foe, false) ?? Alone(bot, new BotQuarrel(foe, ours.Name));
            }
        }

        if (enemy == null)
        {
            // Told apart on purpose: "nobody of theirs was about" and "they were about and a town was
            // between us" are different facts, and a war fought entirely inside Britain would otherwise
            // report as a war nobody turned up to.
            if (sheltered)
            {
                Sheltered++;
            }
            else
            {
                Unseen++;
            }

            return null;
        }

        BotFeud.Call(ours, enemy);
        Offered++;

        return BotFeud.Rally(bot, ours, enemy, false) ?? Alone(bot, new BotQuarrel(enemy, ours.Name));
    }

    /// <summary>Whether this guild is at war with anybody at all. The engine holds the list.</summary>
    /// <summary>
    /// The quarrel a bot fights on its own account when no company will have it — unless it is in a company
    /// already, whose business it is. A member of a hunt is not offered a private war on top of the hunt.
    /// </summary>
    private static BotDeed Alone(IBotWilful bot, BotQuarrel quarrel) =>
        bot is IBotSquadMember { Squad: not null } ? null : quarrel;

    private static bool AnyWar(Guild ours)
    {
        if (ours == null)
        {
            return false;
        }

        // The Shadow on the engine's list is not a war: it has no page in the ledger on purpose (BotUnderworld.EnemyOfAll),
        // and counting it would put every guild on the island at war for good and wake this proposer on every beat.
        var engine = Warring(ours);
        var ledger = BotWar.Fighting(ours.Name) > 0;

        // <b>The two books disagreeing is the finding, not a detail.</b> At 04:31 on 14.09.2026 the ledger
        // held the Crown's war on the Hammer, read back off the save, and the engine listed no enemy for
        // either — so every review said "at war with nobody", 1,720 times in five minutes, while the war
        // stood. Counted here and said once a while, because the feud needs both: the ledger for the rules
        // and the engine for the blow to be lawful.
        if (engine != ledger)
        {
            Disagreeing++;

            var now = Core.TickCount;

            if (now - (_disagreedTick + DisagreeSayMs) >= 0)
            {
                _disagreedTick = now;

                logger.Warning(
                    "{Guild}: the ledger says {Ledger} and the engine says {Engine}; the engine lists {Count} enemies",
                    ours.Name,
                    ledger ? "at war" : "at peace",
                    engine ? "at war" : "at peace",
                    ours.Enemies?.Count ?? 0
                );
            }
        }

        return engine || ledger;
    }

    /// <summary>Enemies on the engine's list other than The Shadow.</summary>
    private static bool Warring(Guild ours)
    {
        var enemies = ours.Enemies;

        if (enemies == null)
        {
            return false;
        }

        for (var i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] is { } enemy && enemy.Name != BotUnderworld.GuildName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Why a fighter that sees a bandit is not offered the quarrel, or null when it is. In this order: its company, its health, the
    /// odds, the law (a guarded town, no lawful blow), the bandit's company, the bandit walking (unless already beside it), its
    /// steadfast work, and no way to it on the reach ledger. See <see cref="Propose"/>.
    /// </summary>
    private static string BanditLeft(IBotWilful bot, BotMobile body, BotMobile bandit)
    {
        if (bot is IBotSquadMember { Squad: not null })
        {
            return "the fighter is in a company";
        }

        if (body.Hits < body.HitsMax * Fit)
        {
            return "the fighter too hurt";
        }

        if (BotThreat.Now(body) < BotThreat.Power(bandit) * BanditOdds)
        {
            return "the bandit the stronger";
        }

        if (!BotFeud.Quarrel(body, bandit))
        {
            return "a guarded town or no lawful blow";
        }

        if (!BanditStands)
        {
            return null;
        }

        if (bandit.Squad != null)
        {
            return "the bandit in a company";
        }

        if (bandit.Journey.Moving && !body.InRange(bandit.Location, BanditBeside))
        {
            return "the bandit walking";
        }

        if (bandit.Resolve?.Deed is { Steadfast: true })
        {
            return "the bandit at steadfast work";
        }

        if (body.Map is { } map && BotReach.Ask(map, body.Location, bandit.Location, BotArrival.Within(BotQuarrel.Reach)) == BotReachVerdict.Sealed)
        {
            return "no way to the bandit";
        }

        return null;
    }

    /// <summary>Whether a bandit must be standing, out of a company and reachable to be set on (07.10.2026, loops18). Off: as before.</summary>
    public static bool BanditStands { get; set; } = true;

    /// <summary>How near a walking bandit must already be to be set on all the same: beside the fighter.</summary>
    public static int BanditBeside { get; set; } = 2;

    private static readonly Dictionary<string, long> _banditsLeft = new(System.StringComparer.Ordinal);

    private static string DescribeBanditsLeft()
    {
        if (_banditsLeft.Count == 0)
        {
            return "none";
        }

        var said = "";

        foreach (var (why, n) in _banditsLeft)
        {
            said += (said.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return said;
    }

    /// <summary>How much of a bandit's strength a fighter must have to set on it alone. Four fifths: a thief is a thief.</summary>
    public static double BanditOdds { get; set; } = 0.8;

    /// <summary>Bandits of The Shadow set on by a fighter who saw one.</summary>
    public static long Bandits { get; private set; }

    /// <summary>Reviews where the war ledger and the engine's enemy list disagreed. See <see cref="AnyWar"/>.</summary>
    public static long Disagreeing { get; private set; }

    private static long _disagreedTick;

    /// <summary>How often the disagreement is said aloud, in milliseconds.</summary>
    public static int DisagreeSayMs { get; set; } = 60000;

    public static string Describe() =>
        Asked == 0
            ? "nobody has been looked at for a quarrel"
            : $"the feuder looked {Asked} times and sent {Offered} ({Defended} of them to defend their own ground, {Bandits} against a bandit of The Shadow in sight; bandits in sight not set on: {DescribeBanditsLeft()}): {Peaceful} guilds were at war with nobody, "
            + $"{Unseen} saw no enemy within {Watch} tiles or the call, {Sallied} went out after one nobody had seen, {Summoned} looks at members called back from rest, {Mustered} at members drawn in from beyond their reach because their guild was behind in its war, {Sheltered} were kept apart by a town, "
            + $"{Unfit} were too hurt to start one, {Unarmed} carry no weapon, {Unsupplied} kept off somebody else's ground for want of supplies, {Disagreeing} found the ledger and the engine disagreeing about the war; {BotQuarrel.Describe()}; {BotFeud.Describe()}";

    public static void Forget()
    {
        _banditsLeft.Clear();
        Asked = 0;
        Offered = 0;
        Defended = 0;
        Sallied = 0;
        Summoned = 0;
        Mustered = 0;
        Disagreeing = 0;
        Peaceful = 0;
        Unseen = 0;
        Unfit = 0;
        Unsupplied = 0;
        Sheltered = 0;
        Unarmed = 0;
    }
}
