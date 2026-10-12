using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Dividing what a squad took.
///
/// <para>
/// <b>Evenly by worth, not by count.</b> The first version dealt items round-robin and called it fair enough
/// that nobody had grounds to complain — but one bot came away with a katana and another with a rotten skull,
/// and the gold went to whoever opened the corpse, in one pile. Two piles of "one item each" are not two
/// equal shares.
/// </para>
///
/// <para>
/// So: gold is cut by amount, and every other item goes to whoever has received the least worth so far,
/// heaviest item first. That last detail is what makes it work — handing out the valuable things first lets
/// the small ones even up the difference, and handing them out last cannot.
/// </para>
///
/// <para>
/// <b>Settled on the spot, and the dead get nothing.</b> The first version held the corpse untouched until
/// every fallen member had been resurrected, with the survivors standing over it — and standing still is what
/// killed six bots in a ring around a lich. A share-out that waits is a state in which the squad is not going
/// anywhere, and this design has no such states. It is a real loss to whoever died winning the fight, and it
/// is the cheaper of the two losses.
/// </para>
/// </summary>
public static class BotSpoils
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSpoils));

    /// <summary>
    /// What a thing is worth, when anything knows. Supplied by the economy once there is one.
    ///
    /// Left open rather than guessed at, and the fallback is honest: with no prices, everything is worth the
    /// same and the division becomes even by count — which is exactly the first version's rule, arrived at as
    /// a degenerate case rather than as a design.
    /// </summary>
    public static Func<Item, int> Worth { get; set; }

    /// <summary>
    /// How close a member has to be to be counted in. Somebody who watched the fight from the next field did
    /// not fight it.
    /// </summary>
    public static int Earshot { get; set; } = 12;

    public static long Shares { get; private set; }

    public static long Handed { get; private set; }

    public static long GoldSplit { get; private set; }

    /// <summary>Shares a class stood out of. See <see cref="Abstain"/>.</summary>
    public static long Abstained { get; private set; }

    /// <summary>
    /// Corpses left undivided because the only claimant was one who takes no share. A named nought: the
    /// alternative is a share-out that quietly destroys what it will not hand over.
    /// </summary>
    public static long Alone { get; private set; }

    public static void Reset()
    {
        Shares = 0;
        Handed = 0;
        GoldSplit = 0;
        Abstained = 0;
        Alone = 0;
    }

    public static string Describe() =>
        $"{Shares} corpses divided, {Handed} things handed over, {GoldSplit}gp split, {Abstained} shares stood out of, {Alone} corpses left because only somebody who takes no share was there";

    private static readonly List<IBotSquadMember> _claimants = [];

    private static readonly List<Item> _loot = [];

    private static readonly List<long> _given = [];

    /// <summary>Worth of the goods handed out by the share-out being settled. Scratch, like the lists above.</summary>
    private static long _shared;

    /// <summary>
    /// Empties the corpse into the squad. Returns how many things changed hands.
    /// </summary>
    public static int Share(BotSquad squad, IBotSquadMember collector, Container corpse)
    {
        if (squad == null || collector?.Self == null || corpse is not { Deleted: false })
        {
            return 0;
        }

        Gather(squad, collector);

        if (_claimants.Count == 0)
        {
            return 0;
        }

        Shares++;

        var gold = SplitGold(corpse);

        // The city's price on the kind's head, split between the claimants like the gold. See BotCity.Collect (02.10.2026, build 389).
        gold += BotCity.Collect((corpse as Corpse)?.Owner?.GetType(), _claimants);
        var handed = SplitGoods(corpse, collector);

        // What this company has been worth to the bots in it, kept on the company. See BotSquad.Won: it is
        // the only honest measure of work whose whole product is handed to somebody else.
        squad.Won += gold + _shared;

        // <b>And the guilds that stood in this line together think better of each other for it.</b> Patrick's
        // order of 13.09.2026: relations improve through joint expeditions. Every pair of guilds among the
        // claimants, once per corpse, whatever was on it - the standing together is the point, not the loot.
        Comrade();

        // <b>And plunder off a bot of a guild at war with the company's counts towards the war.</b> The
        // first war company of 13.09.2026 split 164gp and 447gp off two of the enemy and the war's plunder
        // read nought: only a lone bot's pickings were counted. See BotWar.Loot.
        if (corpse is Corpse { Owner: BotMobile { Guild: Guilds.Guild theirs } }
            && squad.Leader?.Self?.Guild is Guilds.Guild ours && ours != theirs)
        {
            BotWar.Looted(ours.Name, theirs.Name, gold + (int)Math.Min(int.MaxValue, _shared));
        }

        logger.Information(
            "Squad {Id} split {Count} things and {Gold}gp between {Claimants}",
            squad.Id,
            handed,
            gold,
            _claimants.Count
        );

        return handed;
    }

    /// <summary>Who is here to be paid: alive, near enough, and on the same floor.</summary>
    /// <summary>The distinct guilds among the claimants, for the friendship a shared corpse makes.</summary>
    private static readonly List<string> _guilds = [];

    private static void Comrade()
    {
        _guilds.Clear();

        for (var i = 0; i < _claimants.Count; i++)
        {
            if (_claimants[i]?.Self?.Guild is Guilds.Guild guild && !_guilds.Contains(guild.Name))
            {
                _guilds.Add(guild.Name);
            }
        }

        for (var i = 0; i < _guilds.Count; i++)
        {
            for (var j = i + 1; j < _guilds.Count; j++)
            {
                BotRegard.Comraded(_guilds[i], _guilds[j]);
            }
        }
    }

    private static void Gather(BotSquad squad, IBotSquadMember collector)
    {
        _claimants.Clear();

        var here = collector.Self.Location;
        var map = collector.Self.Map;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            var body = members[i].Self;

            if (body is not { Deleted: false, Alive: true } || body.Map != map)
            {
                continue;
            }

            // Height as well as distance. Every range check in the first version ignored it, and a member
            // three tiles away and twenty units up — on the roof of the crypt — counted as present at a fight
            // it could take no part in.
            if (Math.Abs(body.X - here.X) > Earshot
                || Math.Abs(body.Y - here.Y) > Earshot
                || Math.Abs(body.Z - here.Z) >= BotArrival.PersonHeight)
            {
                continue;
            }

            _claimants.Add(members[i]);
        }

        Abstain();
    }

    /// <summary>
    /// Takes out anybody whose class refuses a share.
    ///
    /// <para>
    /// <b>One class does, and it is what makes a company worth following.</b> A Baron calls five bots to
    /// ground that has killed people; nobody is obliged to come, and what is actually on offer is the
    /// contents of every corpse between here and the far corner divided five ways instead of six. He is paid
    /// out of a stipend that has nothing to do with what the fight drops — see <c>BotStipend</c> — so
    /// counting him in would be taking a sixth of the wage away from the only reason anybody came.
    /// </para>
    ///
    /// <para>
    /// <b>And it is never allowed to empty the list.</b> If he is the only one standing over the corpse the
    /// share-out does not happen at all: the goods stay where they are, whoever arrives next divides them,
    /// and the case is counted rather than silently dropping a corpse's worth of loot into nothing. A rule
    /// that quietly destroys what it refuses to hand out is worse than one that refuses out loud.
    /// </para>
    /// </summary>
    private static void Abstain()
    {
        var abstaining = 0;

        for (var i = 0; i < _claimants.Count; i++)
        {
            if (_claimants[i].Self is BotMobile { Class.Unpaid: true })
            {
                abstaining++;
            }
        }

        if (abstaining == 0)
        {
            return;
        }

        if (abstaining == _claimants.Count)
        {
            Alone++;
            _claimants.Clear();

            return;
        }

        for (var i = _claimants.Count - 1; i >= 0; i--)
        {
            if (_claimants[i].Self is BotMobile { Class.Unpaid: true })
            {
                _claimants.RemoveAt(i);
                Abstained++;
            }
        }
    }

    /// <summary>
    /// Gold cut by amount, the remainder to whoever is first. A pile handed whole to one bot is not a share.
    /// </summary>
    private static int SplitGold(Container corpse)
    {
        var total = 0;
        var items = corpse.Items;

        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] is not Gold coins)
            {
                continue;
            }

            total += coins.Amount;
            coins.Delete();
        }

        if (total <= 0)
        {
            return 0;
        }

        var each = total / _claimants.Count;
        var over = total - each * _claimants.Count;

        for (var i = 0; i < _claimants.Count; i++)
        {
            var amount = each + (i == 0 ? over : 0);

            if (amount <= 0)
            {
                continue;
            }

            _claimants[i].Self.Backpack?.DropItem(new Gold(amount));

            // A company's hunting pays the ground's holder as a lone hunter's does: a squad member settles no work of its
            // own, so without this every company hunted every guild's land for nothing. See BotToll.
            BotChest.Tithe(_claimants[i].Self, corpse.Map, corpse.GetWorldLocation(), amount);
        }

        GoldSplit += total;

        return total;
    }

    /// <summary>
    /// Everything else, biggest first, each to whoever has had least.
    ///
    /// Greedy, and deliberately so: it is one pass, it needs no lookahead, and on a handful of items it lands
    /// within one item's worth of the best possible split. Anything cleverer would be arithmetic nobody can
    /// check against a log line.
    /// </summary>
    private static int SplitGoods(Container corpse, IBotSquadMember collector)
    {
        _loot.Clear();

        // Before the empty-corpse return below, not after it: left until the sort, a corpse with no goods in
        // it would carry the previous share-out's figure into BotSquad.Won and count it twice.
        _shared = 0;

        var items = corpse.Items;

        for (var i = items.Count - 1; i >= 0; i--)
        {
            _loot.Add(items[i]);
        }

        if (_loot.Count == 0)
        {
            return 0;
        }

        _loot.Sort(static (a, b) => Price(b).CompareTo(Price(a)));

        _given.Clear();

        for (var i = 0; i < _claimants.Count; i++)
        {
            _given.Add(0);
        }

        var handed = 0;

        for (var i = 0; i < _loot.Count; i++)
        {
            var item = _loot[i];

            if (item.Deleted)
            {
                continue;
            }

            var poorest = 0;

            for (var c = 1; c < _given.Count; c++)
            {
                if (_given[c] < _given[poorest])
                {
                    poorest = c;
                }
            }

            var taker = _claimants[poorest];
            var pack = taker.Self.Backpack;

            if (pack == null)
            {
                continue;
            }

            corpse.RemoveItem(item);
            pack.DropItem(item);

            var price = Price(item);

            _given[poorest] += price;
            _shared += price;

            if (!ReferenceEquals(taker, collector))
            {
                handed++;
            }
        }

        Handed += handed;

        return handed;
    }

    /// <summary>What one thing is worth, or one if nothing knows.</summary>
    private static int Price(Item item)
    {
        if (item == null || item.Deleted)
        {
            return 0;
        }

        var worth = Worth;

        return worth == null ? 1 : Math.Max(1, worth(item));
    }
}
