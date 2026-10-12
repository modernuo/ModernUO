using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Ground the population is never to want, however attractive whatever is standing on it.
///
/// <para>
/// <b>Patrick's order of 11.09.2026, and the difference between this and <see cref="BotRefused"/> is the
/// whole reason it is a second class.</b> The refusal ledger is a measurement: somebody tried, somebody
/// failed, and the square rests for a while and is cleared the moment anybody arrives in it. That is right
/// for ground whose reachability is a question. It is wrong for ground whose reachability is settled —
/// a walled pocket with no door — because the ledger keeps letting the question be asked again, and each
/// re-ask costs a path search, an errand and the minutes the bot spent walking at it.
/// </para>
///
/// <para>
/// <b>The evidence this was written from.</b> A cooking hearth stands at (2161, 1354) inside a pocket east
/// of Britain that nothing can walk into. In one nine-hour session it took 70 of the shard's 2460 walk
/// failures on its own, every one of them a cook who took the job, walked at it for minutes and gave up;
/// the rest of the pocket — a fleeing bot choosing (2208, 1379) as a refuge, a healer hunting something
/// shut in at (2163, 1384) — took 60 more. The shard had already <i>learned</i> the shape of it:
/// <c>BotReach</c> logged "a pocket of 527 tiles has been walked to its edges" and "a pocket of 25 tiles",
/// and it went on choosing destinations inside them, because knowing a journey will be refused is not the
/// same as not wanting to go.
/// </para>
///
/// <para>
/// <b>So this is a fact stated rather than learned, and it belongs in the choice.</b> It is asked by
/// <see cref="BotRefused.Refusing"/>, which every sampler that picks a place already calls before it picks,
/// so a barred square is never on any shortlist: not a hearth, not a vein, not a refuge, not a corpse. It
/// never expires and arriving does not clear it — those are both properties of a measurement, and this is
/// not one. The only thing that lifts it is somebody editing <c>bot-movement.json</c>.
/// </para>
///
/// <para>
/// <b>Kept honest by its own counter.</b> A gate with no bucket either breaks somebody else's denominator
/// or files its refusals under somebody else's name — this project's most repeated instrumentation defect —
/// so what this turns away is counted here, by square, and printed beside the ledger's own numbers rather
/// than inside them.
/// </para>
/// </summary>
public static class BotBarred
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBarred));

    /// <summary>Whether the bars are enforced at all. Off makes the shard behave exactly as it did before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>One piece of ground nobody may want, and why it is on the list.</summary>
    public sealed class Bar
    {
        public Bar(int x1, int y1, int x2, int y2, string why)
        {
            X1 = Math.Min(x1, x2);
            Y1 = Math.Min(y1, y2);
            X2 = Math.Max(x1, x2);
            Y2 = Math.Max(y1, y2);
            Why = why;
        }

        public int X1 { get; }

        public int Y1 { get; }

        public int X2 { get; }

        public int Y2 { get; }

        /// <summary>In words, for the boot line. Never parsed.</summary>
        public string Why { get; }

        /// <summary>Choices this one has turned away.</summary>
        public long Turned;

        public bool Holds(int x, int y) => x >= X1 && x <= X2 && y >= Y1 && y <= Y2;

        public override string ToString() => $"({X1},{Y1})-({X2},{Y2}) {Why}";
    }

    /// <summary>
    /// The list as it stands: the box the order was written about, and the two traps added on 15.09.2026.
    ///
    /// <para>
    /// <b>Sized from the failures rather than from the pocket.</b> <c>BotReach</c> measured the two pockets
    /// inside it at 527 and 25 tiles; this box is bigger than both together, because what has to be kept out
    /// of a shortlist is every destination a bot actually picked in there, and those spread from
    /// (2161, 1354) to (2223, 1377). Its west edge stops short of (2142, 1350) on purpose: bots hunt there,
    /// one of them died there, and ground somebody has stood on is ground this list has no business holding.
    /// </para>
    ///
    /// <para>
    /// <b>The two traps are sized the same way, from where bots were carried home out of them.</b> Neither can be proved a
    /// pocket — every rescue from either read the ground as too big to enclose — so the reach ledger never files them, and
    /// persisting pockets would not have closed them. Fable proposed boxes a little off the evidence: its south-west box
    /// took in ground at y 2060–2105 and missed nothing, its north box stopped at 1350,1080 and missed five of the twelve
    /// rescues. These are the rescue origins of 14–15.09.2026 with a few tiles' margin, taken on Patrick's word.
    /// </para>
    /// </summary>
    private static readonly List<Bar> _bars =
    [
        new(2155, 1352, 2230, 1405, "a walled pocket east of Britain with no way in — Patrick's order, 11.09.2026"),
        new(1312, 1035, 1382, 1120, "the trap north of Britain round the trolls at (1318–1320, 1047): twelve bots carried home out of (1320–1378, 1048–1116) and 72 hunts refused a road to the trolls on 14–15.09.2026 — Patrick's word, 15.09.2026"),
        new(1020, 2110, 1240, 2295, "the water trap south-west of Britain: 32 bots carried home out of (1031–1230, 2119–2284) on 14–15.09.2026 — Patrick's word, 15.09.2026"),
        new(1905, 960, 2100, 1140, "the plague beast's bog north-east of Britain, until groups of thirty to fifty can be raised for it: about twelve bots died in it between 20:30 and 20:42 on 15.09.2026 while the quadrant record read it safe — Patrick's order, 15.09.2026")
    ];

    public static IReadOnlyList<Bar> Bars => _bars;

    /// <summary>
    /// Ground closed for a while round a death nobody could have survived: a quarantine.
    ///
    /// <para>
    /// <b>The plague beast walked out of its bog to the east wall of Britain on the wiped world (29.09.2026, 02:10–02:20) and
    /// killed fourteen newcomers in ten minutes; its spawn killed as many more.</b> The bog is barred for ever (above), but a
    /// beast that wanders is not in its box, and a danger map wiped clean learns a square a death at a time — the newcomers
    /// were the lesson. A death at the hands of a creature past <see cref="QuarantinePower"/>, or of anything of the plague,
    /// closes a box of <see cref="QuarantineHalf"/> each way round it for <see cref="QuarantineMs"/> to every chooser and to
    /// flight, as a bar does; a death inside a quarantine already standing extends it. Nothing is saved: a beast that has
    /// wandered off by the next boot is not there to fear.
    /// </para>
    /// </summary>
    public static int QuarantineMs { get; set; } = 7200000;

    public static int QuarantineHalf { get; set; } = 45;

    /// <summary>A creature this strong that kills a bot closes the ground round the death. Thirty thousand: the plague beast reads near a hundred thousand, a lich under ten.</summary>
    public static double QuarantinePower { get; set; } = 30000.0;

    /// <summary>
    /// <b>A killing field is closed on the count of its dead, not on the strength of the killer (29.09.2026).</b> The
    /// first two hours after the wipe put 225 deaths into six fixed spawns round Britain — seven under one reaper in
    /// the wood where the woodcutters go, twenty-four to the wraiths and spectres of the graveyard sixty tiles from the
    /// home ground, the trolls and harpies on the Yew road, the ettins south — and every one of those grounds read
    /// safe to the auction, because the quarantine asked only whether the killer was the plague or something of
    /// 30000 power. A novice population is killed by ordinary things, in the same places, over and over; the place
    /// is the evidence. This many bot deaths within <see cref="QuarantineHalf"/> tiles of each other inside
    /// <see cref="KillingWindowMs"/> declare the square a killing field and close it for <see cref="QuarantineMs"/>,
    /// under the same <c>Homely</c> exemption for the home ground, halls and seats. Nought turns it off.
    /// </summary>
    public static int KillingDeaths { get; set; } = 3;

    /// <summary>How far back the deaths are counted for a killing field. Half an hour.</summary>
    public static int KillingWindowMs { get; set; } = 1800000;

    /// <summary>Killing fields declared on the count of the dead. See <see cref="KillingDeaths"/>.</summary>
    public static long Fields { get; private set; }

    /// <summary>Every bot death this window, for the count, with what killed it (a wild creature's kind and strength). Pruned as it is read.</summary>
    private static readonly List<(Map Map, int X, int Y, long Tick, Type Kind, double Power)> _fallen = [];

    /// <summary>Quarantines declared, and choices they turned away.</summary>
    public static long Quarantined { get; private set; }

    public static long QuarantineTurned { get; private set; }

    private sealed class Quarantined_
    {
        public Bar Box;

        public long Until;

        public int Deaths;

        /// <summary>
        /// What killed inside it: the strongest seen of each wild kind (03.10.2026). The evidence a clearing is sized by when the
        /// killers are not standing in sight at the moment it is weighed. See <see cref="Closing"/>.
        /// </summary>
        public readonly Dictionary<Type, double> Killers = [];

        /// <summary>Declared, or extended, by a death at the hands of the plague or of a creature past <see cref="QuarantinePower"/>.</summary>
        public bool Dire;
    }

    /// <summary>
    /// <b>A standing quarantine as a clearing sees it (03.10.2026, build next).</b> Patrick's order of 02.10.2026, item 4: companies
    /// go to the squares where others died and clear them. Its box, why and how often it was closed, whether the plague or a killer
    /// of <see cref="QuarantinePower"/> closed it (<see cref="Dire"/>: never a clearing's), and the strongest thing recorded killing
    /// in it at its kind's company price now (<c>BotCompanyRecord.Price</c>, since 03.10.2026; the island's price before) — the
    /// figure a company withdraws by (<c>BotSquad.Reckon</c>).
    /// </summary>
    public readonly struct Closure
    {
        public Closure(int x1, int y1, int x2, int y2, string why, int deaths, bool dire, double worst, string worstKind, double worstRaw)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Why = why;
            Deaths = deaths;
            Dire = dire;
            Worst = worst;
            WorstKind = worstKind;
            WorstRaw = worstRaw;
        }

        public int X1 { get; }

        public int Y1 { get; }

        public int X2 { get; }

        public int Y2 { get; }

        public string Why { get; }

        public int Deaths { get; }

        public bool Dire { get; }

        /// <summary>The strongest recorded killer's strength at its kind's price now; nought when nothing wild was recorded.</summary>
        public double Worst { get; }

        public string WorstKind { get; }

        public double WorstRaw { get; }

        public Point3D Middle => new((X1 + X2) / 2, (Y1 + Y2) / 2, 0);

        public bool Holds(int x, int y) => x >= X1 && x <= X2 && y >= Y1 && y <= Y2;
    }

    private static Closure View(Quarantined_ q)
    {
        var worst = 0.0;
        var raw = 0.0;
        string kind = null;

        foreach (var (type, power) in q.Killers)
        {
            // At the kind's company price (03.10.2026): a clearing is a company, and the figure it is weighed by is the one a company
            // withdraws by (BotSquad.Reckon). See BotCompanyRecord.
            var priced = power * BotCompanyRecord.Price(type);

            if (priced > worst)
            {
                worst = priced;
                raw = power;
                kind = type.Name;
            }
        }

        return new Closure(q.Box.X1, q.Box.Y1, q.Box.X2, q.Box.Y2, q.Box.Why, q.Deaths, q.Dire, worst, kind, raw);
    }

    /// <summary>
    /// The standing quarantine holding this place, as a clearing weighs it; a pure look, nothing counted and nothing dropped. See
    /// <see cref="Closure"/>.
    /// </summary>
    public static bool Closing(Map map, Point3D where, out Closure closure)
    {
        closure = default;

        if (!Running || map == null || map == Map.Internal)
        {
            return false;
        }

        var now = Core.TickCount;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];

            if (q.Until - now > 0 && q.Box.Holds(where.X, where.Y))
            {
                closure = View(q);

                return true;
            }
        }

        return false;
    }

    /// <summary>Every quarantine standing, as a clearing weighs it. See <see cref="Closing"/>.</summary>
    public static IEnumerable<Closure> Closures()
    {
        var now = Core.TickCount;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            if (_quarantine[i].Until - now > 0)
            {
                yield return View(_quarantine[i]);
            }
        }
    }

    /// <summary>Whether a quarantine the plague or a killer past <see cref="QuarantinePower"/> closed holds this place; a pure look.</summary>
    public static bool DireAt(Map map, Point3D where)
    {
        if (!Running || map == null || map == Map.Internal || _quarantine.Count == 0)
        {
            return false;
        }

        var now = Core.TickCount;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];

            if (q.Dire && q.Until - now > 0 && q.Box.Holds(where.X, where.Y))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether one of the standing bars (Patrick's, never lifted) holds this place; a pure look, nothing counted.</summary>
    public static bool BarredOutright(Map map, Point3D where)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            return false;
        }

        for (var i = 0; i < _bars.Count; i++)
        {
            if (_bars[i].Holds(where.X, where.Y))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The kind and strength of a wild creature that killed one of ours, for the quarantine's record; null for anything else.</summary>
    private static (Type Kind, double Power) Killer(Mobile killer) =>
        killer is Server.Mobiles.BaseCreature { Controlled: false, Summoned: false } beast
            ? (beast.GetType(), BotThreat.Power(beast))
            : (null, 0.0);

    private static bool Record(Quarantined_ q, Type kind, double power)
    {
        if (kind == null || power <= 0.0)
        {
            return false;
        }

        if (!q.Killers.TryGetValue(kind, out var had) || power > had)
        {
            q.Killers[kind] = power;

            return true;
        }

        return false;
    }

    private static readonly List<Quarantined_> _quarantine = [];

    /// <summary>Whether a death at this creature's hands closes the ground round it.</summary>
    public static bool Quarantines(Mobile killer) =>
        killer is Server.Mobiles.BaseCreature beast
        && (beast.GetType().Name.StartsWith("PlagueBeast", StringComparison.Ordinal) || BotThreat.Power(beast) >= QuarantinePower);

    /// <summary>Quarantines that would have closed the population's home, a guild's seat or hall, and were not declared.</summary>
    public static long Spared { get; private set; }

    /// <summary>
    /// Whether a box round this place would shut the population out of its own home, a guild's seat or a hall. Between 02:33
    /// and 02:41 on 29.09.2026 the plague's spawn, chasing bots into Britain, had the boxes round the home ground and two
    /// halls closed — a quarantine of the town is a population with nowhere to work.
    /// </summary>
    private static bool Homely(Map map, Point3D where)
    {
        var half = QuarantineHalf;

        // <b>A guarded town is never a killing field (29.09.2026).</b> Between 05:48 and 05:52 one gazer in Britain's
        // streets killed four bots who came to it one at a time, and the count declared the middle of the town closed —
        // a box over the shops, the inn and every retreat into the town. What a monster inside the walls calls for is
        // the population in numbers (BotPeril), not the population elsewhere.
        if (map != null && Region.Find(where, map)?.IsPartOf<GuardedRegion>() == true)
        {
            return true;
        }


        if (Math.Max(Math.Abs(BotPopulation.Where.X - where.X), Math.Abs(BotPopulation.Where.Y - where.Y)) <= half)
        {
            return true;
        }

        // Nor over the home of a town bots live in (29.09.2026). See BotResidence.Homely.
        if (BotResidence.Homely(where, half))
        {
            return true;
        }

        foreach (var (_, hall) in BotEstate.Held)
        {
            if (hall is { Deleted: false } && Math.Max(Math.Abs(hall.X - where.X), Math.Abs(hall.Y - where.Y)) <= half)
            {
                return true;
            }
        }

        foreach (var (_, seat) in BotSeat.Overrides)
        {
            if (Math.Max(Math.Abs(seat.X - where.X), Math.Abs(seat.Y - where.Y)) <= half)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Every bot death comes here. A death nobody could have survived closes the ground at once
    /// (<see cref="Quarantines"/>); an ordinary one is counted, and the count closes a killing field
    /// (<see cref="KillingDeaths"/>).
    /// </summary>
    public static void Fell(Map map, Point3D where, Mobile killer, string who)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            return;
        }

        var (kind, power) = Killer(killer);

        // What killed inside a quarantine already standing is written into it, whether or not this death extends it: the clearing
        // is sized by it (03.10.2026). See Closing.
        Note(where, kind, power);

        if (killer != null && Quarantines(killer))
        {
            Quarantine(map, where, $"{killer.Name} killed {who} here", [(kind, power)], dire: true);

            return;
        }

        if (KillingDeaths <= 0 || KillingWindowMs <= 0)
        {
            return;
        }

        var now = Core.TickCount;

        for (var i = _fallen.Count - 1; i >= 0; i--)
        {
            if (now - _fallen[i].Tick > KillingWindowMs)
            {
                _fallen.RemoveAt(i);
            }
        }

        _fallen.Add((map, where.X, where.Y, now, kind, power));

        var near = 0;
        List<(Type, double)> killers = [];

        for (var i = 0; i < _fallen.Count; i++)
        {
            var f = _fallen[i];

            if (f.Map == map && Math.Max(Math.Abs(f.X - where.X), Math.Abs(f.Y - where.Y)) <= QuarantineHalf)
            {
                near++;
                killers.Add((f.Kind, f.Power));
            }
        }

        if (near < KillingDeaths)
        {
            return;
        }

        var before = Quarantined;

        Quarantine(
            map,
            where,
            $"a killing field: {near} bots dead within {QuarantineHalf} tiles in {KillingWindowMs / 60000} minutes, the last {who} by {killer?.Name ?? "something"}",
            killers,
            dire: false
        );

        if (Quarantined > before)
        {
            Fields++;
        }
    }

    /// <summary>A wild killer written into the quarantine standing where it killed, if one does. See <see cref="Quarantined_.Killers"/>.</summary>
    private static void Note(Point3D where, Type kind, double power)
    {
        if (kind == null || _quarantine.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;
        var written = false;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];

            if (q.Until - now > 0 && q.Box.Holds(where.X, where.Y))
            {
                written |= Record(q, kind, power);
            }
        }

        // Kept across a restart at once, not only at the next declaration (review of clear12, F11).
        if (written)
        {
            Save();
        }
    }

    /// <summary>Closes the ground round a death for a while. See <see cref="QuarantineMs"/>.</summary>
    public static void Quarantine(Map map, Point3D where, string why) => Quarantine(map, where, why, null, dire: false);

    /// <summary>
    /// Closes the ground round a death for a while, with what killed there (<paramref name="killers"/>) and whether the plague or a
    /// killer past <see cref="QuarantinePower"/> did (<paramref name="dire"/>). See <see cref="QuarantineMs"/>.
    /// </summary>
    private static void Quarantine(Map map, Point3D where, string why, List<(Type Kind, double Power)> killers, bool dire)
    {
        if (!Running || map == null || map == Map.Internal || QuarantineMs <= 0)
        {
            return;
        }

        var now = Core.TickCount;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];

            if (q.Box.Holds(where.X, where.Y))
            {
                q.Until = now + QuarantineMs;
                q.Deaths++;
                q.Dire |= dire;

                for (var k = 0; k < killers?.Count; k++)
                {
                    Record(q, killers[k].Kind, killers[k].Power);
                }

                Save();

                return;
            }
        }

        if (Homely(map, where))
        {
            Spared++;

            return;
        }

        var box = new Bar(where.X - QuarantineHalf, where.Y - QuarantineHalf, where.X + QuarantineHalf, where.Y + QuarantineHalf, why);
        var made = new Quarantined_ { Box = box, Until = now + QuarantineMs, Deaths = 1, Dire = dire };

        for (var k = 0; k < killers?.Count; k++)
        {
            Record(made, killers[k].Kind, killers[k].Power);
        }

        _quarantine.Add(made);
        Quarantined++;
        Save();

        logger.Warning(
            "Quarantine: the ground ({X1},{Y1})-({X2},{Y2}) is closed to the population for {Minutes} minutes — {Why}",
            box.X1,
            box.Y1,
            box.X2,
            box.Y2,
            QuarantineMs / 60000,
            why
        );
    }

    /// <summary>
    /// <b>Quarantines lifted by a company that cleared the ground (02.10.2026).</b> A quarantine closes a killing field for two
    /// hours to everybody; a company that has walked the box and emptied it has dealt with what the quarantine was written for.
    /// Every quarantine whose box holds <paramref name="where"/> on <paramref name="map"/>, or whose middle lies within
    /// <paramref name="spread"/> of it, is dropped and said so. Returns how many. The standing bars are Patrick's and never lifted
    /// here.
    ///
    /// <para>
    /// <b>Only ground the company walked, only when it is empty, and never the plague's (03.10.2026, build next).</b> Patrick's
    /// order of 02.10.2026 (item 4): the quarantine is lifted when the square is cleared. It was lifted for any box within the
    /// company's spread <i>plus</i> a quarantine's half-width of the square — on build 396 Cedric's harrow of (795, 465) lifted
    /// (794,501)-(884,591), whose middle lay 81 tiles off and which overlapped the 75 tiles he walked by one row — and whatever
    /// was still standing in it: a harrow that ran out its clock with a shunned beast unfought lifted the box round the beast
    /// (review of march10, F4). Now the box must hold the square the company walked, or its middle lie within the company's reach
    /// of it; nothing alive in the box may be reckoned past <see cref="EmptyOdds"/> times a lone bot (<c>BotKept.LoneBot</c>) at its kind's price
    /// (<c>BotClearer.Strongest</c>), and a quarantine the plague or a killer past <see cref="QuarantinePower"/> declared stands
    /// its clock. What is left closed is counted (<see cref="Unemptied"/>, <see cref="DireKept"/>) and said.
    /// </para>
    /// </summary>
    public static int Lift(Map map, Point3D where, int spread, string why)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            return 0;
        }

        var lifted = 0;
        var now = Core.TickCount;

        for (var i = _quarantine.Count - 1; i >= 0; i--)
        {
            var q = _quarantine[i];
            var box = q.Box;
            var middle = new Point3D((box.X1 + box.X2) / 2, (box.Y1 + box.Y2) / 2, where.Z);

            var near = box.Holds(where.X, where.Y)
                       || Math.Max(Math.Abs(middle.X - where.X), Math.Abs(middle.Y - where.Y)) <= spread;

            if (!near || q.Until - now <= 0)
            {
                continue;
            }

            if (q.Dire)
            {
                DireKept++;

                logger.Information(
                    "Quarantine kept: ({X1},{Y1})-({X2},{Y2}) was declared for the plague or a killer of {Power:F0} and stands its clock, whatever a company cleared — {Why}",
                    box.X1,
                    box.Y1,
                    box.X2,
                    box.Y2,
                    QuarantinePower,
                    why
                );

                continue;
            }

            // Read fresh, not from the summary's ten-second look (review of clear12, F5).
            var left = BotClearer.Strongest(map, middle, QuarantineHalf, out var worst, fresh: true);
            var empty = EmptyBelow;

            if (left > empty)
            {
                Unemptied++;

                logger.Information(
                    "Quarantine kept: ({X1},{Y1})-({X2},{Y2}) still holds {Worst} at ({WX}, {WY}), reckoned {Left:F0} at its kind's price, past the {Empty:F0} a lone bot takes on — {Why}",
                    box.X1,
                    box.Y1,
                    box.X2,
                    box.Y2,
                    worst?.Name ?? "something",
                    worst?.X ?? 0,
                    worst?.Y ?? 0,
                    left,
                    empty,
                    why
                );

                continue;
            }

            logger.Information(
                "Quarantine lifted: ({X1},{Y1})-({X2},{Y2}) after {Deaths} deaths and {Turned} choices turned away, nothing in it past {Empty:F0} — {Why}",
                box.X1,
                box.Y1,
                box.X2,
                box.Y2,
                q.Deaths,
                box.Turned,
                empty,
                why
            );

            _quarantine.RemoveAt(i);
            lifted++;
        }

        if (lifted > 0)
        {
            Lifted += lifted;
            Save();
        }

        return lifted;
    }

    /// <summary>
    /// How many times a lone bot's strength (<c>BotKept.LoneBot</c>) the strongest thing left alive in a quarantine's box may be
    /// reckoned at, at its kind's price, for the box to count as emptied (<see cref="Lift"/>). One and a half: what a lone bot
    /// takes on (<c>BotThreat.Tolerance</c>). Nought never lifts by a company.
    /// </summary>
    public static double EmptyOdds { get; set; } = 1.5;

    /// <summary>The strength past which a quarantine's box is not empty. See <see cref="EmptyOdds"/>.</summary>
    public static double EmptyBelow => BotKept.LoneBot * Math.Max(0.0, EmptyOdds);

    /// <summary>Quarantines a company's work reached and left standing: something still in them past <see cref="EmptyBelow"/>.</summary>
    public static long Unemptied { get; private set; }

    /// <summary>Quarantines a company's work reached and left standing because the plague or a killer of <see cref="QuarantinePower"/> declared them.</summary>
    public static long DireKept { get; private set; }

    /// <summary>Quarantines lifted by a company that cleared their ground. See <see cref="Lift"/>.</summary>
    public static long Lifted { get; private set; }

    /// <summary>Whether this place lies in a quarantine still standing; the ones run out are dropped as they are met.</summary>
    private static bool InQuarantine(Point3D where)
    {
        var now = Core.TickCount;

        for (var i = _quarantine.Count - 1; i >= 0; i--)
        {
            var q = _quarantine[i];

            if (q.Until - now <= 0)
            {
                logger.Information("Quarantine lifted: ({X1},{Y1})-({X2},{Y2}) after {Deaths} deaths, {Turned} choices turned away", q.Box.X1, q.Box.Y1, q.Box.X2, q.Box.Y2, q.Deaths, q.Box.Turned);
                _quarantine.RemoveAt(i);
                Save();

                continue;
            }

            if (q.Box.Holds(where.X, where.Y))
            {
                q.Box.Turned++;
                QuarantineTurned++;

                return true;
            }
        }

        return false;
    }

    /// <summary>The quarantines in force with the minutes they have left, for the dashboard. See <see cref="Quarantines"/>.</summary>
    public static IEnumerable<(int X1, int Y1, int X2, int Y2, string Why, int Deaths, int MinutesLeft, long Turned)> Standing()
    {
        var now = Core.TickCount;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];
            var left = q.Until - now;

            if (left > 0)
            {
                yield return (q.Box.X1, q.Box.Y1, q.Box.X2, q.Box.Y2, q.Box.Why, q.Deaths, (int)(left / 60000), q.Box.Turned);
            }
        }
    }

    /// <summary>
    /// Where the quarantines are kept between sessions. Not under <c>Saves</c>: the engine moves that whole folder into
    /// <c>Backups/Automatic</c> at every world save and writes a fresh one, so a file put there by hand is gone within
    /// five minutes — the three killing fields of 05:40–05:49 on 29.09.2026 were found in three backups and in no save.
    /// </summary>
    public static string StorePath { get; set; } = Path.Combine("Data", "bot-quarantines.json");

    private sealed class Kept
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public string Why { get; set; }
        public int Deaths { get; set; }
        public DateTime UntilUtc { get; set; }

        /// <summary>Declared or extended by the plague or a killer past <see cref="QuarantinePower"/> (03.10.2026). Absent in older files: false.</summary>
        public bool Dire { get; set; }

        /// <summary>What killed inside it, by kind's type name (03.10.2026). Absent in older files: nothing recorded.</summary>
        public Dictionary<string, double> Killers { get; set; }
    }

    private static Dictionary<string, double> Written(Quarantined_ q)
    {
        if (q.Killers.Count == 0)
        {
            return null;
        }

        Dictionary<string, double> written = [];

        foreach (var (kind, power) in q.Killers)
        {
            written[kind.Name] = Math.Round(power);
        }

        return written;
    }

    private static void Read(Quarantined_ q, Dictionary<string, double> written)
    {
        if (written == null)
        {
            return;
        }

        foreach (var (name, power) in written)
        {
            if (!string.IsNullOrEmpty(name) && AssemblyHandler.FindTypeByName(name) is { } kind)
            {
                Record(q, kind, power);
            }
        }
    }

    /// <summary>
    /// <b>A quarantine outlives the session (29.09.2026).</b> Until this the list lived in memory only, and every deploy
    /// reopened every killing field the last session had learned; the ground does not get safer for a rebuild. Written
    /// whenever one is declared, extended or lifted; read once at boot with the time left, so a two-hour closure declared
    /// ten minutes before a restart stands for an hour and fifty after it.
    /// </summary>
    public static void Save()
    {
        try
        {
            var path = Path.Combine(Core.BaseDirectory, StorePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var now = Core.TickCount;
            List<Kept> kept = [];

            for (var i = 0; i < _quarantine.Count; i++)
            {
                var q = _quarantine[i];
                var left = q.Until - now;

                if (left > 0)
                {
                    kept.Add(
                        new Kept
                        {
                            X1 = q.Box.X1, Y1 = q.Box.Y1, X2 = q.Box.X2, Y2 = q.Box.Y2, Why = q.Box.Why, Deaths = q.Deaths,
                            UntilUtc = DateTime.UtcNow.AddMilliseconds(left), Dire = q.Dire, Killers = Written(q)
                        }
                    );
                }
            }

            File.WriteAllText(path, JsonSerializer.Serialize(kept, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e)
        {
            logger.Warning(e, "The quarantines could not be written to {Path}", StorePath);
        }
    }

    /// <summary>Reads the quarantines a previous session left, with the time they have left. See <see cref="Save"/>.</summary>
    public static void Load()
    {
        try
        {
            var path = Path.Combine(Core.BaseDirectory, StorePath);

            if (!File.Exists(path))
            {
                return;
            }

            var kept = JsonSerializer.Deserialize<List<Kept>>(File.ReadAllText(path)) ?? [];
            var now = Core.TickCount;
            var read = 0;

            for (var i = 0; i < kept.Count; i++)
            {
                var k = kept[i];
                var left = (long)(k.UntilUtc - DateTime.UtcNow).TotalMilliseconds;

                if (left <= 0 || Homely(Map.Felucca, new Point3D((k.X1 + k.X2) / 2, (k.Y1 + k.Y2) / 2, 0)))
                {
                    continue;
                }

                // A file written before 03.10.2026 has no Dire: every quarantine that is not a killing field was declared for the plague
                // or a killer past QuarantinePower (Fell), and says so in its why.
                var dire = k.Dire || k.Why != null && !k.Why.StartsWith("a killing field", StringComparison.Ordinal);
                var back = new Quarantined_ { Box = new Bar(k.X1, k.Y1, k.X2, k.Y2, k.Why ?? "kept from the last session"), Until = now + left, Deaths = k.Deaths, Dire = dire };

                Read(back, k.Killers);
                _quarantine.Add(back);
                read++;
            }

            if (read > 0)
            {
                logger.Information("Quarantines: {Read} read back from the last session, still in force", read);
            }
        }
        catch (Exception e)
        {
            logger.Warning(e, "The quarantines could not be read from {Path}", StorePath);
        }
    }

    /// <summary>The quarantines standing, for the dashboard.</summary>
    public static IEnumerable<(int X1, int Y1, int X2, int Y2, string Why, int Deaths)> Quarantines()
    {
        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];

            yield return (q.Box.X1, q.Box.Y1, q.Box.X2, q.Box.Y2, q.Box.Why, q.Deaths);
        }
    }

    /// <summary>Choices turned away, over every bar.</summary>
    public static long Turned { get; private set; }

    /// <summary>
    /// Replaces the list wholesale. Called by configuration and by nothing else; an empty array is a
    /// deliberate "bar nothing" rather than a file somebody forgot to write.
    /// </summary>
    public static void Set(IEnumerable<Bar> bars)
    {
        _bars.Clear();

        if (bars == null)
        {
            return;
        }

        foreach (var bar in bars)
        {
            if (bar != null)
            {
                _bars.Add(bar);
            }
        }
    }

    /// <summary>
    /// Whether this place is on the list.
    ///
    /// Asked on the choosing path of every sampler on the shard through <see cref="BotRefused.Refusing"/>,
    /// so it is a walk of a list that holds one entry rather than anything cleverer. A list long enough for
    /// that to matter is a list that wants a different structure and a reason for being that long.
    /// </summary>
    /// <summary>Whether this tile lies inside a standing bar or a quarantine in force; a pure look, nothing counted. For the roads (BotDanger).</summary>
    public static bool Holds(Map map, Point3D where)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            return false;
        }

        for (var i = 0; i < _bars.Count; i++)
        {
            if (_bars[i].Holds(where.X, where.Y))
            {
                return true;
            }
        }

        var now = Core.TickCount;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            var q = _quarantine[i];

            if (q.Until - now > 0 && q.Box.Holds(where.X, where.Y))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Barred(Map map, Point3D where)
    {
        if (!Running || _bars.Count == 0 && _quarantine.Count == 0 || map == null || map == Map.Internal)
        {
            return false;
        }

        for (var i = 0; i < _bars.Count; i++)
        {
            if (!_bars[i].Holds(where.X, where.Y))
            {
                continue;
            }

            _bars[i].Turned++;
            Turned++;

            return true;
        }

        return _quarantine.Count > 0 && InQuarantine(where);
    }

    /// <summary>Said once at boot, so the only proof of what is barred is in the log like every other dial.</summary>
    public static void Announce()
    {
        if (!Running)
        {
            logger.Information("No ground is barred to the population; every place on the island may be chosen");

            return;
        }

        for (var i = 0; i < _bars.Count; i++)
        {
            var bar = _bars[i];

            logger.Information(
                "Barred ground: nobody may take work inside ({X1}, {Y1})-({X2}, {Y2}) — {Why}",
                bar.X1,
                bar.Y1,
                bar.X2,
                bar.Y2,
                bar.Why
            );
        }
    }

    public static string Describe()
    {
        if (!Running)
        {
            return "no ground is barred";
        }

        if (_bars.Count == 0)
        {
            return "no ground is barred";
        }

        List<string> said = [];

        for (var i = 0; i < _bars.Count; i++)
        {
            said.Add($"({_bars[i].X1},{_bars[i].Y1})-({_bars[i].X2},{_bars[i].Y2}) turned {_bars[i].Turned}");
        }

        var now = Core.TickCount;
        var inForce = 0;

        for (var i = 0; i < _quarantine.Count; i++)
        {
            if (_quarantine[i].Until - now > 0)
            {
                inForce++;
            }
        }

        return $"{_bars.Count} pieces of ground are barred outright, turning {Turned} choices away: {string.Join("; ", said)}; "
            + $"{Quarantined} quarantines declared ({Fields} of them killing fields of {KillingDeaths} dead in {KillingWindowMs / 60000} minutes), "
            + $"{inForce} in force, {QuarantineTurned} choices turned by them, {Spared} spared for being home, {Lifted} lifted by a company that cleared the ground, {Unemptied} left standing after a company's work for something still in them past {EmptyBelow:F0} at its kind's price, {DireKept} left to their clock for the plague or a killer of {QuarantinePower:F0}";
    }

    /// <summary>
    /// A world reload clears the counters and not the bars.
    ///
    /// The bars are a statement about the map, and the map is the one thing a reload does not change.
    /// </summary>
    public static void Forget()
    {
        Turned = 0;
        Fields = 0;
        Unemptied = 0;
        DireKept = 0;
        _fallen.Clear();

        for (var i = 0; i < _bars.Count; i++)
        {
            _bars[i].Turned = 0;
        }
    }
}
