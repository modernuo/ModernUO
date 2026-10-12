using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The guild's own company for the guild's own ground.
///
/// <para>
/// <b>A claim that only ever cost money meant nothing once it was paid for.</b> A guild bought a square, bots
/// died in it, the square went dire, and the Baron came to clear it — or did not, the island having worse
/// squares — while the guild that held it went on hunting elsewhere. Patrick's fourth Quad idea of 16.09.2026,
/// taken that evening: when a square a guild holds goes dire, the guild raises its own company onto it, led by
/// its own leader, and the Baron leaves such ground to the guild while it has somebody to raise one.
/// </para>
///
/// <para>
/// The errand is the Baron's <see cref="BotHarrow"/>, unchanged: a muster, a march, the square walked and its
/// spawn killed, the company paid what the city put on it. What differs is the office: any fit fighter of the
/// guild — armed, no novice — rather than the Baron, and only the guild's own squares rather than the island's
/// worst. Not the leader classes: the first cut asked for <see cref="BotClass.Leads"/>, and the 92 window read "53
/// guild leaders asked: 53 in no guild" — the Captain is nobody's member, and the guilds' masters are crafters, made
/// ordinary bots by Patrick the same evening. Reckoned by the harrow's own prior, so it outbids ordinary work as
/// the Baron's does; two of a guild cannot raise for the same square, since a square being raised for is passed
/// over (<see cref="BotProwl.Raising"/>).
/// </para>
///
/// <para>
/// <b>Patrick's answer of 26.09.2026, once the world was awake round the bots and halls stood beside live spawns: "they
/// put it there, so let them defend their own ground — that is their duty"; "yes, they gather themselves and clear it
/// themselves".</b> So the company forms at the guild's own hall and is made of the guild's own members, wherever they
/// are (<c>BotHarrow.Rally</c>, <c>BotHarrow.Levy</c>); it is a duty, not a trade
/// (<see cref="BotHarrow.DutyPrior"/>); it takes in the squares round the hall's own whoever else does not hold them
/// (<see cref="Ring"/>); and nobody goes without its supplies, the leader first (<see cref="BotProvision"/>).
/// </para>
/// </summary>
public sealed class BotReeve : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotReeve));

    public string Name => "Reeve";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>Whether guilds raise companies for their own ground at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Fighters asked. For the summary.</summary>
    public static long Asked { get; private set; }

    /// <summary>Asked with no weapon skill, or as a novice.</summary>
    public static long Unfit { get; private set; }

    /// <summary>Asked while in no guild.</summary>
    public static long Guildless { get; private set; }

    /// <summary>Asked for a guild that holds no ground.</summary>
    public static long Landless { get; private set; }

    /// <summary>Asked for a guild none of whose squares was at or below dire.</summary>
    public static long Safe { get; private set; }

    /// <summary>Asked while already in a company.</summary>
    public static long Held { get; private set; }

    /// <summary>Asked while too hurt to lead.</summary>
    public static long Hurt { get; private set; }

    /// <summary>Asked while the harrow was resting after a muster that failed.</summary>
    public static long Resting { get; private set; }

    /// <summary>Asked while short of its own supplies. See <see cref="BotProvision"/>.</summary>
    public static long Unsupplied { get; private set; }

    /// <summary>Asked for a guild with fewer than <see cref="BotHarrow.Least"/> fighters alive.</summary>
    public static long Few { get; private set; }

    /// <summary>Asked while round the far side of something by road from the hall's door. See <c>BotHarrow.Reaches</c>.</summary>
    public static long Across { get; private set; }

    /// <summary>Dire squares of a guild's passed over because their fight was several times its company. See BotPeril.Overwhelm.</summary>
    public static long Overwhelmed { get; private set; }

    /// <summary>
    /// How many squares round the hall's own a guild answers for as well, whoever else does not hold them. One: the hall's
    /// square and the eight round it, the same ground a member at rest is called from (<c>BotRepose</c>).
    /// </summary>
    public static int Ring { get; set; } = 1;

    /// <summary>Harrows of a guild's own ground offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Squares the Baron left to the guild that holds them. See <see cref="Keeps"/>.</summary>
    public static long Left { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (!Running || map == null || map == Map.Internal || body is not { Alive: true })
        {
            return null;
        }

        if (body is not BotMobile leader || leader.Class is BotBaron)
        {
            return null;
        }

        Asked++;

        if (leader.Guild is not Guild guild)
        {
            Guildless++;

            return null;
        }

        if (bot.Bond?.Weapon?.Skill == null || BotLadder.Novice(body))
        {
            Unfit++;

            return null;
        }

        if (BotHarrow.Resting(body))
        {
            Resting++;

            return null;
        }

        if (!BotSquads.Running)
        {
            return null;
        }

        if (bot is not IBotSquadMember { Squad: null })
        {
            Held++;

            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Hurt++;

            return null;
        }

        // Nobody leads its guild onto dire ground without its own supplies. See BotProvision.
        if (!BotProvision.Fit(body, out _))
        {
            Unsupplied++;

            return null;
        }

        if (Fighters(guild) < BotHarrow.Least)
        {
            Few++;

            return null;
        }

        // Nor one that could not walk to its own hall's door, where the company forms: the far side of a river. See
        // BotHarrow.Reaches.
        if (!BotHarrow.Reaches(map, body.Location, BotFeud.MusterPoint(guild, body), count: false))
        {
            Across++;

            return null;
        }

        var (square, dead) = Dire(map, guild, body.Location, BotThreat.Power(body));

        if (square == Point3D.Zero)
        {
            return null;
        }

        // Not a square another member, or the Baron, is already harrowing. See BotHarrow.Taken.
        if (BotHarrow.Taken(map, square))
        {
            BotHarrow.Declined();

            return null;
        }

        Offered++;

        logger.Information(
            "{Name}, {Class} of {Guild}, has been offered the harrowing of the guild's own square at ({X}, {Y}), where {Dead} have died",
            body.Name,
            leader.Class?.Name ?? "nobody",
            guild.Name,
            square.X,
            square.Y,
            dead
        );

        return new BotHarrow(map, square, dead, guild);
    }

    /// <summary>
    /// The guild's members who could answer its company: alive, armed, past the novice, and not of a producing trade, which
    /// the levy exempts (<c>BotHarrow.Levy</c>).
    /// </summary>
    public static int Fighters(Guild guild)
    {
        var members = guild?.Members;
        var fighters = 0;

        for (var i = 0; i < members?.Count; i++)
        {
            if (members[i] is BotMobile { Deleted: false, Alive: true } bot && bot.Class is { } klass
                && klass.Role != BotRole.Producer && klass is not BotBaron && !BotLadder.Novice(bot)
                && bot is IBotWilful { Bond.Weapon: not null })
            {
                fighters++;
            }
        }

        return fighters;
    }

    /// <summary>
    /// The guild that answers for this square: the one that holds it, or, when nobody does, the one whose hall's square is
    /// within <see cref="Ring"/> of it.
    /// </summary>
    public static string Warden(Map map, Point3D where)
    {
        var owner = BotClaim.Owner(map, where);

        if (owner != null || map == null)
        {
            return owner;
        }

        var key = BotQuad.Key(map, where);

        foreach (var (name, hall) in BotEstate.Held)
        {
            if (hall is not { Deleted: false } || hall.Map != map)
            {
                continue;
            }

            var seat = BotQuad.Key(map, hall.Location);

            if (System.Math.Abs(seat.X - key.X) <= Ring && System.Math.Abs(seat.Y - key.Y) <= Ring)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// The nearest square this guild answers for whose safety is at or below <see cref="BotQuad.Dire"/>, with its dead: a
    /// square it holds, or one round its hall's own that nobody holds (<see cref="Warden"/>).
    /// </summary>
    private static (Point3D Square, int Dead) Dire(Map map, Guild guild, Point3D from, double head)
    {
        var best = Point3D.Zero;
        var bestDead = 0;
        var closest = int.MaxValue;
        var owned = 0;

        foreach (var (key, owner, _) in BotClaim.Owned())
        {
            if (key.Map != map.MapID || !string.Equals(owner, guild.Name, System.StringComparison.Ordinal))
            {
                continue;
            }

            owned++;

            Consider(map, key.X, key.Y, from, head, guild, ref best, ref bestDead, ref closest);
        }

        // <b>And the ground round its hall, which nobody holds.</b> The spawn that kills a guild's members at its door is
        // seldom in a square the guild bought: The Hammer's hall at 1754,1482 stands among the gargoyles of 1680–1800,
        // 1350–1500, and The Lantern's at 1082,1402 among ratmen. Patrick, 26.09.2026: the guild clears the spawn by its
        // hall itself. A square another guild holds is that guild's.
        if (BotEstate.Hall(guild) is { Deleted: false } hall && hall.Map == map)
        {
            var seat = BotQuad.Key(map, hall.Location);

            for (var dx = -Ring; dx <= Ring; dx++)
            {
                for (var dy = -Ring; dy <= Ring; dy++)
                {
                    var middle = Middle(seat.X + dx, seat.Y + dy);

                    if (BotClaim.Owner(map, middle) != null)
                    {
                        continue;
                    }

                    owned++;

                    Consider(map, seat.X + dx, seat.Y + dy, from, head, guild, ref best, ref bestDead, ref closest);
                }
            }
        }

        if (owned == 0)
        {
            Landless++;
        }
        else if (best == Point3D.Zero)
        {
            Safe++;
        }

        return (best, bestDead);
    }

    private static Point3D Middle(int x, int y) => new(x * BotQuad.Side + BotQuad.Side / 2, y * BotQuad.Side + BotQuad.Side / 2, 0);

    /// <summary>
    /// Takes this square as the best so far when it is dire, not already being raised for, not a fight several times what
    /// the guild's company would bring (BotPeril.Overwhelms, the auction's own question), and nearer.
    /// </summary>
    private static void Consider(
        Map map, int x, int y, Point3D from, double head, Guild guild, ref Point3D best, ref int bestDead, ref int closest
    )
    {
        var middle = Middle(x, y);

        if (BotQuad.Safety(map, middle) > BotQuad.Dire || BotProwl.Raising(map, middle))
        {
            return;
        }

        var quad = BotQuad.At(map, middle);

        if (quad == null)
        {
            return;
        }

        var at = BotQuad.Stand(quad);

        if (at == Point3D.Zero)
        {
            return;
        }

        if (BotPeril.Overwhelms(map, at, from, BotHarrow.Expected(map, at, head, guild), out _, count: false))
        {
            Overwhelmed++;

            return;
        }

        var away = System.Math.Max(System.Math.Abs(at.X - from.X), System.Math.Abs(at.Y - from.Y));

        if (away >= closest)
        {
            return;
        }

        closest = away;
        best = at;
        bestDead = quad.Deaths;
    }

    /// <summary>
    /// Whether this square is one a guild answers for (<see cref="Warden"/>) and that guild has fighters enough alive to
    /// raise a company for it, so the Baron leaves it. A guild with fewer than <see cref="BotHarrow.Least"/> gets the Baron
    /// as before.
    /// </summary>
    public static bool Keeps(Map map, Point3D where)
    {
        if (!Running)
        {
            return false;
        }

        var owner = Warden(map, where);

        if (owner == null || BaseGuild.FindByName(owner) is not Guild guild || Fighters(guild) < BotHarrow.Least)
        {
            return false;
        }

        Left++;

        return true;
    }

    public static string Describe() =>
        !Running
            ? "guilds raise no companies for their own ground"
            : $"{Asked} fighters asked: {Guildless} in no guild, {Unfit} unarmed or novices, {Landless} of a guild with no ground, {Safe} with no square of their own at or below dire ({BotQuad.Dire:F2}), {Held} already in a company, {Hurt} too hurt, {Resting} while the harrow rested, {Unsupplied} short of their supplies, {Few} of a guild with fewer than {BotHarrow.Least} fighters, {Across} round the far side of something by road from their hall, {Overwhelmed} dire squares passed over for a fight {BotPeril.Overwhelm:F1} times the guild's company, {Offered} offered the harrowing of their guild's own square; the Baron left {Left} dire squares to the guild that holds them";

    public static void Forget()
    {
        Asked = 0;
        Guildless = 0;
        Landless = 0;
        Safe = 0;
        Held = 0;
        Unfit = 0;
        Hurt = 0;
        Resting = 0;
        Unsupplied = 0;
        Few = 0;
        Across = 0;
        Overwhelmed = 0;
        Offered = 0;
        Left = 0;
    }
}
