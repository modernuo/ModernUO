using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The band's fence: the one member of The Shadow who commits no crime, and keeps its goods and its errands.
///
/// <para>
/// <b>Patrick's order of 17.09.2026, night, and his word on it an hour later.</b> "Once there are two of them in the
/// guild let them take in a keeper, who looks after the band's goods and orders. The keeper is shy of every unlawful
/// act." And: "the Baron and the Captain cannot be keepers, but the Sage and the Architect can. If they are found in the
/// company of thieves — say within ten or fifteen tiles of sight — they become criminals, and taking them is worth
/// 5000gp. But they may buy the witness off, or kill it. A killed witness tells nobody; a witness that gets away tells."
/// </para>
///
/// <para>
/// <b>Known by his trade, not by a name in the store.</b> The band's roll is a list of names and nothing else, and a
/// shape it cannot read is a shape that loses the whole record; so the fence is simply the member whose class is a
/// Sage's or an Architect's. Neither robs, neither leads, both can walk into any town the red members cannot, and that
/// is the whole of why the band wants one.
/// </para>
/// </summary>
public static class BotFence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFence));

    /// <summary>Whether the band takes a fence at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How many thieves the band must hold before it takes one in.</summary>
    public static int LeastMembers { get; set; } = 2;

    /// <summary>How often the band looks for a fence to take in.</summary>
    public static int SeekEveryMs { get; set; } = 300000;

    /// <summary>
    /// How near a thief must stand to the fence for the two to be taken for company.
    ///
    /// <para>
    /// <b>Four, and it was twelve, and twelve is why the keeper lived in a cell.</b> Patrick's words were two
    /// distances, not one: "if they are found in the company of thieves — say within ten or fifteen tiles of sight".
    /// The sight is the witness's, and it is <see cref="SeenWithin"/>; the company is the pair, and two bots twelve
    /// tiles apart are not keeping company, they are in the same field. It mattered because of where this happens: the
    /// first telling that ever named its place, at 10:55:48 on 18.09.2026, read "seen at (1445, 1465)" — the
    /// population's own home, where sixty bots stand and every member of the band comes back to. At twelve tiles the
    /// keeper was in the company of thieves whenever it was at home at all.
    /// </para>
    /// </summary>
    public static int CompanyWithin { get; set; } = 4;

    /// <summary>
    /// How long the company must hold before a witness can make anything of it.
    ///
    /// <para>
    /// <b>Passing somebody is not keeping company with them.</b> Patrick's word was "found in the company of thieves",
    /// and the sweep read it as a single instant: one look, one member within reach, and the keeper was a criminal. Ten
    /// seconds — three looks at <see cref="WatchMs"/> — is the difference between two bots crossing the same square and
    /// two bots standing together, and it is also what gives a keeper any chance to walk away from one.
    /// </para>
    /// </summary>
    public static int DwellMs { get; set; } = 10000;

    /// <summary>
    /// How long after a boot the company rule holds its tongue.
    ///
    /// <para>
    /// <b>Three of the keeper's five exposures on 18.09.2026 happened within a minute of a shard starting.</b> At
    /// 15:16:36, thirty-six seconds after a boot: "Hale is wanted for keeping The Shadow's goods, told of by Yarrow,
    /// seen at (1480, 1554)". The population is raised afresh at every boot and set down together before any of it has
    /// walked anywhere, so the band stands in the crowd it lives among and a keeper is in the company of thieves by
    /// arithmetic rather than by anything it did. Two minutes is enough for the island to spread out; after that a
    /// sighting means what Patrick meant it to mean.
    /// </para>
    /// </summary>
    public static int SettleMs { get; set; } = 120000;

    private static long? _woke;

    /// <summary>How near a stranger must stand to see it.</summary>
    public static int SeenWithin { get; set; } = 12;

    /// <summary>How often the fence is looked over for witnesses.</summary>
    public static int WatchMs { get; set; } = 3000;

    /// <summary>The least a witness will be bought off with.</summary>
    public static int Hush { get; set; } = 500;

    /// <summary>
    /// The most a witness can ask for its silence, and so what the keeper carries to pay it with.
    ///
    /// <para>
    /// <b>The two are one number and had to be.</b> A witness asks half of what it is carrying, and a keeper banks
    /// everything above the population's float like every other bot — so the ask was unbounded above and the purse it
    /// was measured against was a hundred coins. Not one witness was bought off in the whole of 17–18.09.2026: the
    /// first of Patrick's three outcomes could not happen at all, and the fence went straight to the second, which it
    /// loses, and thence to an hour in a cell.
    /// </para>
    /// </summary>
    public static int Most { get; set; } = 1500;

    /// <summary>What the city puts on an exposed fence's head.</summary>
    public static int HeadPrice { get; set; } = 5000;

    /// <summary>
    /// How long a witness stays dealt with, once it has been bought or hunted.
    ///
    /// <para>
    /// <b>Without this the fence pays the same bot every three seconds.</b> The watch runs on the outlaws' own clock and
    /// a witness that took its money is still standing there on the next look, still not of the band, still with a
    /// thief beside it — so the whole purse would go to one bot in a minute, and the log would read as a hundred
    /// separate discoveries of the same thing.
    /// </para>
    /// </summary>
    public static int HushMs { get; set; } = 600000;

    /// <summary>Fences taken into the band.</summary>
    public static long Taken { get; private set; }

    /// <summary>Sweeps that wanted a fence and found nobody of the trade free to be one.</summary>
    public static long None { get; private set; }

    /// <summary>Times a stranger saw the fence in a thief's company.</summary>
    public static long Caught { get; private set; }

    /// <summary>Witnesses bought off, and what it cost.</summary>
    public static long Bribed { get; private set; }

    public static long BribeGold { get; private set; }

    /// <summary>How much stronger than the witness the fence must be to set on it rather than let it tell.</summary>
    public static double Odds { get; set; } = 1.2;

    /// <summary>Witnesses the fence set on rather than pay.</summary>
    public static long Hunted { get; private set; }

    /// <summary>Witnesses it could neither buy nor take, and let go.</summary>
    public static long Outmatched { get; private set; }

    /// <summary>Looks passed over because the shard had only just started and nothing had walked anywhere yet.</summary>
    public static long Settling { get; private set; }

    /// <summary>Members that came near the keeper and went again before the company could be made anything of.</summary>
    public static long Brushed { get; private set; }

    /// <summary>Witnesses it could not buy off because its pocket was light, and what they asked for in all.</summary>
    public static long Broke { get; private set; }

    public static long Asked { get; private set; }

    /// <summary>Coin the keeper drew out of the band's chest to keep witnesses quiet with.</summary>
    public static long Pocketed { get; private set; }

    /// <summary>Witnesses that told, and fences made wanted by it.</summary>
    public static long Told { get; private set; }

    public static long Exposed { get; private set; }

    /// <summary>The most of one kind the fence buys for the band in one errand.</summary>
    public static int Lot { get; set; } = 20;

    /// <summary>How many members' worth of kit the chest is stocked with. See <see cref="Wants"/>.</summary>
    public static int Spares { get; set; } = 2;

    /// <summary>What an errand on the band's order is claimed at against other work, a minute: the band's own price, as the stash and the run to market are.</summary>
    public static double Prior { get; set; } = 320.0;

    /// <summary>
    /// How much of the band's order the keeper gathers before it makes the trip out to the chest.
    ///
    /// <para>
    /// <b>The hideout is four hundred tiles from the shops, and that is the whole reason this number exists.</b> The
    /// first supply run ever made, at 11:34:54 on 18.09.2026, set out the moment twenty bandages were in the pack —
    /// and the press broke off a sewing errand that had already paid 33gp for its cloth. The band's order is seven
    /// kinds; seven such trips would spend the island's only Architect walking. Either enough things or enough
    /// different kinds is reason to go.
    /// </para>
    ///
    /// <para>
    /// <b>Twenty rather than forty, by measurement.</b> Forty was never reached: it was set down through the live door as 20 on the afternoon of 18.09.2026, the first delivery into the band's chest followed within minutes, and it had to be restored by hand after every one of four restarts that evening. A number that is corrected by hand at every boot is the wrong default.
    /// </para>
    /// </summary>
    public static int Load { get; set; } = 20;

    /// <summary>How many different kinds of the band's order are reason enough to go, however few of each. See <see cref="Load"/>.</summary>
    public static int Kinds { get; set; } = 3;

    /// <summary>
    /// Looks at which the band's order was the next thing the fence wanted.
    ///
    /// <para>
    /// <b>Offers, not errands.</b> This is counted inside <c>BotShopper.Wanting</c>, which answers "what does this bot
    /// want next" on every look the proposer takes; whether the trip is actually made is the auction's business. It
    /// read 20 in the first five minutes of build 137 — twenty looks, not twenty shopping trips — and a number named
    /// for the wrong thing is how a working gate gets read as a busy one.
    /// </para>
    /// </summary>
    public static long Offers { get; private set; }

    /// <summary>Things put into the chest for the band, and what they were.</summary>
    public static long Supplied { get; private set; }

    private static long _soughtTick;

    private static long _watchedTick;

    private static Serial? _company;

    private static long _companyTick;

    private static readonly Dictionary<Serial, long> _hushed = [];

    /// <summary>Whether this bot is the band's fence: of The Shadow, and of a class that keeps rather than robs.</summary>
    public static bool Is(Mobile m) =>
        Running && m is BotMobile { Deleted: false } bot && bot.Class is BotSage or BotArchitect && BotUnderworld.Member(bot);

    /// <summary>The band's fence, or null while it has none.</summary>
    public static BotMobile Who
    {
        get
        {
            if (!Running || !BotUnderworld.Exists)
            {
                return null;
            }

            var bots = BotPopulation.Bots;

            for (var i = 0; i < bots.Count; i++)
            {
                if (Is(bots[i]))
                {
                    return bots[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// One look for a fence to take in, and one look round the fence for witnesses. From the underworld's own beat.
    /// </summary>
    public static void Beat(long now)
    {
        if (!Running || !BotUnderworld.Exists)
        {
            return;
        }

        if (now - _soughtTick >= SeekEveryMs)
        {
            _soughtTick = now;

            // Asked here and not at the call, because counting the thieves walks the whole population to find the
            // fence among them, and this runs on the outlaws' second.
            if (BotUnderworld.Thieves >= LeastMembers && Who == null)
            {
                Seek();
            }
        }

        if (now - _watchedTick >= WatchMs)
        {
            _watchedTick = now;
            Watch();
            Ground();
        }
    }

    /// <summary>
    /// A keeper with a price on its head goes to the hideout and waits the price out. See <c>BotHoleUp</c>.
    /// </summary>
    private static void Ground()
    {
        if (Who is not { Deleted: false, Alive: true, Fallen: false } fence || fence.Map is not { } map
            || map == Map.Internal || fence.Squad != null)
        {
            return;
        }

        if (BotOutlaw.Jailed(fence) || !(BotOutlaw.IsWanted(fence) || BotOutlaw.IsRed(fence)))
        {
            return;
        }

        // Not in the middle of something that ends in the same place, and not over and over.
        if (fence.Resolve?.Deed is BotHoleUp or BotSilence or BotBrawl or BotSentence)
        {
            return;
        }

        if (BotUnderworld.Hideout == Point3D.Zero || BotLadder.Standing(fence) is not (BotStanding.Free or BotStanding.Busy))
        {
            return;
        }

        BotWill.Press(fence, new BotHoleUp(map, BotUnderworld.Hideout), "there is a price on its head");
    }

    /// <summary>Takes in the fittest Sage or Architect the island has: the richest in its own trade, and nobody's leader.</summary>
    private static void Seek()
    {
        var bots = BotPopulation.Bots;
        BotMobile best = null;
        var bestAt = -1.0;
        var trade = 0;
        var passed = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true } || bot.Class is not (BotSage or BotArchitect))
            {
                continue;
            }

            trade++;

            // <b>Counted, because a keeper that is never taken in looks exactly like a keeper nobody wanted.</b> The
            // band was founded at 23:59:05 on 17.09.2026 and the first two sweeps for a fence found nobody without
            // saying which gate did it.
            if (bot.Class.Leads || BotUnderworld.Member(bot) || BotOutlaw.Outlaw(bot) || BotOutlaw.Jailed(bot)
                || bot.Squad != null)
            {
                passed++;

                continue;
            }

            // The best at its own trade: a keeper of goods is worth having for what it can sell them for.
            var worth = bot.Class.MainSkill is { } skill ? bot.Skills[skill].Base : 0.0;

            if (worth <= bestAt)
            {
                continue;
            }

            best = bot;
            bestAt = worth;
        }

        if (best == null)
        {
            None++;

            logger.Information(
                "The Shadow wanted a fence and found none: {Trade} of the island's bots keep the right trade and {Passed} of those were spoken for",
                trade,
                passed
            );

            return;
        }

        if (!BotUnderworld.Take(best))
        {
            None++;

            logger.Information("The Shadow would have taken {Name} as its fence and the guild would not have it", best.Name);

            return;
        }

        Taken++;

        logger.Information(
            "The Shadow took in {Name} the {Class} as its fence, to keep the band's goods and errands",
            best.Name,
            best.Class.Name
        );
    }

    /// <summary>
    /// One look round the fence. A stranger within sight of it while a thief of the band stands as near is a witness:
    /// the fence buys it off if it can, and sets on it if it cannot.
    /// </summary>
    private static void Watch()
    {
        if (Who is not { Deleted: false, Alive: true } fence || fence.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        // Already a wanted bot: there is nothing left for a witness to tell.
        if (BotOutlaw.Outlaw(fence) || BotOutlaw.Jailed(fence) || fence.Hidden)
        {
            return;
        }

        // The island has not spread out yet. See SettleMs. Kept as the first tick seen rather than against nought,
        // because a tick count is only safe to compare by subtraction.
        var woke = Core.TickCount;

        _woke ??= woke;

        if (woke - _woke.Value < SettleMs)
        {
            Settling++;

            return;
        }

        BotMobile thief = null;

        foreach (var near in map.GetMobilesInRange<BotMobile>(fence.Location, CompanyWithin))
        {
            if (near != fence && near is { Deleted: false, Alive: true, Hidden: false } && BotUnderworld.Member(near)
                && !BotOutlaw.Jailed(near))
            {
                thief = near;

                break;
            }
        }

        // <b>A thief in a cell is not company.</b> Hale the fence was reported "seen with the band" at 00:08:24 on
        // 18.09.2026 with both of the band's thieves serving their hour, and the line did not say who had been standing
        // there — so it could not be told from a true discovery. The company must be at large, and is named.
        var now = Core.TickCount;

        if (thief == null)
        {
            _company = null;

            return;
        }

        // <b>The company must hold before a witness can make anything of it.</b> See DwellMs: one look at one member
        // within reach was enough, and at home that is every look there is.
        if (_company != thief.Serial)
        {
            _company = thief.Serial;
            _companyTick = now;
        }

        if (now - _companyTick < DwellMs)
        {
            Brushed++;

            return;
        }

        foreach (var near in map.GetMobilesInRange<BotMobile>(fence.Location, SeenWithin))
        {
            if (near == fence || near is not { Deleted: false, Alive: true, Hidden: false } witness
                || BotUnderworld.Member(witness) || !witness.InLOS(fence))
            {
                continue;
            }

            // Bought or hunted lately: this one has been dealt with. See HushMs.
            if (_hushed.TryGetValue(witness.Serial, out var dealt) && now - dealt < HushMs)
            {
                continue;
            }

            _hushed[witness.Serial] = now;
            Caught++;
            Seen(fence, witness, thief);

            return;
        }
    }

    /// <summary>The fence has been seen. It pays, or it sets on the witness; either way it is one bot's word away from a price on its head.</summary>
    private static void Seen(BotMobile fence, BotMobile witness, BotMobile thief)
    {
        var purse = fence.Backpack?.GetAmount(typeof(Gold)) ?? 0;
        var asks = Math.Clamp((witness.Backpack?.GetAmount(typeof(Gold)) ?? 0) / 2, Hush, Math.Max(Hush, Most));

        // <b>Out of the pocket first and the account for the rest</b>, which is how every other bill on this shard is
        // settled — the guild's dues, the horse, the levy on a hall. Asking the pocket alone was the whole reason no
        // witness was ever bought off: a seller here is paid by deposit and a bot banks everything over the float, so
        // the keeper's money is never where the bribe was looking for it. Build 137 gave it a pocket to carry and that
        // only helps once it has earned one; this is the part that works from the first minute of a shard.
        var fromPack = Math.Min(purse, asks);
        var fromBank = asks - fromPack;
        var took = fromPack <= 0 || fence.Backpack?.ConsumeTotal(typeof(Gold), fromPack) == true;

        if (took && fromBank > 0 && !Mobiles.Banker.Withdraw(fence, fromBank))
        {
            // The account would not cover it: what came out of the pocket goes back rather than out of the world.
            if (fromPack > 0)
            {
                fence.AddToBackpack(new Gold(fromPack));
            }

            took = false;
        }

        if (took)
        {
            witness.AddToBackpack(new Gold(asks));
            Bribed++;
            BribeGold += asks;

            BotVoice.Aloud(fence, $"Not a word of this, {witness.Name}. Here.");

            logger.Information(
                "{Name} the fence bought {Witness}'s silence for {Gold}gp at ({X}, {Y}), seen there with {Thief}",
                fence.Name,
                witness.Name,
                asks,
                fence.X,
                fence.Y,
                thief.Name
            );

            return;
        }

        if (purse < asks)
        {
            Broke++;
            Asked += asks;
        }

        // <b>It may kill the witness; it may not throw itself at one.</b> Patrick's word was "buy the witness off, or
        // kill it" — may, and a keeper of goods is a Sage or an Architect. Hale was exposed six times in the night of
        // 17–18.09.2026 and died in three of those chases, twice inside a second of starting one, and the witness told
        // anyway: a loop that costs the band its keeper and buys nothing. Weighed by what each can bring to a fight
        // now, as the law-abiding weigh a murderer (BotThreat.Now, build 122); outmatched, it lets the witness go and
        // takes the price on its head.
        if (BotThreat.Now(fence) < BotThreat.Now(witness) * Odds)
        {
            Outmatched++;

            Tell(fence, witness);

            return;
        }

        Hunted++;

        BotVoice.Aloud(fence, $"You saw nothing, {witness.Name}.");

        if (!BotWill.Press(fence, new BotSilence(witness), $"{witness.Name} saw it with {thief.Name}"))
        {
            Tell(fence, witness);
        }
    }

    /// <summary>The witness told: the fence is a criminal, and the city puts a price on it. From <see cref="BotSilence"/>.</summary>
    public static void Tell(BotMobile fence, Mobile witness)
    {
        if (fence is not { Deleted: false })
        {
            return;
        }

        Told++;

        if (!BotOutlaw.Want(fence, witness?.Name, $"keeping The Shadow's goods, told of by {witness?.Name ?? "somebody"}"))
        {
            return;
        }

        Exposed++;

        var priced = BotCity.Head(fence, HeadPrice, witness?.Name ?? "the city");

        logger.Information(
            "{Witness} told the Baron of {Name} the fence, seen at ({X}, {Y}): {Answer}",
            witness?.Name ?? "somebody",
            fence.Name,
            fence.X,
            fence.Y,
            priced
        );
    }

    /// <summary>
    /// The band's standing order: what its members' kit asks for, less what is already in the chest.
    ///
    /// <para>
    /// <b>Patrick's keeper "looks after the band's goods and orders", and only the goods half was built.</b> The goods
    /// go out through <c>BotFetch</c> and come back as coin; this is the other direction. Every counter and every
    /// shopkeeper on this island stands inside a guarded town, a red is turned back at the ward, and so a thief out of
    /// bandages, arrows or reagents simply stops working. The fence can walk in as itself, and it is the only member
    /// that can.
    /// </para>
    ///
    /// <para>
    /// <b>The order is read off the members rather than written down anywhere.</b> What a thief draws out of the chest
    /// for its own kit is <c>BotUnload.Keeps</c> — the list <c>BotFetch</c>'s kit mode already filters by — so the
    /// band's order is the sum of its members' kit less what the chest holds. A band that takes in an archer starts
    /// asking for arrows without anybody editing a list.
    /// </para>
    ///
    /// <para>
    /// Members in a cell count in, not out: since build 135 a catch takes everything a bandit has, so the member that
    /// will need the chest most is the one serving an hour for having been caught.
    /// </para>
    /// </summary>
    public static Dictionary<Type, int> Wants()
    {
        Dictionary<Type, int> want = [];

        if (!Running || !BotUnderworld.Exists)
        {
            return want;
        }

        var bots = BotPopulation.Bots;

        // <b>The most one member keeps, not the sum of what they all keep.</b> The chest is a larder against the day a
        // member is stripped at a cell door or cannot reach a town, and the sum made it a warehouse: with four members
        // the first order read "80 Bandage, 8 Scissors, 8 SkinningKnife, 8 Skillet" — eight skillets, for a band that
        // needs one to cook with. Spares says how many members' worth to hold.
        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || Is(bot) || !BotUnderworld.Member(bot))
            {
                continue;
            }

            foreach (var (kind, count) in BotUnload.Keeps(bot))
            {
                if (count <= 0 || count == int.MaxValue)
                {
                    continue;
                }

                want.TryGetValue(kind, out var asked);
                want[kind] = Math.Max(asked, count * Math.Max(1, Spares));
            }
        }

        if (BotLair.Chest is { } chest)
        {
            for (var i = 0; i < chest.Items.Count; i++)
            {
                if (chest.Items[i] is not { Deleted: false } item || !want.TryGetValue(item.GetType(), out var asked))
                {
                    continue;
                }

                want[item.GetType()] = asked - Math.Max(1, item.Amount);
            }
        }

        List<Type> filled = null;

        foreach (var (kind, left) in want)
        {
            if (left <= 0)
            {
                (filled ??= []).Add(kind);
            }
        }

        for (var i = 0; filled != null && i < filled.Count; i++)
        {
            want.Remove(filled[i]);
        }

        return want;
    }

    /// <summary>
    /// The kind the band is shortest of and how many of it to buy — <b>on top of what the keeper keeps for itself</b>.
    ///
    /// <para>
    /// <b>The first supply run ever made delivered nothing, and this is why.</b> At 11:34 on 18.09.2026 Hale bought
    /// twenty bandages on the band's order, rode four hundred tiles to the hideout and put down nothing: "finished
    /// stash: nothing worth putting down". The chest only ever gets a keeper's <em>surplus</em> — what it carries over
    /// its own kit, because a keeper that tips its own bandages into the chest is short of bandages and walks the same
    /// circle for ever — and an Architect's kit asks for twenty bandages. It had bought the band's order and then kept
    /// all of it, correctly, as its own.
    /// </para>
    ///
    /// <para>
    /// So the number to buy is what the chest wants plus what the bot keeps, less what it already holds; and a kind
    /// that works out at nothing to buy is passed over for the next one, which is what fills the pack with several
    /// kinds and makes one trip out of what would otherwise be seven.
    /// </para>
    /// </summary>
    public static bool Shortest(Mobile keeper, out Type wanted, out int amount)
    {
        wanted = null;
        amount = 0;

        var kept = keeper is IBotWilful will ? BotUnload.Keeps(will) : [];
        var held = Holding(keeper as BotMobile, null);
        var worst = 0;

        foreach (var (kind, left) in Wants())
        {
            if (left <= worst)
            {
                continue;
            }

            kept.TryGetValue(kind, out var mine);

            held.TryGetValue(kind, out var has);

            var buy = mine + Math.Min(left, Math.Max(1, Lot)) - has;

            if (buy <= 0)
            {
                continue;
            }

            wanted = kind;
            amount = buy;
            worst = left;
        }

        if (wanted == null)
        {
            return false;
        }

        Offers++;

        return true;
    }

    /// <summary>
    /// What the bot is holding of each kind that it could actually part with: unbound, in the pack, <b>summed across
    /// stacks</b>.
    ///
    /// <para>
    /// <b>A stack is not a kind, and reading one for the other is what kept the band's chest empty all of
    /// 18.09.2026.</b> Bound gear is dyed (<c>BotBinding.BoundHue</c>), and a dyed stack will not merge with a plain
    /// one, so a bot's bandages routinely sit in two or three piles. Both the walk-out test and the putting-down
    /// itself compared <em>one stack's</em> amount against the allowance for the <em>whole kind</em>: at 12:00 that
    /// day Hale carried "Bandage x50 (keeps 30, 20 surplus)" and neither would give a single one away, because the
    /// two piles of thirty and twenty are each no bigger than the allowance of thirty. Every other place on this shard
    /// that asks the question — <c>BotUnload</c>, the shopper, the pack instrument — sums by kind, and this is why.
    /// </para>
    /// </summary>
    public static Dictionary<Type, int> Holding(BotMobile bot, Dictionary<Type, int> kinds)
    {
        Dictionary<Type, int> held = [];

        if (bot?.Backpack is not { } pack)
        {
            return held;
        }

        for (var i = 0; i < pack.Items.Count; i++)
        {
            if (pack.Items[i] is not { Deleted: false, Movable: true } item || item is Gold
                || BotBinding.IsBound(item, bot.Bond) || kinds?.ContainsKey(item.GetType()) == false)
            {
                continue;
            }

            var kind = item.GetType();
            held[kind] = (held.TryGetValue(kind, out var many) ? many : 0) + Math.Max(1, item.Amount);
        }

        return held;
    }

    /// <summary>What the keeper is holding over its own kit, by kind: exactly what a run to the chest would put down.</summary>
    public static Dictionary<Type, int> Spare(BotMobile bot, Dictionary<Type, int> asked)
    {
        Dictionary<Type, int> spare = [];
        var kept = BotUnload.Keeps(bot);

        foreach (var (kind, many) in Holding(bot, asked))
        {
            kept.TryGetValue(kind, out var mine);

            if (many - mine > 0)
            {
                spare[kind] = many - mine;
            }
        }

        return spare;
    }

    /// <summary>
    /// Whether the fence is carrying enough of what the band asked for to be worth the walk out to the hideout, even
    /// with an empty purse. See <c>BotUnderworld.Stash</c> and <see cref="Load"/>.
    /// </summary>
    public static bool Carrying(BotMobile bot)
    {
        if (!Is(bot) || Wants() is not { Count: > 0 } asked)
        {
            return false;
        }

        var spare = Spare(bot, asked);

        if (spare.Count >= Math.Max(1, Kinds))
        {
            return true;
        }

        var things = 0;

        foreach (var (_, many) in spare)
        {
            things += many;
        }

        return things >= Math.Max(1, Load);
    }

    /// <summary>Errands actually built on the band's order, as against the looks counted by <see cref="Offers"/>.</summary>
    public static long Sent { get; private set; }

    /// <summary>One errand built on the band's order. See <c>BotShopper.Ordered</c>.</summary>
    public static void Sending() => Sent++;

    /// <summary>What a run to the chest put down for the band.</summary>
    public static void Stocked(int things)
    {
        if (things > 0)
        {
            Supplied += things;
        }
    }

    /// <summary>What the keeper drew out of the band's chest for its pocket. See <c>BotFetch</c>.</summary>
    public static void Drew(int gold)
    {
        if (gold > 0)
        {
            Pocketed += gold;
        }
    }

    public static string Describe() =>
        Who is { } fence
            ? $"The Shadow's fence is {fence.Name} the {fence.Class?.Name}: {Caught} times seen in the band's company ({Brushed} looks at company too brief to count, {Settling} in the first minutes of a boot), {Bribed} witnesses bought off for {BribeGold}gp, {Hunted} set on, {Outmatched} too strong to set on, {Told} told and {Exposed} prices put on its head; {Offers} looks wanting something for the band, {Sent} errands set out on it and {Supplied} things bought into the chest; {Broke} witnesses it could not afford (they asked {Asked}gp) and {Pocketed}gp drawn out of the chest to pay them with; {BotHoleUp.Describe()}"
            : $"The Shadow has no fence ({Taken} taken in so far, {None} sweeps found nobody of the trade free, {Caught} times one was seen with the band)";

    public static void Forget()
    {
        Taken = 0;
        None = 0;
        Caught = 0;
        Bribed = 0;
        BribeGold = 0;
        Hunted = 0;
        Outmatched = 0;
        Told = 0;
        Exposed = 0;
        Offers = 0;
        Sent = 0;
        Supplied = 0;
        Brushed = 0;
        Settling = 0;
        _company = null;
        BotHoleUp.Forget();
        Broke = 0;
        Asked = 0;
        Pocketed = 0;
        _hushed.Clear();
    }
}
