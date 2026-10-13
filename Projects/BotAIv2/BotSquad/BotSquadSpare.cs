using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// <b>A company whose fight is well in hand spares a member or two to a dire cry near it (06.10.2026, react18).</b>
///
/// <para>
/// At prime time nearly everybody near a cry is in a company: on build 410 (11:44–12:35) 84 of 167 dire cries had "nobody free,
/// fit and friendly within 60 tiles", and 607 of the bots near them were members. A company came only when it was idle
/// (<c>BotCry.Brings</c>: 5 brought, 144 refused for a fight of their own). Here the company keeps its enemy and its line, and lends
/// what it can spare: the one or two members nearest the thing, who turn on it and walk to it as the blow's reflex would have them
/// (<c>BotMobile.Engage</c>: the combatant set, the walk put on top of the road), and whom the press leaves on it
/// (<see cref="Sparing"/>) until the crier is safe or dead, the thing is down, or <see cref="SpareMs"/> is up — then the press
/// turns them back and the stations walk them home. No deed, no roster change, nothing dropped: the company's own machinery.
/// </para>
///
/// <para>
/// Well in hand: the company's enemy reckoned at no more than <see cref="SpareShare"/> of what the company can bring — and of
/// what it keeps after sparing — no member under <see cref="SpareDyingShare"/>, no pull-back running, no muster under way, no
/// war. Spared: not the leader, not a medic, fit, within <see cref="SpareReach"/> of the thing, not running or raising; the
/// nearest the thing first, as many as the thing asks (<c>BotCry.Summon</c>'s need), and none when all the company could
/// spare would still be short — nobody is sent to die beside the crier.
/// </para>
///
/// <para>
/// <b>Relaxed and made honest on 12.10.2026 (w5).</b> On the night of 12.10 (23:18–03:48) 1 857 cries turned dire; 579 found
/// "nobody free, fit and friendly within 60 tiles" with 7 250 of the bots near them in a company, and of the 998 companies
/// asked to spare 66 did: 436 were refused "with a member dying" (under <c>DyingShare</c>, a half — in any fight that lasts,
/// somebody is), 368 "with a fight not in hand", 126 pulling back or mustering. The dying gate now reads the failing line
/// (<see cref="SpareDyingShare"/>): under a half a member is the medics' business, under the failing line the company's. A war
/// company's enemy is a bot, which <see cref="Reckon"/> prices at nought, so it read as well in hand and lent members to cries
/// in the middle of a war; it spares nobody now. And the spare told the stopwatch nothing, so every cry it answered read "never"
/// on the <c>Reflexes:</c> line (<c>BotReflex.Spared</c>).
/// </para>
/// </summary>
public sealed partial class BotSquad
{
    /// <summary>Whether a company lends members to a dire cry near it. A switch.</summary>
    public static bool Spares { get; set; } = true;

    /// <summary>The share of the company's strength its own enemy may come to for the fight to count as well in hand. A half.</summary>
    public static double SpareShare { get; set; } = 0.5;

    /// <summary>How far from the thing on the crier a member may be to be spared to it. Twenty-four tiles.</summary>
    public static int SpareReach { get; set; } = 24;

    /// <summary>The most members spared to one cry.</summary>
    public static int MostSpared { get; set; } = 2;

    /// <summary>How long a spared member is left on the thing before the press takes it back whatever stands.</summary>
    public static int SpareMs { get; set; } = 30000;

    /// <summary>
    /// Under this share of its health a member makes its company one that spares nobody. The failing line (<c>BotLadder.FailingFraction</c>,
    /// 0.35), not <see cref="DyingShare"/> (a half): 436 of 998 companies asked on the night of 12.10 were refused for a member
    /// under a half. See the class note.
    /// </summary>
    public static double SpareDyingShare { get; set; } = 0.35;

    /// <summary>Members spared, cries answered so, and companies asked that would not: their fight not in hand, a member dying, pulling back or mustering, nobody fit near enough.</summary>
    public static long Spared { get; private set; }

    public static long SparedTo { get; private set; }

    public static long SpareRefusedFight { get; private set; }

    public static long SpareRefusedDying { get; private set; }

    public static long SpareRefusedBleeding { get; private set; }

    public static long SpareRefusedNobody { get; private set; }

    /// <summary>
    /// Companies asked that were at war; companies whose own enemy would stop being in hand against what they kept the moment the
    /// first member went (a gate of its own since 13.10.2026, w5 review A — it borrowed <see cref="SpareRefusedFight"/> for a day);
    /// and companies whose members near, all spared, would still have been short of what the thing asks — the members already on the
    /// crier's side in the summons' reckoning (standing by it and held by nothing else) adding nothing.
    /// </summary>
    public static long SpareRefusedWar { get; private set; }

    public static long SpareRefusedKept { get; private set; }

    public static long SpareRefusedShort { get; private set; }

    /// <summary>Beats the press left a spared member on the thing.</summary>
    public static long SparingBeats { get; private set; }

    private Mobile _spareFoe;

    private Mobile _spareFor;

    private long _spareTick;

    /// <summary>The members that could be spared to one cry, nearest the thing first. One list for all companies: asked one at a time.</summary>
    private static readonly List<(IBotSquadMember Member, BotMobile Body, int OffFoe, int OffCrier)> _spareable = [];

    /// <summary>Whether this company's own fight leaves it something to spare. See the class note.</summary>
    internal bool WellInHand()
    {
        if (!Spares || Disbanded || Recovering || Mustering)
        {
            SpareRefusedBleeding++;

            return false;
        }

        if (Warring)
        {
            SpareRefusedWar++;

            return false;
        }

        if (Focus is { Deleted: false, Alive: true } foe && Reckon(foe) > SpareShare * Math.Max(1.0, Able()))
        {
            SpareRefusedFight++;

            return false;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (_members[i].Self is { Deleted: false, Alive: true, HitsMax: > 0 } body && body.Hits < body.HitsMax * SpareDyingShare)
            {
                SpareRefusedDying++;

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Sends the members nearest the thing to it, up to <see cref="MostSpared"/>, until what they bring meets <paramref name="need"/>
    /// (at least one), and none when all of them together would not; what the company keeps must still have its own enemy in hand.
    /// Returns how many went and, in <paramref name="brought"/>, the strength they add to the crier's side. Asked once a calling,
    /// after <see cref="WellInHand"/>; a company already sparing to a cry is not asked for another until that stands down.
    /// </summary>
    internal int Spare(Mobile crier, BaseCreature foe, long now, double need, out double brought)
    {
        brought = 0.0;

        if (foe is not { Deleted: false, Alive: true } || foe.Map != Map || Leader?.Self is not { } lead || crier == null)
        {
            return 0;
        }

        if (_spareFoe is { Deleted: false, Alive: true } && now - _spareTick < SpareMs)
        {
            return 0;
        }

        _spareable.Clear();

        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];

            if (ReferenceEquals(member, Leader) || BotFormation.RoleOf(member) == BotRole.Medic
                || member.Self is not BotMobile { Deleted: false, Alive: true, HitsMax: > 0 } body
                || body.Hits < body.HitsMax * BotRescuer.FitAt || body.Map != Map || Raising(body)
                || body.Resolve?.Deed is BotBolt || !body.InRange(foe.Location, SpareReach) || !body.CanBeHarmful(foe, false))
            {
                continue;
            }

            var off = Math.Max(Math.Abs(body.X - foe.X), Math.Abs(body.Y - foe.Y));
            var at = _spareable.Count;

            while (at > 0 && _spareable[at - 1].OffFoe > off)
            {
                at--;
            }

            if (at >= MostSpared)
            {
                continue;
            }

            _spareable.Insert(at, (member, body, off, Math.Max(Math.Abs(body.X - crier.X), Math.Abs(body.Y - crier.Y))));

            if (_spareable.Count > MostSpared)
            {
                _spareable.RemoveAt(_spareable.Count - 1);
            }
        }

        if (_spareable.Count == 0)
        {
            SpareRefusedNobody++;

            return 0;
        }

        // Nearest first, until the thing's need is met; never past the point where the company's own enemy stops being in hand
        // against what the company keeps. One standing by the crier and held by nothing else is already counted on its side
        // (BotThreat.OurPower with the foe); one standing by it but held by the company's own enemy is not, and adds its strength
        // (13.10.2026, w5 review A: until then every member beside the crier read as on its side whatever it fought, so the need read
        // as met by bots that would never turn and the company was refused "short").
        var enemy = Focus is { Deleted: false, Alive: true } focus ? Reckon(focus) : 0.0;
        var kept = Able();
        var count = 0;

        for (var i = 0; i < _spareable.Count; i++)
        {
            var power = BotThreat.Power(_spareable[i].Body);

            if (enemy > SpareShare * Math.Max(1.0, kept - power))
            {
                break;
            }

            kept -= power;
            count++;

            if (_spareable[i].OffCrier > BotMobile.NoticeRange || BotThreat.Held(_spareable[i].Body, foe))
            {
                brought += power;
            }

            if (brought >= need)
            {
                break;
            }
        }

        if (count == 0)
        {
            SpareRefusedKept++;

            return 0;
        }

        if (brought < need)
        {
            SpareRefusedShort++;
            brought = 0.0;

            return 0;
        }

        var sent = 0;

        for (var i = 0; i < count; i++)
        {
            sent += Send(_spareable[i].Member, foe);
        }

        if (sent > 0)
        {
            _spareFoe = foe;
            _spareFor = crier;
            _spareTick = now;
            SparedTo++;
            Spared += sent;

            // The cry is answered: the stopwatch is told, as a rescue taken or a company turning tells it. See BotReflex.
            BotReflex.Spared(crier);
        }
        else
        {
            brought = 0.0;
        }

        return sent;
    }

    private int Send(IBotSquadMember member, BaseCreature foe)
    {
        if (member.Self is not BotMobile body)
        {
            return 0;
        }

        body.Warmode = true;
        body.Combatant = foe;

        // On top of the road, as the blow's reflex puts a fight: the station beneath is kept and walked back to when this ends.
        if (!ReferenceEquals(body.Journey?.Current?.Follow, foe))
        {
            body.Journey?.Interrupt(Map, foe, BotArrival.Beside, "spare");
        }

        return 1;
    }

    /// <summary>Whether the press leaves this member on the thing it was spared to. See <see cref="Spare"/>.</summary>
    private bool Sparing(Mobile body)
    {
        if (!Spares || _spareFoe is not { Deleted: false, Alive: true } foe || !ReferenceEquals(body.Combatant, foe))
        {
            return false;
        }

        var now = Core.TickCount;

        // Over: the thing is down (above), the crier is dead or no longer crying, or the time is up. The press turns the member
        // back, and the walk it was given is shed with the rest of the interruptions when the station is set again.
        if (now - _spareTick >= SpareMs || _spareFor is not { Deleted: false, Alive: true } || !BotCry.Crying(_spareFor))
        {
            // Each spared member sheds its own walk as the press meets it; the spare itself stands until its time is up, so a
            // second one is not sent to the same cry inside it.
            (body as BotMobile)?.Journey?.Shed();

            return false;
        }

        SparingBeats++;

        return true;
    }

    /// <summary>Clears the spares' counters on a world reload. They had no reset before 12.10.2026 (w5).</summary>
    public static void ForgetSpares()
    {
        _spareable.Clear();
        Spared = 0;
        SparedTo = 0;
        SpareRefusedFight = 0;
        SpareRefusedDying = 0;
        SpareRefusedBleeding = 0;
        SpareRefusedNobody = 0;
        SpareRefusedWar = 0;
        SpareRefusedKept = 0;
        SpareRefusedShort = 0;
        SparingBeats = 0;
    }

    /// <summary>The spares, for the <c>Reflexes:</c> line.</summary>
    public static string DescribeSpares() =>
        $"{SparedTo} dire cries answered by a company sparing {Spared} members from a fight well in hand (its enemy under {SpareShare:P0} of its strength, nobody under {SpareDyingShare:P0}), {SparingBeats} beats the press left them on it; companies asked that would not: {SpareRefusedFight} with a fight not in hand, {SpareRefusedKept} whose fight would not stay in hand without the member nearest, {SpareRefusedDying} with a member under {SpareDyingShare:P0}, {SpareRefusedBleeding} pulling back or mustering, {SpareRefusedWar} at war, {SpareRefusedNobody} with nobody fit within {SpareReach} tiles of the thing, {SpareRefusedShort} with too little to spare for what the thing asks (members already on the crier's side adding nothing){(Spares ? "" : " (off)")}";
}
