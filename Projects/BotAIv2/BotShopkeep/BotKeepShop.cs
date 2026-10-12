using System;

namespace Server.BotAI.V2;

/// <summary>
/// A shift keeping shop: walk to a pitch beside the bank, open, call out what is for sale, serve whoever comes, and shut when the
/// goods or the shift run out.
///
/// <para>
/// <b>Standing still is the work, once the shop is open</b> (<see cref="BotDeed.Still"/>). The stall watch takes work off a bot
/// that has neither moved nor changed its stage for four minutes, which is right for a walk into a wall and wrong for a keeper
/// behind a counter; the shift's own clock (<see cref="BotShopkeep.ShiftMinutes"/>) ends it instead, as the drill field's roll
/// ends a class. On the way to the bank it is a walk like any other and the watch keeps its eye on it.
/// </para>
///
/// <para>
/// <b>Held, from the first step, against the next thing on offer</b> (<see cref="BotDeed.Steadfast"/>). A shop is a promise to
/// the bots already walking to it: a keeper that left its counter for a dig worth a little more would send every one of them
/// on to a shopkeeper and teach them that the walk to a bot is not worth making. Put down, rather than dropped, for what will
/// not wait — a flight, a friend in trouble — and the shop is shut while the keeper is away (<see cref="Paused"/>) and opened
/// again when it is back on its pitch, the shift's clock counting only the minutes it was open.
/// </para>
///
/// <para>
/// <b>Honest endings.</b> Finished when the shift has run its course or the goods are gone, with what it sold and to how many in
/// the note — a shift that sold nothing is still a shift kept, and says so, and the ledger reads the nought. Failed when the
/// pitch cannot be reached, the bank will not take another shop, or the goods were gone before the shop could open. The takings
/// are measured as coin (<see cref="Coin"/> one, <see cref="Made"/> nought): the goods were the keeper's before the shift and the
/// coin is what the shift turned them into, which is the peddler's arithmetic.
/// </para>
/// </summary>
public sealed class BotKeepShop : BotDeed
{
    /// <summary>The ledger's key.</summary>
    public const string Trade = "shopkeep";

    private readonly Map _map;

    private readonly Point3D _bank;

    private readonly double _claim;

    private readonly int _shown;

    private Point3D _stand;

    private BotStorefront _front;

    /// <summary>Minutes the shop has been open before the current opening, carried across a pause.</summary>
    private double _openBefore;

    private long _openedTick;

    private long _cryTick;

    private int _repitched;

    private int _sold;

    private int _takings;

    private int _customers;

    /// <summary>What the island was asking for of the goods on show when the shop first opened: the shift's denominator.</summary>
    private int _firstShown = -1;

    /// <summary>When the walk to the present pitch began; nought while not walking to one. See <see cref="BotShopkeep.OpenStandingMs"/>.</summary>
    private long _walkSince;

    /// <summary>How many times a keeper finding a neighbour on its pitch looks for another before opening where it stands.</summary>
    public static int MostRepitches { get; set; } = 2;

    public BotKeepShop(Map map, Point3D stand, Point3D bank, double claim, int shown)
    {
        _map = map;
        _stand = stand;
        _bank = bank;
        _claim = Math.Max(0.01, claim);
        _shown = shown;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _stand;

    public override double Expects => _claim;

    public override double Minutes => BotShopkeep.ShiftMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override int Made => 0;

    public override bool Still => _front != null;

    /// <summary>
    /// <b>An open shop keeps its shift to the end (30.09.2026, build 359).</b> The shift is reckoned at ShiftMinutes from the
    /// taking, the walk to the pitch comes out of it, and the auction outbid the keeper in the last half minute of every shift:
    /// "Dunstan shut its shop in Britain after 7.5 minutes … dropped shopkeep … outbid by cook at 171/min after 8.2 of 9.1
    /// minutes reckoned" — on 358 16 dropped against 5 finished, each a shop that had stood its time counted as work given
    /// up, and buyers still walking to it finding it shut. While the shop is open it is kept from the bidding, as a watch
    /// inside the walls is; a flight, a mending, a rung above and whatever presses still take the keeper.
    /// </summary>
    public override bool Committed => _front != null;

    public override bool Steadfast => true;

    /// <summary>A bend with the shop open is the walk to the pitch ending beside it, which is routine and lifts no hold (07.10.2026, work18).</summary>
    public override bool BendIsTrouble => _front == null;

    /// <summary>The rest of the shift and a minute, once open. See <see cref="BotDeed.HoldsFor"/>.</summary>
    public override double HoldsFor => _front != null ? Math.Max(1.0, BotShopkeep.ShiftMinutes - OpenMinutes(Core.TickCount)) + 1.0 : 0.0;

    public override bool Hurries => _front == null;

    public override string Stage =>
        _front != null
            ? $"keeping shop by the bank in {_front.Town}: {_front.Units} things of {_front.Kinds} kinds, sold {Sold} to {Customers} for {Takings}gp"
            : $"going to the bank at ({_bank.X}, {_bank.Y}) to keep shop with {_shown}gp of goods the island is asking for";

    private int Sold => _sold + (_front?.Sold ?? 0);

    private int Takings => _takings + (_front?.Takings ?? 0);

    private int Customers => _customers + (_front?.Customers ?? 0);

    private double OpenMinutes(long now) => _openBefore + (_front != null ? (now - _openedTick) / 60000.0 : 0.0);

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        if (!BotShopkeep.Running)
        {
            Shut(false, now);

            return BotDoing.Done("the shops were shut by order");
        }

        if (_front == null)
        {
            return Opening(body, now);
        }

        // Taken off the list behind its back — a reload, a sweep of a shop nobody was behind. Opened again where it stands.
        if (_front.Shut)
        {
            Carry(now);
            _front = null;

            return Opening(body, now);
        }

        if (now - _front.StockedTick >= BotShopkeep.StockMs)
        {
            BotShopkeep.Refresh(_front);
        }

        if (_front.Units <= 0)
        {
            var note = Summary(now, "sold out");

            Shut(true, now, soldOut: true);

            return BotDoing.Done(note);
        }

        if (OpenMinutes(now) >= BotShopkeep.ShiftMinutes)
        {
            var note = Summary(now, "the shift is over");

            Shut(true, now);

            return BotDoing.Done(note);
        }

        // Asked to step aside and further off than a customer would look: back to the pitch. Customers follow the keeper
        // itself, so a step or two is nothing.
        if (body.Map != _map || !body.InRange(_stand, BotShopkeep.StandReach + BotShopkeep.ServeReach))
        {
            return BotDoing.Walk(_map, _stand, BotArrival.Within(BotShopkeep.StandReach), "back to its shop by the bank");
        }

        if (now - _cryTick >= BotShopkeep.CryMs)
        {
            _cryTick = now;
            BotShopkeep.Cry(body, _front, "shop:cry");
        }

        return BotDoing.Work(Stage);
    }

    private BotDoing Opening(BotMobile body, long now)
    {
        if (body.Map != _map || !body.InRange(_stand, BotShopkeep.StandReach))
        {
            // <b>Walked long enough near the bank: open where it stands (07.10.2026, work18).</b> The walk's own failure reaches Bend
            // only when the walker gives up; the will's "stopped closing" and the stall watch end the shift without asking. A keeper
            // still walking after OpenStandingMs on fit ground in the bank's ring opens there, as the bend's own fallback does.
            if (_walkSince == 0)
            {
                _walkSince = now;
            }
            else if (BotShopkeep.OpenStandingMs > 0 && now - _walkSince >= BotShopkeep.OpenStandingMs && body.Map == _map
                     && BotShopkeep.StandsFit(body, _bank))
            {
                BotShopkeep.Strike(_map, _stand);
                BotShopkeep.OpenedStandingLate();

                _stand = body.Location;
                BotShopkeep.Reserve(body, _map, _stand, _bank);
            }

            if (!body.InRange(_stand, BotShopkeep.StandReach) || body.Map != _map)
            {
                return BotDoing.Walk(_map, _stand, BotArrival.Within(BotShopkeep.StandReach), $"to the bank at ({_bank.X}, {_bank.Y}) to keep shop");
            }
        }

        // Somebody opened beside this pitch while it walked. Another pitch at the same bank, a couple of times; then here.
        if (BotShopkeep.Taken(_map, _stand, body) && _repitched < MostRepitches)
        {
            var next = BotShopkeep.Pitch(_map, _bank, body);

            if (next != Point3D.Zero && next != _stand)
            {
                _repitched++;
                _stand = next;
                _walkSince = now;

                BotShopkeep.Reserve(body, _map, _stand, _bank);

                return BotDoing.Walk(_map, _stand, BotArrival.Within(BotShopkeep.StandReach), "to another pitch at the bank");
            }
        }

        _front = BotShopkeep.OpenShop(body, this, _map, _stand, _bank, _firstShown >= 0, out var why);

        if (_front == null)
        {
            return BotDoing.Failed(why ?? "the bank would not take another shop");
        }

        if (_front.Units <= 0)
        {
            Shut(false, now);

            return BotDoing.Failed("nothing left to sell by the time it reached the bank");
        }

        _openedTick = now;
        _cryTick = now;
        _walkSince = 0;

        // <b>The walk to the pitch ends when the shop opens (07.10.2026, work18).</b> The shop opens with the keeper a tile from its
        // pitch (StandReach), and the walk the will sent it on stayed on the road: work answers do not end a walk (BotWill), so the
        // walker kept pressing for the pitch's own tile. Lucan opened at (1423, 1693) at 10:00:35 on 07.10.2026, and at 10:06:36
        // "could not get one tile closer to (1423, 1693, 0) in 19 plans": the open shop failed "could not get nearer". On build
        // 416 the pitches were struck 26 times against 195 failures at the strip, because Bend struck only a pitch not yet open;
        // 75 of those failures had already taken coin. Not a company's station nor an interruption: only the shift's own walk.
        if (body.Squad == null && body.Journey is { Active: true } road && road.Current is { Interruption: false })
        {
            road.Finish();
            BotShopkeep.EndedWalkOnOpening();
        }

        if (_firstShown < 0)
        {
            _firstShown = _front.Shown;
        }

        BotShopkeep.Cry(body, _front, "shop:open");

        return BotDoing.Work(Stage);
    }

    /// <summary>What the shift came to, in the words the log and the ledger's line carry.</summary>
    private string Summary(long now, string why)
    {
        var minutes = OpenMinutes(now);

        return Sold > 0
            ? $"{why}: sold {Sold} things to {Customers} customers for {Takings}gp in {minutes:F1} minutes by the bank in {_front?.Town}"
            : $"{why}: kept shop {minutes:F1} minutes by the bank in {_front?.Town} and nobody came to buy";
    }

    /// <summary>The takings of the opening now ending, kept for the shift's note.</summary>
    private void Carry(long now)
    {
        if (_front == null)
        {
            return;
        }

        _openBefore += (now - _openedTick) / 60000.0;
        _sold += _front.Sold;
        _takings += _front.Takings;
        _customers += _front.Customers;
    }

    private void Shut(bool learn, long now, bool soldOut = false)
    {
        if (_front == null)
        {
            return;
        }

        var front = _front;
        var minutes = OpenMinutes(now);

        Carry(now);
        _front = null;

        if (learn && front.Keeper != null)
        {
            BotShopkeep.Cry(front.Keeper, front, "shop:close");
        }

        BotShopkeep.Close(front, learn, soldOut, minutes, Math.Max(0, _firstShown), _sold, _takings);
    }

    /// <summary>
    /// The pitch could not be reached. Filed under this work at that place, which is the word <see cref="BotShopkeep.Pitch"/>
    /// asks in, so the next offer stands somewhere else at the same bank.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        // <b>A walk that failed with the shop open is not a failed shift (07.10.2026, work18).</b> On build 416 (01:16-10:52) the pitch
        // strip by Britain's bank failed shopkeep 195 times while only 26 pitches were struck: Bend struck and opened-where-it-stood
        // only for a shop not yet open, and most failures were the walk to the pitch still running behind an open shop (see
        // Opening). A keeper still within serving reach of its pitch, or on fit ground in the bank's ring, keeps the shift; its
        // stand moves to where it is when that ground fits, so the walk back is not asked for the tile that could not be reached.
        if (_front != null && bot?.Self is BotMobile open && open.Map == _map)
        {
            var fits = BotShopkeep.StandsFit(open, _bank);

            if (fits || open.InRange(_stand, BotShopkeep.StandReach + BotShopkeep.ServeReach))
            {
                if (fits)
                {
                    _stand = open.Location;
                }

                BotShopkeep.SparedOpenWalk();

                return true;
            }
        }

        // <b>Near the bank already: the shop opens where the keeper stands (06.10.2026, work17b).</b> On build 411 shopkeep failed 37
        // times "could not get nearer to (1422, 1685-1693, 0)": the road reached the tile beside the pitch (Argus `road 1430 1700
        // 1422 1690`: Reached, ending at (1422, 1691), the far side TooBig) and the press at the bank door stopped the last step.
        // A keeper standing on open ground in the bank's ring of pitches, with no shop beside it, opens there; the failed
        // pitch is still struck for the others. Counted.
        if (_front == null && bot?.Self is BotMobile keeper && keeper.Map == _map && BotShopkeep.StandsFit(keeper, _bank))
        {
            BotShopkeep.Strike(_map, _stand);
            BotShopkeep.OpenedStanding();

            _stand = keeper.Location;
            BotShopkeep.Reserve(keeper, _map, _stand, _bank);

            return true;
        }

        bot?.Resolve?.Ledger?.Beware(Trade, _map, _stand);

        // And for every keeper, not this one's ledger alone (06.10.2026, work17): the next keeper chose the same strip. See
        // BotShopkeep.Strike.
        if (_front == null)
        {
            BotShopkeep.Strike(_map, _stand);
        }

        // <b>Another pitch at the same bank before the shift is given up (06.10.2026, work15).</b> On 04.10.2026 shopkeep failed 243
        // times, 219 of them "could not get nearer to (1422, 1685-1693, 0)": the pitches on the step of Britain's bank, where the
        // press of bots at the door stops the last tile; the bank at (1425, 1688) burned the walker's whole ceiling 597 times. The
        // shift failed there and the next offer chose afresh. Not yet open, it takes the next free pitch Pitch gives (the ledger
        // now cautious of this one), as a pitch somebody opened beside does; an open shop's way back is not moved.
        if (_front == null && _repitched < MostRepitches && bot?.Self is { } body && body.Map == _map)
        {
            var next = BotShopkeep.Pitch(_map, _bank, bot);

            if (next != Point3D.Zero && next != _stand)
            {
                _repitched++;
                _stand = next;
                Rewalked++;
                _walkSince = Core.TickCount;

                BotShopkeep.Reserve(body, _map, _stand, _bank);

                return true;
            }
        }

        return false;
    }

    /// <summary>Shifts sent on to another pitch at the same bank when the road to the first could not be walked. See <see cref="Bend"/>.</summary>
    public static long Rewalked { get; private set; }

    /// <summary>The shift is this bot's: its pitch is spoken for while it walks there. See <see cref="BotShopkeep.Reserve"/>.</summary>
    public override void Taken(IBotWilful bot) => BotShopkeep.Reserve(bot?.Self, _map, _stand, _bank);

    /// <summary>Away for something that would not wait: the shop is shut while it is, so nobody walks to an empty pitch.</summary>
    public override void Paused(IBotWilful bot)
    {
        Shut(false, Core.TickCount);
        BotShopkeep.Release(bot?.Self);
    }

    /// <summary>Back from it: the pitch is spoken for again while it walks back.</summary>
    public override void Resumed(IBotWilful bot)
    {
        _walkSince = 0;
        BotShopkeep.Reserve(bot?.Self, _map, _stand, _bank);
    }

    /// <summary>
    /// However it ended, the shop is shut and the pitch let go. A shift that ran its course has already learned from it; anything
    /// else has not.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        var now = Core.TickCount;

        Shut(OpenMinutes(now) >= BotShopkeep.ShiftMinutes * 0.5, now);
        BotShopkeep.Release(bot?.Self);
    }
}
