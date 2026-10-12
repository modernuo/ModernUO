using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// <b>The company fights on the strength that is on the field, not on the map (06.10.2026, company16).</b>
///
/// <para>
/// Measured over 429 company deaths on builds 405–409 (`an-enlist16.md`): 89 % fell in fights priced under the company's
/// strength over 1.5 — fights the company "should" have won — and 75 % to a killer priced under the dead bot alone. The dead bot
/// had no other member within 12 tiles in 36 % of them and stood more than 40 tiles from its leader in 43 %; <see cref="Able"/>
/// summed every living member on the map, so a company of twelve spread over a hundred tiles engaged a reaper on the strength
/// of twelve and met it with two. The median death came 18 s after the engagement, 42 % inside 15 s; 35 % of deaths followed
/// another in the same company inside a minute, and only 20 companies withdrew in four builds against 233 that lost somebody:
/// companies were wiped, not withdrawn (company 364 lost seven in 67 s to a reaper reckoned at 0.3 of its sum). Deaths per
/// fight rose from 0.013 at R/A under 0.15 to 0.29 at R/A 1.0–1.5, the worst band, which held 4 % of the fights, 27 % of the
/// deaths and 10 % of the company gold. Casters killed 60 %; 42 % of deaths were to something other than the company's enemy.
/// </para>
///
/// <para>
/// Four rules, each behind a dial and counted on the Companies line:
/// <list type="number">
/// <item><b>The choice gate is stricter than the withdrawal gate.</b> A company chooses, joins a cry to, or is raised at a creature
/// only up to <see cref="ChooseOdds"/> (1.0) times what it brings (<see cref="Beyond"/>); the withdrawal stays at
/// <see cref="WithdrawOdds"/> (1.5), and the protection of a member and the enlistment into a fight already on are judged at the
/// withdrawal's odds, because they are defence, not choice.</item>
/// <item><b>A chosen fight musters at the enemy first.</b> When the company chooses a fight with less than <see cref="MusterShare"/>
/// of its strength within <see cref="FieldReach"/> of the enemy, it forms up at a rally <see cref="MusterOff"/> tiles short of it —
/// the leader and every member stationed there, nobody pressed — until that share has come up or <see cref="MusterMs"/> is out; then
/// it engages with what is on the field, or lets the creature go if what came up is not enough at <see cref="ChooseOdds"/>. A blow on
/// a member ends the muster at once.</item>
/// <item><b>A company that bleeds pulls back.</b> At the <see cref="BleedDeaths"/>th death inside <see cref="BleedMs"/> the company
/// lets its enemy go, shuns it and the killer for <see cref="WithdrawShunMs"/> (casters too: this is the company's results, not the
/// odds), stands its members' flight (<see cref="Withdrawing"/>) and holds together on its leader for <see cref="BleedRestMs"/> with
/// no fight of its own choosing. A delve's party comes up on it (<c>BotDelve</c>).</item>
/// <item><b>The thing killing a member is fought by whoever can reach it.</b> A member within <see cref="PressReach"/> of an attacker
/// that has one of ours under <see cref="DyingShare"/> for its combatant turns on that attacker instead of the company's enemy
/// (<see cref="Peel"/>), whatever the attacker's price; the company's enemy itself prefers a caster that is attacking it
/// (<see cref="CastersFirst"/>, in <see cref="Strongest"/>).</item>
/// </list>
/// </para>
/// </summary>
public sealed partial class BotSquad
{
    // ---- 1. The choice gate -----------------------------------------------------------------------------------------------

    /// <summary>
    /// How many times what a company brings a creature may be reckoned at for the company to choose it, be raised at it, or walk to a
    /// cry against it. One: the band 1.0–1.5 held 318 fights, 92 deaths (0.29 a fight, the worst band), 73 % won and 134 gold a fight
    /// on builds 405–409 — 4 % of the fights, 27 % of the in-fight deaths, 10 % of the gold. The withdrawal stays at
    /// <see cref="WithdrawOdds"/>, so a company set upon in that band fights it out; it only does not go looking for it.
    /// Nought or more than WithdrawOdds reads as WithdrawOdds (the gate of 03.10).
    /// </summary>
    public static double ChooseOdds { get; set; } = 1.0;

    /// <summary>The odds a choice is judged at: <see cref="ChooseOdds"/>, never looser than the withdrawal.</summary>
    public static double ChoiceOdds => ChooseOdds > 0.0 && ChooseOdds < WithdrawOdds ? ChooseOdds : WithdrawOdds;

    /// <summary>Creatures passed over for lying between <see cref="ChooseOdds"/> and <see cref="WithdrawOdds"/> times what the company brought: the stricter gate's own count, once a company a creature.</summary>
    public static long ChoiceRefused { get; private set; }

    /// <summary>The creature this company last refused between the two gates. See <see cref="Beyond"/>.</summary>
    private Mobile _choiceRefused;

    // ---- 2. The muster at the enemy ----------------------------------------------------------------------------------------

    /// <summary>Whether a company that chooses a fight with its strength spread out forms up short of the enemy first. A switch.</summary>
    public static bool Musters { get; set; } = true;

    /// <summary>
    /// Within this many tiles of a point a member is on the field there. Sixteen: the rally's own ring is seven tiles at most for the
    /// back ranks, and a member sixteen tiles off is in the fight in the time a creature takes to notice it. Measured on 405: the dead
    /// had no other member within 12 tiles in 36 % of deaths, exactly one in 42 %.
    /// </summary>
    public static int FieldReach { get; set; } = 16;

    /// <summary>
    /// The share of the company's whole strength (<see cref="Able"/>) that must stand within <see cref="FieldReach"/> of the rally
    /// before the muster ends early. Three quarters: the last straggler is not waited for, the one-by-one is.
    /// </summary>
    public static double MusterShare { get; set; } = 0.75;

    /// <summary>
    /// How far short of the enemy the company forms up. Fourteen: past the twelve a caster or an archer is answered at
    /// (<c>BotDefender.Reach</c>), inside a company finder's forty, so the last walk is one run together.
    /// </summary>
    public static int MusterOff { get; set; } = 14;

    /// <summary>The longest a company musters for the members behind before it engages with what is on the field. Thirty seconds, the band's own.</summary>
    public static int MusterMs { get; set; } = 30000;

    /// <summary>Whether the company is formed up short of its enemy, nobody pressed, waiting for the members behind. See <see cref="BeginMuster"/>.</summary>
    public bool Mustering { get; private set; }

    /// <summary>Where the company forms up while <see cref="Mustering"/>: the anchor of its stations.</summary>
    private Point3D _rally;

    private long _musterTick;

    /// <summary>Musters begun; ended with the share on the field; ended on the clock; ended by a blow on a member; whose enemy went; let go because what came up was not enough.</summary>
    public static long Mustered { get; private set; }

    public static long MusterGathered { get; private set; }

    public static long MusterTimed { get; private set; }

    public static long MusterStruck { get; private set; }

    public static long MusterWent { get; private set; }

    public static long MusterShort { get; private set; }

    /// <summary>Engagements chosen with the share already on the field, which needed no muster.</summary>
    public static long MusterUnneeded { get; private set; }

    /// <summary>
    /// Creatures the company stood to at <see cref="WithdrawOdds"/> on the map's sum that it would have withdrawn from on the strength
    /// within <see cref="FieldReach"/> of them: the withdrawal gate's instrument, once a company a creature. See <see cref="Overmatched"/>.
    /// </summary>
    public static long FieldOvermatched { get; private set; }

    private Mobile _fieldOvermatched;

    /// <summary>The strength of the company's members able to fight within <paramref name="reach"/> of <paramref name="at"/>, on the leader's facet.</summary>
    public double Afield(Point3D at, int reach)
    {
        var lead = Leader?.Self;

        if (lead is not { Deleted: false, Alive: true })
        {
            return 0.0;
        }

        var sum = 0.0;

        for (var i = 0; i < _members.Count; i++)
        {
            if (_members[i] is IBotAlly { AbleToFight: true } && _members[i].Self is { Deleted: false, Alive: true } self
                && self.Map == lead.Map && self.InRange(at, reach))
            {
                sum += BotThreat.Power(self);
            }
        }

        return sum;
    }

    /// <summary>Whether a chosen fight with <paramref name="focus"/> wants a muster: less than <see cref="MusterShare"/> of the company's strength is within <see cref="FieldReach"/> of it.</summary>
    private bool WantsMuster(Mobile focus)
    {
        if (!Musters || focus is not { Deleted: false, Alive: true } || Leader?.Self is not { Deleted: false, Alive: true } lead
            || lead.Map != focus.Map)
        {
            return false;
        }

        var able = Able();

        return able > 0.0 && Afield(focus.Location, FieldReach) < able * MusterShare;
    }

    /// <summary>
    /// The rally: <see cref="MusterOff"/> tiles short of the enemy on the line towards the leader; where the leader stands when it is
    /// nearer than that, or underground, where a point on a line is as likely rock as floor and the leader's own tile is floor.
    /// </summary>
    private static Point3D RallyFor(Mobile lead, Mobile focus)
    {
        var dx = lead.X - focus.X;
        var dy = lead.Y - focus.Y;
        var far = Math.Max(Math.Abs(dx), Math.Abs(dy));

        if (far <= MusterOff || BotDungeon.Under(lead.Location))
        {
            return lead.Location;
        }

        return new Point3D(focus.X + dx * MusterOff / far, focus.Y + dy * MusterOff / far, lead.Z);
    }

    /// <summary>
    /// Takes <paramref name="focus"/> as the company's enemy without engaging it: the stations are drawn round the rally, the press is
    /// stilled, and <see cref="MusterBeat"/> ends it. See the class note.
    /// </summary>
    private void BeginMuster(Mobile focus, IBotSquadMember contact)
    {
        var lead = Leader.Self;

        Tally();

        Focus = focus;
        Contact = contact ?? Leader;
        Stance = BotSquadStance.Fighting;
        Mustering = true;
        _rally = RallyFor(lead, focus);
        _musterTick = Core.TickCount;
        Attempt = 0;
        _fell = focus.Location;
        Mustered++;

        BotSquads.Hurry(this);

        logger.Information(
            "Squad {Id} of {Count} musters at ({X}, {Y}), {Off} tiles short of {What} ({Power:F0}): {Afield:F0} of the {Ours:F0} the company brings is on the field",
            Id,
            Count,
            _rally.X,
            _rally.Y,
            Math.Max(Math.Abs(_rally.X - focus.X), Math.Abs(_rally.Y - focus.Y)),
            focus.Name,
            BotThreat.Power(focus),
            Afield(_rally, FieldReach),
            Able()
        );
    }

    /// <summary>One beat of a muster: the enemy gone, the share come up, the clock out. Called from <see cref="Update"/> in place of <see cref="Judge"/>.</summary>
    private void MusterBeat()
    {
        var focus = Focus;

        if (focus is not { Deleted: false, Alive: true } || focus.Map != Map)
        {
            MusterWent++;
            Mustering = false;
            Disengage("it went before we had come up to it");

            return;
        }

        var able = Able();
        var afield = Afield(_rally, FieldReach);

        if (able > 0.0 && afield >= able * MusterShare)
        {
            MusterGathered++;
            EndMuster("formed up");

            return;
        }

        if (Core.TickCount - _musterTick < MusterMs)
        {
            return;
        }

        // The clock is out: the company engages with what is on the field, or not at all. Judged at the choice odds on the field,
        // never looser than the gate the creature was chosen through; the creature is left alone by companies no stronger for the
        // withdrawal's shun, so the finder does not hand it straight back.
        var threat = Reckon(focus);

        if (threat > 0.0 && threat > afield * ChoiceOdds)
        {
            MusterShort++;
            Mustering = false;

            if (_shunned.Count >= 1024)
            {
                _shunned.Clear();
            }

            _shunned[focus.Serial] = (Core.TickCount + WithdrawShunMs, able, Core.TickCount);

            // And left alone by the finders for the plain shun, as a band's short levy leaves its quarry: the company's own finder
            // would otherwise hand it straight back every three seconds for two minutes and offer nothing smaller.
            BotQuarry.Shun(focus, BotQuarry.ShunMs);

            logger.Information(
                "Squad {Id} of {Count} lets {What} go: {Afield:F0} of the {Ours:F0} it brings came up in {Seconds}s, and it is reckoned {Threat:F0}",
                Id,
                Count,
                focus.Name,
                afield,
                able,
                MusterMs / 1000,
                threat
            );

            Disengage("not enough of us came up to it");

            return;
        }

        MusterTimed++;
        EndMuster("the clock ran out");
    }

    /// <summary>
    /// A blow on a member while the company musters: the muster is over and the company engages now. From <c>BotSquads.Note</c> and
    /// <c>Coming</c>. Only for a striker within <see cref="AnswerReach"/> of the leader: a levied member set upon on its way in fights
    /// for itself, as it does when the company is not mustering.
    /// </summary>
    internal void Struck(Mobile attacker)
    {
        if (!Mustering || attacker is not { Deleted: false } || Leader?.Self is not { Deleted: false } lead || attacker.Map != lead.Map
            || !lead.InRange(attacker.Location, AnswerReach))
        {
            return;
        }

        MusterStruck++;
        EndMuster("set upon");
    }

    /// <summary>The muster is over: the company engages its enemy from where it stands. See <see cref="Begin"/>.</summary>
    private void EndMuster(string why)
    {
        var focus = Focus;

        Mustering = false;

        if (focus is not { Deleted: false, Alive: true })
        {
            Disengage("it went before we had come up to it");

            return;
        }

        logger.Information(
            "Squad {Id} of {Count} ends its muster against {What} ({Why}) with {Afield:F0} of {Ours:F0} on the field after {Seconds:F0}s",
            Id,
            Count,
            focus.Name,
            why,
            Afield(_rally, FieldReach),
            Able(),
            (Core.TickCount - _musterTick) / 1000.0
        );

        Begin(focus, Contact);
    }

    // ---- 3. Withdrawing on results -----------------------------------------------------------------------------------------

    /// <summary>Whether a company that loses <see cref="BleedDeaths"/> members inside <see cref="BleedMs"/> pulls back. A switch.</summary>
    public static bool Bleeds { get; set; } = true;

    /// <summary>The deaths inside <see cref="BleedMs"/> that make a company pull back. Two: the first a cry can answer; the second says the fight is lost.</summary>
    public static int BleedDeaths { get; set; } = 2;

    /// <summary>The window the deaths are counted in. A minute: 150 of 429 deaths on 405–409 followed another in the same company inside it.</summary>
    public static int BleedMs { get; set; } = 60000;

    /// <summary>
    /// How long a company that pulled back holds on its leader with no fight of its own choosing: the members close up
    /// (<c>BotEnlist.Holding</c>), the medics mend (<see cref="Tend"/>). Forty-five seconds: a bandage and a walk, not the evening.
    /// </summary>
    public static int BleedRestMs { get; set; } = 45000;

    /// <summary>When each of the last few members was lost, for <see cref="BleedDeaths"/>. Short by construction.</summary>
    private readonly List<long> _lost = [];

    /// <summary>Whether the company is holding on its leader after a pull-back. See <see cref="Bleed"/>.</summary>
    public bool Recovering => Core.TickCount - _recoverUntil < 0;

    private long _recoverUntil;

    /// <summary>The creature the company pulled back from, and what killed the member that decided it; refused by <see cref="Bleeding"/> for <see cref="WithdrawShunMs"/>.</summary>
    private Mobile _bledFrom;

    private Mobile _bledBy;

    private long _bledUntil;

    /// <summary>How many times this company has pulled back. Read by the delve, which comes up on the first.</summary>
    public int Bled { get; private set; }

    /// <summary>Pull-backs; by the leader's errand; engagements refused for being the creature the company pulled back from; members within the thing's reach at the pull-back.</summary>
    public static long Bleedings { get; private set; }

    public static readonly Dictionary<string, long> BledBy = [];

    public static long BledRefused { get; private set; }

    public static long BledNear { get; private set; }

    /// <summary>Whether this is a creature the company pulled back from lately. Asked by <see cref="Engage"/>, the one door into a fight.</summary>
    public bool Bleeding(Mobile foe) =>
        foe != null && Core.TickCount - _bledUntil < 0 && (ReferenceEquals(foe, _bledFrom) || ReferenceEquals(foe, _bledBy));

    /// <summary>A member was lost: counted, and the company pulls back at the <see cref="BleedDeaths"/>th inside <see cref="BleedMs"/>. From <see cref="Fell"/>.</summary>
    private void Lost(Mobile killer)
    {
        var now = Core.TickCount;

        _lost.Add(now);

        for (var i = _lost.Count - 1; i >= 0; i--)
        {
            if (now - _lost[i] > BleedMs)
            {
                _lost.RemoveAt(i);
            }
        }

        if (!Bleeds || Warring || _lost.Count < BleedDeaths)
        {
            return;
        }

        _lost.Clear();
        Bleed(killer);
    }

    /// <summary>
    /// The company pulls back: its enemy is let go and shunned with the killer, every member within the thing's reach is offered its
    /// flight (<see cref="Withdrawing"/>), and the company holds on its leader for <see cref="BleedRestMs"/>. The company does not end:
    /// the errand that raised it goes on, and a band whose quarry this was fails honestly (<c>BotBand</c>).
    /// </summary>
    private void Bleed(Mobile killer)
    {
        var now = Core.TickCount;
        var focus = Focus;
        var from = focus is { Deleted: false, Alive: true } ? focus : killer is { Deleted: false, Alive: true } ? killer : null;
        var by = (Leader as IBotWilful)?.Resolve?.Deed?.Kind ?? "nothing";

        Bled++;
        Bleedings++;
        BledBy[by] = BledBy.TryGetValue(by, out var had) ? had + 1 : 1;

        _bledFrom = focus;
        _bledBy = killer is BaseCreature ? killer : null;
        _bledUntil = now + WithdrawShunMs;
        _recoverUntil = now + BleedRestMs;

        var ours = Able();

        if (_shunned.Count >= 1024)
        {
            _shunned.Clear();
        }

        if (focus is { Deleted: false, Alive: true })
        {
            _shunned[focus.Serial] = (now + WithdrawShunMs, ours, now);
        }

        if (killer is BaseCreature { Deleted: false, Alive: true } && !ReferenceEquals(killer, focus))
        {
            _shunned[killer.Serial] = (now + WithdrawShunMs, ours, now);
        }

        if (_withdrawn.Count >= 1024)
        {
            _withdrawn.Clear();
        }

        var near = 0;
        var map = Map;

        if (from != null)
        {
            for (var i = 0; i < _members.Count; i++)
            {
                if (_members[i].Self is not { Deleted: false, Alive: true } body || body.Map != map)
                {
                    continue;
                }

                if (body.InRange(from.Location, BotBolt.Watch) || ReferenceEquals(from.Combatant, body))
                {
                    _withdrawn[body.Serial] = (from, now);
                    near++;
                }
            }
        }

        BledNear += near;

        logger.Information(
            "Squad {Id} of {Count} pulls back from {What}: {Deaths} of ours fell inside {Window}s, the last to {Killer}; {Near} within its reach left to run, the company holds on {Leader} for {Rest}s — leader was {By}",
            Id,
            Count,
            focus?.Name ?? "nothing",
            BleedDeaths,
            BleedMs / 1000,
            killer?.Name ?? "nothing",
            near,
            Leader?.Self?.Name ?? "nobody",
            BleedRestMs / 1000,
            by
        );

        Mustering = false;

        if (focus != null)
        {
            Disengage($"we pull back: {BleedDeaths} of ours fell inside a minute");
        }
    }

    // ---- 4. The thing killing a member --------------------------------------------------------------------------------------

    /// <summary>Whether a member turns on an attacker that has one of ours dying for its combatant, whatever the attacker's price. A switch. See <see cref="Peel"/>.</summary>
    public static bool Peels { get; set; } = true;

    /// <summary>Beats a member was turned on what was killing one of ours rather than on the company's enemy.</summary>
    public static long Peeled { get; private set; }

    /// <summary>Whether the company's enemy, among the things attacking it, is a caster first. A switch. See <see cref="Strongest"/>.</summary>
    public static bool CastersFirst { get; set; } = true;

    /// <summary>Times the company's enemy was a caster taken ahead of a stronger thing attacking it.</summary>
    public static long CasterTaken { get; private set; }

    /// <summary>
    /// For a member in the press: the strongest attacker, other than the company's enemy, that has one of ours under
    /// <see cref="DyingShare"/> for its combatant, within the member's <see cref="PressReach"/>, in its sight and lawful to strike;
    /// null when none. Of the 181 deaths to something other than the company's enemy on 405–409, 133 were to a thing priced under
    /// the enemy, which no rule turned the company on; <c>BotSquads.Protects</c> covered the 48 above it and fired three times.
    /// </summary>
    private Mobile Peel(Mobile body)
    {
        if (!Peels || _dyingNow.Count == 0)
        {
            return null;
        }

        Mobile best = null;
        var power = 0.0;

        for (var i = 0; i < _attackers.Count; i++)
        {
            var a = _attackers[i];

            if (ReferenceEquals(a, Focus) || a is not { Deleted: false, Alive: true } || a.Map != body.Map
                || a.Combatant is not Mobile victim || ReferenceEquals(victim, body) || !_dyingNow.Contains(victim)
                || !body.InRange(a.Location, PressReach))
            {
                continue;
            }

            var p = BotThreat.Power(a);

            if (p <= power || !body.InLOS(a) || !body.CanBeHarmful(a, false) || body.IsHarmfulCriminal(a))
            {
                continue;
            }

            power = p;
            best = a;
        }

        if (best != null)
        {
            Peeled++;
        }

        return best;
    }

    /// <summary>The field's clause of the Companies line.</summary>
    public static string Afielded()
    {
        var say = ValueStringBuilder.Create(128);

        try
        {
            foreach (var (kind, n) in BledBy)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append($"{kind} {n}");
            }

            return $"the field: {ChoiceRefused} creatures passed over between ×{ChoiceOdds:F1} and ×{WithdrawOdds:F1} what the company brought (the choice gate{(ChooseOdds > 0.0 && ChooseOdds < WithdrawOdds ? "" : ", off")}); "
                   + $"{Mustered} chosen fights mustered {MusterOff} tiles short of the enemy{(Musters ? "" : " (off)")}: {MusterGathered} engaged with {MusterShare:P0} of their strength within {FieldReach} tiles, {MusterTimed} on the {MusterMs / 1000}s clock, {MusterStruck} at a blow on a member, {MusterWent} found the enemy gone, {MusterShort} let it go for too few come up; {MusterUnneeded} chosen with the share already there; "
                   + $"{Bleedings} companies pulled back at {BleedDeaths} deaths inside {BleedMs / 1000}s{(Bleeds ? "" : " (off)")} (by the leader's errand: {(say.Length > 0 ? say.ToString() : "none")}; {BledNear} members within the thing's reach offered the run, {BledRefused} engagements refused for being what the company pulled back from); "
                   + $"{Peeled} beats a member turned on what was killing one of ours{(Peels ? "" : " (off)")}, {CasterTaken} beats a caster was named the company's enemy ahead of a stronger attacker inside the {TieShare:P0} tie{(CastersFirst ? "" : " (off)")}; "
                   + $"{FieldOvermatched} creatures stood to on the map's sum that the strength within {FieldReach} tiles of them would have withdrawn from (an instrument, not a gate)";
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>The field's counters, for <see cref="Forget"/>.</summary>
    private static void ForgetField()
    {
        ChoiceRefused = 0;
        Mustered = 0;
        MusterGathered = 0;
        MusterTimed = 0;
        MusterStruck = 0;
        MusterWent = 0;
        MusterShort = 0;
        MusterUnneeded = 0;
        Bleedings = 0;
        BledBy.Clear();
        BledRefused = 0;
        BledNear = 0;
        Peeled = 0;
        CasterTaken = 0;
        FieldOvermatched = 0;
    }
}
