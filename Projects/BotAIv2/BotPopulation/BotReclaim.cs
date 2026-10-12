using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Going back for what death took. The bot own corpse, and nobody else.
///
/// <para>
/// <b>Only the weapon and the book are bound; everything else is lying where the bot fell.</b> The kit was
/// deliberately narrowed to that — tools wear out and are bought again, coin is meant to move — which makes
/// dying cost a hammer, a pickaxe, a sewing kit, the bandages, the herbs and whatever was in the purse. One
/// warrior-archer died holding four hundred gold and the ledger recorded it plainly: <c>-400 coin</c>. All of
/// it was still there, twenty tiles away, and nothing in the population had any reason to go and get it.
/// </para>
///
/// <para>
/// So this is not a new mechanic so much as the missing half of an old one. The corpse is remembered on the
/// bot at the moment of death — no sweeping the world for it — and it stops being remembered the moment it is
/// emptied, decays, or turns out to hold nothing.
/// </para>
/// </summary>
public sealed class BotReclaim : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "reclaim";

    /// <summary>
    /// What going back for your own things is reckoned at per minute before experience corrects it.
    ///
    /// High, and honestly so: what is in there was bought with work already done, and the alternative is
    /// buying all of it a second time. The measurement will be real — recovered coin lands in the takings as
    /// coin — so the ledger settles this to the truth within a few deaths.
    /// </summary>
    // Lowered from 80 on 16.09.2026: the overstated alarm measured 13/min over 71 outcomes once gear was bound and
    // stayed on the body (build 55), so a corpse holds coin and loot only. Patrick's decision, that evening.
    public static double Prior { get; set; } = 20.0;

    public static double WorkMinutes { get; set; } = 1.5;

    /// <summary>How near the corpse the bot has to be to go through it.</summary>
    public static int Reach { get; set; } = 2;

    private readonly Corpse _corpse;

    private readonly Map _map;

    private readonly Point3D _where;

    private int _taken;

    private int _coins;

    /// <summary>What the corpse held when the walk was offered: coin and goods at the market's price. See <see cref="Expects"/>.</summary>
    private readonly int _worth;

    public BotReclaim(Corpse corpse)
    {
        _corpse = corpse;
        _map = corpse?.Map;
        _where = corpse?.Location ?? Point3D.Zero;
    }

    public BotReclaim(Corpse corpse, int worth) : this(corpse)
    {
        _worth = worth;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>
    /// The claim is what lies in the corpse over the walk's minutes, never under the flat prior (02.10.2026, build 389; Patrick's
    /// option 4 via 5). Twenty-seven reclaims recovered 1,271 of the 16,835gp the dead dropped on build 386: a flat twenty a
    /// minute lost the auction to anything, while a corpse holding 80gp and twenty bandages is worth 67 a minute.
    /// </summary>
    public override double Expects => Math.Max(Prior, _worth / Math.Max(0.1, WorkMinutes));

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Picking your own things back up teaches you nothing at all.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Whatever coin was in it comes back as coin, which the takings measure by themselves.</summary>
    public override double Coin => 1.0;

    /// <summary>
    /// Seen through: a bot going back for its own things is not outbid by a hunt on the way, and is taken up again
    /// after a rescue. Six of the nine reclaim drops in the first quarter hour of build 269 were one or the other, and
    /// the corpse does not wait. No clock inside.
    /// </summary>
    public override bool Steadfast => true;

    /// <summary>
    /// Held for <see cref="HoldMinutes"/> rather than the will's eight-minute cap: a corpse is the bot's own gear and purse, and
    /// three of the four dropped reclaims of build 386 were "outbid by restock" at 8.1 minutes, still walking to it. A corpse
    /// that has decayed by the time it arrives ends the walk "there was nothing left of it" (finished, honestly), so the longer
    /// hold costs nothing stuck; the trek watchdog runs under it.
    /// </summary>
    public override double HoldsFor => HoldMinutes;

    /// <summary>Minutes a walk back to one's own corpse is held for whatever it pays. See <see cref="HoldsFor"/> (build 386).</summary>
    public static double HoldMinutes { get; set; } = 15.0;

    public override int Made => 0;

    public override string Stage =>
        _taken > 0 ? $"recovered {_taken} things and {_coins}gp" : "going back for its own things";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (_corpse == null || _corpse.Deleted || _corpse.Map != _map)
        {
            // Decayed, or somebody else was there first. Not a failure worth marking the ground for: the
            // place did nothing wrong.
            Forget(bot);

            return BotDoing.Done("there was nothing left of it");
        }

        if (!body.InRange(_corpse.Location, Reach))
        {
            return BotDoing.Walk(_map, _corpse.Location, BotArrival.Within(Reach), "back for its own things");
        }

        Empty(body);

        // Straight back on. Everything recovered arrives in the pack, and a bot that walks away from its own
        // corpse with its weapon stowed is a bot walking to its next one.
        (bot as BotMobile)?.Rearm();

        Forget(bot);

        if (BotUndertaker.Below(_where))
        {
            BotUndertaker.Reached(_taken, _coins);
        }

        return BotDoing.Done($"{_taken} things and {_coins}gp back off its own corpse");
    }

    /// <summary>
    /// Everything the bot can carry, back into the pack.
    ///
    /// Weight is the limit here as it is on any corpse: what does not fit stays, and the bot may well come
    /// back for it, because the corpse goes on being remembered until it is empty.
    /// </summary>
    private void Empty(Mobile body)
    {
        Strip(body, _corpse, out var taken, out var coins);

        _taken += taken;
        _coins += coins;
    }

    /// <summary>
    /// Everything on a corpse the bot can carry, into its pack: all the coin, and the rest until the pack is at four-fifths of what it
    /// carries. Shared by the walk back (<see cref="Advance"/>) and by a body raised where it fell, which goes through its own corpse
    /// at once (<see cref="BotRaise.Rise"/>, <c>BotDelveParty.Fell</c>) — 02.10.2026.
    /// </summary>
    public static void Strip(Mobile body, Corpse corpse, out int taken, out int coins)
    {
        taken = 0;
        coins = 0;

        var pack = body?.Backpack;

        if (pack == null || corpse is not { Deleted: false })
        {
            return;
        }

        var ceiling = BotLadder.Ceiling(body) * 0.8;

        // A snapshot: moving things out mutates the list being read.
        List<Item> lying = [.. corpse.Items];

        for (var i = 0; i < lying.Count; i++)
        {
            var item = lying[i];

            if (item == null || item.Deleted || !item.Movable)
            {
                continue;
            }

            if (item is Gold coin)
            {
                coins += coin.Amount;

                pack.DropItem(coin);

                continue;
            }

            if (BotLadder.Load(body) >= ceiling)
            {
                break;
            }

            pack.DropItem(item);

            taken++;
        }
    }

    private void Forget(IBotWilful bot)
    {
        if (bot is BotMobile mobile && ReferenceEquals(mobile.Remains, _corpse))
        {
            mobile.Remains = null;
        }
    }
}

/// <summary>
/// Offers a bot the trip back to its own corpse, and only its own.
///
/// <para>
/// On the ordinary rung, competing on the same arithmetic as everything else, which is right: a pickaxe and
/// four hundred gold lying in a field is worth going for, and a corpse holding a handful of bandages is not
/// worth a walk across Britain. The ledger settles which is which from what actually comes back.
/// </para>
/// </summary>
public sealed class BotUndertaker : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotUndertaker));

    public string Name => "Undertaker";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body is not { Deleted: false, Alive: true } || bot is not BotMobile mobile)
        {
            return null;
        }

        var corpse = mobile.Remains;

        if (corpse is not { Deleted: false } || corpse.Map != body.Map || corpse.Map == Map.Internal)
        {
            mobile.Remains = null;

            return null;
        }

        // Nothing left in it. Emptied by somebody, or it only ever held bound gear, which came back with the
        // bot when it rose.
        if (corpse.Items.Count == 0)
        {
            mobile.Remains = null;

            return null;
        }

        if (!BotPopulation.Within(body.Map, corpse.Location))
        {
            // Past the roam in the deep (the Lost Lands): no walker plans that far, so it is let go and said, never skipped in silence.
            if (Deep && Below(corpse.Location))
            {
                DeepNoWay++;
                LetGo(mobile, corpse, Worth(corpse), "it lies past the island's roam");
            }

            return null;
        }

        // <b>A corpse underground is judged for this walker, on the ground where it lies and the road to it (02.10.2026).</b> Patrick:
        // "It is not right that bots dying in dungeons cannot loot themselves. Either the bot must run back to loot itself, or the group
        // should carry resurrection scrolls". See Below.
        if (Deep && Below(corpse.Location))
        {
            return Underground(mobile, body, corpse);
        }

        // <b>A corpse in a cave is gone back for by the strong, or with a party (29.09.2026).</b> Patrick: "do not forget
        // the moment the bots want their things back". With the mouths open the walk to a corpse in the Orc Caves is a walk
        // any bot can plan, and a novice that goes alone for its bandages dies beside them. The measure is the venturer's:
        // the bot's own strength against the dungeon's worst, or a party it is already down with. Kept for Deep off: measured on
        // 02.10.2026 it refused all 47 corpses underground since the wipe, the Orc Caves asking 7,028 and Covetous 42,331 of bots of
        // 84 to 2,893, and said nothing.
        if (BotDungeon.Holding(corpse.Location) is { } deep && !BotDelveParty.Delving(mobile)
            && BotThreat.Power(body) < deep.Worst * BotVenturer.Odds)
        {
            Outmatched++;

            return null;
        }

        // <b>Only a corpse worth the walk, and priced by what lies in it (02.10.2026, build 389; Patrick's option 4 via 5).</b>
        // A corpse holding a few bandages is let go at once rather than offered at a flat claim until it rots; one holding
        // coin and supplies is offered at what it holds, so it wins the auction it used to lose.
        var worth = Worth(corpse);

        if (worth < Least)
        {
            Petty++;
            mobile.Remains = null;

            return null;
        }

        // <b>Not sent back a second time to where it died again (review of build 394).</b> Neither the held ground nor the fight read
        // now remembers deaths, so a walk back the reading got wrong could be offered again at every rising. See Again.
        if (Again(mobile, corpse))
        {
            Refused(corpse, ref _islandAgain);
            mobile.Remains = null;

            return null;
        }

        // <b>Ground its company held is ground it may walk back onto (02.10.2026, build next; Patrick's option 3).</b> See Held.
        var held = Held(mobile, corpse);

        // And on the island, the ground where the corpse lies judged for this walker as it is now (build next). The record of the
        // square (BotQuad.Muscle) was the gate until build 392, and it refused nearly every corpse: a square counts the deaths on
        // it, so the square a bot has just died on asks for more than that bot brings — 239 of 266 deaths on 390–392 lay on ground
        // asking more than 1.5 times the fallen side's strength, 208 of them over 8,000. Four died going back for their things in
        // the first twenty minutes of build 279, so the walk is still judged: see Island. IslandRecord brings the old gate back.
        if (IslandRecord && !held)
        {
            if (BotQuad.Muscle(body.Map, corpse.Location) > BotThreat.Power(body) * BotVenturer.Odds)
            {
                Refused(corpse, ref _islandGround);

                return null;
            }
        }
        else if (Island(mobile, body, corpse, held))
        {
            return null;
        }

        if (held)
        {
            HeldGone++;
        }

        Gone++;
        Sought += worth;
        WalkedBack(mobile, corpse);

        return new BotReclaim(corpse, worth);
    }

    // ---- The walk back on the island, and the company's ground (02.10.2026, build next; Patrick's option 3) -----------------------

    /// <summary>Whether the island's corpses are judged by the record of the square they lie on (build 392) rather than by the walker and the fight there now.</summary>
    public static bool IslandRecord { get; set; } = false;

    /// <summary>How near its corpse a fallen bot's company must still stand for the ground to count as held. The raising's reach.</summary>
    public static int HeldReach { get; set; } = 24;

    /// <summary>How long after a fall the company it fell in is remembered for it. The corpse's own life: fourteen minutes.</summary>
    public static int HeldMs { get; set; } = 840000;

    private static readonly Dictionary<Serial, (BotSquad Squad, long Tick)> _fellWith = [];

    private static readonly HashSet<Serial> _judged = [];

    private static long _islandGround;

    private static long _islandRoad;

    private static long _islandDust;

    private static long _islandAgain;

    /// <summary>Where each bot was last offered or pressed the walk back to a corpse on the island, which corpse, and when.</summary>
    private static readonly Dictionary<Serial, (Map Map, Point3D At, Serial Corpse, long Tick)> _walkedBack = [];

    private static void WalkedBack(BotMobile mobile, Corpse corpse)
    {
        if (_walkedBack.Count > 2048)
        {
            _walkedBack.Clear();
        }

        _walkedBack[mobile.Serial] = (corpse.Map, corpse.Location, corpse.Serial, Core.TickCount);
    }

    /// <summary>
    /// <b>Whether this is a second corpse where the bot was sent back to its first (review of build 394).</b> Within
    /// <see cref="HeldMs"/> of being offered or pressed the walk back to one corpse, a different corpse of its own within
    /// <see cref="HeldReach"/> of it means it died there again: it is not sent back a third time. Bounds the walk back — held or
    /// read by the fight there now, neither of which counts deaths — to one per place per corpse's life.
    /// </summary>
    private static bool Again(BotMobile mobile, Corpse corpse)
    {
        if (!_walkedBack.TryGetValue(mobile.Serial, out var last))
        {
            return false;
        }

        if (Core.TickCount - last.Tick > HeldMs)
        {
            _walkedBack.Remove(mobile.Serial);

            return false;
        }

        return last.Corpse != corpse.Serial && last.Map == corpse.Map
               && Utility.InRange(last.At, corpse.Location, Math.Max(1, HeldReach));
    }

    /// <summary>Corpses gone back for on ground the fallen's company held, and of those the walks pressed on the bot as it rose.</summary>
    public static long HeldGone { get; private set; }

    public static long RisenPressed { get; private set; }

    /// <summary>
    /// A member fell in its company: the company is remembered for its corpse. From <c>BotSquad.Detach</c>, where a dead member leaves
    /// the roster.
    /// </summary>
    public static void FellIn(BotMobile dead, BotSquad squad)
    {
        if (dead == null || squad == null)
        {
            return;
        }

        if (_fellWith.Count > 2048)
        {
            _fellWith.Clear();
        }

        _fellWith[dead.Serial] = (squad, Core.TickCount);
    }

    /// <summary>
    /// <b>Whether the company this bot fell in holds the ground its corpse lies on:</b> the company still stands, its leader alive
    /// within <see cref="HeldReach"/> of the corpse, and nothing it is fighting within <see cref="BotRaise.ClearReach"/> of the body.
    /// Patrick (02.10.2026): "a company that sees its member fall on ground it just held should let the member loot its corpse
    /// right after rising". Measured on 390–392 (14:21–16:12): 112 of 266 deaths were enlisted, and 5 corpses were gone back for and
    /// reached in all; a bot rises a median 419 tiles from its corpse and took a prowl first 100 times of 263.
    /// </summary>
    private static bool Held(BotMobile mobile, Corpse corpse)
    {
        if (!_fellWith.TryGetValue(mobile.Serial, out var fell))
        {
            return false;
        }

        if (Core.TickCount - fell.Tick > HeldMs || fell.Squad is not { Disbanded: false } squad)
        {
            _fellWith.Remove(mobile.Serial);

            return false;
        }

        if (squad.Leader?.Self is not { Deleted: false, Alive: true } leader || ReferenceEquals(leader, mobile) || leader.Map != corpse.Map
            || !leader.InRange(corpse.Location, Math.Max(1, HeldReach)))
        {
            return false;
        }

        var foe = squad.Focus;

        return foe is not { Deleted: false, Alive: true } || foe.Map != corpse.Map
               || !Utility.InRange(foe.Location, corpse.Location, Math.Max(1, BotRaise.ClearReach));
    }

    /// <summary>
    /// Whether a corpse on the island is refused to this walker: let go when it will be dust before the walk arrives
    /// (<see cref="DustMinutes"/>, <see cref="TilesPerMinute"/>); kept, but not walked to now, when the fight at its tile
    /// (<see cref="BotZones.LevelAt(Map, int, int, out float, out float)"/>, what lives there now and what its spawners promise —
    /// not asked of ground its company holds, <paramref name="held"/>) or the road there (<see cref="BotDanger.Road"/>, always)
    /// asks more than the bot — or its company — brings × <see cref="BotVenturer.Odds"/>. Each corpse counted once for its reason.
    /// True when refused.
    /// </summary>
    private static bool Island(BotMobile mobile, Mobile body, Corpse corpse, bool held)
    {
        var map = body.Map;
        var at = corpse.Location;
        var tiles = Tiles(body.Location, at);
        var left = DustMinutes - (Core.Now - corpse.TimeOfDeath).TotalMinutes;

        if (tiles / Math.Max(0.1, TilesPerMinute) > left)
        {
            Refused(corpse, ref _islandDust, false);
            mobile.Remains = null;

            return true;
        }

        var brought = BotQuad.Strength(body);
        var bar = brought * BotVenturer.Odds;

        BotZones.LevelAt(map, at.X, at.Y, out _, out var fight);

        if (!held && fight > bar)
        {
            Refused(corpse, ref _islandGround);

            return true;
        }

        if (BotDanger.Road(body, map, body.Location, at, brought, out _, out _, out _))
        {
            Refused(corpse, ref _islandRoad);

            return true;
        }

        return false;
    }

    /// <summary>
    /// A corpse refused for a reason, counted once for it however many reviews refuse it, and once in <see cref="Outmatched"/> when
    /// the reason is the ground (<paramref name="ground"/>). Until build 392 that count was bumped on every review of every risen bot.
    /// </summary>
    private static void Refused(Corpse corpse, ref long bucket, bool ground = true)
    {
        if (corpse == null || _judged.Contains(corpse.Serial))
        {
            return;
        }

        if (_judged.Count > 4096)
        {
            _judged.Clear();
        }

        _judged.Add(corpse.Serial);
        bucket++;

        if (ground)
        {
            Outmatched++;
        }
    }

    /// <summary>
    /// A bot back on its feet (<c>BotPopulation.Revive</c>): when the company it fell in holds the ground its corpse lies on, the walk
    /// back is the first thing it does — pressed, as a company's call is — rather than an offer weighed against a prowl.
    /// </summary>
    public static void Risen(BotMobile bot)
    {
        if (bot is not { Deleted: false, Alive: true } || bot.Remains is not { Deleted: false } corpse || corpse.Map != bot.Map
            || corpse.Items.Count == 0 || !Held(bot, corpse))
        {
            return;
        }

        var worth = Worth(corpse);

        if (worth < Least || Again(bot, corpse) || Island(bot, bot, corpse, true))
        {
            return;
        }

        if (!BotWill.Press(bot, new BotReclaim(corpse, worth), $"its company held the ground where it fell, {worth}gp in its corpse"))
        {
            return;
        }

        WalkedBack(bot, corpse);
        RisenPressed++;
        HeldGone++;
        Gone++;
        Sought += worth;
    }

    /// <summary>
    /// <b>The walk back underground (02.10.2026).</b> Measured from 01.10 11:32 to 02.10 11:35: 47 deaths underground — 31 in
    /// Covetous, 6 in the Orc Caves, 4 in the Lost Lands, 3 in Despise, 2 in Shame, 1 more — and not one corpse gone back for
    /// (1,218gp of pocket and 283gp of supplies left on them). The gate below refused every one of them for the dungeon's worst room
    /// (Covetous: 28,221 of strength, asking 42,331 of a bot that brings at most 2,893), whatever lay where the corpse did, and said
    /// nothing, so the bot simply went about its day and the corpse rotted.
    ///
    /// <para>
    /// Now the corpse is judged where it lies and for the bot that would walk to it, by the same readings a lone hunter's ground is
    /// judged by (<c>BotHunter</c>): a way there over the gates (<see cref="BotGates.Joined"/>; the walk itself is routed through the
    /// teleporters by the journey); time to get there before the bones are gone (a player's corpse is bones at seven minutes and
    /// gone at fourteen, <c>Corpse</c>); the record of the square it lies in (<see cref="BotQuad.Muscle"/>) and the fight at its tile
    /// (<see cref="BotZones.LevelAt(Map, int, int, out float, out float)"/>) against what the bot brings × <see cref="BotVenturer.Odds"/>;
    /// and the road, island side to the mouth and cave side from it (<see cref="BotDanger.Road"/>). A corpse that passes is gone
    /// back for at what it holds; one that does not is let go at once and the bot says why (<see cref="LetGo"/>), counted once.
    /// A delver still down there with its party goes as before.
    /// </para>
    /// </summary>
    private static BotDeed Underground(BotMobile mobile, Mobile body, Corpse corpse)
    {
        var worth = Worth(corpse);

        if (worth < Least)
        {
            DeepPetty++;
            LetGo(mobile, corpse, worth, $"it holds {worth}gp, under the {Least}gp worth the walk");

            return null;
        }

        if (!BotDelveParty.Below(mobile) && Barred(body, corpse) is { } why)
        {
            LetGo(mobile, corpse, worth, why);

            return null;
        }

        Gone++;
        Sought += worth;
        DeepGone++;
        DeepSought += worth;

        return new BotReclaim(corpse, worth);
    }

    /// <summary>Why a lone bot may not go down to this corpse, or null when it may. Counts the reason. See <see cref="Underground"/>.</summary>
    private static string Barred(Mobile body, Corpse corpse)
    {
        var map = body.Map;
        var at = corpse.Location;

        if (!BotGates.Joined(map, body.Location, at))
        {
            DeepNoWay++;

            return "there is no way down to it over the gates";
        }

        var mouth = BotGates.Mouth(map, body.Location, at);
        var tiles = mouth == null ? Tiles(body.Location, at) : Tiles(body.Location, mouth.From) + Tiles(mouth.To, at);
        var left = DustMinutes - (Core.Now - corpse.TimeOfDeath).TotalMinutes;

        if (tiles / Math.Max(0.1, TilesPerMinute) > left)
        {
            DeepDust++;

            return $"it lies {tiles} tiles off by the gates and will be gone in {Math.Max(0.0, left):F0} minutes";
        }

        var brought = BotQuad.Strength(body);
        var bar = brought * BotVenturer.Odds;
        var record = BotQuad.Muscle(map, at);

        if (record > bar)
        {
            DeepGround++;

            return $"the ground where it lies asks {record:F0} by the record of what has killed there, against the {brought:F0} it brings";
        }

        BotZones.LevelAt(map, at.X, at.Y, out _, out var fight);

        if (fight > bar)
        {
            DeepGround++;

            return $"the fight where it lies is {fight:F0} against the {brought:F0} it brings";
        }

        var road = mouth == null
            ? BotDanger.Road(body, map, body.Location, at, brought, out var asked, out var where, out var what)
            : BotDanger.Road(body, map, body.Location, mouth.From, brought, out asked, out where, out what)
              || BotDanger.Road(body, map, mouth.To, at, brought, out asked, out where, out what);

        if (road)
        {
            DeepRoad++;

            return $"the road down asks {asked:F0} at ({where.X}, {where.Y}) by {what}, against the {brought:F0} it brings";
        }

        return null;
    }

    private static int Tiles(Point3D a, Point3D b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>Lets the corpse go, says so once, and counts what was left in it.</summary>
    private static void LetGo(BotMobile mobile, Corpse corpse, int worth, string why)
    {
        mobile.Remains = null;
        DeepLeft++;
        DeepLeftGold += worth;

        var where = corpse.Location;
        var deep = BotDungeon.Holding(where)?.Name ?? "the deep";

        logger.Information(
            "{Name} lets its corpse in {Deep} at ({X}, {Y}) go, {Worth}gp in it: {Why}",
            mobile.Name,
            deep,
            where.X,
            where.Y,
            worth,
            why
        );

        BotEvents.Post(
            "corpse",
            mobile,
            $"let its corpse in {deep} at ({where.X}, {where.Y}) go, {worth}gp in it: {why}",
            null,
            BotJson.Object(
                w =>
                {
                    w.WriteString("deep", deep);
                    w.WriteNumber("cx", where.X);
                    w.WriteNumber("cy", where.Y);
                    w.WriteNumber("worth", worth);
                    w.WriteString("why", why);
                }
            )
        );
    }

    /// <summary>Whether this place is underground: inside a dungeon's box, or anywhere east of the island's edge.</summary>
    public static bool Below(Point3D where) => BotDungeon.Under(where) || where.X >= BotGates.Edge;

    /// <summary>A walk back underground reached its corpse and went through it. See <c>BotReclaim.Advance</c>.</summary>
    internal static void Reached(int taken, int coins)
    {
        DeepReached++;
        DeepThings += taken;
        DeepCoin += coins;
    }

    /// <summary>Whether a corpse underground is judged for its walker (true, 02.10.2026) or by the dungeon's worst room (false, build 389).</summary>
    public static bool Deep { get; set; } = true;

    /// <summary>Minutes from a death to a player's corpse being gone: seven to bones and seven more (<c>Corpse</c>).</summary>
    public static double DustMinutes { get; set; } = 14.0;

    /// <summary>
    /// Tiles a walk back is reckoned to cover a minute, gates and detours counted: a hundred and twenty, two-fifths of a bot's running
    /// pace. The minute face down is already gone by the time this is asked.
    /// </summary>
    public static double TilesPerMinute { get; set; } = 120.0;

    /// <summary>Corpses underground gone back for and what they held; reached, and what came off them.</summary>
    public static long DeepGone { get; private set; }

    public static long DeepSought { get; private set; }

    public static long DeepReached { get; private set; }

    public static long DeepThings { get; private set; }

    public static long DeepCoin { get; private set; }

    /// <summary>Corpses underground let go and said so, what they held, and why: petty, no way, rotting first, the ground, the road.</summary>
    public static long DeepLeft { get; private set; }

    public static long DeepLeftGold { get; private set; }

    public static long DeepPetty { get; private set; }

    public static long DeepNoWay { get; private set; }

    public static long DeepDust { get; private set; }

    public static long DeepGround { get; private set; }

    public static long DeepRoad { get; private set; }

    /// <summary>What a corpse holds, at the market's price: coin as coin, everything movable at what one is worth.</summary>
    private static int Worth(Corpse corpse)
    {
        var worth = 0;
        var lying = corpse.Items;

        for (var i = 0; i < lying.Count; i++)
        {
            var item = lying[i];

            if (item == null || item.Deleted || !item.Movable)
            {
                continue;
            }

            if (item is Gold coin)
            {
                worth += coin.Amount;

                continue;
            }

            worth += BotMarket.Worth(item.GetType(), 1) * Math.Max(1, item.Amount);
        }

        return worth;
    }

    /// <summary>A corpse holding less than this, coin and goods together, is not gone back for. Twenty.</summary>
    public static int Least { get; set; } = 20;

    /// <summary>Corpses gone back for, what they held, and corpses left for holding under <see cref="Least"/>.</summary>
    public static long Gone { get; private set; }

    public static long Sought { get; private set; }

    public static long Petty { get; private set; }

    /// <summary>Corpses in a dungeon left for now, the bot being too weak to go back for them alone. See Propose.</summary>
    public static long Outmatched { get; private set; }

    /// <summary>For the Money line.</summary>
    public static string Describe() =>
        $"reclaims: {Gone} corpses worth {Sought}gp gone back for, {Petty} left for holding under {Least}gp, {Outmatched} left for the ground round them; "
        + $"underground: {DeepGone} worth {DeepSought}gp gone back for through the gates and {DeepReached} reached ({DeepThings} things, {DeepCoin}gp back), "
        + $"{DeepLeft} let go and said so with {DeepLeftGold}gp in them ({DeepPetty} under {Least}gp, {DeepNoWay} with no way down, {DeepDust} to be gone before it got there, {DeepGround} for the ground where they lay, {DeepRoad} for the road down); "
        + $"on the island ({(IslandRecord ? "by the square's record" : "by the walker against the fight there now")}, corpses counted once): {_islandGround} too strong where they lie, {_islandRoad} for the road there, {_islandDust} let go to be dust before the walk arrived, {_islandAgain} let go for lying where it had died again within {HeldMs / 60000} minutes of being sent back; "
        + $"{HeldGone} walks back offered on ground the fallen's company held, as offers are counted above ({RisenPressed} pressed as the first thing it did on rising)";

    public static void Forget()
    {
        Gone = 0;
        Sought = 0;
        Petty = 0;
        Outmatched = 0;
        DeepGone = 0;
        DeepSought = 0;
        DeepReached = 0;
        DeepThings = 0;
        DeepCoin = 0;
        DeepLeft = 0;
        DeepLeftGold = 0;
        DeepPetty = 0;
        DeepNoWay = 0;
        DeepDust = 0;
        DeepGround = 0;
        DeepRoad = 0;
        HeldGone = 0;
        RisenPressed = 0;
        _islandGround = 0;
        _islandRoad = 0;
        _islandDust = 0;
        _islandAgain = 0;
        _walkedBack.Clear();
        _judged.Clear();
        _fellWith.Clear();
    }
}
