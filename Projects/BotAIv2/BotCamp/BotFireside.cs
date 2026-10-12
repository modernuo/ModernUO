using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Sitting at somebody else's fire: a walk over, a seat on the ring round the hearth, facing the flame, a greeting, and a
/// few minutes of listening and talking — or, for a tired bot, a night's sleep there.
///
/// <para>
/// <b>A seat is spoken for when the walk is taken, not when it is offered.</b> Every idle bot near a fire is asked in the
/// same second after a fire is lit, and a proposer that held a seat for each of them would fill the ring with bots that
/// went elsewhere (<c>BotDeed.Taken</c>, C9 in DECISIONS). The seat is held in <see cref="Taken"/>; a bot that arrives to a
/// ring filled meanwhile takes the next free seat or gives up.
/// </para>
///
/// <para>
/// <b>A guest has its own evening, shorter than the keeper's.</b> <see cref="GuestMinutes"/>, spread either way, so bots
/// come and go round one fire instead of all rising together; a guest who has become the keeper stays until the fire is out.
/// </para>
/// </summary>
public sealed class BotFireside : BotDeed
{
    public const string Trade = "fireside";

    /// <summary>
    /// What a seat at a fire is worth a minute before tiredness and boredom are added (<c>BotCamp.Claim</c>). Twenty-four:
    /// a third more than making camp, because the fire is already burning and the company already there — still below every
    /// trade's prior, so it takes a bot with nothing better to do, or a tired or bored one.
    /// </summary>
    public static double Prior { get; set; } = 24.0;

    /// <summary>How long a guest sits, in minutes. Four.</summary>
    public static double GuestMinutes { get; set; } = 4.0;

    /// <summary>How far either way a guest's sitting is spread, in minutes. A minute and a half.</summary>
    public static double GuestSpread { get; set; } = 1.5;

    /// <summary>How long a guest may take to walk over before it gives up. Three minutes: forty tiles is under one.</summary>
    public static int WalkMs { get; set; } = 180000;

    /// <summary>How long a guest may stand near the ring unable to step onto its seat before it sits where it stands. Ten seconds.</summary>
    public static int SeatPatienceMs { get; set; } = 10000;

    /// <summary>Seats tried when the way to one is refused.</summary>
    public static int MostBends { get; set; } = 2;

    public static long Walked { get; private set; }

    public static long Sat { get; private set; }

    public static long OutFirst { get; private set; }

    public static long NoSeat { get; private set; }

    public static long NeverGot { get; private set; }

    public static long Rose { get; private set; }

    public static long SleptBy { get; private set; }

    private readonly BotCamp.Fire _fire;

    private readonly double _claim;

    private readonly bool _sleep;

    private readonly long _sitMs;

    private int _seat;

    private bool _sat;

    private long _began;

    private long _satTick;

    private bool _near;

    private long _nearTick;

    private int _bends;

    private int _heard;

    private string _walkNote;

    public BotFireside(BotCamp.Fire fire, int seat, double claim, bool sleep)
    {
        _fire = fire;
        _seat = seat;
        _claim = claim;
        _sleep = sleep;

        var spread = Math.Max(0.0, GuestSpread);

        _sitMs = (long)(Math.Max(0.5, GuestMinutes + (Utility.RandomDouble() * 2.0 - 1.0) * spread) * 60000);
    }

    /// <summary>The fire this bot is going to or sitting at.</summary>
    public BotCamp.Fire Fire => _fire;

    public override string Kind => Trade;

    public override Map Map => _fire?.Map;

    public override Point3D Where =>
        _fire == null ? Point3D.Zero : _seat >= 0 && _seat < _fire.Places.Length ? _fire.Places[_seat].Spot : _fire.Centre;

    public override double Expects => _claim;

    public override double Minutes => Math.Max(0.5, GuestMinutes);

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override int Made => 0;

    /// <summary>Not about money. See <c>BotDeed.Unpaid</c>; <see cref="BotBeckon"/> asks every reason it fails first.</summary>
    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Still => _sat;

    /// <summary>A stroll over to the fire, not a run: it is where a bot goes to stop hurrying.</summary>
    public override bool Hurries => false;

    public override string Stage =>
        _fire == null
            ? "going to a fire"
            : _sat
                ? $"sitting by {_fire.Name}, {_fire.SeatedCount()} round it"
                : $"walking to {_fire.Name}";

    /// <summary>A second offer of the same fire is this walk already, and must not start it over.</summary>
    public override bool Repeats(BotDeed other) => other is BotFireside fireside && ReferenceEquals(fireside._fire, _fire);

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;
        Walked++;

        if (bot?.Self is BotMobile body)
        {
            BotBeckon.Went(body);

            if (!BotCamp.Hold(_fire, body, ref _seat))
            {
                _seat = -1;
            }
        }

        _walkNote = _fire == null ? "to a fire" : $"to a seat by {_fire.Name}";
    }

    public override void Resumed(IBotWilful bot)
    {
        _began = Core.TickCount;
        _near = false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || _fire == null || body.Map != _fire.Map)
        {
            return BotDoing.Failed("no body, or no fire on this map");
        }

        var now = Core.TickCount;

        if (_fire.Closed)
        {
            if (_sat)
            {
                Rose++;

                return BotDoing.Done($"sat {(now - _satTick) / 60000.0:F1} minutes by {_fire.Name} and heard {_fire.Lines - _heard} lines; {_fire.Ended}");
            }

            OutFirst++;

            return BotDoing.Failed($"{_fire.Name} was out before it got there: {_fire.Ended}");
        }

        // A seat lost while the walk was put down, or never had: the nearest free one, else give up.
        if (_seat < 0 || _seat >= _fire.Places.Length || !ReferenceEquals(_fire.Places[_seat].Bot, body))
        {
            _seat = _fire.Free(body.Location);

            if (_seat < 0 || !BotCamp.Hold(_fire, body, ref _seat))
            {
                NoSeat++;

                return BotDoing.Failed($"no seat left by {_fire.Name}");
            }

            _sat = false;
        }

        var spot = _fire.Places[_seat].Spot;
        var fromHearth = BotCamp.Tiles(body.Location, _fire.Centre);

        if (_sat && fromHearth > 3)
        {
            // Taken up again after a rescue or a flight, somewhere else: back to the seat.
            _sat = false;
            _near = false;
        }

        if (!_sat)
        {
            var onSeat = body.X == spot.X && body.Y == spot.Y;

            if (!onSeat && fromHearth is >= 1 and <= 3)
            {
                if (!_near)
                {
                    _near = true;
                    _nearTick = now;
                }
            }

            if (!onSeat && !(_near && now - _nearTick >= SeatPatienceMs && fromHearth is >= 1 and <= 3))
            {
                if (now - _began >= WalkMs)
                {
                    NeverGot++;

                    return BotDoing.Failed($"could not get to a seat by {_fire.Name} in {WalkMs / 60000} minutes");
                }

                return BotDoing.Walk(_fire.Map, spot, BotArrival.Exactly, _walkNote);
            }

            _sat = true;
            _satTick = now;
            _heard = _fire.Lines;
            Sat++;
            BotCamp.Sat(_fire, body);

            // Sat down short of the seat, the walk to it would go on under the sitting; it is finished here, once.
            body.Journey.Finish();
        }

        Face(body, _fire.Centre);
        BotCamp.Seated(_fire, body, now);

        if (body.Tired && BotCamp.Secure(body))
        {
            SleptBy++;

            return BotDoing.Done($"secure by {_fire.Name}, and asleep there");
        }

        // A guest's evening ends on its own clock; a guest who has been handed the fire keeps it till it is out.
        if (!_sleep && !ReferenceEquals(_fire.Keeper, body) && now - _satTick >= _sitMs)
        {
            Rose++;

            return BotDoing.Done($"sat {(now - _satTick) / 60000.0:F1} minutes by {_fire.Name} and heard {_fire.Lines - _heard} lines");
        }

        return BotDoing.Work("sitting by a fire");
    }

    public override bool Bend(IBotWilful bot)
    {
        if (_sat || _fire == null || _fire.Closed || _bends >= MostBends || bot?.Self is not BotMobile body)
        {
            return false;
        }

        _bends++;

        for (var i = 0; i < _fire.Places.Length; i++)
        {
            if (i != _seat && _fire.Places[i].Bot == null)
            {
                if (_seat >= 0 && _seat < _fire.Places.Length && ReferenceEquals(_fire.Places[_seat].Bot, body))
                {
                    _fire.Places[_seat].Bot = null;
                }

                _fire.Places[i].Bot = body;
                _seat = i;
                _near = false;

                return true;
            }
        }

        return false;
    }

    public override void Drop(IBotWilful bot)
    {
        if (_fire != null && bot?.Self is BotMobile body)
        {
            BotCamp.Let(_fire, body, "its sitting ended");
        }
    }

    private static void Face(Mobile body, Point3D at)
    {
        var facing = body.GetDirectionTo(at);

        if ((body.Direction & Direction.Mask) != facing)
        {
            body.Direction = facing;
        }
    }

    public static string Describe() =>
        $"{Walked} walked over to a fire, {Sat} sat down, {Rose} rose again at the end of their sitting; {OutFirst} found it out when they got there, "
        + $"{NoSeat} found no seat, {NeverGot} could not get to one; {SleptBy} fell asleep there";

    public static void Forget()
    {
        Walked = 0;
        Sat = 0;
        OutFirst = 0;
        NoSeat = 0;
        NeverGot = 0;
        Rose = 0;
        SleptBy = 0;
    }
}

/// <summary>
/// Offers a free bot a seat at the nearest lit fire within <c>BotCamp.JoinReach</c>. A walk of the fire list — at most
/// <c>BotCamp.MostFires</c> entries — and no spatial query; a bot is not asked again for <see cref="EveryMs"/> after it
/// went to one, whatever came of it.
/// </summary>
public sealed class BotBeckon : IBotProposer
{
    /// <summary>How long one bot waits between fires. Twenty minutes.</summary>
    public static int EveryMs { get; set; } = 1200000;

    public static long Asked { get; private set; }

    public static long TooSoon { get; private set; }

    public static long Hurt { get; private set; }

    public static long Far { get; private set; }

    public static long Full { get; private set; }

    public static long Unwelcome { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    public string Name => "Fireside";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>A walk to a fire was taken: this bot's clock starts. From <c>BotFireside.Taken</c>.</summary>
    public static void Went(Mobile who)
    {
        if (who != null)
        {
            _last[who.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        // Nothing lit anywhere is not a refusal: nobody is asked.
        if (!BotCamp.Running || BotCamp.Fires.Count == 0)
        {
            return null;
        }

        var body = bot?.Self;

        if (body is not BotMobile who || !who.Alive || who.Tired || who.Map == null || who.Map == Map.Internal)
        {
            return null;
        }

        if (who.Squad != null || BotDelveParty.Delving(who) || BotOutlaw.Jailed(who) || BotDuel.Duelling(who) || who.Combatant != null)
        {
            return null;
        }

        // Already at a fire, or keeping one: not asked to another.
        if (who.Resolve?.Deed is BotFireside or BotKindle)
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

        if (who.HitsMax <= 0 || who.Hits < who.HitsMax * BotHunter.FitAt)
        {
            Hurt++;

            return null;
        }

        var fire = BotCamp.Nearest(who, now, out var seat, out var refusal);

        if (fire == null)
        {
            switch (refusal)
            {
                case BotCamp.Refusal.Full:
                    {
                        Full++;

                        break;
                    }
                case BotCamp.Refusal.Unwelcome:
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

        Offered++;

        return new BotFireside(fire, seat, BotCamp.Claim(who, BotFireside.Prior), false);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked to a fire"
            : $"{Asked} asked to a fire: {Offered} offered a seat; {TooSoon} within {EveryMs / 60000} minutes of their last, {Hurt} hurt, "
              + $"{Far} with none open within {BotCamp.JoinReach} tiles ({BotCamp.Ending} fires passed over as about to go out), {Full} with the ring full, {Unwelcome} not welcome at it";

    public static void Forget()
    {
        _last.Clear();
        Asked = 0;
        TooSoon = 0;
        Hurt = 0;
        Far = 0;
        Full = 0;
        Unwelcome = 0;
        Offered = 0;
        BotFireside.Forget();
    }
}
