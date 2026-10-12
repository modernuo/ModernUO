using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// Where each member of a squad ought to be standing, worked out rather than assigned.
///
/// <para>
/// <b>Nobody is told anything.</b> Every member computes every station from the same three facts — the
/// roster, the anchor and the threat axis — in the same order, and therefore arrives at the same answer.
/// This is the one genuinely good idea the first version had about collective behaviour, and it came out of
/// spreading bots over a hunting ground: order by serial, cut the ground into a grid, take the cell at your
/// own index. No messages, so no desynchronisation, no orphaned assignments, and two bots never pick the
/// same patch. A shared mind does not need a shared mailbox; it needs shared arithmetic.
/// </para>
///
/// <para>
/// <b>The shape is not fixed — the order is.</b> A formation can be any shape at all, and trying to specify
/// one would be specifying the wrong thing. What must hold is the ordering along the line to the threat:
/// blades in front, bows behind them, casters and healers behind those, everybody else at the back. That is
/// a single number per role, and every arrangement that satisfies it is acceptable.
/// </para>
///
/// <para>
/// <b>Built from the leader, forward of the leader.</b> If the leader is an archer, the melee ring is still
/// in front — in front of <em>him</em>. The anchor is whoever the squad is organised around, not whoever
/// happens to be closest to the enemy.
/// </para>
/// </summary>
public static class BotFormation
{
    /// <summary>
    /// Where each role stands, measured along the line to the threat. Positive is towards it.
    ///
    /// The spread is deliberately shallow — four tiles from the front rank to the back — because the squad
    /// also has to hold together within about five tiles. A deeper formation looks better on paper and
    /// produces a rear rank that is out of earshot of its own front.
    /// </summary>
    public static int RingFor(BotRole role) =>
        role switch
        {
            BotRole.Melee => 2,
            BotRole.Ranged => 0,
            BotRole.Caster => -1,
            BotRole.Medic => -1,
            _ => -2
        };

    /// <summary>
    /// Tiles between neighbours in the same rank. Two rather than one so that a file has room to walk past
    /// its neighbour instead of asking it to move.
    /// </summary>
    public const int FileSpacing = 2;

    /// <summary>
    /// How far a station may be from the anchor at all. The cohesion rule from the other side: a station
    /// nobody can hold within this distance is not a station, it is a bot wandering off.
    /// </summary>
    public const int MaxSpread = 5;

    /// <summary>
    /// Where each role stands once there is something to fight, measured in tiles from the creature itself.
    ///
    /// <para>
    /// <b>A rank is a distance from the enemy, and it was being measured from us.</b> Every station came off
    /// the anchor — the member in contact, or the leader while nobody had been hit — with the blades two
    /// tiles in front of it. So a company called by a healer stationed its blades two tiles ahead of the
    /// healer and twelve tiles short of the wraith, pressed a combatant none of them could touch, and broke
    /// off ninety seconds later with the target's health exactly where it started. The evening of 23.08.2026
    /// has it fifty-six times against nineteen kills, and the same wraith re-engaged every two minutes all
    /// night. Nothing in the squad was broken: it was laid out from the wrong origin, and the enemy is the
    /// only origin a fight has.
    /// </para>
    ///
    /// <para>
    /// Melee is <see cref="Contact"/> — one tile, meaning the eight tiles that touch the thing, one to a
    /// blade. The rest keep the ordering they always had: bows behind the blades, spells and bandages behind
    /// the bows, everybody else out of it.
    /// </para>
    /// </summary>
    public static int PressRingFor(BotRole role) =>
        role switch
        {
            BotRole.Melee => Contact,
            BotRole.Ranged => 5,
            BotRole.Caster => 7,
            BotRole.Medic => 7,
            _ => 9
        };

    /// <summary>A blade's own reach. One tile — the ring of eight that touches a creature.</summary>
    public const int Contact = 1;

    /// <summary>
    /// Where a member belongs in a fight, decided by what is in its hands rather than by what its class is
    /// called.
    ///
    /// <para>
    /// <b>A miner with a sword is a sword.</b> Both producing classes are issued a real melee weapon at birth
    /// — <c>BotArsenal.Melee(40.0)</c>, see BotCrafter and BotGatherer — and then the formation read their
    /// class name, filed them under Producer and stationed them <em>nine tiles</em> from the fight. So a
    /// company of five with two gatherers in it brought three blades to the ring and left two armed bots
    /// standing in a field watching. They can fight; they were never asked to.
    /// </para>
    ///
    /// <para>
    /// Only the producing roles are re-read, and only when something is actually held: an empty-handed
    /// gatherer stays out of it, which is the sensible half of what the old rule was trying to say. The reach
    /// of what it holds decides which rank — a bow would go to the archers' rank — because the ring a bot
    /// belongs in is a fact about its reach and never about its job.
    /// </para>
    ///
    /// <para>
    /// <b>A pickaxe counts, and that was decided rather than overlooked.</b> A digging tool was going to be
    /// excluded here — it is a tool, and a miner holding one is working, not soldiering. But a pickaxe and a
    /// hatchet are <c>BaseAxe</c> to the engine: they swing on the same timer as any axe and they do real
    /// damage. Patrick's ruling on being told that was to leave it alone. So the rule stays the simple one,
    /// and it is the honest one — if the thing in its hands can kill, the bot belongs where things are being
    /// killed.
    /// </para>
    /// </summary>
    public static BotRole RoleOf(IBotSquadMember member)
    {
        var role = member?.Class?.Role ?? BotRole.Melee;

        if (role != BotRole.Producer)
        {
            return role;
        }

        // Fists are a weapon to the engine and are not one here — see BotArms.Armed for the same test.
        var held = member.Self?.Weapon;

        if (held is null or Fists)
        {
            return role;
        }

        return held.MaxRange > Contact ? BotRole.Ranged : BotRole.Melee;
    }

    /// <summary>The eight tiles round a thing, clockwise from north.</summary>
    private static readonly (int X, int Y)[] Compass =
    [
        (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)
    ];

    /// <summary>
    /// The order the tiles round the enemy are tried in, as turns off the line our side is standing on:
    /// straight at it first, then out to either hand, and round the back last.
    /// </summary>
    private static readonly int[] Fan = [0, 1, -1, 2, -2, 3, -3, 4];

    /// <summary>Scratch for the share-out. One game thread, so one list is enough.</summary>
    private static readonly List<IBotSquadMember> _peers = [];

    /// <summary>
    /// This member's file in its rank: its place among the company's members of its role in serial order, or -1.
    ///
    /// <para>
    /// <b>Worked out once a roster, not once a member (01.10.2026).</b> Each station sorted the whole rank again, so a round of
    /// stationing cost the square of the company's size: nothing at five, four hundred sorts' worth at twenty, the companies'
    /// ceiling since this night. The files of one company are kept until its roster changes (<see cref="BotSquad.Roster"/>),
    /// one company at a time — a round of stationing is one company's.
    /// </para>
    /// </summary>
    private static int File(BotSquad squad, IBotSquadMember member, BotRole role)
    {
        if (!ReferenceEquals(_filedFor, squad) || _filedAt != squad.Roster)
        {
            Refile(squad);
        }

        if (_files.TryGetValue(member, out var filed) && filed.Role == role)
        {
            return filed.File;
        }

        // A role read off the pack (a producer's weapon, see RoleOf) that changed since the roster was filed: filed afresh, once.
        Refile(squad);

        return _files.TryGetValue(member, out filed) && filed.Role == role ? filed.File : -1;
    }

    private static void Refile(BotSquad squad)
    {
        _filedFor = squad;
        _filedAt = squad.Roster;
        _files.Clear();

        var members = squad.Members;

        foreach (var rank in Ranks)
        {
            _peers.Clear();

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i]?.Self != null && RoleOf(members[i]) == rank)
                {
                    _peers.Add(members[i]);
                }
            }

            _peers.Sort(static (a, b) => a.Self.Serial.Value.CompareTo(b.Self.Serial.Value));

            for (var i = 0; i < _peers.Count; i++)
            {
                _files[_peers[i]] = (rank, i);
            }
        }

        _peers.Clear();
    }

    private static readonly BotRole[] Ranks = Enum.GetValues<BotRole>();

    private static readonly Dictionary<IBotSquadMember, (BotRole Role, int File)> _files = [];

    private static BotSquad _filedFor;

    private static int _filedAt;

    /// <summary>
    /// The station this member should be holding.
    ///
    /// <para>
    /// Two checks before the answer is handed back, and both come from the first version's bill. The point
    /// must be somewhere a body can stand — worked out from the ground near the anchor's own height, never
    /// from a wide vertical window, because a wide window in a built-up world hands back a roof. And it must
    /// be somewhere this bot can actually walk to, checked with the same search that will later walk it: the
    /// first version's rally points were not checked, and landed inside buildings and behind the enemy.
    /// </para>
    /// </summary>
    public static Point3D StationFor(BotSquad squad, IBotSquadMember member)
    {
        if (squad == null || member?.Self == null)
        {
            return Point3D.Zero;
        }

        var map = squad.Map;

        if (map == null || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var anchor = squad.Anchor;
        var role = RoleOf(member);

        // Everyone of the same role, in serial order. Deterministic, so every member of the squad works out
        // the same file for the same bot without anybody being told.
        var file = File(squad, member, role);

        if (file < 0)
        {
            return Point3D.Zero;
        }

        // A fight is laid out round the thing being fought. See PressRingFor for what that cost to learn. A company forming up
        // short of its enemy is laid out as a line at the rally, facing it (06.10.2026, company16): see BotSquad.Mustering.
        if (squad.Stance == BotSquadStance.Fighting && !squad.Mustering && squad.Focus is { Deleted: false, Alive: true })
        {
            return PressStation(map, squad, member, role, file);
        }

        // 0, +1, -1, +2, -2 ... so the rank grows outwards from the axis rather than off to one side.
        var lateral = (file + 1) / 2 * (file % 2 == 0 ? 1 : -1) * FileSpacing;
        var ring = RingFor(role);

        var (fx, fy) = squad.Axis;

        // Ninety degrees off the axis. Integer arithmetic on a unit offset, so no trigonometry and no drift.
        var (rx, ry) = (-fy, fx);

        var x = anchor.X + fx * ring + rx * lateral;
        var y = anchor.Y + fy * ring + ry * lateral;

        return Reachable(map, x, y, anchor, member);
    }

    /// <summary>
    /// The station this member holds while the squad is fighting: a place at the creature, not a place in a
    /// line drawn from us.
    ///
    /// <para>
    /// <b>The blades get a tile each and no two of them get the same one.</b> A rank with a file spacing puts
    /// the second blade two tiles from the thing it is supposed to be hitting — which is one tile past the
    /// only range it has — so the man in the middle fought and the rest watched. Eight tiles touch a
    /// creature; the fan hands them out in a fixed order that starts on whichever side the squad is coming
    /// from, and the file comes off the serial ordering every member computes identically, so nobody is told
    /// anything and nobody is sent where somebody else is already going. It is also the answer to standing in
    /// a heap round a mob: a heap is what you get when everybody is sent to one tile.
    /// </para>
    ///
    /// <para>
    /// <b>No reachability search here, unlike a march, and that is the point of the difference.</b> Marching
    /// to an unreachable station is a bot standing in a field; walking at an unreachable enemy is a bot
    /// getting as close to it as the world permits, which is exactly what a partial path delivers and exactly
    /// what is wanted. It also keeps a fight from costing eight A* searches a member a beat.
    /// </para>
    /// </summary>
    private static Point3D PressStation(Map map, BotSquad squad, IBotSquadMember member, BotRole role, int file)
    {
        var at = squad.Focus.Location;
        var (ax, ay) = squad.Axis;

        // Rotated as the squad keeps failing to take up its stations, so a blade whose tile is behind a fence
        // tries the next one round rather than the same one for ever.
        var turn = file + squad.Attempt;

        // The compass line our side of the fight is on. Both ranks are laid out from it: the blades fan
        // around it onto the eight tiles that touch the creature, and the shooters go a quarter circle off it
        // so that their line to the target is not through the blades.
        var face = Bearing(ax, ay);

        if (role == BotRole.Melee)
        {
            for (var i = 0; i < Compass.Length; i++)
            {
                var (dx, dy) = Compass[((face + Fan[Math.Abs(turn + i) % Fan.Length]) % 8 + 8) % 8];

                if (Stand(map, at.X + dx, at.Y + dy, at, out var spot))
                {
                    return spot;
                }
            }

            return Point3D.Zero;
        }

        var ring = PressRingFor(role);

        // <b>Off to the flank, not behind the blades, and standing behind them was the whole trouble.</b> A
        // rank measured straight back down the line our side came in on puts every bow and every spell on
        // exactly the axis the melee ring is standing on — so the shot's path to the creature runs through
        // our own front rank, which is the one place an arrow must not go. Turned a quarter circle, the same
        // distance from the same creature becomes a clear line: the blades are in front of the thing and the
        // shooters are beside it. Alternating hands by file spreads them to both sides rather than stacking
        // one wing, and it costs nothing — the ring distance is unchanged, so nobody is further from the
        // fight than their weapon wants to be.
        var hand = turn % 2 == 0 ? 2 : -2;
        var (fx, fy) = Compass[((face + hand) % 8 + 8) % 8];

        // Files beyond the first pair fan further round rather than piling onto the two flanks.
        var wider = turn / 2;
        var (wx, wy) = Compass[((face + hand + (hand > 0 ? wider : -wider)) % 8 + 8) % 8];

        // Pulled in towards the fight a tile at a time when the ground behind will not hold anybody — never
        // pushed further out, because further out is out of the fight.
        for (var back = 0; ring - back >= 2; back++)
        {
            var r = ring - back;

            if (Stand(map, at.X + wx * r, at.Y + wy * r, at, out var spot))
            {
                return spot;
            }

            // The wider fan may be facing a wall. The plain flank is the fallback before giving ground.
            if (Stand(map, at.X + fx * r, at.Y + fy * r, at, out spot))
            {
                return spot;
            }
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// How far above the ground a pair of eyes sits, for asking whether a place a bot is not standing in yet
    /// could see what the company is fighting. Taken from <c>BotSlay</c>, which took it from the engine:
    /// <c>Map.LineOfSight</c> raises both ends by this before tracing, and a line traced from the floor
    /// clips on the first step of ground it crosses and calls every open field blind.
    /// </summary>
    private const int Eye = 14;

    /// <summary>
    /// Whether a blow could leave this tile at all.
    ///
    /// <para>
    /// <b>The half of a station nobody was asking for, and it was the whole of the trouble.</b> Every swing
    /// in the engine is gated on <c>InLOS</c> — see <c>Mobile.CheckCombatTime</c>, which returns without so
    /// much as advancing the swing clock when the line is broken — so a blade standing on the tile that
    /// touches a wraith through a crypt wall is not fighting it, it is watching it, and there is no line
    /// anywhere saying so. The evening of 03.09.2026 has 71 companies breaking off with the target's health
    /// exactly where it started against 103 kills, and the break-off line reading <c>3 of 3 in reach,
    /// nearest 1 tiles off</c> — three bots touching a troll for twelve seconds and never once swinging.
    /// </para>
    ///
    /// <para>
    /// Asked of the tile before anybody is sent to it rather than of the bot after it arrives, because the
    /// cure for a blind station is a different station and the formation is the only thing that picks one.
    /// The lone hunter has had this since 25.08.2026 (<c>BotSlay</c>); a company fight is the same rule and
    /// was written apart from it, which is the shape of defect this project keeps paying for.
    /// </para>
    /// </summary>
    private static bool Sighted(Map map, int x, int y, int z, Point3D at) =>
        map.LineOfSight(new Point3D(x, y, z + Eye), new Point3D(at.X, at.Y, at.Z + Eye));

    /// <summary>
    /// Whether a body fits on this tile, at the height of whatever the squad is standing round — and whether
    /// it could hit the thing from there. See <see cref="Sighted"/>.
    /// </summary>
    /// <summary>
    /// How far above or below the creature a fighting station may stand.
    ///
    /// <para>
    /// <b>Eight, not the standing reach of twenty-four.</b> A station is a tile beside the creature, and the
    /// wide tolerance let it settle on the top of a cave wall or a dungeon ledge twenty tiles up — standable
    /// ground, in sight, and not a place any road leads to from the corridor floor. Every such station was
    /// asked for every beat with <c>Exactly</c> precision and answered "partial" after several thousand tiles:
    /// 200 of the 300 failed searches on build 270, 158 of them one tile off, Perri in the Orc Caves 21 times
    /// at one station. A creature's own feet are the height a blade wants to stand at.
    /// </para>
    /// </summary>
    public static int PressReach { get; set; } = 8;

    private static bool Stand(Map map, int x, int y, Point3D near, out Point3D spot)
    {
        // <b>Not a tile the reach map says nobody stands on (01.10.2026, build next).</b> The press station asked the ground and the
        // line and nothing else, and the walker then refused it from open ground: Jhelom's band at (1280, 3720) handed out (1260,
        // 3720) — water by the map — and 135 of 150 Sealed walks in Jhelom on build 384 were to it (the analyst's
        // sonnet-hunt-delve-384, C4). Water and blocked tiles are passed over like any tile without a floor.
        if (BotStep.Ground(map, x, y, near.Z, PressReach, out var z) && Sighted(map, x, y, z, near)
            && BotReachMap.Class(map, x, y, z) is not (BotReachClass.Water or BotReachClass.Blocked))
        {
            spot = new Point3D(x, y, z);

            return true;
        }

        spot = Point3D.Zero;

        return false;
    }

    /// <summary>Which of the eight compass lines a unit offset is, as an index into <see cref="Compass"/>.</summary>
    private static int Bearing(int x, int y)
    {
        for (var i = 0; i < Compass.Length; i++)
        {
            if (Compass[i].X == x && Compass[i].Y == y)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Turns a proposed tile into one a bot can stand on and reach, walking it in towards the anchor if it
    /// cannot. Returns the anchor itself as the last resort — standing on top of the leader is untidy and
    /// better than standing in a wall.
    /// </summary>
    /// <summary>Station candidates passed over for lying in another component of the walkers' graph than the member.</summary>
    public static long Uncomponented { get; private set; }

    /// <summary>Whether the walkers' graph puts these two points in different components; false when it cannot say.</summary>
    private static bool OtherComponent(Map map, Point3D from, Point3D to)
    {
        if (!Server.Engines.Pathing.Tiered.NavigationService.ComponentsCounted(map))
        {
            return false;
        }

        var a = Server.Engines.Pathing.Tiered.NavigationService.ComponentOf(map, from);
        var b = Server.Engines.Pathing.Tiered.NavigationService.ComponentOf(map, to);

        return a >= 0 && b >= 0 && a != b;
    }

    internal static Point3D Reachable(Map map, int x, int y, Point3D anchor, IBotSquadMember member)
    {
        var steps = Math.Max(Math.Abs(x - anchor.X), Math.Abs(y - anchor.Y));

        for (var back = 0; back <= steps; back++)
        {
            // Walk the candidate in towards the anchor a tile at a time.
            var cx = x + Math.Sign(anchor.X - x) * back;
            var cy = y + Math.Sign(anchor.Y - y) * back;

            if (!BotStep.Ground(map, cx, cy, anchor.Z, BotStep.StandingReach, out var z))
            {
                continue;
            }

            var candidate = new Point3D(cx, cy, z);

            // <b>What the shard has already proved, asked before a search is paid for.</b> A station on a
            // crypt roof is a station nobody can walk to, and the formation was handing one out every beat:
            // the member dropped it, the next beat computed the same tile, and the company stood there. On
            // 03.09.2026 that read in the log as "Alden 2 has dropped (1370, 1467, 30) because it is shut in
            // and this bot is outside it (station)" while the Baron's party sat still after a kill — three
            // roofs in that cluster had been filed as pockets an hour earlier.
            //
            // Free, and it does not disagree with the search below: a pocket is only filed when a search
            // walked ground to its edges. It simply knows sooner, and it goes on knowing after the tile that
            // was reachable when the station was chosen has been proved otherwise.
            // <b>Asked at the width the ledger actually files at, not at the tile.</b> With Exactly the sweep
            // is nought cells wide, so a station two tiles from a filed roof came back Unknown here and
            // Sealed to the walker a second later — the same question, two tolerances, two answers, and the
            // company standing between them. Within(2) is BotReach's own MaxSweep.
            if (BotReach.Ask(map, member.Self.Location, candidate, BotArrival.Within(2)) == BotReachVerdict.Sealed)
            {
                continue;
            }

            // <b>And the walkers' own graph, which is what the walk will be planned over (30.09.2026, build 363).</b> The mountain
            // west of Britain at (1030–1059, 1300–1349) is the one ground a company keeps fighting on, and its places came back
            // short to the walker 262 times in the first quarter hour of 362 — Galathil 57, Quelindor 38 and 24, Lorandil 24,
            // Dunstan 23 — while the check below passed them: its search and the graph's routes disagree there. A candidate in
            // another component of the graph than the member is walked in towards the anchor like any other refused tile.
            if (OtherComponent(map, member.Self.Location, candidate))
            {
                Uncomponented++;

                continue;
            }

            if (BotPath.CanReach(map, member.Self.Location, candidate, BotArrival.Exactly))
            {
                return candidate;
            }
        }

        // <b>The anchor is the last resort only while the anchor is somewhere this bot can get to.</b>
        //
        // The sentence above — "standing on top of the leader is untidy and better than standing in a wall" —
        // is about anchoring on one of ours, and it is right about that. In a fight the anchor is the
        // creature (see BotSquad.Anchor), and a creature can be perfectly visible through a gap in the rock
        // with no walking route to it at all. Then every candidate is refused, this hands back the creature's
        // own tile, the walker refuses that too, and the order is reissued on the next restation — once every
        // second and a half, for as long as the fight lasts.
        //
        // Measured in the Orc Caves on 11.09.2026: four delving parties, every one of them stuck on a
        // station at x 5294 that nothing could reach, a dozen refusals apiece in half a minute. A refusal a
        // beat is also what the stranded rescue counts, so the leaders were then carried to Britain and the
        // delves ended.
        //
        // Standing fast is the honest answer to "where should I be" when the answer is nowhere: the company
        // already breaks a fight off after four seconds in which nobody who has arrived can land a blow, and
        // that is exactly this situation seen from the other side.
        if (BotPath.CanReach(map, member.Self.Location, anchor, BotArrival.Within(1)))
        {
            return anchor;
        }

        Unanchored++;

        return member.Self.Location;
    }

    /// <summary>
    /// Times a station came back as "stay where you are" because nothing round the anchor — nor the anchor
    /// itself — could be walked to.
    ///
    /// Its own counter rather than silence: this is the shard telling you that a company is engaged with
    /// something it cannot reach, which is worth knowing and used to read as a bot that would not move.
    /// </summary>
    public static long Unanchored { get; private set; }

    /// <summary>
    /// Whether the asker has a better claim to a tile than the holder does.
    ///
    /// <para>
    /// This is the whole of the chokepoint problem, and it is a question about roles rather than about paths.
    /// Two trees with a gap between them, a mage standing in the gap, something hostile on the far side: the
    /// mage will be killed there in seconds and the blade in front of it would take minutes. So the gap
    /// belongs to the blade — not because anybody worked out that it is a chokepoint, but because the tile is
    /// nearer the threat than the mage's own station is, and ground nearer the threat belongs to whoever
    /// stands nearer the threat.
    /// </para>
    ///
    /// <para>
    /// Note what this does <em>not</em> do: a mage cannot move a blade out of the way. The blade is where it
    /// belongs.
    /// </para>
    /// </summary>
    public static bool OutranksFor(IBotSquadMember asker, IBotSquadMember holder) =>
        asker?.Class != null
        && holder?.Class != null
        && RingFor(asker.Class.Role) > RingFor(holder.Class.Role);
}
