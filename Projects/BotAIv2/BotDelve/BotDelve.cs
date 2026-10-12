using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Tiered;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// A party taken down a dungeon by a member of its guild — two for every creature of the dungeon's worst room, five at the
/// fewest — and kept there for twenty minutes or twenty corpses.
///
/// <para>
/// <b>Patrick's order of 11.09.2026.</b> A guild's leader calls a delve, five go down — a fighter, the
/// leader, and three more of whatever answers — they run the tunnels and fight everything that moves, and
/// what they take is divided at the end: half to the leader and the rest evenly between the other four. It
/// is the first thing on this shard that a guild does together for its own sake rather than for a hall or
/// a piece of ground.
/// </para>
///
/// <para>
/// <b>They are put down there rather than walking, and that is a fact about the map rather than a
/// convenience.</b> The dungeon block is cut into its own quarter of the world from x 5120 eastward and has
/// no walkable connection to the island at all — the engine's way in is a teleporter on an entrance, and a
/// bot asked to walk to one is asked for a road that does not exist. This shard has measured what that
/// costs: two and a half thousand walk failures in a session, a third of them at three destinations nothing
/// could reach. So the descent is the door, the way up is the same door, and <see cref="BotDelveParty"/>
/// holds the promise that everybody who went down comes back up.
/// </para>
///
/// <para>
/// <b>The muster is the Baron's, because his is the one on this shard that has been made to work.</b> A
/// call stands in a place while the population walks past it and whoever is free falls in; nobody is asked
/// in advance, because a population that is working well has no volunteers standing about in it. What is
/// different here is who is called first: this guild's own members, before anybody else, which is what
/// makes a delve a thing a band does rather than a thing five strangers do.
/// </para>
///
/// <para>
/// <b>One fighter is a condition and the other three are not.</b> A party of a crafter and four healers is
/// a party that will spend twenty minutes bandaging each other, so the line is required; past that, the
/// order says "three others who answer" and this takes them in the order they answer.
/// </para>
///
/// <para>
/// <b>Five became the fewest, not the number (30.09.2026, build 359; Patrick: "a multiple above the mobs in the dungeon").</b>
/// A party calls two for every creature of its dungeon's worst room and goes down only outnumbering it. See <see cref="PartyFor"/>.
/// </para>
/// </summary>
public sealed class BotDelve : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDelve));

    public const string Trade = "delve";

    /// <summary>
    /// How many go down, the leader included, at the fewest: a party calls <see cref="PartyFor"/>, which is never less.
    /// </summary>
    public static int Company { get; set; } = 5;

    /// <summary>
    /// <b>How many a party for this dungeon calls: two for every creature of its worst room (30.09.2026, build 359; Patrick:
    /// "a multiple above the mobs in the dungeon").</b>
    ///
    /// <para>
    /// The room is the one the proving ground fights (<c>BotRoomTrial.Worst</c>, up to <c>BotProving.MostFoes</c> creatures) and
    /// the multiple is the ring's own (<c>BotProving.PartyMultiple</c>), read from there rather than copied: on 356 the ring's
    /// eight cleared Wrong's four golem controllers with one of eight fallen, where five against the same four never did in
    /// any trial before 356 and companies of three and four took one of four at best. Never fewer than <see cref="Company"/>,
    /// never more than <see cref="MostParty"/> (twenty since 01.10.2026; twelve, <c>BotDelver.HeadsPerParty</c>, before).
    /// </para>
    ///
    /// <para>
    /// <b>The easiest dungeon too.</b> Parties of five into the Orc Caves came out on their twenty corpses 97 times against 16
    /// leaders fallen on 21–25.09, while the world slept round anybody with no client; since it was woken (build 259, 13:16 on
    /// 26.09) 43 delves into the Orc Caves have ended, 1 on its twenty corpses and 40 with the leader fallen — 34 of them on
    /// 28.09 — and the three parties of five put down there on the evening of 29.09 lost their leaders inside two to three
    /// minutes, two of them with every raising spent (Mirthal's, put down at (5317, 1309) at 19:58:50, "whoever was leading it
    /// fell … 0 raisings unspent" at 20:00:28). Its worst room is a spawner of six (orcish lords and mages, a bomber, orcs:
    /// Data/Spawns/shared/felucca/OrcCaves.json), so a party calls twelve and goes down with seven at the fewest.
    /// </para>
    /// </summary>
    public static int PartyFor(Map map, BotDungeon.Deep deep)
    {
        var want = Math.Max(Company, RoomOf(map, deep) * Math.Max(1, BotProving.PartyMultiple));

        return Math.Min(want, Math.Max(Company, MostParty));
    }

    /// <summary>
    /// <b>The most a party calls: twenty, the companies' ceiling (01.10.2026, Patrick: «companies may gather up to 20»).</b> It was
    /// <c>BotDelver.HeadsPerParty</c> (twelve), which is also how many creatures a dungeon keeps alive for each party in it and
    /// so how many parties it holds — two numbers on one shelf. The call's size is still two for each creature of the worst
    /// room (<c>BotProving.MostFoes</c> six at most, so twelve today); only the cap is lifted, and HeadsPerParty keeps its own
    /// meaning.
    /// </summary>
    public static int MostParty { get; set; } = 20;

    /// <summary>
    /// The fewest a party for this dungeon goes down with when the call runs out, and marches on with: one more than its worst
    /// room holds — outnumbering it at all — and never fewer than <see cref="Fewest"/> nor more than it calls (<see cref="PartyFor"/>).
    /// Seven for the Orc Caves.
    /// </summary>
    public static int Least(Map map, BotDungeon.Deep deep) =>
        Math.Min(PartyFor(map, deep), Math.Max(Fewest, RoomOf(map, deep) + 1));

    /// <summary>How many creatures each dungeon's worst room holds, read once from its spawners, with the MostFoes it was read at.</summary>
    private static readonly Dictionary<string, (int Most, int Count)> _worstRooms = [];

    /// <summary>How many creatures the worst room of this dungeon holds, as the proving ground builds it. Nought when unknown.</summary>
    public static int RoomOf(Map map, BotDungeon.Deep deep)
    {
        if (map == null || map == Map.Internal || deep == null)
        {
            return 0;
        }

        var most = BotProving.MostFoes;

        if (_worstRooms.TryGetValue(deep.Name, out var had) && had.Most == most)
        {
            return had.Count;
        }

        var count = BotRoomTrial.Worst(map, deep, most).Count;

        // Kept once the dungeons have been surveyed, when the spawners are certainly in the world; asked again until then.
        if (count > 0 || BotDungeon.Surveyed)
        {
            _worstRooms[deep.Name] = (most, count);
        }

        return count;
    }

    /// <summary>What each dungeon read so far calls and goes down with, for the summary.</summary>
    private static string Sizes()
    {
        if (_worstRooms.Count == 0)
        {
            return "no dungeon's worst room read yet";
        }

        var say = ValueStringBuilder.Create(256);

        try
        {
            foreach (var (name, room) in _worstRooms)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                var want = Math.Min(Math.Max(Company, room.Count * Math.Max(1, BotProving.PartyMultiple)), Math.Max(Company, BotDelver.HeadsPerParty));
                var least = Math.Min(want, Math.Max(Fewest, room.Count + 1));

                // The effective floor per dungeon: the fewest less the seats a party of that size keeps for the weak. See WeakShare.
                say.Append($"{name} {want} against {room.Count} ({least} at the fewest, {least - WeakSeats(least)} of them over the floor)");
            }

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>How many of them must be able to hold a line.</summary>
    public static int Fighters { get; set; } = 1;

    /// <summary>
    /// <b>The least a body is worth, as a share of its dungeon's worst creature, to hold an ordinary seat in the party; under it
    /// the body takes one of the few seats a party keeps for the weak (<see cref="WeakShare"/>). Two fifths (12.10.2026, build 434).</b>
    ///
    /// <para>
    /// The evening the population grew from 310 to 450 the Orc Caves took 112 deaths in 4.26 hours (26 an hour against 6–8 on
    /// 432–433) from 60 parties, 1.9 a party. The party's sum cleared the dungeon's ask three times over at every mouth count
    /// (median 22 567 of strength against the 7 710 wanted), so the sum was not the measure; nor was the body's own strength,
    /// since members died at 0.12–0.16 a descent in every band of <c>BotThreat.Power</c> from under 1 500 to over 6 000. What
    /// separated the parties was how many bodies under 2 000 they carried: with two or fewer, 0.25 a party (4 parties) and
    /// no newcomer dead of 13; with three or more, 1.88 (56 parties, 55 of 322 newcomers dead). 2 000 is two fifths of the
    /// orcish lord the Orc Caves are measured by (5 140); the proving ground's fifth (<c>BotDelver.WeakestShare</c>, 1 028) is
    /// a different rule — a body that may not go at all — and at that floor the parties read alike (1.4 a party with nobody
    /// under it), which is why it is not reused here.
    /// </para>
    ///
    /// <para>
    /// <b>434's dose–response did not reproduce on 436 (13.10.2026, reviews A and B).</b> The overnight run of 436 (23:18–03:37,
    /// 450 bots, no floor) made 105 descents and lost 34 in the box, 0.32 a party, with newcomers still a third of the pressings
    /// (432 of 1 355) — the make-up this comment's table puts at 1.86. The 434 spike was the first hours of a 140-bot burst, before
    /// the newcomers had levelled or found gear, and it closed on its own. So the floor is not kept for a sink that is shown to
    /// be composition; it is kept for Patrick's standing rule (25.09, 30.09) that newcomers level before dungeons. The floor is a
    /// share of the dungeon's worst, so at Worst 10 000 it is 4 000, over the veterans' median of 2 276: untested past the Orc
    /// Caves (every descent of 434–436 was the Orc Caves) and a possible wall on the way to Despise; <c>dial BotDelve.WeakFloor 0</c>
    /// is the off switch (nothing is worth less than nought).
    /// </para>
    /// </summary>
    public static double WeakFloor { get; set; } = 0.4;

    /// <summary>
    /// <b>The share of a party's bodies, the leader among them, that may be under <see cref="WeakFloor"/>. A third.</b>
    ///
    /// <para>
    /// <b>The effective rule, in one sentence: a party goes down with at least <c>Least − ⌊Least × share⌋</c> bodies over the
    /// floor</b> — 5 of the Orc Caves' 7 at a third (⌊7 × 0.34⌋ = 2 seats), 6 at a quarter (⌊7/4⌋ = 1). One rule asked at three
    /// doors on the one count that is true there: the offer counts the smallest party that may go (<c>BotDelver.Raisable</c>, seats
    /// of <see cref="Least"/>); the levy admits a weak body only while the seats of the party it would make are not all taken
    /// (<c>WeakIn &lt; WeakSeats(Count + 1)</c>, so the seats grow with the company and nobody is pressed to be let go again); the
    /// descent counts the seats of the party that stood up, once, before letting anybody go (<see cref="Trimmed"/>). Reviews A
    /// and B of 13.10 found three counts for one party in the first draft — seats of <see cref="Least"/>, of the called size and
    /// of the shrinking count inside the trim — under which 8 with 3 weak trimmed to 6 and failed.
    /// </para>
    ///
    /// <para>
    /// Counted with the leader, because the leader is the body the line is there to cover and the one that fell in 17 of the
    /// 56 parties over the share on 434, and in none of the 4 under it; a medic never counts (<see cref="Weak"/>). A third rather
    /// than the quarter 434 argued for: 436 ran a night at a third newcomers and 0.32 a party (see <see cref="WeakFloor"/>), and
    /// the quarter's 0.33–0.38 rests on 6–8 parties. The quarter stays at the door for the next burst: if its first four hours
    /// repeat 434's 1.9 a party, <c>dial BotDelve.WeakShare 0.25</c>.
    /// </para>
    /// </summary>
    public static double WeakShare { get; set; } = 0.34;

    /// <summary>
    /// <b>Whether a party that bled (<c>BotSquad.Bleed</c>, two lost inside a minute) is lifted out where it stands rather than
    /// walked out by the mouth. On.</b> Of the 118 Orc Caves deaths on 434, 33 came after the party's first pull-back: 17 parties
    /// bled, 27 died before the bleed and 33 after it, walking out through the room that had just killed two, and in 5 of them
    /// the leader fell on the way. A party that lost two in a minute is routed, and a rout walked out in formation through the
    /// same orcs is the second half of the rout. Patrick's walk-out order (29.09) stands for a party that finished.
    /// </summary>
    public static bool LiftWhenBled { get; set; } = true;

    /// <summary>
    /// Whether this body is under the party's floor for this dungeon. See <see cref="WeakFloor"/>.
    ///
    /// <b>Never a medic.</b> <c>BotThreat.Power</c> is health times the weapon's hit plus half the Magery; Healing counts for
    /// nothing, so a healer of 80 — the one body that can raise the fallen (<c>BotDelveParty.Fell</c>) — read as the weakest thing
    /// in the company and, levied in the last pass, was the first let go at the descent (review B, 13.10.2026). A tamer's pets
    /// are not in the number either, but a tamer fights with its bow and is read by it; left as it is, unmeasured.
    /// </summary>
    public static bool Weak(Mobile who, BotDungeon.Deep deep) =>
        who is { Deleted: false } && who is not BotMobile { Class.Role: BotRole.Medic } && deep is { Worst: > 0.0 }
        && BotThreat.Power(who) < deep.Worst * WeakFloor;

    /// <summary>How many of a party of this many bodies may be under the floor. See <see cref="WeakShare"/>.</summary>
    public static int WeakSeats(int bodies) => (int)(Math.Max(0, bodies) * WeakShare);

    /// <summary>The living members of the company on this map under the floor, the leader among them.</summary>
    private int WeakIn(BotSquad squad)
    {
        var weak = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is { Deleted: false, Alive: true } self && self.Map == _map && Weak(self, _deep))
            {
                weak++;
            }
        }

        return weak;
    }

    /// <summary>Bots the levy passed over for being under the floor with the party's weak seats already taken. See <see cref="WeakShare"/>.</summary>
    public static long PassedWeak { get; private set; }

    /// <summary>Members let go at the descent for being under the floor beyond the seats the party that stood up keeps for them.</summary>
    public static long Trimmed { get; private set; }

    /// <summary>Descents not made because, with the weak beyond their seats let go, fewer than <see cref="Least"/> were left.</summary>
    public static long ShortOfFloor { get; private set; }

    /// <summary>Parties turned back at a count on the road or at the mouth for carrying more under the floor than their seats, after losses.</summary>
    public static long WeakAtMouth { get; private set; }

    /// <summary>Bodies lifted out at once because their party bled. See <see cref="LiftWhenBled"/>.</summary>
    public static long BledLifted { get; private set; }

    /// <summary>Bodies lifted out of a settled party whose leader fell on the walk out, rather than left to the watchdog's ten minutes.</summary>
    public static long LeaderlessLifted { get; private set; }

    /// <summary>
    /// Descents made while a quarantine (<c>BotBarred</c>, a killing field) stood over any room of the dungeon. Counted, not
    /// refused: 23 of 434's 60 descents went in under the one raised over the Orc Caves at 19:27 and lost 2.26 a party against
    /// 1.38 outside the window, but the window was also the newcomers' peak and 436 raised none; the count is what a later test
    /// of a gate needs (review B, 13.10.2026). The delve itself asks the quarantine only about its muster.
    /// </summary>
    public static long UnderQuarantine { get; private set; }

    /// <summary>Whether a standing quarantine holds any room of this dungeon. Asked once a descent.</summary>
    private static bool Quarantined(Map map, BotDungeon.Deep deep)
    {
        if (map == null || deep == null)
        {
            return false;
        }

        var rooms = deep.Rooms;

        for (var i = 0; i < rooms.Count; i++)
        {
            if (BotBarred.Holds(map, rooms[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The fewest a party goes down with when the call runs out. Three: 17 of the 18 delves that failed between 28.09 and
    /// 29.09 failed "only N of the 5 stood up", most of them with three or four standing. A band of three with a line is a
    /// party; the dungeon's own measure (BotDelver.Recheck) still sends it somewhere it can take. Since build 359 the floor
    /// under <see cref="Least"/>, which asks one more than the dungeon's worst room holds.
    /// </summary>
    public static int Fewest { get; set; } = 3;

    /// <summary>
    /// <b>How far round the muster a line-holder is called while the company has none (30.09.2026, build 359).</b> The rest are
    /// called from <see cref="Reach"/>. Rhistel of The Wolf, a guild with no warrior in it, stood at (2640, 640) on 356 with four
    /// archers gathered in a minute, and waited out the call "for a fighter": nobody who holds a line stood within 320, one
    /// brawler stood at 390 and four line-holders of The Needle and The Hammer at 530 to 600 — "nobody who could hold a line
    /// answered the call for the Orc Caves", the ninth such since 25.09. Four hundred, the company's own door
    /// (<c>BotSquads.JoinReach</c>), past which a bot is refused a place however it is called; never read past it.
    /// </summary>
    public static int LineReach { get; set; } = 400;

    /// <summary>Line-holders called from beyond <see cref="Reach"/> for a company that had none. See <see cref="LineReach"/>.</summary>
    public static long LinesFar { get; private set; }

    /// <summary>Calls that ended with a line-holder in the company and not at the muster, counted in <see cref="Lineless"/> too.</summary>
    public static long LineAway { get; private set; }

    /// <summary>Bots that went down with the parties, the leaders included: over <see cref="Marches"/>, the size of a party.</summary>
    public static long Heads { get; private set; }

    /// <summary>
    /// Corpses that finish a delve early.
    ///
    /// Twenty, by order, and it is the same figure the Baron's harrowing uses — which is the only evidence
    /// on this shard about what a company of five can actually get through in the time.
    /// </summary>
    public static int Quota { get; set; } = 20;

    /// <summary>How long a delve lasts. Twenty minutes, by order.</summary>
    public static int CapMs { get; set; } = 1200000;

    /// <summary>
    /// How long the leader stands calling before it gives up.
    ///
    /// Three minutes rather than the Baron's five. A delve is twenty minutes long; a call that can eat a
    /// quarter of it is a call that has changed what the errand is.
    /// </summary>
    public static int MusterMs { get; set; } = 180000;

    /// <summary>How far round the muster point bodies are called up. Three hundred and twenty since 29.09.2026: the guilds seat themselves by other towns now.</summary>
    public static int Reach { get; set; } = 320;

    /// <summary>How near the muster a called bot has to be standing to count as gathered.</summary>
    public static int Assembly { get; set; } = 24;

    /// <summary>
    /// How long a party is given to walk from the muster to the cave mouth and through it. Fifteen minutes: Despise's mouth
    /// is four hundred tiles from home by road, Shame's a thousand.
    ///
    /// <para>
    /// <b>Patrick's order of 29.09.2026: the party walks in by the entrance and walks out by it.</b> The descent by
    /// <c>MoveToWorld</c> (<see cref="Put"/>) is kept only for a dungeon no gate joins to where the party stands.
    /// </para>
    /// </summary>
    public static int MarchMs { get; set; } = 900000;

    /// <summary>
    /// <b>Past <see cref="MarchMs"/> a march still closing goes on (01.10.2026, build next).</b> Fifteen minutes is a fixed sum for
    /// a road of any length, and on build 384 (00:42–10:20) 13 of the 41 delves ended "could not reach the mouth of the Orc Caves
    /// in 15 minutes" from musters by Britain — Brynhild's twelve from (1391, 1359) were at (1101, 1186) when the clock ran out,
    /// walking the road north round the hills; Thrain's from (1455, 799) reached the mouth in 11.5. A march past MarchMs fails
    /// only when its leader has come no nearer to its next gate (or to the room, past the last gate) by
    /// <see cref="ProgressTiles"/> in <see cref="MarchStallMs"/> of walking time; fights stay off both clocks; and no march
    /// runs past <see cref="MarchCapTimes"/> × MarchMs.
    /// </summary>
    public static int MarchStallMs { get; set; } = 300000;

    /// <summary>How many tiles nearer the next gate count as the march getting on. See <see cref="MarchStallMs"/>.</summary>
    public static int ProgressTiles { get; set; } = 8;

    /// <summary>How often the march's progress is read: a gate lookup on the tiered graph, so not every beat.</summary>
    public static int ProgressEveryMs { get; set; } = 5000;

    /// <summary>The whole march's ceiling, in MarchMs. Three: forty-five minutes.</summary>
    public static int MarchCapTimes { get; set; } = 3;

    /// <summary>
    /// How far behind its leader a member must be, on one land, to be read as on the far side of a gate the leader has just
    /// walked through (01.10.2026, build next). A moongate joins Minoc to Britain on the same land, so the land test never
    /// fired there: at 00:51:48–52 on build 384 Cerys stepped through at (2701, 692) and left ten members "1369–1374 tiles back
    /// after 0s … journey moving towards (1337, 1996, 5) (closing up on Cerys)" in four seconds, turned back, and failed "nobody
    /// left to march with". Nobody marching is three hundred tiles behind its column but across a gate.
    /// </summary>
    public static int GateApart { get; set; } = 300;

    /// <summary>Marches kept going past MarchMs for still closing on the mouth; members struck off the march for no longer serving it.</summary>
    public static long KeptMarching { get; private set; }

    public static long Deserted { get; private set; }

    /// <summary>Leaders put through a moongate on the road by the march itself. See the note at its use in <see cref="Marching"/>.</summary>
    public static long MoonLed { get; private set; }

    /// <summary>How long the party's stragglers are given after the leader enters before "nobody left to delve with" can end it. Two minutes.</summary>
    public static int ArriveMs { get; set; } = 120000;

    /// <summary>How long the leader walks toward the mouth on the way out before the delve ends without it. Five minutes.</summary>
    public static int LeaveMs { get; set; } = 300000;

    /// <summary>Parties that marched to a cave mouth, and those put down by hand because no gate joined the dungeon.</summary>
    public static long Walked { get; private set; }

    public static long Carried { get; private set; }

    /// <summary>Marches that never reached the mouth in <see cref="MarchMs"/>.</summary>
    public static long Unreached { get; private set; }

    /// <summary>Delves that ended by walking out, and leaders that gave up walking out after <see cref="LeaveMs"/>.</summary>
    public static long WalkedOut { get; private set; }

    public static long LeftBelow { get; private set; }

    /// <summary>Settled parties lifted out at once because the leader's walk out by the mouth was refused, and the bodies lifted (work17). See <see cref="Bend"/>.</summary>
    public static long LiftedAtRefusal { get; private set; }

    public static long LiftedBodies { get; private set; }

    /// <summary>Whether this delve's party was lifted out after its walk out was refused. See <see cref="Bend"/>.</summary>
    private bool _lifted;

    /// <summary>How close the leader stands to the muster point before the clock starts.</summary>
    public static int Station { get; set; } = 2;

    /// <summary>
    /// <b>The muster stands before the danger, not at the seat (29.09.2026, Patrick's word).</b> Ingrun led three from the
    /// muster at Britain 430 tiles west to the mouth of the Orc Caves at 09:11 and arrived alone at 09:17; Hilde's party
    /// was attacked on the same road at 08:28 and fled. The road from the seat to the mouth is read off the tiered
    /// planner; the muster is set this many tiles before the first point of it that the quadrant map reads as bloodied
    /// (<c>BotQuad.Muscle</c>) or that is barred, so every member walks the safe part alone and the company walks the
    /// rest together. A road with no such point musters this far before the mouth. Nought keeps the seat.
    /// </summary>
    public static int MusterBack { get; set; } = 48;

    /// <summary>How far a member may fall behind the leader on the march before the leader stops for it. Eight tiles.</summary>
    public static int MarchSpread { get; set; } = 8;

    /// <summary>Behind this the member is not waited for but left; and a hold longer than <see cref="HoldMs"/> ends the same way.</summary>
    public static int MarchLost { get; set; } = 60;

    public static int HoldMs { get; set; } = 90000;

    /// <summary>
    /// <b>How long one held turn holds the leader's walk (01.10.2026, build next).</b> A hold used to finish the walk and the next
    /// turn asked for it afresh — a new errand, the route drawn again over the graph and a new plan, two or three times a second
    /// for a column of twelve (506 holds over the two delves of build 386, both dropped on the road by the walker's loop guard
    /// "because it planned 31 roads to one place in 18s"). The walk is now held with its plan and route kept
    /// (<see cref="BotJourney.Hold"/>), for this long per held turn; every held turn renews it, and the first turn that is not a
    /// hold walks on from where the plan left off. Three times a turn, so a turn missed does not let the leader step.
    /// </summary>
    public static int HoldBeatMs { get; set; } = 1500;

    /// <summary>
    /// The march's own order, with the walk held where it stands. The same place and arrival as <see cref="Marching"/> sends, so the
    /// Will matches it to the walk in hand rather than rebasing the road (BotWill: a changed order is a new plan); the note says
    /// what the hold is for.
    /// </summary>
    private BotDoing Holding(IBotSquadMember member, string why)
    {
        member.Journey?.Hold(HoldBeatMs);

        return _returning
            ? BotDoing.Walk(_map, _muster, BotArrival.Within(Station), why)
            : BotDoing.Walk(_map, _room, BotArrival.Within(3), why);
    }

    /// <summary>How long the leader waits at the landing for a member still on the island side of the mouth. Three minutes.</summary>
    public static int MouthHoldMs { get; set; } = 180000;

    /// <summary>
    /// <b>How near the mouth the party is counted before anybody steps through (30.09.2026, build 338).</b> Twelve tiles: inside
    /// it, with the company closed up (nobody past <see cref="MarchSpread"/>), the party is counted once and goes in or turns
    /// back together (<see cref="Short"/>). On 336 Durgan marched five from the muster, lost Helja to a reaper, left Dagna,
    /// lost Thalion and Norbert forty tiles short of the mouth, and walked in alone at 01:10:56 — "has led 1 into the Orc
    /// Caves" — for the delve to end two minutes later "there was nobody left to delve with". Patrick's word: the company
    /// decides together, before the danger.
    /// </summary>
    public static int MouthTiles { get; set; } = 12;

    /// <summary>
    /// The share of the strength that set out a party must still hold to go on — unless what is left clears the dungeon's own
    /// bar (its worst × <c>BotDelver.Odds</c>) outright. At full health, so a member hurt on the road still counts; what
    /// the share measures is who is missing. Three fifths: a party of five that has lost two is not the party the dungeon
    /// was chosen for.
    /// </summary>
    public static double GoOnShare { get; set; } = 0.6;

    /// <summary>
    /// How much of the march's own clock (<see cref="MarchMs"/>) fights on the road may stop, all together; nought for no limit.
    /// <b>No limit since 30.09.2026 (build 374), Patrick's word that evening:</b> «when they fight on the way to the goal, their timer
    /// stands still, because they are fighting» — while they fight on the way to the goal, their clock stands still. It was five minutes, and
    /// Daeron's party of nine on 371 — the day's first to go down — halted four times on 240 tiles from Britain to the Orc
    /// Caves' mouth and was given up at fifteen minutes.
    /// </summary>
    public static int MarchFightMs { get; set; } = 0;

    /// <summary>
    /// Refused roads on the march shrugged off before the delve is given up. Two. Until build 338 a refused road on the march
    /// moved the muster to the leader's tile — the calling's rule, and the muster is also the way out — and the second
    /// refusal in the same second ended the delve (Falk, 00:16:28 on 336).
    /// </summary>
    public static int MarchBends { get; set; } = 2;

    /// <summary>Fights the company stood to on the march, the leader with it. See <see cref="Marching"/>.</summary>
    public static long Stood { get; private set; }

    /// <summary>Parties counted at the mouth and found whole enough to go in.</summary>
    public static long WentOn { get; private set; }

    /// <summary>Parties that turned back together, at the mouth or on the way; and of those, the ones that reached the muster.</summary>
    public static long TurnedBack { get; private set; }

    public static long BroughtBack { get; private set; }

    /// <summary>Roads refused to a leader on the march. See <see cref="MarchBends"/>.</summary>
    public static long MarchRefused { get; private set; }

    /// <summary>Musters set forward on the road, holds for stragglers, and members left behind on the march.</summary>
    public static long MusteredForward { get; private set; }

    public static long Held { get; private set; }

    public static long LeftBehind { get; private set; }

    /// <summary>Levied bots whose errand was put down for the enlistment. See <c>Take</c>.</summary>
    public static long Pressed { get; private set; }

    /// <summary>Members let go at the descent for not standing at the muster.</summary>
    public static long Ungathered { get; private set; }

    /// <summary>How far the party looks for something to fight.</summary>
    public static int Sight { get; set; } = 18;

    /// <summary>How long the party works one room before moving to the next.</summary>
    public static int RoomMs { get; set; } = 60000;

    /// <summary>How often the pot is swept up out of the members' packs.</summary>
    public static int SweepMs { get; set; } = 5000;

    /// <summary>How often the leader calls up more bodies while the muster stands.</summary>
    public static int LevyMs { get; set; } = 2000;

    /// <summary>How long a leader leaves the idea alone after a call that raised nobody.</summary>
    public static int RestMs { get; set; } = 600000;

    /// <summary>
    /// How many refused roads a party may shrug off before the delve is given up.
    ///
    /// <para>
    /// <b>Generous on purpose, because a dungeon is a maze and a refused step is ordinary in one.</b> The
    /// first party that ever went down was out again in forty-one seconds: the squad asked a member to take
    /// its place in the formation three tiles away, the search said there was no way from where it stood —
    /// a wall between them, which in a cavern is most of the time — and the shard's ordinary answer to a
    /// refused road is to end the errand. That answer is right for a bot that cannot reach a forge. It is
    /// wrong for five bots in a cave, where the correct response is to go somewhere else in the cave.
    /// </para>
    ///
    /// <para>
    /// <b>Set high enough that it is not the thing which ends a delve, because the first value was.</b> At
    /// twenty-four, the second party ever to go down spent the whole budget in under eighteen minutes and
    /// was cancelled four minutes short of its own clock — a counter standing exactly at its ceiling, which
    /// on this shard has twice turned out to be a backstop that had quietly become the design. What bounds
    /// a delve is <see cref="CapMs"/>; this bounds only the case where the walls really will not let a
    /// party anywhere at all, and <see cref="Walled"/> names it if it ever fires again.
    /// </para>
    /// </summary>
    public static int MaxBends { get; set; } = 200;

    /// <summary>
    /// How many times one party's leader may be fetched back out of the world before the delve is given up.
    ///
    /// Two. Enough that a single accident does not cost four other bots their twenty minutes, small enough
    /// that a leader being pulled out over and over is not answered by putting it back over and over. See
    /// <see cref="Strayed"/>.
    /// </summary>
    public static int MaxStrays { get; set; } = 2;

    /// <summary>
    /// What a delve is reckoned at per minute before the ledger has measured one.
    ///
    /// <para>
    /// <b>120 from 260 (12.10.2026, build 434).</b> The auction reads a claim no lower than a quarter of it (<c>BotCommons.LeastShare</c>),
    /// so 260 held the reading at 65 a minute while the ledger measured 35–42 over 826–1 502 outcomes, and the shard's own alarm
    /// stood on it for 1 441 minutes on 09–10.10 and from 18:36 on 12.10: "a number in the source the correction cannot bring
    /// down". At 120 the floor is 30, under anything the ledger has measured, and the reading is the ledger's: 48 a minute at
    /// 42 measured, 41 at 35. Twice the measured pay rather than the measured pay itself because a delve that stops dying
    /// pays more — the 41 that finished on 434 paid 57 a minute; the 20 whose leader fell paid nothing over 2 598 minutes.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 120.0;

    /// <summary>How long the decision layer is told it takes.</summary>
    public static double WorkMinutes { get; set; } = 25.0;

    public static long Calls { get; private set; }

    public static long Marches { get; private set; }

    public static long Undermanned { get; private set; }

    /// <summary>Levies made while the leader was still walking to its muster (01.10.2026). See <see cref="Calling"/>.</summary>
    public static long LeviedEarly { get; private set; }

    /// <summary>How often the levy looks round the muster while the leader is still on its way there: a sweep of the call's whole reach, so less often than at the muster.</summary>
    public static int LevyWalkingMs { get; set; } = 10000;

    public static long Lineless { get; private set; }

    public static long Called { get; private set; }

    public static long Killed { get; private set; }

    public static long Emptied { get; private set; }

    public static long Timedout { get; private set; }

    public static long Broken { get; private set; }

    /// <summary>Parties that came up because the company lost two inside a minute (06.10.2026, company16). See <c>BotSquad.Bleed</c>.</summary>
    public static long BledOut { get; private set; }

    /// <summary>Refused roads shrugged off underground. See <see cref="MaxBends"/>.</summary>
    public static long Bends { get; private set; }

    /// <summary>Calls whose gathering place was moved to the leader's own tile because it could not be reached.</summary>
    public static long Shifted { get; private set; }

    /// <summary>Delves given up because the refusals would not stop.</summary>
    public static long Walled { get; private set; }

    /// <summary>
    /// Delves that ended because their leader was no longer in the dungeon.
    ///
    /// <b>Its own bucket rather than counted with the parties that lost too many members</b>, which is the
    /// rule this project keeps relearning: two reasons sharing one number is a number that cannot answer the
    /// question it was added for. The first session with both in <c>Broken</c> read "4 because too few were
    /// left" when the truth was "4 because the rescue carried their leaders to Britain".
    /// </summary>
    public static long Leaderless { get; private set; }

    /// <summary>
    /// Leaders found out of their dungeon while their party was still fighting, and put back.
    ///
    /// <b>Nought is the only reading that means nothing is wrong.</b> Nobody walks out of the dungeon block —
    /// there is no road — so every one of these is something on the island having moved a body, and the
    /// counter exists to say that it is still happening after the two known causes were shut.
    /// </summary>
    public static long Strayed { get; private set; }

    private static readonly Dictionary<Serial, long> _resting = [];

    /// <summary>Whether this leader is inside the rest a failed call earns it.</summary>
    public static bool Resting(Mobile body) =>
        body != null && _resting.TryGetValue(body.Serial, out var when) && Core.TickCount - (when + RestMs) < 0;

    public static void Rest(Mobile body)
    {
        if (body != null)
        {
            _resting[body.Serial] = Core.TickCount;
        }
    }

    /// <summary>Where it goes. Set at the offer, and reckoned again on the party actually raised (BotDelver.Recheck).</summary>
    private BotDungeon.Deep _deep;

    private readonly Map _map;

    private BotSquad _squad;

    private BotDelveParty _party;

    private Point3D _muster;

    private Point3D _room;

    private bool _calling;

    private bool _down;

    /// <summary>On the way from the muster to the cave mouth. See <see cref="MarchMs"/>.</summary>
    private bool _marching;

    /// <summary>
    /// <b>One call at a time round a muster (30.09.2026, build 376).</b> Since build 370 a call takes any guild's bots but a
    /// hostile one's, and the calls of different guilds went on side by side over the same bots: Miriel at (1169, 1851),
    /// Faunalyn at (1168, 1856) and Brokk at (1254, 1803) all began at 20:05:07, Faunalyn and Brokk 25 tiles apart at
    /// 22:31:01, and every one ended "only 2–5 of the 12 … passed over within reach: 16–17 in a company already" — each
    /// other's companies. The calls under way, so a leader within <see cref="Reach"/> of one is not offered its own
    /// (<see cref="CallingNear"/>) and is levied into that one instead.
    /// </summary>
    private static readonly System.Collections.Generic.List<BotDelve> _callsNow = [];

    private Mobile _caller;

    private long _calledTick;

    /// <summary>
    /// <b>And the offers not yet taken up (build 378).</b> A call joins <see cref="_callsNow"/> on its first beat, and every
    /// leader asked in the same beat was offered before any of them had one: after the restart at 22:49 Mirthal called at
    /// (1109, 1917) and Brom at (1110, 1915). An offer is kept here for <see cref="OfferHoldMs"/>, where the call itself takes over.
    /// </summary>
    private static readonly System.Collections.Generic.List<(Mobile Leader, Map Map, Point3D At, long Tick)> _offersNow = [];

    /// <summary>How long a delve offered to a leader stands for a call before the call itself is under way.</summary>
    public static int OfferHoldMs { get; set; } = 15000;

    /// <summary>A delve was offered to this leader here. From <c>BotDelver.Propose</c>.</summary>
    public static void Offering(Mobile leader)
    {
        if (leader?.Map != null)
        {
            _offersNow.Add((leader, leader.Map, leader.Location, Core.TickCount));
        }
    }

    /// <summary>Whether a call not hostile to this bot is gathering within <paramref name="reach"/> of it. See <see cref="_callsNow"/>.</summary>
    public static bool CallingNear(Mobile body, int reach)
    {
        if (body == null)
        {
            return false;
        }

        var now = Core.TickCount;

        for (var i = _offersNow.Count - 1; i >= 0; i--)
        {
            var offer = _offersNow[i];

            if (now - offer.Tick > OfferHoldMs || offer.Leader is not { Deleted: false, Alive: true })
            {
                _offersNow.RemoveAt(i);

                continue;
            }

            if (offer.Map == body.Map && offer.Leader != body && Utility.InRange(offer.At, body.Location, reach)
                && !BotRegard.Hostile(offer.Leader, body))
            {
                return true;
            }
        }

        for (var i = _callsNow.Count - 1; i >= 0; i--)
        {
            var call = _callsNow[i];

            if (!call._calling || call._marching || call._caller is not { Deleted: false, Alive: true }
                || now - call._calledTick > MusterMs * 2L)
            {
                _callsNow.RemoveAt(i);

                continue;
            }

            if (call._map == body.Map && call._caller != body && Utility.InRange(call._muster, body.Location, reach)
                && !BotRegard.Hostile(call._caller, body))
            {
                return true;
            }
        }

        return false;
    }

    private long _marchTick;

    /// <summary>When the leader began holding for a straggler on the march; nought when it is not. See <see cref="MarchSpread"/>.</summary>
    private long _holdTick;

    /// <summary>When the leader began holding at the landing for a member still crossing the mouth. See <see cref="MouthHoldMs"/>.</summary>
    private long _crossTick;

    /// <summary>When the company began the fight it is standing to on the march; nought when it is in none. See <see cref="StandTo"/>.</summary>
    private long _fightTick;

    /// <summary>How long the company has stood to fights on this march, which the march's clock does not count up to <see cref="MarchFightMs"/>.</summary>
    private long _fought;

    /// <summary>What the company was last seen fighting on the march, so a new enemy is a new stage for the will's clock.</summary>
    private Mobile _foe;

    /// <summary>What the party that marched was worth against the dungeon, and how many it was. See <see cref="Short"/>.</summary>
    private double _setOut;

    private int _setOutCount;

    /// <summary>How many were in the company when it was last counted on the march. A smaller company is counted again.</summary>
    private int _judged;

    /// <summary>The gate off the island into the dungeon block on the party's way, and whether the party has been counted there.</summary>
    private Point3D _mouth;

    private bool _atMouth;

    /// <summary>The delve given up on the way: the company walks back to the muster together. See <see cref="TurnBack"/>.</summary>
    private bool _returning;

    private int _marchBends;

    /// <summary>
    /// Whether two points stand on different lands of the navigation graph (a gate between them), rather than on one land
    /// far apart. Where the graph cannot say, a distance past any march on one land (a thousand tiles) reads as a gate.
    /// </summary>
    private bool OtherLand(Point3D a, Point3D b, int apart)
    {
        var la = BotGates.LandOf(_map, a);
        var lb = BotGates.LandOf(_map, b);

        return la >= 0 && lb >= 0 && la != lb || apart > GateApart;
    }

    /// <summary>
    /// Whether a member is still marching with the company: holding its enlistment, or the flight, bandage or rescue that the
    /// enlistment was put down for. See the note at its use in <see cref="Marching"/>.
    /// </summary>
    private static bool Serving(IBotSquadMember member)
    {
        if (member is not IBotWilful { Resolve: { } resolve })
        {
            return true;
        }

        return resolve.Deed is BotEnlist || resolve.Paused?.Deed is BotEnlist || resolve.Deed is BotDelve;
    }

    /// <summary>
    /// Reads how near the leader stands to the next gate on its way (or to the room, past the last), every
    /// <see cref="ProgressEveryMs"/>; a new least by <see cref="ProgressTiles"/>, or a gate crossed, restarts the stall clock.
    /// </summary>
    private void Progress(Mobile body, long now)
    {
        if (_progressTick == 0)
        {
            _progressTick = now;
            _foughtAtProgress = _fought;
        }

        if (now - _progressReadTick < ProgressEveryMs)
        {
            return;
        }

        _progressReadTick = now;

        var target = _returning ? _muster : _room;
        var next = BotGates.Next(_map, body.Location, target, out var gate) && gate != null ? gate.From : target;
        var away = Math.Max(Math.Abs(body.X - next.X), Math.Abs(body.Y - next.Y));

        // <b>By the road while the leader holds one (01.10.2026, build next).</b> The graph's road from north Britain to the passage
        // at (1013, 1434) leaves the muster north-east and comes round the hills: measured by the crow to the gate the leader was
        // further from it for the first minutes of the road, and the stall clock read a march getting on as one that had stopped.
        // The rest of the route to the target, through the gate, is the measure while the walker holds one (BotJourney.RoadLeft);
        // the crow to the next gate, as before, otherwise. The two are told apart below so a change of measure is a fresh start.
        var journey = (body as BotMobile)?.Journey;

        if (journey != null && journey.HoldsRoadTo(target))
        {
            // The road to the next gate: the rest of the route less the crow from the gate to the target, which RoadLeft adds.
            away = Math.Max(0, journey.RoadLeft(body.Location, target) - Math.Max(Math.Abs(next.X - target.X), Math.Abs(next.Y - target.Y)));
            next = new Point3D(next.X, next.Y, next.Z + 1);
        }

        if (next != _progressGate || away + ProgressTiles <= _progressLeast)
        {
            _progressGate = next;
            _progressLeast = away;
            _progressTick = now;
            _foughtAtProgress = _fought;
        }
    }

    private long _progressTick;

    private long _progressReadTick;

    private long _foughtAtProgress;

    private int _progressLeast = int.MaxValue;

    private Point3D _progressGate;

    private bool _keptMarching;

    /// <summary>A mage's gate taking the company to its rune by the mouth, while it lasts. See <see cref="BotPassage"/>.</summary>
    private BotPassage _passage;

    /// <summary>On the way out by the mouth, the pot already handed out. See <see cref="Leaving"/>.</summary>
    private bool _leaving;

    private long _leaveTick;

    private string _why;

    /// <summary>How many times this party's leader has had to be fetched back. See the note at its use.</summary>
    private int _strays;

    private bool _atMuster;

    private long _musteredTick;

    private long _began;

    private long _sweptTick;

    private long _leviedTick;

    private long _roomTick;

    private int _which;

    private int _kills;

    /// <summary>The company's pull-backs when the party went down; one more and it comes up. See <c>BotSquad.Bled</c> (company16).</summary>
    private int _bledAt;

    private int _here;

    private int _count;

    private Mobile _quarry;

    private int _said0 = -1;

    private int _said1 = -1;

    private int _bends;

    /// <summary>The part of the dungeon this party is in, and may be sent anywhere within. See BotHalls.</summary>
    private BotHalls.Hall _hall;

    /// <summary>How many this party calls, and the fewest it goes down and marches on with. See <see cref="PartyFor"/> and <see cref="Least"/>.</summary>
    private int _want;

    private int _least;

    /// <summary>What a party for the dungeon it is bound for calls and goes down with; asked again whenever that dungeon changes.</summary>
    private void Size()
    {
        _want = PartyFor(_map, _deep);
        _least = Least(_map, _deep);
    }

    public BotDelve(Map map, BotDungeon.Deep deep)
    {
        _map = map;
        _deep = deep;
        _began = Core.TickCount;

        Size();

        // <b>Known before the offer is weighed, not when the errand starts.</b> Where is read by the
        // appraisal the moment this is proposed, and a deed whose place is (0, 0, 0) is priced against a
        // point in the sea — this project's own rule about a coordinate that came out of arithmetic, said
        // about the cheapest possible version of the mistake.
        _muster = Rally(null);
    }

    public override string Kind => Trade;

    /// <summary>The guild's clock and the dungeon's claim start here, when the delve is taken. See <c>BotDelver.Stamp</c>.</summary>
    public override void Taken(IBotWilful bot) => BotDelver.Stamp(bot?.Self, _deep);

    /// <summary>Goes where the fighting is on purpose. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    public override Map Map => _map;

    /// <summary>
    /// The muster, not the dungeon.
    ///
    /// <b>And this is the one place the difference has teeth.</b> Everything on this shard that prices a
    /// piece of work by where it is — the ledger, the reach ledger, the refusal list — would measure a road
    /// to x 5400 that does not exist, mark the ground refused, and teach the population that delving is
    /// impossible. Where this work happens, as far as the rest of the assembly is concerned, is the place
    /// the party gathers; what happens after that is this file's business.
    /// </summary>
    public override Point3D Where => _muster;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. The fights are the shard's ordinary combat and are credited where they happen.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>
    /// What this delve paid the bot holding it, which is the leader's own share and not the party's pot.
    ///
    /// <b>The pot would be a figure four other bots earned.</b> The ledger prices a trade by what the bot
    /// that took it came away with, and reporting the whole pot would teach every maker on the island that
    /// delving pays twice what it does. The same rule the Baron's harrowing states from the other side: a
    /// measure that reads the wrong purse teaches the ledger the wrong thing.
    /// </summary>
    public override int Made => _party == null ? 0 : _party.Owed(_party.Leader);

    public override bool Alongside => true;

    /// <summary>
    /// Once the party is down, this work has spent something dropping it will not get back.
    ///
    /// <para>
    /// <b>The thing spent is five bots' position in the world.</b> Every other errand on this shard can be
    /// abandoned for the price of the walking already done; abandoning this one means lifting a party out
    /// of a dungeon it has been fighting through, and the rest of its twenty minutes is simply gone. So the
    /// auction is told, in the one way it understands, not to outbid a delve that is already happening.
    /// </para>
    ///
    /// <para>
    /// It does not make the work immortal: <c>BotDeed.Pressing</c> still takes precedence, so a leader
    /// being killed still flees, and the delve ends the way it ends. And the call itself is not committed —
    /// a leader standing in the square asking for volunteers has spent nothing.
    /// </para>
    /// </summary>
    public override bool Committed => _down;

    /// <summary>
    /// Standing in one place killing things is what a delve looks like when it is going well.
    ///
    /// The stall watch takes work off a bot that has neither moved nor changed what it says about itself
    /// for four minutes — right for a walk to somewhere unreachable, wrong for a party holding a corridor
    /// against what is coming down it. This errand set its own clock, and by this shard's rule whoever set
    /// a clock owns ending the thing: see <see cref="CapMs"/>.
    /// </summary>
    public override bool Still => _down;

    public override string Stage =>
        _leaving
            ? $"out of {_deep?.Name} by its mouth: {_why}"
            : _returning
                ? $"turning back from {_deep?.Name} with {_squad?.Count ?? 0}: {_why}"
            : _marching
                ? $"marching {_count} to the mouth of {_deep?.Name}"
                : !_down
                    ? $"calling a party for {_deep?.Name}: {_here} of {_want} gathered"
                    : $"in {_deep?.Name} with {_count} of us, {_kills} of {Quota} down, {_party?.Left ?? 0} raisings left";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || _deep == null)
        {
            return BotDoing.Failed("no body, or nowhere to go");
        }

        if (bot is not IBotSquadMember member)
        {
            return BotDoing.Failed("not the sort of thing that leads a party");
        }

        return _leaving
            ? Leaving(member, body)
            : _down
                ? Delving(member, body)
                : _marching
                    ? Marching(member, body)
                    : Calling(member, body);
    }

    /// <summary>
    /// The walk from the muster to the cave mouth and through it. The journey draws the road to the mouth and the engine
    /// puts the walker through; the members follow in formation and come through the same way (their stations lie in the
    /// cave, and a station across a gate is a walk through it — BotSquad, BotGates).
    /// </summary>
    private BotDoing Marching(IBotSquadMember member, Mobile body)
    {
        var now = Core.TickCount;
        var squad = member.Squad;

        if (squad == null)
        {
            return BotDoing.Failed("the party came apart on the way to the mouth");
        }

        // Runes (29.09.2026): while a mage's gate takes the company to its rune by the mouth, the passage has the leader; the
        // march goes on from the far side. See BotPassage.
        if (_passage != null)
        {
            if (_passage.Lead(body, out var through))
            {
                return through;
            }

            _passage = null;
        }

        if (!_returning && _deep.Holds(body.Location))
        {
            return Arrived(squad, body);
        }

        // <b>The company answers an attack on any member as one (30.09.2026, build 338).</b> See StandTo.
        if (squad.Stance == BotSquadStance.Fighting && squad.Focus is { Deleted: false, Alive: true } foe)
        {
            return StandTo(member, squad, body, foe, now);
        }

        if (_fightTick != 0)
        {
            _fought += now - _fightTick;
            _fightTick = 0;
            _foe = null;
        }

        // The fights stood to on the road are not the march's time, up to MarchFightMs of them.
        var marched = now - _marchTick - (MarchFightMs > 0 ? Math.Min(_fought, MarchFightMs) : _fought);

        Progress(body, now);

        // Past MarchMs a march still closing goes on, up to MarchCapTimes of it. See MarchStallMs.
        if (marched >= MarchMs && MarchStallMs > 0 && marched < (long)MarchMs * Math.Max(1, MarchCapTimes)
            && now - _progressTick - (_fought - _foughtAtProgress) < MarchStallMs)
        {
            if (!_keptMarching)
            {
                _keptMarching = true;
                KeptMarching++;

                // One line a march, so a long road is seen going on rather than read off the clock at its end.
                logger.Information(
                    "{Name}'s march {Way} {Deep} goes on past {Minutes} minutes: {Away} tiles from ({X}, {Y}) and still closing, {Count} in the company",
                    body.Name,
                    _returning ? "back from" : "to",
                    _deep.Name,
                    MarchMs / 60000,
                    _progressLeast,
                    _progressGate.X,
                    _progressGate.Y,
                    squad.Count
                );
            }

            marched = 0;
        }

        if (marched >= MarchMs)
        {
            Unreached++;
            Release(squad);
            BotSquads.Leave(member);
            _squad = null;
            _marching = false;

            return BotDoing.Failed(
                _returning
                    ? $"could not bring the party back to the muster at ({_muster.X}, {_muster.Y}) in {MarchMs / 60000} minutes, having turned back from {_deep.Name}: {_why}"
                    : $"could not reach the mouth of {_deep.Name} in {MarchMs / 60000} minutes, from the muster at ({_muster.X}, {_muster.Y}); {(_progressLeast == int.MaxValue ? "never placed on its road" : $"last {_progressLeast} tiles from ({_progressGate.X}, {_progressGate.Y})")}"
            );
        }

        if (_room == Point3D.Zero)
        {
            return BotDoing.Failed($"{_deep.Name} has no room anybody could walk to");
        }

        // Back at the muster, having turned back: the company is home together and the delve is over.
        if (_returning && body.Map == _map && body.InRange(_muster, Assembly))
        {
            return Home(member, squad, body);
        }

        // <b>Counted again whenever it loses somebody, and at the mouth before anybody steps through (30.09.2026, build 338).</b>
        // See MouthTiles and Short: a party that has lost too much on the road turns back together rather than walking in with
        // what is left. At the mouth only once the company has closed up, so the count is of who is there.
        if (!_returning)
        {
            var mouth = !_atMouth && _mouth != Point3D.Zero && body.Map == _map && body.InRange(_mouth, MouthTiles)
                        && squad.Straggler(MarchSpread, out _) == null;

            if (mouth || squad.Count < _judged)
            {
                _atMouth |= mouth;
                _judged = squad.Count;

                var where = mouth ? $"at the mouth of {_deep.Name}" : $"on the way to {_deep.Name}";

                if (Short(squad, body, out var why, out var worth))
                {
                    return TurnBack(member, squad, body, why, where);
                }

                if (mouth)
                {
                    WentOn++;

                    logger.Information(
                        "{Name} brings {Living} of the {SetOut} that set out to the mouth of {Deep}: {Worth} of strength of the {Had} that marched, {Wanted} wanted by its worst; they go in",
                        body.Name,
                        Living(squad),
                        _setOutCount,
                        _deep.Name,
                        worth.ToString("F0"),
                        _setOut.ToString("F0"),
                        (_deep.Worst * BotDelver.Odds).ToString("F0")
                    );
                }
            }
        }

        // <b>The company holds together (29.09.2026).</b> See MarchSpread: a straggler within MarchLost is waited for, up to
        // HoldMs; beyond either it is left, and the march goes on with what is left if that is still enough.
        // <b>A column of twelve is longer than a column of five (01.10.2026, build next).</b> A member closing up stops CloseUp (6)
        // from the leader and keeps its pace, and the march held for anyone past MarchSpread (8) — two thresholds on one shelf for
        // a company of twelve walking in file: 12544 holds for stragglers over the 36 marches of build 384, about 350 a march,
        // and Brynhild's twelve made 470 tiles in fifteen minutes. The spread grows by a tile for every two members past five.
        var spread = MarchSpread + Math.Max(0, squad.Count - BotSquad.Usual) / 2;
        var straggler = squad.Straggler(spread, out var behind);

        // <b>Not for a member that is no longer marching (01.10.2026, build next).</b> A member holding neither its enlistment
        // nor the flight or bandage the enlistment was put down for has left the company's service — its enlistment failed, or
        // something took it — and the hold waited ninety seconds for it all the same: Lorandil, "9 tiles back after 90s —
        // holding nothing (), journey idle", at 10:23:32 on build 384, while the company stood at the Orc Caves' first landing.
        // Such a member is struck off at once and the march goes on.
        if (straggler != null && !Serving(straggler))
        {
            Deserted++;

            logger.Information(
                "{Name} strikes {Who} off the march to {Deep}: {Behind} tiles back and holding {Deed}, not its place in the company",
                body.Name,
                straggler.Self?.Name ?? "a member",
                _deep.Name,
                behind,
                (straggler as IBotWilful)?.Resolve?.Deed?.Kind ?? "nothing"
            );

            BotSquads.Leave(straggler);
            straggler = squad.Straggler(spread, out behind);
        }

        // <b>A member on the other side of the mouth is crossing, not straggling (29.09.2026, build 313).</b> The leader
        // stepped through the mouth to its landing four thousand tiles away on the map and the rule measured that: "0
        // left of 5" in one second. A member whose ground is not joined to the leader's is on its way through the
        // gate; the leader holds at the landing for it up to MouthHoldMs.
        // Not Joined: two lands a gate connects are joined, and the mouth is exactly such a gate — on build 313 every member
        // was "joined" to a leader on the far side and left in the same second (Brokk and Keyleth, 18:36, after marching
        // ten minutes to the mouth without a single straggler). The question is whether they stand on the same land.
        if (straggler?.Self is { } crossing && OtherLand(body.Location, crossing.Location, behind))
        {
            // Turned back with the mouth between leader and member: the leader walks back through it towards the muster, and the
            // member follows it through or is met on the island side. Nobody is left for the width of the map (build 338).
            if (_returning)
            {
                _crossTick = 0;
                _holdTick = 0;

                return BotDoing.Walk(_map, _muster, BotArrival.Within(Station), $"back to the muster at ({_muster.X}, {_muster.Y}) from the way to {_deep.Name}");
            }

            if (_crossTick == 0)
            {
                _crossTick = now;
            }

            if (now - _crossTick < MouthHoldMs)
            {
                // A hold is a stop: the walk in hand is held, or the leader keeps walking while it "holds" (314). See Holding.
                return Holding(member, $"holding at the mouth of {_deep.Name} for {crossing.Name} to come through");
            }

            LeftBehind++;
            logger.Information("{Name} leaves {Who} behind at the mouth of {Deep}: it did not come through in {Minutes} minutes", body.Name, crossing.Name, _deep.Name, MouthHoldMs / 60000);
            BotSquads.Leave(straggler);
            _crossTick = 0;

            return BotDoing.Work($"at the mouth of {_deep.Name}");
        }

        _crossTick = 0;

        if (straggler != null)
        {
            if (_holdTick == 0)
            {
                _holdTick = now;
                Held++;
            }

            if ((behind <= MarchLost || BotSquad.Coming(straggler, behind)) && now - _holdTick < HoldMs)
            {
                // <b>A hold is a stop (29.09.2026, build 314).</b> Returning Work left the walk in hand running, so the leader
                // "held" at five tiles a second and the members closing up on it were sixty tiles back in seventeen seconds
                // on 313. The walk is held here, its plan kept; the next turn that is not a hold walks on. See Holding.
                return Holding(member, $"holding for {straggler.Self?.Name ?? "a member"}, {behind} tiles behind, on the way {(_returning ? "back from" : "to")} {_deep.Name}");
            }

            LeftBehind++;

            // Instrumented (29.09.2026): three marches lost every member within two minutes and the events said nothing
            // about what the members were doing. The straggler's squad, deed and journey are written here.
            var stragglerDeed = (straggler as IBotWilful)?.Resolve?.Deed;
            var stragglerJourney = straggler.Journey;
            logger.Information(
                "{Name} leaves {Who} behind on the march to {Deep}: {Behind} tiles back after {Seconds}s — in squad {Squad}, holding {Deed} ({Stage}), journey {Moving} towards {Target} ({Reason})",
                body.Name,
                straggler.Self?.Name ?? "a member",
                _deep.Name,
                behind,
                (now - _holdTick) / 1000,
                straggler.Squad?.Id.ToString() ?? "none",
                stragglerDeed?.Kind ?? "nothing",
                stragglerDeed?.Stage ?? "",
                stragglerJourney is { Moving: true } ? "moving" : stragglerJourney is { Active: true } ? "active, not moving" : "idle",
                stragglerJourney?.Target ?? Point3D.Zero,
                stragglerJourney?.Reason ?? ""
            );
            BotSquads.Leave(straggler);
            _holdTick = 0;

            // <b>What is left turns back together rather than coming apart (build 338).</b> Until now: "the party came apart on
            // the way", the company released where it stood, and every member walked home alone from the worst of the road.
            // Fewer than outnumber the dungeon's worst room is not the party it was called for (build 359): see Least.
            if (squad.Count < _least)
            {
                _judged = squad.Count;

                return TurnBack(
                    member,
                    squad,
                    body,
                    $"{squad.Count} left of the {_setOutCount} that set out once {straggler.Self?.Name ?? "a member"} was left behind",
                    _returning ? $"on the way back from {_deep.Name}" : $"on the way to {_deep.Name}"
                );
            }
        }
        else
        {
            _holdTick = 0;
        }

        // <b>A moongate on the road is stepped through by the leader itself (01.10.2026, build next).</b> From Skara Brae's muster
        // the road to the Orc Caves begins with the moongate at (643, 2067); on build 384 eight of nine calls from there reached
        // it in forty seconds and failed in the same second, three plans "no way from here (into the Orc Caves by its mouth)"
        // from the gate's own tile (Balgrim, 01:24:59): the walker had spent its route standing on a gate it was not put through.
        // Standing on the next gate of its way and that gate a moongate, the leader goes through, as the walker does for anybody
        // (BotGates.Travel); its company follows through the same gate (BotEnlist.Holding) and is held for across it (GateApart).
        if (_progressGate != Point3D.Zero && body.X == _progressGate.X && body.Y == _progressGate.Y
            && BotGates.Next(_map, body.Location, _returning ? _muster : _room, out var moon) && moon is { Moongate: true }
            && moon.From.X == body.X && moon.From.Y == body.Y && BotGates.Travel(body, moon))
        {
            MoonLed++;
            member.Journey?.Finish();
            _progressReadTick = 0;

            return BotDoing.Work($"through the moongate to ({moon.To.X}, {moon.To.Y}) on the way {(_returning ? "back from" : "to")} {_deep.Name}");
        }

        return _returning
            ? BotDoing.Walk(_map, _muster, BotArrival.Within(Station), $"back to the muster at ({_muster.X}, {_muster.Y}) from the way to {_deep.Name}")
            : BotDoing.Walk(_map, _room, BotArrival.Within(3), $"into {_deep.Name} by its mouth");
    }

    /// <summary>
    /// <b>The company answers an attack on any member as one (30.09.2026, build 338; Patrick's order of 29.09: "full mutual
    /// aid — it is a company").</b>
    ///
    /// <para>
    /// A blow on any member turns the company on what struck (<c>BotSquads.Note</c>), and until now the leader marched on
    /// through it: this errand answered the walk to the mouth on every beat and never looked at the company's stance, while
    /// the members were stationed round the thing and the straggler rule counted them falling behind. On 336 Durgan's company
    /// was on a reaper at 01:06:07 — Helja ran with "0 of ours near" and died at (1100, 998) holding her enlistment — the
    /// leader held ninety seconds for Dagna, hurt and drinking, and left her; at 01:09:58–01:10:27 the company was on a gazer,
    /// an ogre and a ratman at (1021, 1395) while Durgan went on and through the mouth at 01:10:56 "with 1". Thalion died at
    /// 01:10:28, Norbert at 01:10:33. Every sweep that came apart on 336–337 came apart the same way.
    /// </para>
    ///
    /// <para>
    /// As the Baron's harrowing has done since 16.09.2026 (<c>BotHarrow</c>): while the company is on something the leader
    /// answers Work and is stationed and pressed like everybody else; nobody is left behind while it lasts; the march clock
    /// stops for it (<see cref="MarchFightMs"/>); the march goes on when the thing is down, broken off, or given up — and the
    /// company is counted again then (<see cref="Short"/>). The walk in hand is finished once, when the fight begins, so
    /// the company's places take the leader into it.
    /// </para>
    /// </summary>
    private BotDoing StandTo(IBotSquadMember member, BotSquad squad, Mobile body, Mobile foe, long now)
    {
        if (_fightTick == 0)
        {
            _fightTick = now;
            _holdTick = 0;
            _crossTick = 0;
            Stood++;

            member.Journey?.Finish();

            logger.Information(
                "{Name} halts the march {Way} {Deep}: the company of {Count} turns on {Foe}, which set upon {Who}",
                body.Name,
                _returning ? "back from" : "to",
                _deep.Name,
                squad.Count,
                foe.Name,
                squad.Contact?.Self?.Name ?? "one of us"
            );
        }

        // Each new enemy is a new stage for the will's labour clock, as the harrowing's is.
        if (!ReferenceEquals(foe, _foe))
        {
            _foe = foe;

            if (member is IBotWilful wilful && wilful.Resolve != null)
            {
                wilful.Resolve.StirredTick = now;
            }
        }

        return BotDoing.Work($"standing with the company against {foe.Name} on the way {(_returning ? "back from" : "to")} {_deep.Name}");
    }

    /// <summary>The living members of the company on this delve's map, the leader included.</summary>
    private int Living(BotSquad squad)
    {
        var living = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is { Deleted: false, Alive: true } self && self.Map == _map)
            {
                living++;
            }
        }

        return living;
    }

    /// <summary>
    /// <b>Whether what is left of the party is still the party the dungeon was chosen for (30.09.2026, build 338).</b> Fewer
    /// than <see cref="Least"/> standing (<see cref="Fewest"/> until build 359), nobody left to hold a line, or less than <see cref="GoOnShare"/> of the strength
    /// that marched while short of the dungeon's own bar: the party turns back together. The same measure the descent was
    /// chosen by (<c>BotDelver.Worth</c>), on the members still standing.
    /// </summary>
    private bool Short(BotSquad squad, Mobile body, out string why, out double worth)
    {
        worth = 0.0;

        var living = Living(squad);

        if (living < _least)
        {
            why = $"{living} of the {_setOutCount} that set out are standing, fewer than the {_least} that outnumber the worst room of {_deep.Name}";

            return true;
        }

        if (Line(squad) < Fighters)
        {
            why = "nobody is left who could hold a line";

            return true;
        }

        // The seats for the weak are counted again on who is standing: a party that lost its strong on the road is a party
        // of the weak, whatever it was at the muster. See WeakShare.
        var weak = WeakIn(squad);
        var seats = WeakSeats(living);

        if (weak > seats)
        {
            WeakAtMouth++;

            why = $"{weak} of the {living} standing are under {_deep.Name}'s floor of {_deep.Worst * WeakFloor:F0}, more than the {seats} seats a party of {living} keeps for them";

            return true;
        }

        worth = BotDelver.Worth(squad, body, _deep);

        if (_setOut > 0.0 && worth < _setOut * GoOnShare && worth < _deep.Worst * BotDelver.Odds)
        {
            why = $"{worth:F0} of strength is left of the {_setOut:F0} that set out, {living} of {_setOutCount} standing";

            return true;
        }

        why = null;

        return false;
    }

    /// <summary>
    /// The delve is given up on the way, and the company walks back to the muster together — the ground before the danger it
    /// gathered on (<see cref="MusterBack"/>) — rather than being let go where it stands. A leader with nobody left ends it.
    /// </summary>
    private BotDoing TurnBack(IBotSquadMember member, BotSquad squad, Mobile body, string why, string where)
    {
        var living = Living(squad);

        if (living < 2)
        {
            TurnedBack++;

            logger.Information("{Name} gives up the delve into {Deep} {Where}: {Why}, and nobody is left to walk back with", body.Name, _deep.Name, where, why);

            Release(squad);
            BotSquads.Leave(member);
            _squad = null;
            _marching = false;
            _returning = false;

            return BotDoing.Failed($"nobody left to march with {where}: {why}");
        }

        if (_returning)
        {
            // Already on the way back; what is left walks on.
            return BotDoing.Work($"turning back from {_deep.Name}: {_why}");
        }

        TurnedBack++;

        _returning = true;
        _why = why;
        _marchTick = Core.TickCount;
        _progressTick = 0;
        _progressLeast = int.MaxValue;
        _progressGate = Point3D.Zero;
        _keptMarching = false;
        _fought = 0;
        _fightTick = 0;
        _holdTick = 0;
        _crossTick = 0;

        member.Journey?.Finish();

        logger.Information(
            "{Name} turns the party for {Deep} back {Where}: {Why}; the {Living} of them walk back to the muster at ({X}, {Y}) together",
            body.Name,
            _deep.Name,
            where,
            why,
            living,
            _muster.X,
            _muster.Y
        );

        return BotDoing.Work($"turning back from {_deep.Name}: {why}");
    }

    /// <summary>The company that turned back is at the muster: the delve is over and the company let go there, together.</summary>
    private BotDoing Home(IBotSquadMember member, BotSquad squad, Mobile body)
    {
        BroughtBack++;

        var living = Living(squad);

        logger.Information(
            "{Name} has brought {Living} back to the muster at ({X}, {Y}) from the way to {Deep}: {Why}",
            body.Name,
            living,
            _muster.X,
            _muster.Y,
            _deep.Name,
            _why
        );

        Release(squad);
        BotSquads.Leave(member);
        _squad = null;
        _marching = false;
        _returning = false;

        return BotDoing.Failed($"turned back from {_deep.Name} and brought {living} back to the muster: {_why}");
    }

    /// <summary>The leader is through the mouth: the party is founded on whoever is already down, and the clock starts.</summary>
    private BotDoing Arrived(BotSquad squad, Mobile body)
    {
        var now = Core.TickCount;

        _party = new BotDelveParty(_deep, body as BotMobile);
        _marching = false;
        _down = true;
        _bledAt = squad.Bled;
        squad.OnTheMarch = false;
        _began = now;
        _roomTick = now;
        _deep.Runs++;
        Marches++;
        Heads += Living(squad);

        if (Quarantined(_map, _deep))
        {
            UnderQuarantine++;
        }

        Adopt(squad);

        // "Led 1" read as a party of one (10:23:41 on build 384, Thrain's twelve): the leader steps through first, and only who is
        // already down is counted here. The company and who is down are said apart (01.10.2026, build next).
        logger.Information(
            "{Name} has led its company of {Living} into {Deep} by its mouth ({Count} down so far) after {Minutes:F1} minutes' march: {Quota} of them to kill or {Cap} minutes, {Raisings} raisings between them",
            body.Name,
            Living(squad),
            _deep.Name,
            _count,
            (now - _marchTick) / 60000.0,
            Quota,
            CapMs / 60000,
            _party.Left
        );

        return BotDoing.Work($"in {_deep.Name}");
    }

    /// <summary>Every member of the company that is down the dungeon and not yet in the party joins it. Asked every beat below: the stragglers come through the mouth one by one.</summary>
    private void Adopt(BotSquad squad)
    {
        if (_party == null || squad == null)
        {
            return;
        }

        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is BotMobile { Deleted: false, Alive: true } who && _deep.Holds(who.Location) && !_party.Holds(who))
            {
                _party.Add(who);
            }
        }

        _count = _party.Down;
    }

    /// <summary>
    /// The way out is the way in. The pot has been handed out (<see cref="BotDelveParty.Settle(string, bool)"/>); the leader
    /// walks to the muster and the journey takes it through the mouth; the company follows in formation; whoever is still
    /// below <see cref="BotDelveParty.LeaveMs"/> later is lifted by the watchdog.
    /// </summary>
    private BotDoing Leaving(IBotSquadMember member, Mobile body)
    {
        var now = Core.TickCount;
        var pot = _party?.Pot ?? 0;

        // Lifted out after the walk was refused (work17): the delve ends here, wherever the leader stands. See Bend.
        if (_lifted)
        {
            Release(_squad);

            return BotDoing.Done($"{_why} — {_count} of us, {pot}gp between them; the way out by the mouth was refused and the party was lifted out");
        }

        if (!_deep.Holds(body.Location))
        {
            WalkedOut++;
            Release(_squad);

            return BotDoing.Done($"{_why} — {_count} of us, {pot}gp between them; out by the mouth");
        }

        if (now - _leaveTick >= LeaveMs)
        {
            LeftBelow++;
            Release(_squad);

            return BotDoing.Done($"{_why} — {_count} of us, {pot}gp between them; the leader could not reach the mouth in {LeaveMs / 60000} minutes");
        }

        return BotDoing.Walk(_map, _muster, BotArrival.Within(Station), $"out of {_deep.Name} by its mouth");
    }

    /// <summary>Standing where the band can see you, and taking whoever falls in.</summary>
    private BotDoing Calling(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad ?? BotSquads.Form(member);

        if (squad == null)
        {
            return BotDoing.Failed("could not call a party together");
        }

        // Re-adopted every beat rather than remembered once: a squad is an object and the one this errand
        // started with can be thrown away underneath it. The harrowing paid for that lesson over a whole day.
        _squad = squad;
        squad.Ceiling = _want;
        squad.Charged = true;

        var now = Core.TickCount;

        if (!_calling)
        {
            _calling = true;

            // Worked out when the delve was offered, if it was (BotDelver asks how many the muster can raise); else now.
            if (_planned != Point3D.Zero)
            {
                _muster = _planned;

                if (_plannedForward)
                {
                    MusteredForward++;
                    logger.Information("{Name}'s muster for {Deep} is set forward to ({X}, {Y})", body.Name, _deep.Name, _muster.X, _muster.Y);
                }
            }
            else
            {
                _muster = Muster(body);
            }

            Calls++;

            _caller = body;
            _calledTick = now;
            _callsNow.Add(this);

            logger.Information(
                "{Name} is calling a party of {Want} at ({X}, {Y}) for a delve into {Deep}, where the worst of them is worth {Worst} and the worst room holds {Room}; it goes down with {Least} at the fewest",
                body.Name,
                _want,
                _muster.X,
                _muster.Y,
                _deep.Name,
                _deep.Worst.ToString("F0"),
                RoomOf(_map, _deep),
                _least
            );
        }

        // <b>The levy begins with the call, not with the leader's arrival (01.10.2026).</b> The bots round the muster were called
        // only once the leader stood on it, and a muster is set forward on the road to the mouth: Sigra took the call at 22:52:32
        // on 378, walked eight minutes to her muster at (689, 1120), and found "10 in a company already" — "only 2 of the 12
        // stood up … of 2 called". Of 104 delves that ended on 30.09 none went down: 43 "only N of the 12 stood up", 18 "nobody
        // who could hold a line answered". Levied now, the called walk to the muster with their leader (BotEnlist.Holding) and are
        // nobody else's meanwhile; the three minutes of the call still start when the leader stands on the muster.
        var walking = !body.InRange(_muster, Station);

        if (now - _leviedTick >= (walking ? Math.Max(LevyMs, LevyWalkingMs) : LevyMs))
        {
            _leviedTick = now;

            if (walking)
            {
                LeviedEarly++;
            }

            Levy(squad, body);
        }

        if (walking)
        {
            // On the road to the muster the called company seeks no fight of its own (what sets upon it is still answered): the
            // delve is what it was raised for, as on the march (build 360). See BotSquad.OnTheMarch.
            squad.OnTheMarch = true;

            return BotDoing.Walk(_map, _muster, BotArrival.Within(Station), $"to the muster at ({_muster.X}, {_muster.Y})");
        }

        if (!_atMuster)
        {
            _atMuster = true;
            _musteredTick = now;
            squad.OnTheMarch = false;
        }

        _count = squad.Count;
        _here = Gathered(squad);

        // <b>The line is counted where the party stands, not in the company's list (30.09.2026, build 359).</b> Amberle's call
        // on 356 had one line-holder, Edmund, levied from a hundred tiles off at 11:24:53 and two hundred off when the call ran
        // out at 11:27:54, walking away from it; the call counted him, went down "4 strong, short of 5", let him go at
        // the descent for not standing at the muster ("company 6 went on without it") and marched with no line — to be turned
        // back at its first count on the road, "nobody is left who could hold a line".
        var line = LineHere(squad);

        if (_here != _said0 || _count != _said1)
        {
            _said0 = _here;
            _said1 = _count;

            logger.Information(
                "{Name}'s party for {Deep}: {Here} of {Want} standing in it, {Called} in the company, {Line} of them able to hold a line at the muster, {Left}s left",
                body.Name,
                _deep.Name,
                _here,
                _want,
                _count,
                line,
                Math.Max(0, (MusterMs - (now - _musteredTick)) / 1000)
            );
        }

        if ((_here < _want || line < Fighters) && now - _musteredTick < MusterMs)
        {
            // Waiting is not walking: a walk to the tile the bot is standing on never shortens, and the
            // jam detector then cancels the errand. Said plainly by the harrowing, which lost three musters
            // and eighteen hundred called bots to exactly this before it was written down.
            return _here >= _want
                ? BotDoing.Work($"calling for a fighter at ({_muster.X}, {_muster.Y})")
                : BotDoing.Work($"calling at ({_muster.X}, {_muster.Y}): {_here} of {_want} gathered");
        }

        // The call has run out, or everybody stands at the muster with a line among them.
        if (line < Fighters)
        {
            // Counted and named rather than folded into "undermanned": a party that could not raise a
            // fighter and a party that could not raise five bodies are different evenings, and a nought
            // with no name is the failure this shard has paid for most often.
            var away = Line(squad) > line;

            Lineless++;

            if (away)
            {
                LineAway++;
            }

            Rest(body);
            BotSquads.Leave(member);
            _squad = null;

            return BotDoing.Failed(
                away
                    ? $"nobody who could hold a line stood at the muster for {_deep.Name}: the one called did not come in {MusterMs / 60000} minutes"
                    : $"nobody who could hold a line answered the call for {_deep.Name}"
            );
        }

        // Fewer than outnumber the worst room is not a party for that dungeon. See Least.
        if (_here < _least)
        {
            Undermanned++;
            Rest(body);
            BotSquads.Leave(member);
            _squad = null;

            return BotDoing.Failed(
                $"only {_here} of the {_want} stood up for {_deep.Name} in {MusterMs / 60000} minutes, of {_count} called, fewer than the {_least} that outnumber its worst room; {PassedOver()}"
            );
        }

        // The call has run out: Least with a line among them is a party. See Fewest (29.09.2026) and Least.
        if (_here < _want)
        {
            Undersized++;

            logger.Information("{Name}'s party for {Deep} goes down {Here} strong, short of {Want}: the call ran out", body.Name, _deep.Name, _here, _want);
        }

        return Descend(member, squad, body);
    }

    /// <summary>
    /// The way down: everybody at once, into the same room, and the party opened.
    ///
    /// <para>
    /// <b>Everybody at once is the whole of it.</b> A squad whose leader is moved and whose followers are
    /// not is four bots trying to walk to a place with no road, for as long as the errand lasts. So the
    /// descent moves the bodies itself, and the squad's ceiling is closed behind them so that nobody on the
    /// island can be added to a company that is no longer on the island.
    /// </para>
    /// </summary>
    private BotDoing Descend(IBotSquadMember leader, BotSquad squad, Mobile body)
    {
        // Whoever is not standing at the muster does not march: a name in the company sixty tiles off is a straggler
        // from the first step. See Gathered and the hold in Marching.
        for (var i = squad.Members.Count - 1; i >= 0; i--)
        {
            var member = squad.Members[i];

            if (ReferenceEquals(member, squad.Leader) || member?.Self is not { } self)
            {
                continue;
            }

            if (self is not { Deleted: false, Alive: true } || self.Map != _map || !self.InRange(_muster, Assembly))
            {
                BotSquads.Leave(member);
                Ungathered++;
            }
        }

        // The party that stood up, reckoned again against where it was called for. See BotDelver.Recheck.
        var planned = _deep;

        _deep = BotDelver.Recheck(squad, body, _deep, out var strength, out var shortfall) ?? planned;

        if (_deep != planned)
        {
            logger.Information(
                "{Name}'s party for {Planned} is {Strength} of proven strength against its worst ({Why}), so it goes down {Deep} instead",
                body.Name,
                planned.Name,
                strength.ToString("F0"),
                shortfall ?? $"{planned.Worst * BotDelver.Odds:F0} wanted",
                _deep.Name
            );

            Size();
        }

        // <b>The weak beyond their seats are let go here, on the party that actually stood up (12.10.2026, build 434).</b> The levy
        // keeps the seats of the party it called (WeakSeats of _want); a call that ran out short has fewer seats, and a party sent
        // elsewhere is judged against that dungeon's floor. The last levied go first: the levy put the leader's guild and then the
        // nearest at the front, so the last are the strangers from furthest off. See WeakShare.
        var stood = squad.Count;
        var weak = WeakIn(squad);

        // The seats of the party that stood, counted once: read again on the shrinking count, 8 with 3 weak trimmed to 6 and failed
        // where 7 with 2 is a party (review B, 13.10.2026).
        var seats = WeakSeats(stood);

        for (var i = squad.Members.Count - 1; i >= 0 && weak > seats; i--)
        {
            var member = squad.Members[i];

            if (ReferenceEquals(member, squad.Leader) || member?.Self is not { Deleted: false, Alive: true } self || !Weak(self, _deep))
            {
                continue;
            }

            BotSquads.Leave(member);
            Trimmed++;
            weak--;
        }

        // A party sent somewhere else is sized for there, and one too few to outnumber its worst room does not go (build 359).
        if (squad.Count < _least)
        {
            // Its own count when the floor made the difference: enough bodies stood at the muster, too few of them hold the floor.
            if (stood >= _least)
            {
                ShortOfFloor++;
            }
            else
            {
                Undermanned++;
            }

            Rest(body);
            BotSquads.Leave(leader);
            _squad = null;

            return BotDoing.Failed(
                stood >= _least
                    ? $"only {squad.Count} of the {stood} that stood at the muster for {_deep.Name} hold its floor of {_deep.Worst * WeakFloor:F0} or a seat kept for the weak, fewer than the {_least} that outnumber its worst room"
                    : $"only {squad.Count} stood at the muster for {_deep.Name}, fewer than the {_least} that outnumber its worst room"
            );
        }

        // <b>Into the largest hall of the dungeon, where there is one.</b> A dungeon is several caves that
        // share a name, and a party put down in a cave of two rooms has two rooms to fight through and no
        // road to the rest of it. See BotHalls: until it has been charted this falls back to any room at
        // all, which is what the first parties got.
        _hall = BotHalls.Largest(_deep);
        _which = Utility.Random(Math.Max(1, _hall?.Rooms.Count ?? _deep.Rooms.Count));

        _room = _hall != null
            ? BotHalls.Room(_hall, _which)
            : BotDungeon.Room(_map, _deep, _which);

        if (_room == Point3D.Zero)
        {
            return BotDoing.Failed($"{_deep.Name} has no room anybody could be put down in");
        }

        // <b>By the mouth, where the gates join the dungeon to where the party stands (29.09.2026).</b> The party is
        // founded when the leader is through; the members come through behind it. Only a dungeon no gate joins is still
        // descended by hand, below.
        if (BotGates.Reaches(_map, body.Location, _deep))
        {
            _marching = true;
            _marchTick = Core.TickCount;
            _count = squad.Count;
            squad.Ceiling = Math.Max(1, squad.Count);
            squad.Quarry = Prey;
            Walked++;

            // What marches, and where it will be counted before it goes in (build 338). See Short and MouthTiles.
            squad.OnTheMarch = true;
            _setOut = BotDelver.Worth(squad, body, _deep);
            _setOutCount = squad.Count;
            _judged = squad.Count;
            _mouth = BotGates.Mouth(_map, body.Location, _room)?.From ?? Point3D.Zero;
            _atMouth = false;
            _returning = false;
            _progressTick = 0;
            _progressLeast = int.MaxValue;
            _progressGate = Point3D.Zero;
            _keptMarching = false;
            _fought = 0;
            _fightTick = 0;
            _marchBends = 0;

            var mouth = BotGates.Next(_map, body.Location, _room, out var gate) ? gate.ToString() : "a mouth the graph knows";

            logger.Information(
                "{Name} leads {Count} from the muster at ({X}, {Y}) to {Deep} by {Gate}",
                body.Name,
                _count,
                _muster.X,
                _muster.Y,
                _deep.Name,
                mouth
            );

            // Runes (29.09.2026): a mage of the party holding a rune by the mouth opens a gate here instead. See BotPassage.
            _passage = gate != null ? BotPassage.Open(squad, body, _map, gate.From, $"the mouth of {_deep.Name}", false) : null;

            if (_passage != null && _passage.Lead(body, out var through))
            {
                return through;
            }

            return BotDoing.Walk(_map, _room, BotArrival.Within(3), $"into {_deep.Name} by its mouth");
        }

        Carried++;
        _party = new BotDelveParty(_deep, body as BotMobile);

        var went = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is not BotMobile { Deleted: false, Alive: true } who)
            {
                continue;
            }

            _party.Add(who);
            Put(who);
            went++;
        }

        if (went == 0)
        {
            _party.Settle("nobody could be put down there");

            return BotDoing.Failed($"nobody could be put down in {_deep.Name}");
        }

        // Closed behind them. See the note above.
        squad.Ceiling = went;
        squad.Quarry = Prey;

        _down = true;
        _bledAt = squad.Bled;
        _count = went;
        _began = Core.TickCount;
        _roomTick = _began;
        _deep.Runs++;
        Marches++;
        Heads += went;

        if (Quarantined(_map, _deep))
        {
            UnderQuarantine++;
        }

        logger.Information(
            "{Name} has taken {Count} into {Deep} at ({X}, {Y}): {Quota} of them to kill or {Minutes} minutes, {Raisings} raisings between them",
            body.Name,
            went,
            _deep.Name,
            _room.X,
            _room.Y,
            Quota,
            CapMs / 60000,
            _party.Left
        );

        return BotDoing.Work($"in {_deep.Name}");
    }

    /// <summary>
    /// One body into the room, a little apart from the others so nobody lands on anybody.
    ///
    /// <para>
    /// <b>A tile a body fits on is not the same as a tile a body can leave</b>, and underground the two come
    /// apart far more often than they do on grass — <c>CanSpawnMobile</c> is happy with a niche in a wall.
    /// So the first pass wants a way out as well as a place to stand, and only the second settles for a
    /// place to stand. The same two passes, for the same reason, as <c>BotPopulation.TryPlace</c>.
    /// </para>
    /// </summary>
    private void Put(BotMobile who)
    {
        var map = _map;

        for (var pass = 0; pass < 2; pass++)
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var x = _room.X + Utility.RandomMinMax(-2, 2);
                var y = _room.Y + Utility.RandomMinMax(-2, 2);

                if (!map.CanSpawnMobile(x, y, _room.Z - 8, _room.Z + 8, false, false, out var z))
                {
                    continue;
                }

                if (pass == 0 && BotStep.Mask(map, x, y, (sbyte)Math.Clamp(z, sbyte.MinValue, sbyte.MaxValue)).WalkMask == 0)
                {
                    continue;
                }

                who.MoveToWorld(new Point3D(x, y, z), map);
                who.Journey?.Finish();

                return;
            }
        }

        // The room itself, whatever is standing on it. A crowded tile underground is a party; a bot left on
        // the island is a party of four and a bot with an errand it cannot reach.
        who.MoveToWorld(_room, map);
        who.Journey?.Finish();
    }

    /// <summary>Running the tunnels: fight what is here, and when there is nothing here, go to the next room.</summary>
    private BotDoing Delving(IBotSquadMember member, Mobile body)
    {
        var now = Core.TickCount;

        if (now - _sweptTick >= SweepMs)
        {
            _sweptTick = now;

            _party?.Sweep();
        }

        Count();
        Adopt(member.Squad);

        if (_kills >= Quota)
        {
            Emptied++;

            return Finish($"{_kills} of them are down in {_deep.Name}", body);
        }

        if (now - _began >= CapMs)
        {
            Timedout++;

            return Finish($"{CapMs / 60000} minutes in {_deep.Name} was enough at {_kills} down", body);
        }

        var down = _party?.Down ?? 0;

        // <b>A party that lost two inside a minute comes up (06.10.2026, company16).</b> No gate withdrew a company underground
        // (BotSquad.Beyond and Overmatched answer false there), and the Orc Caves party lost 10.5 % of its enlistments on 405-409:
        // company 11 of 407 lost nine in 22 minutes to orcish mages and bombers reckoned at 0.31 of its sum at most. The squad's
        // pull-back (BotSquad.Bleed) counts the company's deaths whatever the odds; the delve reads it and walks the party out
        // with its pot, as the quota and the clock do.
        if (member.Squad is { } bleeding && bleeding.Bled > _bledAt)
        {
            BledOut++;

            // Lifted where it stands rather than walked out through the room that just killed two: 33 of the 118 Orc Caves deaths
            // on 434 came after the bleed, on the walk out. See LiftWhenBled.
            if (LiftWhenBled)
            {
                var lifted = Finish($"the party lost {BotSquad.BleedDeaths} inside a minute in {_deep.Name} at {_kills} down; it is lifted out", null);

                // What the settle surfaced, not what the book said was down: Fell clears Down on a death, and a straggler on the walk
                // in is surfaced by where it stands (review A, 13.10.2026).
                BledLifted += _surfaced;

                return lifted;
            }

            return Finish($"the party lost {BotSquad.BleedDeaths} inside a minute in {_deep.Name} at {_kills} down; it comes up", body);
        }

        // The stragglers of a march come through the mouth one by one; the leader alone for ArriveMs is not a broken party.
        if (down < 2 && now - _began >= ArriveMs)
        {
            Broken++;

            return Finish($"there was nobody left to delve with in {_deep.Name}", body);
        }

        // <b>A leader that is no longer in the dungeon is fetched back, twice, and then the delve is over.</b>
        //
        // <para>
        // Something on the island can still take a body out from under a party: a death with the last
        // raising spent sends it home by design, and anything that decides a bot is stranded and puts it
        // where the population lives does the same by accident. The alternative to noticing is a leader
        // standing in Britain asking for a room in a cavern, refused, bending to the next room, refused,
        // until the bends run out — which reads in the log as walls and is nothing of the kind.
        // </para>
        //
        // <para>
        // But ending on the first one throws away four other bots' twenty minutes over one body in the
        // wrong place, and the body is the cheap thing to fix: the descent already knows how to put
        // somebody into the room. So a living leader is put back. Twice, and the count is the point — a
        // backstop with no budget becomes the mechanism, and this project has paid for that one twice
        // already. If it fires at all, <see cref="Strayed"/> says so and there is something left to find;
        // if it fires three times for one party, the party is not being unlucky, it is being pulled, and
        // ending is the honest answer.
        // </para>
        if (!_deep.Holds(body.Location))
        {
            if (body.Alive && _strays < MaxStrays)
            {
                _strays++;
                Strayed++;

                logger.Information(
                    "{Name} was leading a delve into {Deep} and was found at ({X}, {Y}); it has been put back with its party",
                    body.Name,
                    _deep.Name,
                    body.Location.X,
                    body.Location.Y
                );

                // Back by the mouth where there is one; by hand where there is not.
                if (BotGates.Reaches(_map, body.Location, _deep))
                {
                    return BotDoing.Walk(_map, _room, BotArrival.Within(3), $"back into {_deep.Name} by its mouth");
                }

                if (body is BotMobile lead)
                {
                    Put(lead);
                }

                return BotDoing.Work($"back in {_deep.Name}: {_kills} of {Quota} down");
            }

            Leaderless++;

            return Finish($"whoever was leading it was no longer in {_deep.Name}");
        }

        _count = down;

        // The next room, when this one has been stood in long enough. The clock is the honest half:
        // "nothing in sight" is true for a second at a time while a company is rounding a corner, and a
        // party that moved on every such second would never fight anything.
        //
        // <b>Asked before the fight and answered with the order, which is not only tidiness.</b> The
        // decision layer fails any work that answers "standing here" without a break for LabourMs — fifteen
        // minutes, against a delve's twenty — and only a *changed* order pushes that clock forward. A party
        // with something in front of it every beat would otherwise be cancelled at fifteen minutes with
        // "nothing has come of this", which is the opposite of what was happening. One new order a minute
        // is enough, and it costs nothing: the next beat answers Work again while the fight lasts.
        if (now - _roomTick >= RoomMs)
        {
            _roomTick = now;
            _room = Next(body, ++_which);

            if (_room == Point3D.Zero)
            {
                return Finish($"{_deep.Name} ran out of rooms", body);
            }

            return BotDoing.Walk(_map, _room, BotArrival.Within(3), $"on to ({_room.X}, {_room.Y}) in {_deep.Name}");
        }

        if (_room == Point3D.Zero)
        {
            return Finish($"{_deep.Name} ran out of rooms", body);
        }

        // Standing still to fight rather than walking, whenever there is something to fight: a walk order
        // renewed every beat throws the plan away, and a party trying to reach a room while the line is
        // being hit is a party that does neither.
        if (Prey(body) != null)
        {
            return BotDoing.Work($"fighting in {_deep.Name}: {_kills} of {Quota} down");
        }

        return body.InRange(_room, 3)
            ? BotDoing.Work($"in {_deep.Name}: {_kills} of {Quota} down")
            : BotDoing.Walk(_map, _room, BotArrival.Within(3), $"on to ({_room.X}, {_room.Y}) in {_deep.Name}");
    }

    /// <summary>
    /// The next room to make for: one out of the hall the party is actually standing in.
    ///
    /// <para>
    /// <b>This is the whole of what the chart buys.</b> Before it, the next room was any room in the
    /// dungeon, so most of them had rock between the party and the destination — the planner answered "no
    /// way from here", which is true, and the delve spent its afternoon being refused. A room out of the
    /// party's own hall has been proved walkable from where the party is, by a real search, once.
    /// </para>
    ///
    /// <para>
    /// Falls back to any room while a dungeon is uncharted. A chart being built is not a reason to stop
    /// delving; it is a reason for the delving to get better as it fills in.
    /// </para>
    /// </summary>
    private Point3D Next(Mobile body, int which)
    {
        var hall = _hall;

        if (body != null && BotHalls.Ready(_deep))
        {
            // Asked again rather than held: a party can be moved between halls by anything that teleports —
            // a raising, a rescue — and the hall it set out in is then not the hall it is in.
            var standing = BotHalls.Holding(_deep, body.Location);

            if (standing != null)
            {
                hall = _hall = standing;
            }
        }

        return hall != null ? BotHalls.Room(hall, which) : BotDungeon.Room(_map, _deep, which);
    }

    /// <summary>Watches what the company is fighting, and counts it when it stops being there.</summary>
    private void Count()
    {
        if (_quarry != null && (_quarry.Deleted || !_quarry.Alive))
        {
            _kills++;
            Killed++;
            _quarry = null;
        }

        var focus = _squad?.Focus;

        if (focus is { Deleted: false, Alive: true } && !ReferenceEquals(focus, _quarry))
        {
            _quarry = focus;
        }
    }

    private BotDoing Finish(string why) => Finish(why, null);

    /// <summary>
    /// The delve is over. With a living leader and a mouth the gates know, the pot is handed out and the party walks out by
    /// it (<see cref="Leaving"/>); otherwise everybody is lifted out as before.
    /// </summary>
    private BotDoing Finish(string why, Mobile body)
    {
        var pot = _party?.Pot ?? 0;

        if (_down && body is { Deleted: false, Alive: true } && _deep.Holds(body.Location) && BotGates.Joined(_map, body.Location, _muster))
        {
            _leaving = true;
            _leaveTick = Core.TickCount;
            _why = why;
            _party?.Settle(why, lift: false);

            return BotDoing.Work($"leaving {_deep.Name} by its mouth: {why}");
        }

        _surfaced = _party?.Settle(why) ?? 0;
        Release(_squad);

        return BotDoing.Done($"{why} — {_count} of us, {pot}gp between them");
    }

    /// <summary>How many bodies the last lifting settle surfaced. See <see cref="BledLifted"/>.</summary>
    private int _surfaced;

    /// <summary>Parties that went down short of what they called, with enough to outnumber the worst room and a line. See <see cref="Least"/>.</summary>
    public static long Undersized { get; private set; }

    /// <summary>
    /// Where the party forms up: the guild's own seat — its hall's door, or where it settled — and the population's home
    /// only for a leader without one.
    ///
    /// <para>
    /// <b>The home ground is the graveyard's edge, and the muster stood there for three minutes (29.09.2026).</b> Ten of
    /// the delves that ended "died" between 23:03 and 00:22 on 28–29.09 ended with the leader killed at the muster, by
    /// orc bombers and orcish mages that walk that ground; none of them died in the dungeon. A guild's seat is where its
    /// members are, and since the guilds settle by other towns it is also where their walk to the mouth starts from.
    /// </para>
    /// </summary>
    /// <summary>The last point of the road from the seat to the mouth that stands <see cref="MusterBack"/> before the first bloodied or barred one; else the seat. See <see cref="MusterBack"/>.</summary>
    private Point3D Muster(Mobile body, bool quiet = false)
    {
        var seat = Rally(body);

        if (MusterBack <= 0 || _deep == null || _map == null || _map == Map.Internal)
        {
            return seat;
        }

        var land = BotGates.LandOf(_map, seat);
        var gate = land < 0 ? null : BotGates.First(land, seat, _deep);

        if (gate == null)
        {
            return seat;
        }

        List<Point3D> points = [];

        if (NavigationService.Route(_map, seat, gate.From, points) is not (NavStatus.Ok or NavStatus.Direct) || points.Count < 2)
        {
            return seat;
        }

        var end = points.Count - 1;

        for (var i = 0; i < points.Count; i++)
        {
            if (BotQuad.Muscle(_map, points[i]) > 0.0 || BotBarred.Barred(_map, points[i]))
            {
                end = i;

                break;
            }
        }

        var anchor = points[end];
        var pick = -1;

        for (var i = end - 1; i >= 0; i--)
        {
            if (Math.Max(Math.Abs(points[i].X - anchor.X), Math.Abs(points[i].Y - anchor.Y)) >= MusterBack)
            {
                pick = i;

                break;
            }
        }

        if (pick <= 0)
        {
            return seat;
        }

        var at = points[pick];

        if (!BotStep.Settle(_map, at.X, at.Y, out var z))
        {
            return seat;
        }

        if (quiet)
        {
            _plannedForward = true;

            return new Point3D(at.X, at.Y, z);
        }

        MusteredForward++;
        logger.Information(
            "{Name}'s muster for {Deep} is set forward to ({X}, {Y}), {Back} tiles before {What} at ({AX}, {AY})",
            body?.Name ?? "a leader",
            _deep.Name,
            at.X,
            at.Y,
            MusterBack,
            end == points.Count - 1 ? "the mouth" : "the first bloodied ground on the road",
            anchor.X,
            anchor.Y
        );

        return new Point3D(at.X, at.Y, z);
    }

    /// <summary>
    /// <b>Where this call would muster, worked out before it is offered (01.10.2026).</b> The proposer counted the bodies it could
    /// raise round the leader, and the muster is the guild's seat set forward on the road to the mouth — two hundred tiles and
    /// more from where the leader stood: Sigra's offer at (694, 904) on 378 counted enough, and her muster at (689, 1120), eight
    /// minutes' walk off, found "10 in a company already, 1 without supplies" and nobody else. Kept for the call, so the road is
    /// searched once.
    /// </summary>
    internal Point3D Plan(Mobile body)
    {
        if (_planned == Point3D.Zero)
        {
            _planned = Muster(body, quiet: true);
        }

        return _planned;
    }

    private Point3D _planned;

    private bool _plannedForward;

    private Point3D Rally(Mobile body)
    {
        // Where the leader lives (29.09.2026): its guild's hall or seat, or its town. See BotPopulation.HomeOf.
        var where = BotPopulation.HomeOf(body);

        if (where == Point3D.Zero)
        {
            where = BotPopulation.Where;
        }

        // <b>Never on killing ground, and never simply where the leader stands (01.10.2026, build 382).</b> When the home's
        // tile had no floor the muster fell back to the leader's own place: Verity of The Blade called at (819, 1324) at 23:57
        // on 381, lich ground, 381's levy pulled eight bots there within four seconds and six of them died to liches inside a
        // minute (the Fable review of 381). A floor is looked for round the home, then round the population's home, and a
        // place the danger map or the zones call dangerous is passed over for the next.
        if (Standing(where, out var home) && SafeMuster(home))
        {
            return home;
        }

        MusterMoved++;

        if (Standing(BotPopulation.Where, out var town))
        {
            return town;
        }

        return body?.Location ?? where;
    }

    /// <summary>Musters moved off a home that had no floor or lay on dangerous ground. See <see cref="Rally"/>.</summary>
    public static long MusterMoved { get; private set; }

    /// <summary>A standing tile within four of <paramref name="near"/>, nearest ring first.</summary>
    private bool Standing(Point3D near, out Point3D at)
    {
        at = Point3D.Zero;

        if (near == Point3D.Zero || _map == null)
        {
            return false;
        }

        for (var r = 0; r <= 4; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    if (BotStep.Settle(_map, near.X + dx, near.Y + dy, out var z))
                    {
                        at = new Point3D(near.X + dx, near.Y + dy, z);

                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Whether a muster may stand here: no blood on the square, and no hostile or deadly zone. See <see cref="Rally"/>.</summary>
    private bool SafeMuster(Point3D at) =>
        BotQuad.Muscle(_map, at) <= 0.0
        && BotZones.LevelAt(_map, at.X, at.Y) is not (BotZoneLevel.Hostile or BotZoneLevel.Deadly);

    /// <summary>
    /// Calls up the nearest bodies that can fight — this guild's own first.
    ///
    /// <para>
    /// <b>The band before the island, and that is the difference between this and a harrowing.</b> A
    /// harrowing is a public duty and takes whoever is nearest; a delve is a guild's own undertaking, and a
    /// guild whose delves are made of strangers is a guild in name. Guildmates are simply put at the front
    /// of the same list, so a leader whose band is too small still gets a party rather than nothing — a
    /// rule that reads "only with my own" is a veto, and this project has paid for that one twice.
    /// </para>
    ///
    /// <para>
    /// Producers are passed over for the reason the levy gives: their work is a chain, and breaking one
    /// link idles everything downstream of it. The leader is itself a producer and is the exception, being
    /// the one who called.
    /// </para>
    /// </summary>
    /// <summary>Bots a call passed over for belonging to a guild hostile to the leader's. See <see cref="BotRegard.Hostile"/>.</summary>
    public static long Hostiles { get; private set; }

    /// <summary>
    /// <b>Who was round the muster and why they were not called (30.09.2026, build 371).</b> Every undermanned call all day read
    /// "only 6 of the 12 … of 7 called": the call could only ever find seven, and nothing said what the other bots within its
    /// reach were doing. Counted per call on each levy's last look, by the first test that turned a bot away, and printed in
    /// the failure. Instrument first: the fix follows the largest of these.
    /// </summary>
    private readonly int[] _passed = new int[10];

    private static readonly string[] _passedFor =
    [
        "in a company already", "unable to fight", "producers", "not holding a line and beyond reach", "down a dungeon",
        "answering to a company or class", "without supplies", "of a hostile guild", "under the floor with the weak seats taken", "no bot"
    ];

    private string PassedOver()
    {
        var said = "";

        for (var i = 0; i < _passed.Length - 1; i++)
        {
            if (_passed[i] > 0)
            {
                said += (said.Length == 0 ? "" : ", ") + $"{_passed[i]} {_passedFor[i]}";
            }
        }

        return said.Length == 0 ? "nobody else within reach" : $"passed over within reach: {said}";
    }

    private void Levy(BotSquad squad, Mobile body)
    {
        if (squad.Count >= _want)
        {
            return;
        }

        _called0.Clear();
        Array.Clear(_passed);

        // A company with no line looks for one further off than for the rest (build 359). See LineReach.
        var lined = Line(squad) >= Fighters;
        var reach = lined ? Reach : Math.Max(Reach, Math.Min(LineReach, BotSquads.JoinReach - Station));

        foreach (var mobile in _map.GetMobilesInRange<Mobile>(_muster, reach))
        {
            if (mobile == body || mobile is not BotMobile other)
            {
                continue;
            }

            if (other.Squad != null)
            {
                _passed[0]++;

                continue;
            }

            if (other is not IBotAlly { AbleToFight: true })
            {
                _passed[1]++;

                continue;
            }

            if (other.Class is not { } klass || klass.Role == BotRole.Producer)
            {
                _passed[2]++;

                continue;
            }

            // Beyond Reach only somebody who holds a line, and only while the company has none.
            if (klass.Role != BotRole.Melee && !other.InRange(_muster, Reach))
            {
                _passed[3]++;

                continue;
            }

            // Somebody already underground with another party is not standing here, whatever the world
            // thinks: it is in a dungeon and its body is a hundred squares away.
            if (BotDelveParty.Delving(other))
            {
                _passed[4]++;

                continue;
            }

            // Nor one that already answers to a company or a class: see BotHarrow.Levy, where a drill kept three of a
            // muster's six at the training field.
            if (other.Resolve?.Deed is { Alongside: true })
            {
                _passed[5]++;

                continue;
            }

            // Nor one without its supplies: Patrick's order of 26.09.2026, "no right to go" without them. See BotProvision.
            if (!BotProvision.Fit(other, out _))
            {
                _passed[6]++;

                continue;
            }

            // <b>Everybody goes with everybody but a hostile guild (30.09.2026, build 370).</b> The call always took any guild's
            // bots after its own; it now passes over only a guild at war with the leader's or thought of below the war line.
            // See BotRegard.Hostile.
            if (BotRegard.Hostile(body, other))
            {
                Hostiles++;
                _passed[7]++;

                continue;
            }

            _called0.Add(other);
        }

        _called0.Sort(
            (a, b) =>
            {
                var mine = Mates(body, b).CompareTo(Mates(body, a));

                return mine != 0 ? mine : Apart(a, body).CompareTo(Apart(b, body));
            }
        );

        Take(squad, BotRole.Melee, Fighters, _want, false);

        // From further off only when nobody who holds a line stands within Reach: a guildmate at 390 tiles is not called over
        // a stranger at fifty. See LineReach.
        if (!lined && Line(squad) < Fighters)
        {
            Take(squad, BotRole.Melee, Fighters - Line(squad), _want, true);
        }

        // <b>The line's seat is kept while the company has nobody in it (30.09.2026, build 359).</b> The rest were taken up to the
        // company's ceiling, and a company filled before its line-holder came had no room left for one: Rhistel's four archers
        // filled five places at 11:33–11:34 on 356, and the call stood nearly two minutes "calling for a fighter" that the
        // levy, finding the company full, no longer looked for, until the call ran out at 11:36:17.
        Take(squad, null, _want, _want - Math.Max(0, Fighters - Line(squad)), false);
    }

    private static int Mates(Mobile leader, Mobile other) => BotGuilds.Same(leader, other) ? 1 : 0;

    /// <summary>
    /// Takes up to <paramref name="most"/> of the called, of this role if one is named, while the company is under <paramref name="upTo"/>;
    /// those beyond <see cref="Reach"/> only when <paramref name="afar"/> (the line from further off, see <see cref="LineReach"/>).
    /// </summary>
    private void Take(BotSquad squad, BotRole? role, int most, int upTo, bool afar)
    {
        var taken = 0;

        for (var i = 0; i < _called0.Count && taken < most && squad.Count < upTo; i++)
        {
            var other = _called0[i];

            if (other.Squad != null || role != null && other.Class?.Role != role)
            {
                continue;
            }

            // <b>A body under the floor takes one of the seats kept for the weak, and no more than those (12.10.2026, build 434).</b>
            // The seats are those of the party this body would make (WeakSeats of Count + 1), so they grow with the company and a
            // weak body is never pressed off its errand to be let go at the descent for a seat that was never going to be there —
            // review A found the called size (12) admitting three that a party of seven then trimmed. Asked again every levy beat,
            // so a body passed over now is taken once the strong have filled the seats around it. Counted by the floor of the
            // dungeon the call is for; the descent counts once more on the party that stood up. See WeakFloor and WeakShare.
            if (Weak(other, _deep) && WeakIn(squad) >= WeakSeats(squad.Count + 1))
            {
                PassedWeak++;
                _passed[8]++;

                continue;
            }

            // Nor a carrier with another bot's goods in its pack (06.10.2026, deliver16): the party is half an hour, the walk minutes,
            // and a walk put down that long is dropped. See BotSquads.Pressable.
            if (BotSquads.Carrying(other))
            {
                BotSquads.Spare();

                continue;
            }

            // Nor a hunter at its fight (07.10.2026, work18): the fight is seconds, the party an hour. See BotSquads.Pressable.
            if (BotSquads.Fighting(other))
            {
                BotSquads.SpareFighter();

                continue;
            }

            // Nor a thief at its turn in a crowd (07.10.2026, loops18). See BotSquads.Pressable.
            if (BotSquads.Thieving(other))
            {
                BotSquads.SpareThief();

                continue;
            }

            // Those beyond Reach were called for the line alone. See LineReach.
            var far = !other.InRange(_muster, Reach);

            if (far && !afar)
            {
                continue;
            }

            if (BotSquads.Join(squad, other))
            {
                taken++;
                Called++;

                if (far)
                {
                    LinesFar++;
                }

                // <b>A levied bot falls in; it does not finish its errand first (29.09.2026).</b> Joining put the name in the
                // company and left the work in the hands: at 10:39 Harald was levied into Bardryn's party and went on
                // foraging, Mirthal went on mending Marcus, and when the party marched at 10:40 they were sixty tiles
                // off doing it. The enlistment is pressed on the bot as a duel or a summons is (BotWill.Press): the
                // errand is put down, the bot walks to the leader and holds its station from then on.
                if (BotWill.Press(other, new BotEnlist(squad, _map, _muster, true), $"called to {squad.Leader?.Self?.Name ?? "a leader"}'s party for {_deep?.Name}"))
                {
                    Pressed++;
                }
            }
        }
    }

    /// <summary>How many of the company could hold a line.</summary>
    private static int Line(BotSquad squad)
    {
        var line = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is BotMobile { Deleted: false, Alive: true } who && who.Class?.Role == BotRole.Melee)
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>How many of the company could hold a line and stand at the muster. See the note in <see cref="Calling"/>.</summary>
    private int LineHere(BotSquad squad)
    {
        var line = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is BotMobile { Deleted: false, Alive: true } who && who.Class?.Role == BotRole.Melee
                && who.Map == _map && who.InRange(_muster, Assembly))
            {
                line++;
            }
        }

        return line;
    }

    private int Gathered(BotSquad squad)
    {
        if (squad == null || _muster == Point3D.Zero)
        {
            return 0;
        }

        var here = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            var body = members[i]?.Self;

            if (body is { Deleted: false, Alive: true } && body.Map == _map && body.InRange(_muster, Assembly))
            {
                here++;
            }
        }

        return here;
    }

    private static int Apart(Mobile a, Mobile b) =>
        Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    private static readonly List<BotMobile> _called0 = [];

    /// <summary>
    /// Everything alive in the dungeon near the party that is not one of ours.
    ///
    /// Bounded by the dungeon's own box rather than by a radius: a delve is a place, and something drawn
    /// out of the next cavern is still in the dungeon. Bots need no exclusion — they are players to the
    /// engine and this only ever asks about creatures.
    /// </summary>
    private BaseCreature Prey(Mobile leader)
    {
        var map = leader?.Map;

        if (map == null || map != _map || _deep == null)
        {
            return null;
        }

        BaseCreature nearest = null;
        var closest = int.MaxValue;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(leader.Location, Sight))
        {
            if (creature is not { Deleted: false, Alive: true } or BaseVendor)
            {
                continue;
            }

            if (creature.Controlled || creature.Summoned || creature.IsDeadBondedPet)
            {
                continue;
            }

            // The same wider question as the leader's, and for the same reason: an orc standing one tile
            // north of the box is what is hitting the party, whatever the arithmetic says about it.
            if (!_deep.Holds(creature.Location))
            {
                continue;
            }

            if (creature.Region?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            // <b>The shard's own word for what a bot fights, as the harrow asks it: not innocent, and something the engine
            // lets us strike.</b> The engine alone let an innocent through — striking one is lawful to the engine and a
            // crime to it — and the company's every blow at one is refused (BotSquad.Strike): Fenna's party in the Orc
            // Caves took on "Gerry" and "Velma" 25 times in half an hour on 24.09.2026, "we are standing on it and the
            // engine refuses every blow".
            if (!BotThreat.Hostile(leader, creature))
            {
                continue;
            }

            // Nor one a company lately found it could not hurt, which is what the hopeless break-off's shun is for.
            if (BotQuarry.Shunned(creature))
            {
                continue;
            }

            var apart = Math.Max(Math.Abs(creature.X - leader.X), Math.Abs(creature.Y - leader.Y));

            if (apart >= closest)
            {
                continue;
            }

            closest = apart;
            nearest = creature;
        }

        return nearest;
    }

    /// <summary>
    /// A road inside the dungeon was refused. Somewhere else in the same dungeon, or give up.
    ///
    /// <para>
    /// <b>This is the difference between a delve and a forty-second visit.</b> Every refused road on this
    /// shard ends the errand that asked for it unless the errand says otherwise here, and underground that
    /// rule is exactly backwards: a wall between a bot and its place in the line is what a cavern is made
    /// of. Above ground the call has no second place to try and the ordinary answer stands.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        // <b>The walk out refused is the watchdog's case now, not in ten minutes (06.10.2026, work17).</b> On build 410 the leader of
        // a settled party (Frida at (5329, 1362), 12:07:10) had its walk to the muster refused 126 times in two seconds, each one
        // shrugged off below as a refused room, until the delve gave up; its company was let go in the cave, and every member's
        // next errand failed "no way from here" (company 153: 37 failures in 45 s at 12:35, 125 of the 639 bad endings on b410
        // were bots standing in the Orc Caves). The party is lifted out the way BotDelveParty.Watch lifts it LeaveMs later, and
        // the delve ends on the next beat with the leader on the island. Counted (LiftedAtRefusal).
        if (_leaving && !_lifted && _party != null)
        {
            _lifted = true;
            LiftedAtRefusal++;
            LiftedBodies += _party.Lift();

            var walker = bot?.Self;

            logger.Information(
                "{Name}'s walk out of {Deep} by its mouth was refused at ({X}, {Y}, {Z}); the party is lifted out now. The gates from there: {Chain}",
                walker?.Name ?? "a leader",
                _deep?.Name ?? "a dungeon",
                walker?.X ?? 0,
                walker?.Y ?? 0,
                walker?.Z ?? 0,
                walker == null ? "none" : BotGates.Chain(_map, walker.Location, _muster)
            );

            return true;
        }

        if (++_bends > MaxBends)
        {
            Walled++;

            return false;
        }

        // <b>On the march a refused road is not the calling's (30.09.2026, build 338).</b> The calling's answer below moved the
        // muster to the leader's tile — right for a call that cannot reach its own gathering place, wrong on the road, where
        // the muster is also the way out and the ground a turned-back company walks home to; and the second refusal ended the
        // delve. Falk on 336 at 00:16:28, twice in one second, at the landing of the passage to Buccaneer's Den. Shrugged off
        // MarchBends times (the next beat draws the road again), and said with where the leader stands, so the next one can
        // be read: its height against the floor there, its land, the gates the graph would take from there.
        if (_marching || _returning)
        {
            MarchRefused++;

            var walker = bot?.Self;

            if (walker != null)
            {
                var floor = BotStep.Ground(_map, walker.X, walker.Y, walker.Z, BotStep.GroundReach, out var under) ? under.ToString() : "none within reach";
                var goal = _returning ? _muster : _room;

                logger.Information(
                    "{Name}'s road {Way} {Deep} was refused at ({X}, {Y}, {Z}) — floor {Floor}, land {Land}; the gates from there: {Chain}",
                    walker.Name,
                    _returning ? "back from" : "to",
                    _deep.Name,
                    walker.X,
                    walker.Y,
                    walker.Z,
                    floor,
                    BotGates.LandOf(_map, walker.Location),
                    BotGates.Chain(_map, walker.Location, goal)
                );
            }

            return ++_marchBends <= MarchBends;
        }

        // <b>A call that cannot reach its own muster moves the muster.</b> The gathering place is one tile
        // taken from the middle of where the population lives, and a leader has to get within two of it; if
        // that tile is behind a wall or under a building the whole call ends on "the walk stopped closing"
        // before anybody has been asked anything. Four of five delves died that way at 22:28 on 11.09.2026
        // and the two before them survived only by having been stood on the spot already.
        //
        // Where the leader is standing is the answer, and it is not a compromise: the muster exists to be
        // a place the band can see the leader, the band is looked for within two hundred tiles of it, and
        // the leader is by definition able to reach where it already is.
        if (!_down)
        {
            var body = bot?.Self;

            if (body == null || body.Location == _muster)
            {
                return false;
            }

            Shifted++;
            _muster = body.Location;

            return true;
        }

        Bends++;

        _roomTick = Core.TickCount;
        _room = Next(bot?.Self, ++_which);

        return _room != Point3D.Zero;
    }

    /// <summary>
    /// However the errand ended, the party comes up.
    ///
    /// <b>This is the one override that must never be got wrong.</b> Everything else about a delve going
    /// wrong costs a party an afternoon; this going wrong costs five bots the rest of the session, standing
    /// in a cavern with no road to anywhere. <see cref="BotDelveParty.Watch"/> is the second net under it.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        // <b>Named apart, because one of these is the commonest way a delve ends and reads as a bug.</b> A
        // leader that is killed has its work taken off it by the decision layer inside OnDeath, before
        // anything has a chance to raise it — so a delve ends on its leader's death and the party's three
        // raisings are never spent on the one bot they could not have saved anyway. That is the rule as
        // implemented and it is worth a reader being told which of the two happened.
        var fell = bot?.Self is { Deleted: false, Alive: false };

        // <b>A party already settled and walking out, whose leader then fell, is lifted (12.10.2026, build 434).</b> Settle does nothing
        // twice, so Elaria's party, settled at 19:26:19 on 434 to walk out and leaderless twelve seconds later, stood in the room
        // that had killed her until the watchdog's ten minutes; seven of it died there. 27 members died after their leader on 434.
        if (fell && _party is { Ended: true })
        {
            LeaderlessLifted += _party.Lift();
        }

        _party?.Settle(fell ? "whoever was leading it fell" : "the errand was let go");

        // Runes: a gate half used is let go with the errand; whoever went through is on the island by the mouth. See BotPassage.
        _passage?.Abort("the delve was let go");
        _passage = null;

        Release(_squad);

        if (bot is IBotSquadMember member && member.Squad != null && ReferenceEquals(member.Squad, _squad))
        {
            BotSquads.Leave(member);
        }

        _squad = null;
        _down = false;
        _calling = false;
        _marching = false;
        _leaving = false;
        _returning = false;
    }

    private static void Release(BotSquad squad)
    {
        if (squad == null)
        {
            return;
        }

        squad.Charged = false;
        squad.Quarry = null;
        squad.Ceiling = BotSquad.MaxSize;
        squad.OnTheMarch = false;
    }

    public static string Describe() =>
        Calls == 0
            ? "no delve has been called"
            : $"{Calls} delves called and {Called} bots called up ({LeviedEarly} levies made while the leader walked to its muster; {LinesFar} of them line-holders called from beyond {Reach} tiles for a company that had none; {Hostiles} passed over for a guild hostile to the leader's; {MusterMoved} musters moved off a home with no floor or on dangerous ground), {Marches} parties went down with {Heads} bots between them ({Undersized} of them short of what they called, with enough to outnumber the worst room; {Walked} marched to a cave mouth ({MusteredForward} musters set forward on the road, {Pressed} levied bots put their errand down for it, {Ungathered} let go at the descent for not standing at the muster, {Held} holds for stragglers, {LeftBehind} left behind, {Stood} fights stood to on the road with the leader, {MarchRefused} roads refused on the march; {WentOn} counted at the mouth and went in, {TurnedBack} turned back together and {BroughtBack} of those reached the muster), {Carried} were put down by hand, {Unreached} never reached the mouth in {MarchMs / 60000} minutes ({KeptMarching} marches kept going past it for still closing on their next gate, up to {MarchMs * MarchCapTimes / 60000} minutes; {Deserted} members struck off a march for holding something other than their place in it; members across a gate read as {GateApart} tiles behind or more; {MoonLed} leaders and {BotEnlist.MoonFollowed} members put through a moongate on the road); {WalkedOut} walked out by the mouth, {LeftBelow} leaders gave up the walk out, {LiftedAtRefusal} parties lifted out at once when the walk out was refused ({LiftedBodies} bodies)); {Undermanned} could not raise "
            + $"enough to outnumber their dungeon's worst room in {MusterMs / 60000} minutes and {Lineless} could raise nobody to hold a line ({LineAway} of them with one called who never reached the muster); "
            + $"a party calls {BotProving.PartyMultiple} for each creature of its dungeon's worst room, {Company} at the fewest and {Math.Max(Company, MostParty)} at the most: {Sizes()}; "
            + $"{Emptied} came out on {Quota} corpses, {Timedout} on the {CapMs / 60000} minutes, {Broken} because too few were left, {BledOut} because the company lost {BotSquad.BleedDeaths} inside a minute ({BledLifted} bodies lifted out at once for it{(LiftWhenBled ? "" : ", off")}), {Leaderless} because whoever led them was no longer down there "
            + $"and {Walled} because the walls would not let them past ({Bends} refused roads shrugged off, {Shifted} musters moved to the caller's own tile, {Strayed} leaders fetched back out of the world; {LeaderlessLifted} bodies lifted out of a settled party whose leader fell on the walk out); "
            + $"the floor: a party of n keeps {WeakShare:P0} of its seats (n × share, rounded down) for bodies under {WeakFloor:P0} of its dungeon's worst, the leader counted and medics never, so it goes down with at least the fewest less those seats over the floor — {PassedWeak} passed over at the levy with the seats of the party they would make taken, {Trimmed} let go at the descent beyond the seats of the party that stood, {ShortOfFloor} descents not made for too few holding the floor, {WeakAtMouth} parties turned back at a count for the same; {UnderQuarantine} descents made under a standing quarantine of the dungeon (counted, not refused); "
            + $"{Killed} things killed down there; {BotHalls.Describe()}; {BotDelveParty.Describe()}";

    public static void Forget()
    {
        Calls = 0;
        Undersized = 0;
        Walked = 0;
        Carried = 0;
        Unreached = 0;
        KeptMarching = 0;
        Deserted = 0;
        MoonLed = 0;
        WalkedOut = 0;
        LeftBelow = 0;
        LiftedAtRefusal = 0;
        LiftedBodies = 0;
        Marches = 0;
        Undermanned = 0;
        LeviedEarly = 0;
        Lineless = 0;
        Called = 0;
        Killed = 0;
        Emptied = 0;
        Timedout = 0;
        Broken = 0;
        BledOut = 0;
        Bends = 0;
        Shifted = 0;
        Walled = 0;
        Leaderless = 0;
        Strayed = 0;
        Stood = 0;
        WentOn = 0;
        TurnedBack = 0;
        BroughtBack = 0;
        MarchRefused = 0;
        LinesFar = 0;
        LineAway = 0;
        Heads = 0;
        PassedWeak = 0;
        Trimmed = 0;
        ShortOfFloor = 0;
        WeakAtMouth = 0;
        BledLifted = 0;
        LeaderlessLifted = 0;
        UnderQuarantine = 0;
        _resting.Clear();
        _called0.Clear();
        _worstRooms.Clear();

        BotDelveParty.Forget();
    }
}
