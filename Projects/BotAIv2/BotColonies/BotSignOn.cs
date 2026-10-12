using System;
using Server.Guilds;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a member of a guild with a party mustering the place in it: for a passage out, a member on the dock's land within
/// <see cref="BotColonies.MusterReach"/> of it — one with a pick first, at a better price, since the colony is a mine; for a run
/// home, a colonist on the colony's land whose shift is over. A guild's muster gets through a member's hold (<see cref="BotSignOn.Summons"/>).
/// </summary>
public sealed class BotSigner : IBotProposer
{
    public string Name => "signer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var passages = BotColonies.Passages;

        if (passages.Count == 0 || !BotColonies.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body)
        {
            return null;
        }

        if (body.Guild is not Guild guild || body.Squad != null || body.Tired || BotTrading.Refuses(body))
        {
            return null;
        }

        for (var i = 0; i < passages.Count; i++)
        {
            var passage = passages[i];

            if (passage.Guild != guild || passage.State != BotSailing.Tide.Mustering || passage.Captain == body || passage.Signed(body)
                || passage.Roster.Count >= passage.Wanted || passage.Map != body.Map)
            {
                continue;
            }

            if (passage.Heading == BotSailing.Way.Out)
            {
                var shore = passage.From.First?.Shore ?? passage.From.Shore;

                if (!BotColonies.SameLand(body.Map, body.Location, shore) || BotHelm.Away(body.Location, shore) > BotColonies.MusterReach)
                {
                    continue;
                }
            }
            else
            {
                if (passage.Colony == null || BotColonies.OnLand(body) != passage.Colony
                    || !passage.Colony.Shift.TryGetValue(body.Serial, out var since) || Core.TickCount - since < BotColonies.ShiftMs)
                {
                    continue;
                }
            }

            BotSignOn.Offers++;

            return new BotSignOn(passage, body);
        }

        return null;
    }
}

/// <summary>
/// A member's place in a party: to the dock, to the captain's side, aboard as the helm's company, and ashore — on the island to
/// the colony's site and its square held while the claim is made, or on the mainland home from the colony. Paid nothing itself:
/// the colony is a mine and its takings come in the store and at the counters.
/// </summary>
public sealed class BotSignOn : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSignOn));

    public const string Trade = "sign-on";

    /// <summary>What a place in a party is reckoned at: a pick-holder's, and anybody else's.</summary>
    public static double Prior { get; set; } = 80.0;

    public static double HandsPrior { get; set; } = 40.0;

    /// <summary>How long a member waits at the dock for a captain that has not come before it gives the passage up.</summary>
    public static int WaitMs { get; set; } = 900000;

    /// <summary>How long the party stands on the square once ashore while its claim is open.</summary>
    public static int StandMs { get; set; } = 330000;

    public static long Offers { get; set; }

    public static long Signed { get; private set; }

    public static long Rode { get; private set; }

    public static long Arrived { get; private set; }

    public static long LeftBehind { get; private set; }

    public static long GaveUp { get; private set; }

    public static long Stood { get; private set; }

    private readonly BotSailing _passage;

    private Mobile _body;

    private long _taken;

    private long _ashore;

    private bool _rode;

    private bool _atSide;

    /// <summary>Standing at the dock for a captain still on its way: still on purpose, as the muster is.</summary>
    private bool _waiting;

    public BotSignOn(BotSailing passage, Mobile body)
    {
        _passage = passage;
        _body = body;
    }

    public override string Kind => Trade;

    public override Map Map => _passage.Map;

    public override Point3D Where =>
        _passage.State == BotSailing.Tide.Mustering || !_rode && !Aboard(_body)
            ? _passage.From?.First?.Shore ?? _passage.From?.Shore ?? Point3D.Zero
            : _passage.Goal;

    public override double Expects => _body != null && BotOre.Tool(_body) != null || _passage.Heading == BotSailing.Way.Home ? Prior : HandsPrior;

    public override double Minutes => Math.Max(5.0, BotColonies.MusterMs / 60000.0 + (_passage.Founding ? StandMs / 60000.0 : 0) + 5.0);

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Resumes => true;

    /// <summary>The guild's party calling: it gets through a member's hold as the guild's muster does.</summary>
    public override bool Summons => true;

    public override bool Committed => _atSide || _rode;

    public override bool Still => _atSide || _waiting || Aboard(_body) || _ashore != 0;

    public override double HoldsFor => Minutes;

    public override string Stage =>
        Aboard(_body)
            ? $"at sea with {_passage.Captain?.Name} for {_passage.To?.Name}"
            : _rode
                ? _passage.Heading == BotSailing.Way.Out ? $"ashore on {_passage.To?.Name}, to the colony" : "home from the colony"
                : _atSide
                    ? $"at {_passage.Captain?.Name}'s side at {_passage.From?.Name}, waiting to sail"
                    : $"to the dock at {_passage.From?.Name} to sail with {_passage.Captain?.Name}";

    public override void Taken(IBotWilful bot)
    {
        _body = bot?.Self;
        _taken = Core.TickCount;

        if (_body != null && !_passage.Roster.Contains(_body))
        {
            _passage.Roster.Add(_body);
        }

        Signed++;

        BotVoice.Say(
            _body,
            "guild",
            _passage.Heading == BotSailing.Way.Out ? $"I'll sail with {_passage.Captain?.Name} for {_passage.To?.Name}." : $"My shift is done. I'm going home on {_passage.Captain?.Name}'s ship."
        );
    }

    public override void Drop(IBotWilful bot)
    {
        if (!_rode && _body != null)
        {
            _passage.Roster.Remove(_body);
        }
    }

    private bool Aboard(Mobile body)
    {
        if (body == null || _passage.Captain == null)
        {
            return false;
        }

        return BotHelm.Of(_passage.Captain) is { } voyage && voyage.Crew.Contains(body) && voyage.Boat is { Deleted: false } boat && boat.Contains(body);
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Guild != _passage.Guild)
        {
            return BotDoing.Failed("no body, or no longer of the guild");
        }

        _body = body;

        if (Aboard(body))
        {
            if (!_rode)
            {
                _rode = true;
                Rode++;
            }

            return BotDoing.Work($"at sea with {_passage.Captain?.Name} for {_passage.To?.Name}");
        }

        var map = body.Map;

        // Ashore at the far end: the passage landed (or is over), and the bot stands on its goal's land.
        if (_rode || (_passage.State is BotSailing.Tide.Landed or BotSailing.Tide.Over) && BotColonies.SameLand(map, body.Location, _passage.Goal))
        {
            if (!_rode)
            {
                _rode = true;
                Rode++;
            }

            if (_passage.Heading == BotSailing.Way.Home)
            {
                if (_passage.Colony != null && BotColonies.SameLand(map, body.Location, _passage.Colony.Site))
                {
                    LeftBehind++;

                    return BotDoing.Failed($"still on the colony's island: {_passage.Why ?? "the ship did not get away"}");
                }

                Arrived++;

                return BotDoing.Done($"home from the colony on {_passage.From?.Name}, landed at {_passage.To?.Name}");
            }

            if (!BotColonies.SameLand(map, body.Location, _passage.Goal))
            {
                LeftBehind++;

                return BotDoing.Failed($"put ashore off the colony's land: {_passage.Why ?? "the voyage did not reach it"}");
            }

            if (!body.InRange(_passage.Goal, 4))
            {
                return BotDoing.Walk(map, _passage.Goal, BotArrival.Within(3), $"to {_passage.Guild?.Name}'s colony");
            }

            var now = Core.TickCount;

            if (_ashore == 0)
            {
                _ashore = now;
                Arrived++;
            }

            // A founding party holds its square while the claim on it is open: three of them standing there is the claim's muster.
            if (_passage.Founding && BotClaim.On(map, _passage.Goal) is { } bid && bid.Guild == _passage.Guild?.Name && now - _ashore < StandMs)
            {
                if (now - _ashore < 2000)
                {
                    Stood++;
                }

                return BotDoing.Work($"holding the square at ({_passage.Goal.X}, {_passage.Goal.Y}) while {_passage.Guild?.Name} claims it");
            }

            return BotDoing.Done($"at {_passage.Guild?.Name}'s colony on {_passage.To?.Name}");
        }

        if (_passage.State != BotSailing.Tide.Mustering)
        {
            LeftBehind++;

            return BotDoing.Failed(_passage.State == BotSailing.Tide.Sailing ? $"{_passage.Captain?.Name}'s ship sailed without it" : $"the passage is over: {_passage.Why}");
        }

        var captain = _passage.Captain;

        if (captain is not { Deleted: false, Alive: true })
        {
            return BotDoing.Failed("the captain is gone");
        }

        // The captain at the berth: to its side, where the helm takes the party aboard.
        if (_passage.AtDock != 0)
        {
            _waiting = false;

            if (_passage.AtHand(body))
            {
                _atSide = true;

                return BotDoing.Work($"at {captain.Name}'s side at {_passage.From?.Name}, waiting to sail");
            }

            _atSide = false;

            return BotDoing.Walk(map, captain, BotArrival.Within(2), $"to {captain.Name}'s side at the dock");
        }

        if (Core.TickCount - _taken >= WaitMs)
        {
            GaveUp++;

            return BotDoing.Failed($"waited {WaitMs / 60000} minutes at {_passage.From?.Name} and {captain.Name} never came");
        }

        var shore = _passage.From?.First?.Shore ?? _passage.From?.Shore ?? Point3D.Zero;

        if (!body.InRange(shore, 4))
        {
            _waiting = false;

            return BotDoing.Walk(map, shore, BotArrival.Within(3), $"to the dock at {_passage.From?.Name}");
        }

        _waiting = true;

        return BotDoing.Work($"waiting at the dock at {_passage.From?.Name} for {captain.Name}");
    }

    public static string Describe() =>
        Signed == 0
            ? "nobody has signed on"
            : $"hands: {Offers} places offered, {Signed} signed on, {Rode} rode, {Arrived} arrived, {Stood} held a square being claimed, {LeftBehind} left behind, {GaveUp} gave up waiting";

    public static void Forget()
    {
        Offers = 0;
        Signed = 0;
        Rode = 0;
        Arrived = 0;
        LeftBehind = 0;
        GaveUp = 0;
        Stood = 0;
    }
}
