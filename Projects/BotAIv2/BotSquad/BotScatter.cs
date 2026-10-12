using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// How a squad stands on ground that has nothing left on it: broken into small knots, spread out, covering
/// the place instead of standing in it.
///
/// <para>
/// <b>What it fixes.</b> A party sent to a hunting ground is sent to <em>one coordinate</em>, so it arrives
/// as a heap. Every bot in the heap sees the same eight tiles as every other, which means all but one of them
/// are contributing nothing — and the corner of the ground where the spawner is quietly refilling is watched
/// by nobody at all. Spawn timers on this shard run five to ten minutes, so a heap on an emptied graveyard is
/// a heap doing nothing for five minutes at a time. Spread over the same ground they cover it, and whatever
/// comes up comes up next to somebody.
/// </para>
///
/// <para>
/// <b>Why it is safe to spread out, which is the whole question.</b> Because being attacked is an event that
/// reaches the whole squad: one member hit sets the squad's focus and moves the anchor onto them, and every
/// station is derived from the anchor — so the knots re-form on the trouble without anybody calling for help
/// or deciding to answer. Spreading out is only a mistake in a world where nobody can hear a cry.
/// </para>
///
/// <para>
/// <b>Knots rather than singletons.</b> A bot alone in a corner meets a spectre alone, and a spectre is twice
/// a lone bot: 53 health times nine damage plus thirty-one of magery. Two or three together are a party that
/// survives long enough for the rest to arrive. That is why the unit of scouting is a pair or a trio and not
/// a person.
/// </para>
/// </summary>
public static class BotScatter
{
    /// <summary>
    /// How many bots make a knot. Three, which for a squad of five means two knots — one of three and one of
    /// two — and neither is alone.
    /// </summary>
    public static int KnotSize { get; set; } = 3;

    /// <summary>
    /// How far a knot goes from the anchor.
    ///
    /// <para>
    /// Ten as first specified, matching what the first version arrived at from the other direction: a square
    /// of the danger map is thirty tiles, and three across is ten. <b>Raised to twenty-four on 24.08.2026 by
    /// order</b> — a company is meant to cover ground, and at ten the three knots sat inside one screen and
    /// swept the patch they were already standing in. Still well inside the reach a company can be called
    /// across, so a knot that finds something can still gather the rest.
    /// </para>
    /// </summary>
    public static int Spread { get; set; } = 24;

    /// <summary>Tiles between the members of one knot. Close enough to be one fight, far enough to see past each other.</summary>
    private const int WithinKnot = 2;

    /// <summary>The eight compass lines, which is how knots are placed apart from one another.</summary>
    private static readonly (int X, int Y)[] Compass =
    [
        (0, -1),
        (1, -1),
        (1, 0),
        (1, 1),
        (0, 1),
        (-1, 1),
        (-1, 0),
        (-1, -1)
    ];

    private static readonly List<IBotSquadMember> _order = [];

    private static readonly Dictionary<IBotSquadMember, int> _seats = [];

    private static BotSquad _orderedFor;

    private static int _orderedAt;

    /// <summary>
    /// The patch of ground that belongs to this member while the squad is sweeping.
    ///
    /// <para>
    /// A share-out rather than a search, and worked out identically by everybody: the squad is ordered by
    /// serial, cut into knots of <see cref="KnotSize"/>, and knot <c>k</c> takes a compass line chosen so the
    /// knots end up as far from each other as the count allows. Two bots can never pick the same patch and
    /// nobody has to be told which is theirs.
    /// </para>
    /// </summary>
    public static Point3D PatchFor(BotSquad squad, IBotSquadMember member)
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

        // <b>The order is worked out once a roster, not once a member (01.10.2026):</b> at twenty to a company (BotSquad.MaxSize)
        // sorting the whole company for each member's patch is four hundred sorts' worth a round. See BotFormation.File.
        if (!ReferenceEquals(_orderedFor, squad) || _orderedAt != squad.Roster)
        {
            _orderedFor = squad;
            _orderedAt = squad.Roster;
            _order.Clear();
            _seats.Clear();

            var members = squad.Members;

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i]?.Self != null)
                {
                    _order.Add(members[i]);
                }
            }

            _order.Sort(static (a, b) => a.Self.Serial.Value.CompareTo(b.Self.Serial.Value));

            for (var i = 0; i < _order.Count; i++)
            {
                _seats[_order[i]] = i;
            }
        }

        if (!_seats.TryGetValue(member, out var seat))
        {
            return Point3D.Zero;
        }

        var size = Math.Max(2, KnotSize);
        var knots = (_order.Count + size - 1) / size;
        var knot = seat / size;
        var within = seat % size;

        var anchor = squad.Anchor;

        // Knots as far apart as the count allows: with two they end up opposite, with three at the thirds.
        var line = Compass[knot * Compass.Length / Math.Max(1, knots) % Compass.Length];

        var x = anchor.X + line.X * Spread;
        var y = anchor.Y + line.Y * Spread;

        // And the members of a knot a couple of tiles off each other, along the line they came out on, so a
        // knot is a short file rather than a pile.
        if (within > 0)
        {
            var side = within % 2 == 1 ? 1 : -1;
            var step = (within + 1) / 2 * WithinKnot * side;

            x += -line.Y * step;
            y += line.X * step;
        }

        return BotFormation.Reachable(map, x, y, anchor, member);
    }
}
