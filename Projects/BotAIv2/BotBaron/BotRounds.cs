using System;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// The Baron walking his town, because nowhere has taken anybody lately.
///
/// <para>
/// <b>The point of this errand is that it produces nothing, and it still has to exist.</b> Every other bot
/// on the shard has somewhere to be at all times — the mine, the forge, the counter, a corpse — because
/// every other bot is making a living. The Baron is not, and a Baron with no ground to harrow is a bot with
/// literally nothing on the board. Left there he would stand on the spot he was born on until somebody died
/// somewhere, which is not a bot at rest, it is a bot that has stopped. So this is deliberately not busywork
/// dressed up as trade: it is where he is between harrowings, it pays nothing, it claims nothing, and it is
/// the only work on the shard whose whole justification is that a person watching should see him.
/// </para>
///
/// <para>
/// <b>Inside the walls, and the walls are the engine's own.</b> A guarded region is where nothing can be
/// fought, which is exactly the property that makes it the right place to be idle in — a Baron loitering in
/// open country would be a Baron picking fights alone, and the whole design of him is that he fights with
/// five bots behind him or not at all.
/// </para>
/// </summary>
public sealed class BotRounds : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRounds));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "stroll";

    /// <summary>
    /// What a walk through the town is reckoned at per minute.
    ///
    /// <para>
    /// Below everything, and above nothing, which is the whole of its place in the order — the same position
    /// <see cref="BotProwl"/> holds for a fighter with nothing in sight. It must never beat a harrowing and
    /// it must never lose to standing still, and since the Baron may be offered exactly two things, those
    /// two facts are the entire specification of this number.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 6.0;

    /// <summary>How long one walk lasts before he looks up and asks again.</summary>
    public static double WorkMinutes { get; set; } = 4.0;

    /// <summary>How far from the counter he will wander. A town, not a district.</summary>
    public static int Reach { get; set; } = 30;

    /// <summary>How many places are tried before a walk gives up on finding one inside the walls.</summary>
    public static int Tries { get; set; } = 12;

    /// <summary>How long he lingers at one place before choosing another.</summary>
    public static int LingerMs { get; set; } = 20000;

    public static long Walks { get; private set; }

    public static long Steps { get; private set; }

    /// <summary>Walks that could find nowhere inside the walls to go. A named nought.</summary>
    public static long Walled { get; private set; }

    private readonly Map _map;

    private readonly Point3D _town;

    private long _began;

    private long _steppedTick;

    private Point3D _post;

    private int _posts;

    private bool _walking;

    /// <summary>
    /// Whether <see cref="_post"/> holds a real place yet.
    ///
    /// A flag rather than testing the point against <c>Point3D.Zero</c>. Nothing here can ever legitimately
    /// choose the origin, so the sentinel would work — and that is exactly the argument this project has
    /// already lost twice, once on a tick stamp of nought and once on a height of nought. A value that means
    /// "unset" and is also a value stops being either the first time the world disagrees.
    /// </summary>
    private bool _posted;

    public BotRounds(Map map, Point3D town)
    {
        _map = map;
        _town = town;
        _began = Core.TickCount;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _town;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>
    /// It pays nothing and it is meant to. See <see cref="BotDeed.Unpaid"/> for the twelve minutes this cost
    /// before it existed.
    /// </summary>
    public override bool Unpaid => true;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override string Stage => $"walking the town, {_posts} corners of it so far";

    /// <summary>
    /// At a walk. It is the only errand on the shard that says so, and the reason is what it looks like: a
    /// Baron sprinting laps of his own town reads as a bot with a bug rather than a bot with nothing pressing
    /// to do. Everything else here runs because its errand is at the far end of the walk; this errand is the
    /// walk.
    /// </summary>
    public override bool Hurries => false;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        if (!_walking)
        {
            _walking = true;

            // Stamped when the walk actually begins rather than when the offer was built: most offers are
            // weighed and thrown away, and a clock started in the constructor is a clock already running.
            _began = now;
            _steppedTick = now;

            Walks++;
        }

        if (now - _began >= (long)(WorkMinutes * 60000))
        {
            return BotDoing.Done($"walked the town, {_posts} corners of it");
        }

        if (!_posted || now - _steppedTick >= LingerMs || body.InRange(_post, 1))
        {
            var next = Corner(body);

            if (next == Point3D.Zero)
            {
                Walled++;

                return BotDoing.Failed("nowhere inside the walls to walk to");
            }

            _post = next;
            _posted = true;
            _steppedTick = now;
            _posts++;
            Steps++;
        }

        return BotDoing.Walk(_map, _post, BotArrival.Within(1), "walking the town");
    }

    /// <summary>
    /// Somewhere else in the town: a place with ground under it, inside the walls, and not the one he is
    /// standing on.
    ///
    /// <para>
    /// Sampled rather than laid out in a ring. A ring is right for a patrol, whose job is to cover a square
    /// evenly and be seen to; a town is a shape nothing here knows, and a bot walking a perfect circle around
    /// a bank is more obviously a machine than one wandering.
    /// </para>
    /// </summary>
    private Point3D Corner(Mobile body)
    {
        for (var i = 0; i < Tries; i++)
        {
            var x = _town.X + Utility.RandomMinMax(-Reach, Reach);
            var y = _town.Y + Utility.RandomMinMax(-Reach, Reach);

            if (!BotStep.Settle(_map, x, y, out var z))
            {
                continue;
            }

            var at = new Point3D(x, y, z);

            if (body.InRange(at, 2))
            {
                continue;
            }

            // The engine's own walls. Asked of the destination and not of the bot: a Baron standing just
            // inside the gate would otherwise be free to walk out of it.
            if (Region.Find(at, _map)?.IsPartOf<GuardedRegion>() != true)
            {
                continue;
            }

            return at;
        }

        return Point3D.Zero;
    }

    /// <summary>Somewhere else in the same town, because a town is the errand and a street corner is not.</summary>
    public override bool Bend(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return false;
        }

        var next = Corner(body);

        if (next == Point3D.Zero)
        {
            return false;
        }

        _post = next;
        _posted = true;
        _steppedTick = Core.TickCount;

        return true;
    }

    public static string Describe() =>
        $"{Walks} walks through the town, {Steps} corners of it, {Walled} that could find nowhere inside the walls";

    public static void Forget()
    {
        Walks = 0;
        Steps = 0;
        Walled = 0;
    }
}
