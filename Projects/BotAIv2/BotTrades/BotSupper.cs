using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Supper at a tavern: the walk to the cook, a meal bought hand to hand and eaten, and a few minutes at the table resting and
/// talking — the fireside's evening, indoors and fed (07.10.2026, trades17).
///
/// <para>
/// <b>Not about money</b> (<see cref="BotDeed.Unpaid"/>): the guest pays for its meal and is paid nothing, so the auction prices it
/// at its claim and <see cref="BotSupperer"/> asks every reason it would fail before it offers it — a tavern in reach with a seat,
/// a welcome, the price and a purse that keeps <see cref="BotTaverns.GuestKeeps"/> after it.
/// </para>
/// </summary>
public sealed class BotSup : BotDeed
{
    public const string Trade = "supper";

    private readonly BotTavern _tavern;

    private readonly double _claim;

    private readonly long _sitMs;

    private long _began;

    private bool _sat;

    private long _satTick;

    private int _paid;

    private Type _meal;

    public BotSup(BotTavern tavern, double claim)
    {
        _tavern = tavern;
        _claim = claim;
        _sitMs = (long)(Math.Max(0.5, BotTaverns.GuestMinutes + (Utility.RandomDouble() * 2.0 - 1.0)) * 60000);
    }

    public override string Kind => Trade;

    public override Map Map => _tavern?.Map;

    public override Point3D Where => _tavern?.Stand ?? Point3D.Zero;

    public override double Expects => _claim;

    public override double Minutes => Math.Max(0.5, BotTaverns.GuestMinutes);

    public override int Outlay => _tavern?.Keeper == null ? 0 : BotTaverns.Price(BotTaverns.MostHeld(_tavern.Keeper, out _));

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Still => _sat;

    public override bool Hurries => false;

    public override string Stage =>
        _tavern == null
            ? "going to supper"
            : _sat
                ? $"at {_tavern.Name} after {_meal?.Name ?? "supper"}"
                : $"walking to {_tavern.Name} for supper";

    public override bool Repeats(BotDeed other) => other is BotSup sup && ReferenceEquals(sup._tavern, _tavern);

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;

        if (bot?.Self is { } body)
        {
            BotSupperer.Went(body);
        }
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _tavern == null || body.Map != _tavern.Map)
        {
            return BotDoing.Failed("no body, or no tavern on this map");
        }

        var now = Core.TickCount;
        var keeper = _tavern.Keeper;

        if (_tavern.Shut || keeper is not { Deleted: false, Alive: true })
        {
            if (_sat)
            {
                return BotDoing.Done($"ate {_meal?.Name} for {_paid}gp and sat {(now - _satTick) / 60000.0:F1} minutes at {_tavern.Name} until it shut");
            }

            BotSupperer.NoteShut();

            return BotDoing.Failed($"{_tavern.Name} shut before it got there");
        }

        if (!_sat)
        {
            if (!body.InRange(keeper, BotHandover.MeetTiles))
            {
                if (now - _began >= BotTaverns.WalkMs)
                {
                    BotSupperer.NoteNeverGot();

                    return BotDoing.Failed($"could not get to {_tavern.Name} in {BotTaverns.WalkMs / 60000} minutes");
                }

                return BotDoing.Walk(_tavern.Map, keeper, BotArrival.Within(BotHandover.MeetTiles), $"to {_tavern.Name} for supper");
            }

            _paid = BotTaverns.Serve(_tavern, body, out _meal, out var refused);

            if (_paid <= 0)
            {
                BotSupperer.NoteUnserved();

                return BotDoing.Failed($"no supper at {_tavern.Name}: {refused}");
            }

            _sat = true;
            _satTick = now;
            BotTaverns.Sit(_tavern, body, now);
            body.Journey.Finish();
            BotTaverns.Say(body, "tavern:greet", keeper, _tavern);
        }

        // Taken up again after a flight or a rescue somewhere else: back to the table.
        if (!body.InRange(_tavern.Stand, 4))
        {
            return BotDoing.Walk(_tavern.Map, keeper, BotArrival.Within(3), $"back to {_tavern.Name}");
        }

        BotTaverns.Seated(_tavern, body, now);

        if (body.Tired)
        {
            return BotDoing.Done($"ate {_meal?.Name} for {_paid}gp at {_tavern.Name} and sleeps at the inn");
        }

        if (now - _satTick >= _sitMs)
        {
            return BotDoing.Done($"ate {_meal?.Name} for {_paid}gp and sat {(now - _satTick) / 60000.0:F1} minutes at {_tavern.Name}");
        }

        return BotDoing.Work("at supper");
    }

    public override void Drop(IBotWilful bot)
    {
        if (bot?.Self is BotMobile body)
        {
            BotTaverns.Rise(_tavern, body, _sat ? (Core.TickCount - _satTick) / 60000.0 : 0.0);
        }
    }
}

/// <summary>
/// Offers a free bot that is tiring, bored, hurt or has nothing to eat a seat at the nearest open tavern (07.10.2026, trades17).
/// A walk of the open taverns — at most <see cref="BotTaverns.MostOpen"/> — and no spatial query; a bot is not asked again for
/// <see cref="EveryMs"/> after it went to one. Priced as the fireside is (<c>BotCamp.Claim</c>), a little above it because supper
/// is waiting, and more for a wound.
/// </summary>
public sealed class BotSupperer : IBotProposer
{
    public string Name => "Tavern guest";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>What supper is worth a minute before tiredness, boredom, a wound and hunger are added. Twenty-six: the fireside's 24 and a plate.</summary>
    public static double Prior { get; set; } = 26.0;

    /// <summary>What a wound adds a minute, times the share of hits missing.</summary>
    public static double HurtWorth { get; set; } = 30.0;

    /// <summary>What having nothing cooked in the pack adds a minute.</summary>
    public static double HungryWorth { get; set; } = 6.0;

    /// <summary>How long one bot waits between suppers. Forty minutes.</summary>
    public static int EveryMs { get; set; } = 2400000;

    /// <summary>The weariness — share of the session played — from which a bot counts as tiring. Two fifths.</summary>
    public static double WearyFrom { get; set; } = 0.4;

    /// <summary>The boredom from which a bot counts as bored.</summary>
    public static double BoredFrom { get; set; } = 0.3;

    /// <summary>The share of its hits under which a bot counts as hurt.</summary>
    public static double HurtUnder { get; set; } = 0.8;

    public static long Asked { get; private set; }

    public static long TooSoon { get; private set; }

    public static long Fed { get; private set; }

    public static long NoNeed { get; private set; }

    public static long Far { get; private set; }

    public static long Full { get; private set; }

    public static long Unwelcome { get; private set; }

    public static long Poor { get; private set; }

    public static long Offered { get; private set; }

    public static long ForWound { get; private set; }

    public static long ForHunger { get; private set; }

    public static long Walked { get; private set; }

    public static long Shut { get; private set; }

    public static long NeverGot { get; private set; }

    public static long Unserved { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    internal static void Went(Mobile who)
    {
        if (who != null)
        {
            _last[who.Serial] = Core.TickCount;
            Walked++;
        }
    }

    internal static void NoteShut() => Shut++;

    internal static void NoteNeverGot() => NeverGot++;

    internal static void NoteUnserved() => Unserved++;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotTrades.TavernsOn || BotTaverns.Open.Count == 0)
        {
            return null;
        }

        if (bot?.Self is not BotMobile who || !who.Alive || who.Tired || who.Map == null || who.Map == Map.Internal)
        {
            return null;
        }

        if (who.Squad != null || BotDelveParty.Delving(who) || BotOutlaw.Jailed(who) || BotDuel.Duelling(who) || who.Combatant != null)
        {
            return null;
        }

        if (who.Resolve?.Deed is BotSup or BotKeepTavern or BotFireside or BotKindle)
        {
            return null;
        }

        Asked++;

        var now = Core.TickCount;

        if (_last.TryGetValue(who.Serial, out var when) && now - when < EveryMs)
        {
            TooSoon++;

            return null;
        }

        if (BotMeal.IsFed(who))
        {
            Fed++;

            return null;
        }

        var weary = BotCamp.Weariness(who) >= WearyFrom;
        var bored = (who.Resolve?.Urges?.Boredom ?? 0.0) >= BoredFrom;
        var hurt = who.HitsMax > 0 && who.Hits < who.HitsMax * HurtUnder;
        var hungry = BotTaverns.MealsIn(who.Backpack) == 0;

        if (!weary && !bored && !hurt && !hungry)
        {
            NoNeed++;

            return null;
        }

        var tavern = BotTaverns.Nearest(who, now, out var refusal);

        if (tavern == null)
        {
            switch (refusal)
            {
                case "full":
                    {
                        Full++;

                        break;
                    }
                case "unwelcome":
                    {
                        Unwelcome++;

                        break;
                    }
                default:
                    {
                        Far++;

                        break;
                    }
            }

            return null;
        }

        // <b>A poor bot hunts; it does not buy suppers</b> (Patrick's standing rule). What is left after the plate must be its keep.
        var price = BotTaverns.Price(BotTaverns.MostHeld(tavern.Keeper, out _));

        if (price <= 0 || BotYield.Spendable(who) < price + BotTaverns.GuestKeeps)
        {
            Poor++;

            return null;
        }

        var claim = BotCamp.Claim(who, Prior);

        if (hurt)
        {
            claim += HurtWorth * (1.0 - (double)who.Hits / who.HitsMax);
            ForWound++;
        }

        if (hungry)
        {
            claim += HungryWorth;
            ForHunger++;
        }

        Offered++;

        return new BotSup(tavern, claim);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked to supper"
            : $"{Asked} asked to supper: {Offered} offered a seat ({ForWound} hurt, {ForHunger} with nothing cooked on them), {Walked} went; {TooSoon} within {EveryMs / 60000} minutes of their last, {Fed} still fed, "
              + $"{NoNeed} neither tired, bored, hurt nor hungry, {Far} with no tavern within {BotTaverns.GuestReach} tiles, {Full} found the table full, {Unwelcome} not welcome there, {Poor} too poor for supper; "
              + $"{Shut} found it shut, {NeverGot} could not get there, {Unserved} were not served";

    public static void Forget()
    {
        _last.Clear();
        Asked = 0;
        TooSoon = 0;
        Fed = 0;
        NoNeed = 0;
        Far = 0;
        Full = 0;
        Unwelcome = 0;
        Poor = 0;
        Offered = 0;
        ForWound = 0;
        ForHunger = 0;
        Walked = 0;
        Shut = 0;
        NeverGot = 0;
        Unserved = 0;
    }
}
