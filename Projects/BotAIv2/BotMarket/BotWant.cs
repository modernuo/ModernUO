using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// One bot's spoken want: what it wants, how many, and what it says it will pay for one. Said aloud in the trade chat and
/// nothing more.
///
/// <para>
/// <b>Nothing is held, and that is the whole difference from the want it replaces (01.10.2026, Patrick's order of 30.09).</b>
/// The auction's want took the buyer's gold into escrow the moment it was posted and paid a supplier out of it from wherever
/// the supplier stood; the goods then waited in the market until the buyer's own beat fetched them. "Every order and every
/// purchase is hand to hand" leaves neither half of that standing. The gold stays in the buyer's pack and account until a
/// supplier is standing beside it with the goods (<see cref="BotHandover.Exchange"/>), and the goods stay in the supplier's
/// pack until then. So a want is a sentence with a price in it, and the only guarantee it needs is that the buyer could pay
/// when it said it — <see cref="BotMarket.Ask"/> asks the purse, and the market's beat drops a want whose buyer can no longer
/// pay for one.
/// </para>
///
/// <para>
/// <b>What is on its way is counted, so two carriers do not walk to one cook for the same five ribs.</b> A courier that takes
/// the delivery reserves the units it carries (<see cref="Reserve"/>); what is still open to another carrier is
/// <see cref="Payable"/>. The reservation goes with the delivery's end, however it ends.
/// </para>
///
/// <para>
/// The price moves the way the auction's want moved it: dearer when nobody answers for <see cref="BotMarket.RaiseMs"/>,
/// cheaper when answered again inside <see cref="BotMarket.BriskMs"/>, within ×<see cref="BotMarket.LeastMultiple"/> to
/// ×<see cref="BotMarket.MostMultiple"/> of what it opened at — and, since 03.10.2026, never past that multiple of what the
/// cheapest shopkeeper asks for one (<see cref="Shelf"/>, <see cref="Highest"/>).
/// </para>
/// </summary>
public sealed class BotWant
{
    /// <summary>
    /// Carriers bringing units, and the price each agreed for one: the want's own offer for a carrier's walk, a trader's carriage
    /// when its book wrote more (03.10.2026, build next). Nought is "at the offer".
    /// </summary>
    private readonly List<(IBotWilful Courier, int Units, int Price)> _coming = [];

    private readonly List<BotLine> _lines = [];

    /// <summary>
    /// Carriers whose reservation the sweep let go by the summed clock while they were put down (<see cref="BotDeliver.Away"/>,
    /// 03.10.2026, build next, review F3): their units are open to another carrier (<see cref="Payable"/>), and they are still
    /// counted as coming (<see cref="Coming"/>), as they were before the release. Dropped when they hold again, end, or pass the
    /// old single pause.
    /// </summary>
    private readonly List<(IBotWilful Courier, int Units)> _away = [];

    public BotWant(int id, IBotWilful buyer, Type kind, int units, int offer)
    {
        var look = Look(kind);

        Id = id;
        Buyer = buyer;
        Kind = kind;
        Label = look.Label;
        ItemId = look.ItemId;
        Hue = look.Hue;
        Amount = Math.Max(1, units);
        Offer = Math.Max(1, offer);
        Anchor = Offer;
        Opened = Offer;
        PostedTick = Core.TickCount;
        NearTick = PostedTick;
        TouchedTick = PostedTick;
        FilledTick = PostedTick;
        DiedTick = PostedTick;
        RoseTick = PostedTick;
    }

    /// <summary>
    /// A want read back from the save (02.10.2026, build 391): its clocks set back by the ages it was saved with, its lines made
    /// again by the caller. Nothing a carrier reserved comes back, because no carrier's delivery does. See <see cref="BotMarketStore"/>.
    /// </summary>
    internal BotWant(int id, IBotWilful buyer, Type kind, int amount, int offer, int anchor, long postedTick, long touchedTick)
    {
        var look = Look(kind);

        Id = id;
        Buyer = buyer;
        Kind = kind;
        Label = look.Label;
        ItemId = look.ItemId;
        Hue = look.Hue;
        Amount = Math.Max(0, amount);
        Offer = Math.Max(1, offer);
        Anchor = Math.Max(1, anchor);
        Opened = Anchor;
        PostedTick = postedTick;
        NearTick = postedTick;
        TouchedTick = touchedTick;
        FilledTick = touchedTick;
        DiedTick = touchedTick;
        RoseTick = touchedTick;
    }

    /// <summary>What was handed over against it before the restart, read back with it.</summary>
    internal void Restore(int filled, int paid, int raises, int cuts, bool traded, long filledTick)
    {
        Filled = Math.Max(0, filled);
        Paid = Math.Max(0, paid);
        Raises = Math.Max(0, raises);
        Cuts = Math.Max(0, cuts);
        Traded = traded;
        FilledTick = filledTick;
    }

    // ---- Its lines, and what a failure needs to name its cause (02.10.2026, build 391). See BotLine and BotLines. ----------

    /// <summary>Its lines, oldest first. Units handed over fill them in this order.</summary>
    public IReadOnlyList<BotLine> Lines => _lines;

    /// <summary>The newest line, or null.</summary>
    public BotLine Newest => _lines.Count > 0 ? _lines[^1] : null;

    internal BotLine OpenLine(int units, long now, bool procurement)
    {
        var line = new BotLine(this, (Newest?.Seq ?? 0) + 1, units, now, procurement);

        _lines.Add(line);

        return line;
    }

    internal void AddLine(BotLine line)
    {
        if (line != null)
        {
            _lines.Add(line);
        }
    }

    private readonly int[] _passes = new int[BotLines.PassKinds];

    /// <summary>
    /// An able bot looked at this want and passed it over, for this reason: a carrier holding the goods, a maker with the recipe,
    /// a trader with a source. Counted per look, so a want many bots walk past reads high; the reason with the most looks names a
    /// failure (<see cref="BotLines"/>). One increment, no allocation.
    /// </summary>
    public void Passed(BotPass why) => _passes[(int)why]++;

    public int PassCount(int kind) => kind >= 0 && kind < _passes.Length ? _passes[kind] : 0;

    /// <summary>Work offered for it to a bot's auction: a carrier's walk, a trader's haul, a maker's order.</summary>
    public int Offers { get; private set; }

    /// <summary>
    /// Offered by a hand within reach: a carrier inside <see cref="BotCourier.Reach"/>, a trader's haul, a maker's order. Restarts
    /// the quiet clock a far carrier waits on (<see cref="Quiet"/>).
    /// </summary>
    public void Offered()
    {
        Offers++;

        var now = Core.TickCount;

        // A maker's order the far rule let in on this very tick (BotCourier.Answerable) is a far offer: it does not keep that same
        // maker's walk off its own piece for another quiet spell.
        if (!_farAsked || _farAskedTick != now)
        {
            NearTick = now;
        }
    }

    private bool _farAsked;

    private long _farAskedTick;

    /// <summary>A maker past reach found this want answerable by the far rule, now. See <see cref="Offered"/>.</summary>
    internal void FarAsked()
    {
        _farAsked = true;
        _farAskedTick = Core.TickCount;
    }

    /// <summary>
    /// Offered by a carrier past <see cref="BotCourier.Reach"/> (06.10.2026, needs15): counted as an offer for the line's cause, and
    /// leaving the quiet clock alone, so the next far carrier is not kept off by the first one's offer that its auction refused.
    /// </summary>
    public void OfferedFar() => Offers++;

    /// <summary>When a hand within reach last offered work for it; its posting until then. See <see cref="Offered"/>.</summary>
    public long NearTick { get; private set; }

    /// <summary>
    /// <b>Nobody within reach has answered it for <paramref name="quietMs"/> (06.10.2026, needs15).</b> No carrier bringing units, and
    /// no walk, haul or order offered by a hand within <see cref="BotCourier.Reach"/> since then: the far carrier's question
    /// (<see cref="BotCourier.FarOpen"/>). A trader's haul or a near carrier's offer restarts the clock, so the long haul stays the
    /// traders' first and the near carriers' first.
    /// </summary>
    public bool Quiet(long now, int quietMs) => Coming == 0 && now - NearTick >= quietMs;

    /// <summary>Carriers that took a walk for it and reserved units on it (<see cref="Reserve"/>).</summary>
    public int Takes { get; private set; }

    // ---- Its buyer's death, kept on the want so a dead buyer's want is not stood down for an empty pocket (build 391) --------

    /// <summary>Whether the buyer was dead at the market's last look.</summary>
    internal bool Dead { get; private set; }

    internal long DiedTick { get; private set; }

    /// <summary>When the buyer was last seen back on its feet after a death; meaningful while <see cref="Rising"/>.</summary>
    internal long RoseTick { get; private set; }

    /// <summary>Back on its feet and inside its grace to walk back to its corpse (<see cref="BotMarket.RisenGraceMs"/>).</summary>
    internal bool Rising { get; set; }

    /// <summary>Whether the buyer died at all while this want stood.</summary>
    internal bool DiedWhileStanding { get; private set; }

    /// <summary>Milliseconds the buyer lay dead while this want stood, counted at each rising.</summary>
    public long DeadMs { get; private set; }

    /// <summary>Dead milliseconds as of now, the current death included.</summary>
    public long DeadMsAt(long now) => Dead ? DeadMs + Math.Max(0L, now - DiedTick) : DeadMs;

    /// <summary>The market saw the buyer dead. Returns true on the first look of this death.</summary>
    internal bool Fell(long now)
    {
        if (Dead)
        {
            return false;
        }

        Dead = true;
        Rising = false;
        DiedTick = now;
        DiedWhileStanding = true;

        return true;
    }

    /// <summary>The market saw the buyer alive again. Returns true on the first look after a death.</summary>
    internal bool Rose(long now)
    {
        if (!Dead)
        {
            return false;
        }

        Dead = false;
        DeadMs += Math.Max(0L, now - DiedTick);
        RoseTick = now;
        Rising = true;

        return true;
    }

    // ---- Its buyer out of the world, resting (BotRest parks a bot on the internal map for hours) -----------------------------

    /// <summary>Whether the buyer was out of the world at the market's last look.</summary>
    internal bool Away { get; private set; }

    private long _awaySince;

    /// <summary>Milliseconds the buyer spent out of the world while this want stood, counted at each return.</summary>
    public long AwayMs { get; private set; }

    public long AwayMsAt(long now) => Away ? AwayMs + Math.Max(0L, now - _awaySince) : AwayMs;

    /// <summary>The market saw the buyer out of the world. Returns true on the first look of this absence.</summary>
    internal bool Left(long now)
    {
        if (Away)
        {
            return false;
        }

        Away = true;
        _awaySince = now;

        return true;
    }

    /// <summary>The market saw the buyer back in the world.</summary>
    internal void Returned(long now)
    {
        if (!Away)
        {
            return;
        }

        Away = false;
        AwayMs += Math.Max(0L, now - _awaySince);
    }

    /// <summary>The buyer's purse, read on the market's beat with its older wants paid for first, cannot pay for one of this.</summary>
    internal bool Short { get; set; }

    /// <summary>The buyer's purse cannot cover the whole of this want's reservation: money left it that the reservation held.</summary>
    internal bool Uncovered { get; set; }

    public int Id { get; }

    public IBotWilful Buyer { get; }

    public Type Kind { get; }

    public string Label { get; }

    public int ItemId { get; }

    public int Hue { get; }

    /// <summary>Units still wanted.</summary>
    public int Amount { get; private set; }

    /// <summary>What the buyer says it pays for one.</summary>
    public int Offer { get; private set; }

    /// <summary>The offer it opened at: the floor and the ceiling of its movement are multiples of this.</summary>
    public int Anchor { get; }

    /// <summary>
    /// <b>The most one of this is worth to anybody as a material (02.10.2026, build next; Patrick: "a raw material is never worth
    /// more than twice what it becomes").</b> Nought for a kind that is not a material, or whose products nothing prices. Set when
    /// the want is said (<see cref="BotMarket.Ask"/>); a raise stops at it (<see cref="Stepped"/>) and a trader's price for it never
    /// passes it (<see cref="BotTrading.Alternative"/>). Not saved: a want read back from the save is given it again at the start.
    /// </summary>
    public int Ceiling { get; internal set; }

    /// <summary>
    /// Read back from the save before anything on the island priced its material, so its ceiling is still to be read
    /// (03.10.2026, build next): the market's beat reads it again until it has one. See <see cref="BotMarket.Recap"/>.
    /// </summary>
    internal bool CeilingPending { get; set; }

    /// <summary>
    /// <b>What the cheapest shopkeeper on the island asks for one (03.10.2026, build next), or nought when none sells it or none
    /// was read.</b> A raise stops at <see cref="BotMarket.MostMultiple"/> times it (<see cref="Stepped"/>), whatever the want
    /// opened at: a buyer can always walk to the counter, so a want past four times the counter's price is not a price, it is a
    /// climb. Read when the want is said; for a want read back from the save, or said before the counters were surveyed, on the
    /// market's beat (<see cref="ShelfPending"/>). Not saved. See <see cref="BotMarket.Shelve"/>.
    /// </summary>
    public int Shelf { get; internal set; }

    /// <summary>Its shopkeeper's price is still to be read on the beat. See <see cref="Shelf"/>.</summary>
    internal bool ShelfPending { get; set; }

    /// <summary>
    /// What it opened at, as said (03.10.2026, build next): the price a want said again by the same buyer after this one gave up
    /// may not open above (<see cref="BotMarket.Ask"/>). For a want read back from the save, the anchor it was saved with.
    /// </summary>
    public int Opened { get; }

    /// <summary>
    /// The most its offer may rise to: <see cref="BotMarket.MostMultiple"/> times what it opened at, no higher than its material's
    /// ceiling, and no higher than that multiple of its shopkeeper's price (03.10.2026, build next).
    /// </summary>
    public int Highest(double mostMultiple)
    {
        var top = Math.Max(1, (int)(Anchor * mostMultiple));

        // A material's own ceiling stops the raise before the market's multiple does (02.10.2026, build next). See Ceiling.
        if (Ceiling > 0 && Ceiling < top)
        {
            top = Ceiling;
        }

        // And four times what a shopkeeper asks for one (03.10.2026, build next). See Shelf.
        if (Shelf > 0)
        {
            var shelf = (int)Math.Min(int.MaxValue, Shelf * (long)Math.Max(1.0, mostMultiple));

            if (shelf < top)
            {
                top = Math.Max(1, shelf);
            }
        }

        return top;
    }

    /// <summary>
    /// Brought down to its material's ceiling: a want read back above it, from a build that had none or a start that could not
    /// read one. Returns the gold a unit came down by, or nought.
    /// </summary>
    internal int Bring(int ceiling)
    {
        if (ceiling <= 0 || Offer <= ceiling)
        {
            return 0;
        }

        var down = Offer - ceiling;

        Offer = ceiling;
        TouchedTick = Core.TickCount;
        BotMarket.Touched();

        return down;
    }

    public long PostedTick { get; }

    /// <summary>The last time anything happened to it: posted, topped up, answered, or its price moved.</summary>
    public long TouchedTick { get; private set; }

    /// <summary>The last time its buyer said it out loud, once <see cref="Said"/>. See <see cref="BotMarket.SayWantMs"/>.</summary>
    public long SaidTick { get; private set; }

    /// <summary>Whether it has been said aloud at all: the tick above means nothing until it has (no zero sentinel on a tick).</summary>
    public bool Said { get; private set; }

    /// <summary>Said aloud now.</summary>
    internal void Spoken(long now)
    {
        Said = true;
        SaidTick = now;
    }

    /// <summary>Units handed to the buyer against it, and what they came to.</summary>
    public int Filled { get; private set; }

    public int Paid { get; private set; }

    /// <summary>Whether it has ever been answered, and when last.</summary>
    public bool Traded { get; private set; }

    public long FilledTick { get; private set; }

    public int Raises { get; private set; }

    public int Cuts { get; private set; }

    /// <summary>
    /// Units carriers have taken the delivery of and are bringing — held, and put down past the summed clock (BotDeliver.Away),
    /// never more than are wanted unless the held ones alone are.
    /// </summary>
    public int Coming
    {
        get
        {
            var held = Held;

            if (_away.Count == 0)
            {
                return held;
            }

            var away = 0;

            for (var i = 0; i < _away.Count; i++)
            {
                away += _away[i].Units;
            }

            return Math.Max(held, Math.Min(Amount, held + away));
        }
    }

    /// <summary>Units carriers hold a reservation on now.</summary>
    private int Held
    {
        get
        {
            var total = 0;

            for (var i = 0; i < _coming.Count; i++)
            {
                total += _coming[i].Units;
            }

            return total;
        }
    }

    /// <summary>Units still open to a carrier: wanted and not held by one. A carrier put down past the summed clock holds none.</summary>
    public int Payable => Math.Max(0, Amount - Held);

    public bool IsOpen => Payable > 0;

    /// <summary>What the open part would pay a carrier.</summary>
    public int Worth => Payable * Offer;

    /// <summary>Asked again: the same want, more of the thing. The offer is not overwritten — it is what this bot has learned.</summary>
    public void Top(int units)
    {
        if (units > 0)
        {
            Amount += units;
        }

        TouchedTick = Core.TickCount;
        BotMarket.Touched();
    }

    /// <summary>
    /// A carrier has taken the delivery of <paramref name="units"/>. Replaces what it had reserved before, at the price it had
    /// agreed before (a delivery taken up again after a flight keeps its word).
    /// </summary>
    internal void Reserve(IBotWilful courier, int units) => Reserve(courier, units, PriceOf(courier));

    /// <summary>
    /// The same, at the price agreed for one (03.10.2026, build next). A price above the offer is the buyer's word to the carrier
    /// too: the difference on these units is held from the buyer's free money with the rest of its wants (<see cref="Premium"/>,
    /// <see cref="BotMarket.Reserved"/>), so nothing else spends it before the meeting. See <see cref="BotMarket.Affords"/>.
    /// <paramref name="retake"/>: the same carrier holding again what the sweep let go while its work was put down (03.10.2026,
    /// build next), not a carrier taking the walk, so not counted in <see cref="Takes"/>.
    /// </summary>
    internal void Reserve(IBotWilful courier, int units, int price, bool retake = false)
    {
        var again = Carried(courier);

        Release(courier);

        if (courier != null && units > 0)
        {
            _coming.Add((courier, units, Math.Max(0, price)));
            BotMarket.Touched();

            // Counted once a carrier, not once a resumption: a failure "taken and not brought" names carriers, not beats.
            if (!again && !retake)
            {
                Takes++;
            }
        }
    }

    /// <summary>The carrier's reservation, gone with its delivery.</summary>
    internal void Release(IBotWilful courier)
    {
        for (var i = _coming.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_coming[i].Courier, courier))
            {
                _coming.RemoveAt(i);
                BotMarket.Touched();
            }
        }

        for (var i = _away.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_away[i].Courier, courier))
            {
                _away.RemoveAt(i);
            }
        }
    }

    /// <summary>The price this carrier agreed for one, or nought when it has none reserved (or agreed the offer).</summary>
    internal int PriceOf(IBotWilful courier)
    {
        for (var i = 0; i < _coming.Count; i++)
        {
            if (ReferenceEquals(_coming[i].Courier, courier))
            {
                return _coming[i].Price;
            }
        }

        return 0;
    }

    /// <summary>
    /// <b>What the buyer agreed to pay carriers over its offer (03.10.2026, build next).</b> A trader's book writes carriage over
    /// the offer ("21 each (offered 6)"), and the buyer says "agreed" in the trade chat; until now nothing held that money, so the
    /// buyer spent it on arrows and reagents before the meeting, and five traders' books each counted the same free coin. Held
    /// now with the want's own reservation (<see cref="BotMarket.Reserved"/>): the units each carrier is bringing times what its
    /// price is over the offer, read against the offer as it stands.
    /// </summary>
    public int Premium
    {
        get
        {
            long total = 0;

            for (var i = 0; i < _coming.Count; i++)
            {
                total += (long)_coming[i].Units * Math.Max(0, _coming[i].Price - Offer);
            }

            return (int)Math.Min(int.MaxValue, total);
        }
    }

    /// <summary>The carriage over the offer held for this one carrier's units. See <see cref="Premium"/>.</summary>
    public int PremiumOf(IBotWilful courier)
    {
        long total = 0;

        for (var i = 0; i < _coming.Count; i++)
        {
            if (ReferenceEquals(_coming[i].Courier, courier))
            {
                total += (long)_coming[i].Units * Math.Max(0, _coming[i].Price - Offer);
            }
        }

        return (int)Math.Min(int.MaxValue, total);
    }

    /// <summary>Whether this carrier has units reserved on it.</summary>
    public bool Carried(IBotWilful courier)
    {
        for (var i = 0; i < _coming.Count; i++)
        {
            if (ReferenceEquals(_coming[i].Courier, courier))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reservations held by carriers that are gone or no longer on a delivery. Asked on the market's beat.</summary>
    internal int Sweep()
    {
        var swept = 0;

        for (var i = _coming.Count - 1; i >= 0; i--)
        {
            var courier = _coming[i].Courier;

            // Held or put down for a flight: a delivery paused is still a delivery. See BotDeed.Paused. Put down for longer than
            // its own give-up clock it is not coming for now (BotDeliver.Stale): the units are open to another carrier again.
            // Any work that carries against wants (IBotWantCarrier, 01.10.2026 build 385): the carrier's walk and the trader's haul.
            var held = courier?.Resolve?.Deed as IBotWantCarrier ?? courier?.Resolve?.Paused?.Deed as IBotWantCarrier;

            if (courier?.Self is not { Deleted: false, Alive: true } || held == null || !held.Carries(this) || held.Stale)
            {
                // Let go by the summed clock, still on its way: still coming, no longer holding. See BotDeliver.Away.
                if (held is BotDeliver { Away: true } && held.Carries(this) && courier?.Self is { Deleted: false, Alive: true })
                {
                    _away.Add((courier, _coming[i].Units));
                }

                _coming.RemoveAt(i);
                swept++;
                BotMarket.Touched();
            }
        }

        for (var i = _away.Count - 1; i >= 0; i--)
        {
            var courier = _away[i].Courier;
            var held = courier?.Resolve?.Deed as IBotWantCarrier ?? courier?.Resolve?.Paused?.Deed as IBotWantCarrier;

            if (courier?.Self is not { Deleted: false, Alive: true } || held is not BotDeliver { Away: true } || !held.Carries(this))
            {
                _away.RemoveAt(i);
            }
        }

        return swept;
    }

    /// <summary>
    /// Goods handed over against it. Returns true when the answer came again inside <paramref name="briskMs"/> — the offer
    /// was generous, and the want may ask for less.
    /// </summary>
    internal bool Took(int units, int gold, int briskMs)
    {
        var now = Core.TickCount;

        Amount = Math.Max(0, Amount - units);
        Filled += units;
        Paid += gold;
        BotMarket.Touched();

        var brisk = Traded && now - FilledTick < briskMs;

        Traded = true;
        FilledTick = now;
        TouchedTick = now;

        return brisk;
    }

    /// <summary>What the offer would become if raised once now, or nought when it is already at its ceiling.</summary>
    public int Stepped(double step, double mostMultiple)
    {
        // Its anchor's multiple, its material's ceiling and its shopkeeper's multiple, the lowest of them. See Highest.
        var ceiling = Highest(mostMultiple);

        var offering = Math.Max(Offer + 1, (int)(Offer * (1.0 + step)));

        if (offering > ceiling)
        {
            offering = ceiling;
        }

        return offering > Offer ? offering : 0;
    }

    internal void Lift(int offering)
    {
        if (offering <= Offer)
        {
            return;
        }

        Offer = offering;
        Raises++;
        TouchedTick = Core.TickCount;
        BotMarket.Touched();
    }

    /// <summary>
    /// Unanswered for <see cref="BotMarket.RaiseMs"/> and its buyer cannot reserve a louder price: it waits at this one for another
    /// raise's time (02.10.2026, build 391). See <see cref="BotMarket.Held"/>.
    /// </summary>
    internal void Waited() => TouchedTick = Core.TickCount;

    /// <summary>
    /// Lowered to a price it should never have passed, and held there: a scroll want read back above the price of writing it
    /// (03.10.2026, <see cref="BotScribes.CapStanding"/>). Not a market cut — nobody answered it — so not counted in <see cref="Cuts"/>.
    /// </summary>
    internal void CutTo(int offer, int ceiling)
    {
        Ceiling = Math.Max(1, ceiling);

        // Given its ceiling here, so the beat's Recap (lines9) does not give it again and count it twice as a material's.
        CeilingPending = false;

        if (offer < Offer)
        {
            Offer = Math.Max(1, offer);
            BotMarket.Touched();
        }
    }

    internal bool Cut(double step, double leastMultiple)
    {
        var floor = Math.Max(1, (int)(Anchor * leastMultiple));
        var offering = Math.Min(Offer - 1, (int)(Offer * (1.0 - step)));

        if (offering < floor)
        {
            offering = floor;
        }

        if (offering >= Offer)
        {
            return false;
        }

        Offer = offering;
        Cuts++;
        TouchedTick = Core.TickCount;
        BotMarket.Touched();

        return true;
    }

    /// <summary>What a kind looks like: its name, its art and its hue, from one throwaway instance kept for the life of the process.</summary>
    internal static (string Label, int ItemId, int Hue) Look(Type kind)
    {
        if (kind == null)
        {
            return ("?", 0x1F4C, 0);
        }

        if (_looks.TryGetValue(kind, out var known))
        {
            return known;
        }

        var look = (BotMarket.Spaced(kind.Name), 0x1F4C, 0);

        // The engine's own activator: nearly everything worth wanting is declared Foo(int amount = 1), for which plain
        // reflection finds no parameterless constructor, and a row with no picture would follow.
        var sample = kind.CreateInstance<Item>();

        if (sample != null)
        {
            var name = sample.Name;

            look = (string.IsNullOrWhiteSpace(name) ? BotMarket.Spaced(kind.Name) : name, sample.ItemID, sample.Hue);

            sample.Delete();
        }

        _looks[kind] = look;

        return look;
    }

    private static readonly Dictionary<Type, (string Label, int ItemId, int Hue)> _looks = [];

    public override string ToString() => $"{Amount} × {Label} wanted at {Offer}gp";
}
