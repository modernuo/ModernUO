using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Who on the island can write which circle, what a scroll no shelf sells may be asked for, which scribe is writing which want,
/// and the scribes' counts for the <c>Needs:</c> line.
///
/// <para>
/// <b>Measured before it (03.10.2026, build 395, Patrick: "the focus of tonight's work is 99 % fulfilment of Needs").</b> Scroll
/// lines failed in every session of 02.10: Greater Heal 0 met of 5, 4 and 2 lines, Lightning 0 of 9, Harm 1 of 6. The failure
/// named was "passed over: trader found nowhere to have it" (Lightning: passed over 1252 times, 1089 of them for that), which is
/// true and says nothing: no shelf sells a circle above the third, so only a bot scribe can answer one, and the scribes did not
/// write — "took on inscribe" 0 times in 85 minutes at 15:26, 0 in 60 at 16:51, once at 17:54. Meanwhile the wants climbed:
/// each new Greater Heal want opened at the dearest one standing (63, 82, 142gp) and raised four times over that, so Cirdan's
/// reached 326gp for a scroll whose paper and herbs cost 17gp, and at 03:12:25 on 03.10 its want for Raw Ribs stood down because
/// "329gp will not pay for one at 5gp".
/// </para>
///
/// <para>
/// <b>What a scroll may be asked for is what writing it costs here, and a fifth on top</b> (<see cref="Ceiling"/>): the paper and
/// herbs of one attempt at the island's lowest counter prices, over the success chance of the best scribe on the island who can
/// write the circle, with <see cref="BotQuill.Markup"/> — never below the shop step the want opens at. Raising past it buys
/// nothing: a scribe takes a paid scroll before practice whatever the price (<see cref="BotQuill.Plan"/>), so a louder offer only
/// holds more of the buyer's money away from its other lines. A circle nobody on the island can write is said once in the log and
/// its wants are held at the price they open at.
/// </para>
/// </summary>
public static class BotScribes
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotScribes));

    /// <summary>How long the census of scribes is kept before the population is read again.</summary>
    public static int CensusMs { get; set; } = 60000;

    /// <summary>What a blank scroll is reckoned at where no counter's price is known.</summary>
    public static int PaperGuess { get; set; } = 5;

    private const int Circles = 8;

    // ---- The circles, read off the shard's own inscription system --------------------------------------------------------------

    private static readonly double[] _min = new double[Circles + 1];

    private static readonly double[] _max = new double[Circles + 1];

    private static readonly int[] _mana = new int[Circles + 1];

    private static readonly Dictionary<Type, CraftItem> _recipes = [];

    /// <summary>The recipe that writes this scroll, or null.</summary>
    public static CraftItem Recipe(Type scroll)
    {
        Table();

        return scroll != null && _recipes.TryGetValue(scroll, out var recipe) ? recipe : null;
    }

    private static void Table()
    {
        if (_recipes.Count > 0 || BotQuill.System?.CraftItems is not { } recipes)
        {
            return;
        }

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            var spell = BotGrimoire.SpellOf(recipe?.ItemType);

            if (spell < 0)
            {
                continue;
            }

            var circle = BotGrimoire.Circle(spell);

            if (circle < 1 || circle > Circles)
            {
                continue;
            }

            _recipes[recipe.ItemType] = recipe;

            var skills = recipe.Skills;

            for (var s = 0; s < skills.Count; s++)
            {
                if (skills[s].SkillToMake == SkillName.Inscribe)
                {
                    _min[circle] = skills[s].MinSkill;
                    _max[circle] = skills[s].MaxSkill;
                }
            }

            _mana[circle] = recipe.Mana;
        }
    }

    /// <summary>The chance of the least able scribe the chooser lets try a circle: <see cref="BotQuill.Margin"/> over the band.</summary>
    private static double LeastChance(int circle) =>
        _max[circle] > _min[circle] ? Math.Clamp(BotQuill.Margin / (_max[circle] - _min[circle]), 0.01, 1.0) : 1.0;

    // ---- The census --------------------------------------------------------------------------------------------------------

    private static readonly int[] _able = new int[Circles + 1];

    private static readonly double[] _best = new double[Circles + 1];

    private static int _scanned;

    private static long _censusTick;

    private static bool _censused;

    /// <summary>
    /// Every bot in the world that writes — its class lists Inscribe, so it is born with a pen and buys another when one wears
    /// through, or it carries one — counted against each circle above the shelves that its Inscribe and its pool of mana let it
    /// write, with the best chance among them. The dead are counted: they rise where they fell. Bots resting out of the world
    /// (<see cref="BotPopulation.Away"/>, hours at a time outside prime time) are not: a price set by a sage asleep for the
    /// afternoon would be a price no scribe awake breaks even at.
    /// </summary>
    private static void Census()
    {
        var now = Core.TickCount;

        if (_censused && now - _censusTick < CensusMs)
        {
            return;
        }

        Table();

        if (_recipes.Count == 0)
        {
            return;
        }

        _censused = true;
        _censusTick = now;
        _scanned = 0;
        Array.Clear(_able);
        Array.Clear(_best);

        Count(BotPopulation.Bots);

        if (_scanned > 0 && !_capped)
        {
            _capped = true;
            CapStanding();
        }
    }

    private static bool _capped;

    /// <summary>
    /// The scroll wants standing when the population is first read — those read back from the save, said before any ceiling for
    /// them existed — given the price of writing them as their ceiling, and cut to it where they stand above it. Once a start.
    ///
    /// <para>
    /// <b>Cirdan's Greater Heal stood at 326gp across the restart of 03:08 on 03.10.2026</b>, its paper and herbs 17gp, holding its
    /// buyer's money away from its other lines (Raw Ribs stood down at 03:12:25: "329gp will not pay for one at 5gp"); Thora's
    /// at 247 and Osric's at 215. The anchor is kept, the line is not touched, nothing is paid or refunded: a want holds no gold,
    /// only a promise, and the promise is made smaller.
    /// </para>
    /// </summary>
    private static void CapStanding()
    {
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want.Ceiling > 0 || !Writ(want.Kind))
            {
                continue;
            }

            var writ = Ceiling(want.Kind, want.Offer, out _);

            if (writ <= 0)
            {
                continue;
            }

            if (want.Offer > writ)
            {
                Cut(want.Amount, want.Offer - writ);

                logger.Information(
                    "{Name}'s want for {Kind} stood at {Offer}gp and is cut to {Writ}gp, what writing one costs on the island with a fifth on top (want #{Id})",
                    want.Buyer?.Self?.Name ?? "somebody",
                    want.Label,
                    want.Offer,
                    writ,
                    want.Id
                );
            }

            want.CutTo(writ, writ);
        }
    }

    private static void Count(IReadOnlyList<BotMobile> bots)
    {
        var lowest = _min[BotGrimoire.ShopCircles + 1];

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot == null || bot.Deleted)
            {
                continue;
            }

            _scanned++;

            var skill = bot.Skills[SkillName.Inscribe].Value;

            if (skill - BotQuill.Margin < lowest || !Writes(bot))
            {
                continue;
            }

            for (var c = BotGrimoire.ShopCircles + 1; c <= Circles; c++)
            {
                if (_max[c] <= _min[c] || skill - BotQuill.Margin < _min[c] || bot.ManaMax < _mana[c])
                {
                    continue;
                }

                _able[c]++;

                var chance = Math.Clamp((skill - _min[c]) / (_max[c] - _min[c]), 0.0, 1.0);

                if (chance > _best[c])
                {
                    _best[c] = chance;
                }
            }
        }
    }

    private static bool Writes(BotMobile bot)
    {
        var skills = bot.Class?.Skills;

        if (skills != null)
        {
            for (var i = 0; i < skills.Count; i++)
            {
                if (skills[i].Skill == SkillName.Inscribe)
                {
                    return true;
                }
            }
        }

        return BotQuill.Pen(bot) != null;
    }

    // ---- What a scroll no shelf sells may be asked for ---------------------------------------------------------------------

    /// <summary>Scroll wants opened at the price of writing them instead of above it, and the gold over it they would have asked.</summary>
    public static long CappedAsks { get; private set; }

    public static long CappedGold { get; private set; }

    /// <summary>Scroll wants read back from the save above the price of writing them, and cut to it; the gold the cut released.</summary>
    public static long CutOnRestore { get; private set; }

    public static long CutGold { get; private set; }

    /// <summary>Raises a scroll want did not take for standing at the price of writing it (the want waited at its price).</summary>
    public static long HeldAtPrice { get; private set; }

    /// <summary>Scroll wants said for a circle nobody on the island can write: held at the price they opened at.</summary>
    public static long Beyond { get; private set; }

    private static readonly HashSet<Type> _saidBeyond = [];

    /// <summary>Whether this is a scroll that no shelf sells, so that only a bot scribe can answer a want for it.</summary>
    public static bool Writ(Type kind)
    {
        var spell = BotGrimoire.SpellOf(kind);

        return spell >= 0 && !BotGrimoire.Sold(spell);
    }

    /// <summary>
    /// The most a want for this scroll may ask, or nought for anything else (a scroll a shelf sells included: a trader's carriage
    /// from the counter is real work and keeps its own price). See the class summary. <paramref name="opening"/> is returned, and
    /// <paramref name="beyond"/> set, when nobody on the island can write it; when the population has not been read yet (a want
    /// read back at the start) the chance is the least a scribe is let try at, which is the dearest the price can honestly be.
    /// </summary>
    public static int Ceiling(Type kind, int opening, out bool beyond)
    {
        beyond = false;

        if (!Writ(kind) || Recipe(kind) is not { } recipe)
        {
            return 0;
        }

        var circle = BotGrimoire.Circle(BotGrimoire.SpellOf(kind));

        Census();

        double chance;

        if (_scanned == 0)
        {
            chance = LeastChance(circle);
        }
        else if (_able[circle] == 0)
        {
            beyond = true;

            return Math.Max(1, opening);
        }
        else
        {
            chance = Math.Max(LeastChance(circle), _best[circle]);
        }

        var fair = BotQuill.Asking((int)Math.Ceiling(Attempt(recipe) / chance));

        return Math.Max(BotGrimoire.ShopPrice(circle), fair);
    }

    /// <summary>The paper and the herbs of one attempt at this recipe, at the island's lowest counter prices.</summary>
    private static double Attempt(CraftItem recipe)
    {
        var paper = BotShops.Lowest(typeof(BlankScroll));
        var bill = (double)(paper > 0 ? paper : PaperGuess);
        var resources = recipe.Resources;

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType == null || res.ItemType == typeof(BlankScroll))
            {
                continue;
            }

            var herb = BotShops.Lowest(res.ItemType);

            bill += Math.Max(1, res.Amount) * (herb > 0 ? herb : BotQuill.HerbGuess);
        }

        return bill;
    }

    /// <summary>
    /// A want said for a scroll nobody on the island can write: counted, and the circle said once in the log. Called by
    /// <see cref="BotMarket.Ask"/> once the want stands.
    /// </summary>
    internal static void SaidBeyond(Type kind, int offer)
    {
        Beyond++;

        if (kind == null || !_saidBeyond.Add(kind))
        {
            return;
        }

        var circle = BotGrimoire.Circle(BotGrimoire.SpellOf(kind));

        logger.Information(
            "Nobody on the island can write {Kind}: circle {Circle} wants Inscribe {Need:F1} and a pool of {Mana} mana, and no bot that writes has both; its wants are held at the price they open at ({Offer}gp) and not raised",
            kind.Name,
            circle,
            _min[circle] + BotQuill.Margin,
            _mana[circle],
            offer
        );
    }

    /// <summary>A new scroll want opened at the price of writing it. Called by <see cref="BotMarket.Ask"/>.</summary>
    internal static void Capped(int units, int over)
    {
        CappedAsks++;
        CappedGold += (long)Math.Max(1, units) * Math.Max(0, over);
    }

    /// <summary>A want read back from the save above the price of writing it, cut to it.</summary>
    private static void Cut(int units, int over)
    {
        CutOnRestore++;
        CutGold += (long)Math.Max(0, units) * Math.Max(0, over);
    }

    /// <summary>A scroll want at the price of writing it, unanswered for a raise's time: it waits at its price.</summary>
    internal static void Held() => HeldAtPrice++;

    // ---- Which scribe is writing which want --------------------------------------------------------------------------------

    private static readonly Dictionary<int, IBotWilful> _claims = [];

    /// <summary>When the holder of a claim was first seen carrying the written scroll with no session in hand. See <see cref="ClaimedByOther"/>.</summary>
    private static readonly Dictionary<int, long> _carrying = [];

    /// <summary>A scribe has taken a session for this want. See <see cref="ClaimedByOther"/>.</summary>
    internal static void Claim(BotWant want, IBotWilful scribe)
    {
        if (want == null || scribe == null)
        {
            return;
        }

        if (_claims.Count > 1024)
        {
            _claims.Clear();
            _carrying.Clear();
        }

        _claims[want.Id] = scribe;
        _carrying.Remove(want.Id);
    }

    /// <summary>
    /// Whether another scribe is writing this want now: its session in hand, or put down for less than
    /// <see cref="BotWill.AsideCapMs"/>, is for this very want — or it has written it and carries it, over what it keeps, for the
    /// carrier's walk. One scroll is written for one want, rather than two scribes each buying the paper and the herbs for it and
    /// one of them carrying it to nobody. A claim whose scribe has moved on is dropped here.
    ///
    /// <para>
    /// <b>Not through a pause of any length (03.10.2026, the review of this build).</b> A scribe pressed into a company's call or
    /// a delve held the want for the whole of it, and the put-down work is dropped only when the pause ends: the claim now lapses
    /// at the same ten minutes the will gives work put down.
    /// </para>
    /// </summary>
    public static bool ClaimedByOther(BotWant want, IBotWilful scribe)
    {
        if (want == null || !_claims.TryGetValue(want.Id, out var holder) || ReferenceEquals(holder, scribe))
        {
            return false;
        }

        var resolve = holder?.Resolve;
        var deed = resolve?.Deed as BotInscribe;

        if (deed == null && resolve?.Paused is { Deed: BotInscribe paused } pause && Core.TickCount - pause.At < BotWill.AsideCapMs)
        {
            deed = paused;
        }

        if (holder?.Self is { Deleted: false })
        {
            if (deed != null && deed.Serves(want))
            {
                _carrying.Remove(want.Id);

                return true;
            }

            // Carrying the written scroll holds the want for the same ten minutes as a pause does, and no longer: a writer
            // pressed into a company or a delve straight after writing would otherwise sit on the line past its hour with the
            // scroll in its pack (03.10.2026, the re-review of this build).
            if (holder.Self.Alive && Carries(holder, want))
            {
                var now = Core.TickCount;

                if (!_carrying.TryGetValue(want.Id, out var since))
                {
                    _carrying[want.Id] = since = now;
                }

                if (now - since < BotWill.AsideCapMs)
                {
                    return true;
                }
            }
        }

        _claims.Remove(want.Id);
        _carrying.Remove(want.Id);

        return false;
    }

    /// <summary>Whether this bot carries enough of the want's kind over what it keeps to answer it: BotCourier.Holding without its count.</summary>
    private static bool Carries(IBotWilful bot, BotWant want)
    {
        var have = BotHandover.Carried(bot, want.Kind);

        if (have <= 0)
        {
            return false;
        }

        if (BotUnload.Keeps(bot).TryGetValue(want.Kind, out var allowed))
        {
            have = allowed >= int.MaxValue ? 0 : Math.Max(0, have - allowed);
        }

        return have >= want.Payable;
    }

    /// <summary>
    /// The want this scribe would write this scroll for: somebody else's, open, within a carrier's reach of it, and not being
    /// written by another scribe; a partner guild's under standing orders first, as <see cref="BotMarket.Demand"/> has it, then a
    /// bot's own need before a trader's procurement for one (the trader's want is outside the Needs share and only carries the
    /// scroll on to the line it was bought for), then the dearest. Null when it wants the kind itself, which is its own order.
    /// </summary>
    public static BotWant Order(IBotWilful scribe, Type kind)
    {
        if (kind == null || scribe?.Self == null || BotMarket.Wanted(scribe, kind) != null)
        {
            return null;
        }

        BotWant best = null;
        var bestServed = false;
        var bestNeed = false;
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want.Kind != kind || !want.IsOpen || ReferenceEquals(want.Buyer, scribe) || want.Buyer?.Self is not { Deleted: false } buyer)
            {
                continue;
            }

            if (ClaimedByOther(want, scribe))
            {
                Claimed++;

                continue;
            }

            if (!BotCourier.Answerable(scribe, want))
            {
                continue;
            }

            var served = BotPact.Serves(scribe.Self, buyer);
            var need = !BotTrading.Trader(buyer);

            if (best == null || served && !bestServed
                || served == bestServed && (need && !bestNeed || need == bestNeed && want.Offer > best.Offer))
            {
                best = want;
                bestServed = served;
                bestNeed = need;
            }
        }

        return best;
    }

    // ---- The scribes' counts, since the shard started ------------------------------------------------------------------------

    /// <summary>Bots with a pen asked what to write, and how each answer went. No bucket called "other".</summary>
    public static long Looks { get; private set; }

    public static long NoBook { get; private set; }

    public static long NoPaper { get; private set; }

    public static long Nothing { get; private set; }

    /// <summary>Sessions offered: for somebody's want (a line), for its own book or its own scrolls to throw, for practice.</summary>
    public static long ForLine { get; private set; }

    public static long ForOwn { get; private set; }

    public static long ForPractice { get; private set; }

    /// <summary>Of the sessions offered, those whose paper and herbs came to more than the scribe had free (the auction refuses them).</summary>
    public static long Short { get; private set; }

    /// <summary>Funded wants a scribe able to write them passed over, by why: another scribe on it, paying under what the scroll costs
    /// at this scribe's chance, a herb short that the paper's counter does not sell.</summary>
    public static long Claimed { get; private set; }

    public static long UnderCost { get; private set; }

    public static long NoHerb { get; private set; }

    /// <summary>Funded wants passed over because the paper and herbs for them came to more than the scribe had free.</summary>
    public static long Broke { get; private set; }

    /// <summary>Sessions taken, by what they were for.</summary>
    public static long TookLine { get; private set; }

    public static long TookOwn { get; private set; }

    public static long TookPractice { get; private set; }

    /// <summary>
    /// Scrolls written, by what they were for. For a want: only what the pack holds over the bot's own keep of the kind, which is
    /// what the carrier walks (the review of this build); a want is answered when one is handed over (<see cref="HandedByScribe"/>).
    /// </summary>
    public static long WroteLine { get; private set; }

    public static long WroteOwn { get; private set; }

    public static long WrotePractice { get; private set; }

    /// <summary>
    /// Scrolls no shelf sells handed over against wants, all routes, and of them by a bot that writes: the lines a scribe actually
    /// answered. Counted at the meeting (<see cref="BotMarket.Received"/>), not when the scroll was written.
    /// </summary>
    public static long Handed { get; private set; }

    public static long HandedByScribe { get; private set; }

    /// <summary>Funded wants a scribe passed over because it keeps every one of that kind for itself, so the carrier would walk none.</summary>
    public static long KeptWhole { get; private set; }

    /// <summary>Herbs bought at the paper's counter for a session written to order.</summary>
    public static long HerbsBought { get; private set; }

    /// <summary>
    /// Scrolls a bot left to its own pen rather than say a want for, because it writes them for less than the want would pay:
    /// counted once for each bot and kind, not on every look. See <see cref="BotQuill.OwnPen"/>.
    /// </summary>
    public static long OwnPen { get; private set; }

    private static readonly HashSet<(Serial, Type)> _leftToPen = [];

    internal static void Looked() => Looks++;

    internal static void Bookless() => NoBook++;

    internal static void Paperless() => NoPaper++;

    internal static void Idle() => Nothing++;

    internal static void Under() => UnderCost++;

    internal static void Herbless() => NoHerb++;

    internal static void Poor() => Broke++;

    internal static void Keeping() => KeptWhole++;

    /// <summary>Units handed over against a want. Called by <see cref="BotMarket.Received"/>; counts scrolls no shelf sells.</summary>
    internal static void Handover(BotWant want, int units, Mobile seller)
    {
        if (units <= 0 || !Writ(want?.Kind))
        {
            return;
        }

        Handed += units;

        if (seller is BotMobile scribe && Writes(scribe))
        {
            HandedByScribe += units;
        }
    }

    internal static void Offered(BotQuillPlan plan, bool shortOfIt)
    {
        if (plan?.For != null)
        {
            ForLine++;
        }
        else if (plan?.Own == true)
        {
            ForOwn++;
        }
        else
        {
            ForPractice++;
        }

        if (shortOfIt)
        {
            Short++;
        }
    }

    internal static void Took(BotQuillPlan plan)
    {
        if (plan?.For != null)
        {
            TookLine++;
        }
        else if (plan?.Own == true)
        {
            TookOwn++;
        }
        else
        {
            TookPractice++;
        }
    }

    internal static void Wrote(BotQuillPlan plan, int forLine, int scrolls)
    {
        if (scrolls <= 0)
        {
            return;
        }

        var line = Math.Clamp(forLine, 0, scrolls);

        WroteLine += line;

        if (plan?.Own == true)
        {
            WroteOwn += scrolls - line;
        }
        else
        {
            WrotePractice += scrolls - line;
        }
    }

    internal static void Bought(int herbs) => HerbsBought += Math.Max(0, herbs);

    internal static void LeftToPen(Mobile bot, Type kind)
    {
        if (bot == null || kind == null)
        {
            return;
        }

        if (_leftToPen.Count > 4096)
        {
            _leftToPen.Clear();
        }

        if (_leftToPen.Add((bot.Serial, kind)))
        {
            OwnPen++;
        }
    }

    /// <summary>The scribes' clause of the <c>Needs:</c> line.</summary>
    public static string Describe()
    {
        Census();

        List<string> census = [];

        for (var c = BotGrimoire.ShopCircles + 1; c <= Circles; c++)
        {
            census.Add(_able[c] == 0 ? $"circle {c} none" : $"circle {c} {_able[c]} (best {_best[c]:P0})");
        }

        return $"scribes able to write: {string.Join(", ", census)}; "
               + $"{Looks} looks by bots with a pen: {NoBook} with no book, {NoPaper} with no counter for paper, {Nothing} with nothing it could write, "
               + $"{ForLine} sessions offered for somebody's want, {ForOwn} for its own book or scrolls to throw, {ForPractice} for practice ({Short} of them dearer than what it had free); "
               + $"funded wants passed over by a scribe able to write them: {Claimed} for another scribe writing it, {UnderCost} for paying under its cost at that scribe's chance, {NoHerb} for a herb the paper's counter does not sell, {Broke} for paper and herbs dearer than the scribe had free, {KeptWhole} for a kind it keeps whole (all counted per look); "
               + $"sessions taken: {TookLine} for a want, {TookOwn} own, {TookPractice} practice; scrolls written: {WroteLine} for a want and over the writer's own keep, {WroteOwn} own, {WrotePractice} practice, {HerbsBought} herbs bought for them; {Handed} scrolls no shelf sells handed over against wants, {HandedByScribe} of them by a bot that writes; "
               + $"{OwnPen} scrolls (bot and kind) left to the asker's own pen rather than said as a want, for costing it less; "
               + $"scroll wants at the price of writing them: {CappedAsks} opened at it instead of above ({CappedGold}gp less asked), {CutOnRestore} cut to it when read back ({CutGold}gp), {HeldAtPrice} raises not taken at it, {Beyond} said for a circle nobody here can write";
    }

    /// <summary>Everything counted, claimed and said, forgotten on a world reload.</summary>
    public static void Forget()
    {
        _claims.Clear();
        _carrying.Clear();
        _saidBeyond.Clear();
        _leftToPen.Clear();
        _recipes.Clear();
        _censused = false;
        _capped = false;
        _scanned = 0;
        CappedAsks = 0;
        CappedGold = 0;
        CutOnRestore = 0;
        CutGold = 0;
        HeldAtPrice = 0;
        Beyond = 0;
        Looks = 0;
        NoBook = 0;
        NoPaper = 0;
        Nothing = 0;
        ForLine = 0;
        ForOwn = 0;
        ForPractice = 0;
        Short = 0;
        Claimed = 0;
        UnderCost = 0;
        NoHerb = 0;
        Broke = 0;
        TookLine = 0;
        TookOwn = 0;
        TookPractice = 0;
        WroteLine = 0;
        Handed = 0;
        HandedByScribe = 0;
        KeptWhole = 0;
        WroteOwn = 0;
        WrotePractice = 0;
        HerbsBought = 0;
        OwnPen = 0;
    }
}
