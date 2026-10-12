using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Spells;
using Server.Spells.Seventh;

namespace Server.BotAI.V2;

/// <summary>
/// A company put through a Gate Travel: a mage of the company opens the gate where the company stands, the members step
/// through one by one, the leader last, and the march goes on from the far side.
///
/// <para>
/// <b>Patrick's order of 29.09.2026: "Gate Travel opens a gate for the whole company … instant transfer of a company
/// anywhere they have stood."</b> Asked by the two companies that march a long way — a delve from its muster to the cave
/// mouth (<see cref="BotDelve"/>), a captain's sweep to its square (<see cref="BotSweep"/>) — at the moment they would set
/// out (<see cref="Open"/>). When a member can cast Gate Travel and holds a rune within <see cref="BotRunes.GateNear"/> of
/// where the company is going, the passage takes the leader's beats (<see cref="Lead"/>) and the members'
/// (<see cref="Steer"/>, from the enlistment's hold) until the company is through; then the company's own march takes over
/// from wherever the leader now stands, and the delve's hold at the landing (build 313) regroups it at the mouth as before.
/// </para>
///
/// <para>
/// <b>The engine moves the bodies.</b> The gate is the engine's <see cref="GateTravelMoongate"/>, opened by the engine's
/// spell, and a member is put through by the engine's own use of it: <c>Moongate.EndConfirmation</c>, which is what a
/// player's double-click and "yes" come to — range one, the same map, not casting. Stepping on the gate alone is not enough
/// for a bot: a gate in a guarded town to a place outside one asks a confirmation gump, which a bot has no screen for.
/// </para>
///
/// <para>
/// <b>Never half through.</b> The members go first and the leader last, so the company's anchor stays on the near side until
/// the crossing is done and nobody is re-stationed back through the gate. The leader crosses when nobody is left to wait for
/// or at <see cref="CrossMs"/>, inside the thirty seconds this era's gate stands; whoever has not reached the gate by then
/// stays with the near side and is left to the march's own hold, which leaves a member beyond <c>MarchLost</c> rather than
/// waiting for it. Should the gate close with the leader still on the near side and members through (dispelled, or the leader
/// held), the leader walks to them (<see cref="RejoinMs"/>) and they wait where they landed. A fizzle is tried again up to
/// <see cref="BotRunes.Tries"/>; after that the company marches by road, as it would have.
/// </para>
/// </summary>
public sealed class BotPassage
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPassage));

    /// <summary>How long a captain waits for its company to stand round it before the gate is opened. A minute: a sweep's levy walks in from forty tiles.</summary>
    public static int GatherMs { get; set; } = 60000;

    /// <summary>How near the leader a member stands to count as gathered for the gate. Twelve.</summary>
    public static int Assembly { get; set; } = 12;

    /// <summary>When the leader crosses whatever is left. Twenty-two seconds: the engine's gate stands thirty in this era (<c>Core.T2A</c>).</summary>
    public static int CrossMs { get; set; } = 22000;

    /// <summary>How long a cast waits for mana, or for a fight at the muster, before the company marches by road. A minute.</summary>
    public static int WaitMs { get; set; } = 60000;

    /// <summary>How long a leader left on the near side walks to the members through before the passage is let go. Ten minutes.</summary>
    public static int RejoinMs { get; set; } = 600000;

    public static long Asked { get; private set; }

    public static long TooNear { get; private set; }

    public static long NoMage { get; private set; }

    public static long NoRune { get; private set; }

    public static long Unready { get; private set; }

    public static long Begun { get; private set; }

    public static long Opened { get; private set; }

    public static long Fizzles { get; private set; }

    public static long Fallbacks { get; private set; }

    public static long Crossings { get; private set; }

    public static long Through { get; private set; }

    public static long Behind { get; private set; }

    public static long HalfThrough { get; private set; }

    public static long Rejoined { get; private set; }

    public static long Aborted { get; private set; }

    public static long Saved { get; private set; }

    /// <summary>Members found back on the near side after going through: it should read nought. See <see cref="Crossing"/>.</summary>
    public static long Stepped { get; private set; }

    private static readonly Dictionary<string, int> _why = new(StringComparer.Ordinal);

    private static readonly Dictionary<BotSquad, BotPassage> _open = [];

    private enum Stage
    {
        Gather,
        Cast,
        Cross,
        Rejoin,
        Over
    }

    private readonly BotSquad _squad;

    private readonly Map _map;

    private readonly Mobile _leader;

    private readonly BotMobile _mage;

    private readonly RunebookEntry _entry;

    private readonly Point3D _destination;

    private readonly string _toward;

    private readonly int _from;

    private readonly HashSet<Mobile> _through = [];

    private Stage _stage;

    private long _stageTick;

    private long _nextTick;

    private Spell _spell;

    private int _tries;

    private Point3D _castAt;

    private Moongate _gate;

    private Point3D _far;

    private Point3D _wait;

    private BotPassage(BotSquad squad, Map map, Mobile leader, BotMobile mage, RunebookEntry entry, Point3D destination, string toward, int from, bool gather)
    {
        _squad = squad;
        _map = map;
        _leader = leader;
        _mage = mage;
        _entry = entry;
        _destination = destination;
        _toward = toward;
        _from = from;
        _stage = gather ? Stage.Gather : Stage.Cast;
        _stageTick = Core.TickCount;
        _nextTick = _stageTick;
        _castAt = mage.Location;
    }

    private string Place => _entry?.Description ?? "a rune";

    private static void Why(string why)
    {
        if (why != null)
        {
            _why.TryGetValue(why, out var had);
            _why[why] = had + 1;
        }
    }

    /// <summary>
    /// A passage for this company to <paramref name="destination"/>, or null when it will march: nobody of it can cast Gate
    /// Travel, nobody holds a rune near enough, or the rune saves too little. <paramref name="gather"/> waits for the company
    /// to stand round the leader first, for a company raised a moment ago.
    /// </summary>
    public static BotPassage Open(BotSquad squad, Mobile leader, Map map, Point3D destination, string toward, bool gather)
    {
        if (!BotRunes.Running || squad == null || leader is not { Deleted: false, Alive: true } || map == null || map == Map.Internal)
        {
            return null;
        }

        if (_open.TryGetValue(squad, out var already))
        {
            return already;
        }

        Asked++;

        var from = BotRunes.Apart(leader.Location, destination);

        if (from < BotRunes.LeastSaved)
        {
            TooNear++;

            return null;
        }

        BotMobile mage = null;
        RunebookEntry entry = null;
        var best = -1.0;
        var anyMage = false;
        var anyRune = false;
        string why = null;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is not BotMobile { Deleted: false, Alive: true } one || one.Map != map || !BotRunes.Knows(one, BotRunes.Gate))
            {
                continue;
            }

            anyMage = true;

            if (!BotRunes.Holds(one, map, destination, BotRunes.GateNear, out var rune)
                || from - BotRunes.Apart(rune.Location, destination) < BotRunes.LeastSaved
                || !BotGates.Joined(map, rune.Location, destination))
            {
                continue;
            }

            anyRune = true;

            // Mana and the engine's recovery come back while the company stands; the rest will not.
            if (!BotRunes.Ready(one, BotRunes.Gate, out _, out var chance, out var refused) && refused is not ("too little mana" or "not recovered"))
            {
                why ??= refused;

                continue;
            }

            if (!SpellHelper.CheckTravel(one, TravelCheckType.GateFrom, out _)
                || !SpellHelper.CheckTravel(one, map, rune.Location, TravelCheckType.GateTo, out _))
            {
                why ??= "the engine forbids the gate";

                continue;
            }

            if (chance > best)
            {
                best = chance;
                mage = one;
                entry = rune;
            }
        }

        if (mage == null)
        {
            if (!anyMage)
            {
                NoMage++;
            }
            else if (!anyRune)
            {
                NoRune++;
            }
            else
            {
                Unready++;
                Why(why);
            }

            return null;
        }

        var passage = new BotPassage(squad, map, leader, mage, entry, destination, toward, from, gather);

        _open[squad] = passage;
        Begun++;

        logger.Information(
            "{Mage} will open a gate for {Leader}'s company at ({X}, {Y}) to {Place}, {Near} tiles from {Toward}, instead of a march of {From} ({Chance:P0} a cast)",
            mage.Name,
            leader.Name,
            leader.Location.X,
            leader.Location.Y,
            entry.Description,
            BotRunes.Apart(entry.Location, destination),
            toward,
            from,
            best
        );

        return passage;
    }

    /// <summary>
    /// The leader's beat. True while the passage has the leader, with what it does; false once the company is through or
    /// the passage is given up, and the leader's own march goes on from wherever it now stands.
    /// </summary>
    public bool Lead(Mobile body, out BotDoing doing)
    {
        doing = default;

        if (_stage == Stage.Over)
        {
            return false;
        }

        if (_squad.Disbanded || !ReferenceEquals(body, _leader) || !_leader.Alive || _leader.Map != _map)
        {
            Abort("the company or its leader is gone");

            return false;
        }

        var now = Core.TickCount;

        return _stage switch
        {
            Stage.Gather => Gathering(now, out doing),
            Stage.Cast   => Casting(now, out doing),
            Stage.Cross  => Crossing(now, out doing),
            _            => Rejoining(now, out doing)
        };
    }

    /// <summary>
    /// A member's beat, asked by the enlistment's hold (<c>BotEnlist.Holding</c>). True when the passage says where this
    /// member stands: the mage on its tile while it casts, the others at the gate while it is open, and those through at the
    /// far side until the leader comes.
    /// </summary>
    public static bool Steer(IBotSquadMember member, BotSquad squad, out BotDoing doing)
    {
        doing = default;

        return squad != null && _open.TryGetValue(squad, out var passage) && passage.Direct(member, out doing);
    }

    private bool Direct(IBotSquadMember member, out BotDoing doing)
    {
        doing = default;

        var self = member?.Self;

        if (self is not { Deleted: false, Alive: true } || ReferenceEquals(self, _leader) || self.Map != _map)
        {
            return false;
        }

        switch (_stage)
        {
            case Stage.Cast:
                {
                    // Only while the spell is in the air: before it the mage closes up on the leader like anybody else.
                    if (!ReferenceEquals(self, _mage) || _spell == null)
                    {
                        return false;
                    }

                    // Held on its tile by a walk rather than a stand, so that a re-station of the company is answered at the
                    // next beat instead of walking the caster off mid-cast.
                    doing = BotDoing.Walk(_map, _castAt, BotArrival.Within(1), $"opening a gate to {Place}");

                    return true;
                }
            case Stage.Cross:
                {
                    if (_through.Contains(self))
                    {
                        doing = BotDoing.Walk(_map, _wait, BotArrival.Within(2), $"through the gate at {Place}, waiting for {_leader.Name}");

                        return true;
                    }

                    if (_gate is not { Deleted: false })
                    {
                        return false;
                    }

                    if (self.InRange(_gate.Location, 1))
                    {
                        doing = Step(self)
                            ? BotDoing.Walk(_map, _wait, BotArrival.Within(2), $"through the gate at {Place}, waiting for {_leader.Name}")
                            : BotDoing.Work($"stepping into {_mage.Name}'s gate");

                        return true;
                    }

                    doing = BotDoing.Walk(_map, _gate.Location, BotArrival.Within(1), $"to {_mage.Name}'s gate to {Place}");

                    return true;
                }
            case Stage.Rejoin:
                {
                    if (!_through.Contains(self))
                    {
                        return false;
                    }

                    doing = BotDoing.Work($"waiting at {Place} for {_leader.Name}");

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    private bool Gathering(long now, out BotDoing doing)
    {
        var near = 0;
        var all = 0;
        var members = _squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is not { Deleted: false, Alive: true } self || self.Map != _map)
            {
                continue;
            }

            all++;

            if (self.InRange(_leader.Location, Assembly))
            {
                near++;
            }
        }

        if (near >= all || now - _stageTick >= GatherMs)
        {
            To(Stage.Cast, now);
        }

        (_leader as BotMobile)?.Journey?.Finish();
        doing = BotDoing.Work($"gathering the company for {_mage.Name}'s gate to {Place}: {near} of {all} here");

        return true;
    }

    private bool Casting(long now, out BotDoing doing)
    {
        doing = default;

        if (_mage is not { Deleted: false, Alive: true } || _mage.Map != _map || !_squad.Has(_mage))
        {
            Fallbacks++;
            Abort("the mage is no longer with the company");

            return false;
        }

        var hold = Hold($"waiting for {_mage.Name} to open a gate to {Place}");

        if (_spell != null)
        {
            if (ReferenceEquals(_mage.Spell, _spell) && _spell.State != SpellState.None)
            {
                doing = hold;

                return true;
            }

            _spell = null;

            if (Find())
            {
                To(Stage.Cross, now);
                Opened++;

                logger.Information(
                    "{Mage} opened a gate at ({X}, {Y}) to {Place} for {Leader}'s company of {Count}",
                    _mage.Name,
                    _gate.X,
                    _gate.Y,
                    Place,
                    _leader.Name,
                    _squad.Count
                );

                doing = hold;

                return true;
            }

            _tries++;
            Fizzles++;

            if (_tries >= BotRunes.Tries)
            {
                Fallbacks++;
                Abort($"the gate would not open in {_tries} tries");

                return false;
            }

            _nextTick = now + BotRunes.RetryMs;
            doing = hold;

            return true;
        }

        if (now - _nextTick < 0)
        {
            doing = hold;

            return true;
        }

        // A fight at the muster is the company's first business; the gate waits for it, a while.
        if (_squad.Stance == BotSquadStance.Fighting)
        {
            if (now - _stageTick >= WaitMs)
            {
                Fallbacks++;
                Abort("the company was fighting where it stood");

                return false;
            }

            doing = hold;

            return true;
        }

        if (!BotRunes.Ready(_mage, BotRunes.Gate, out var scroll, out _, out var why))
        {
            if (why is "too little mana" or "not recovered" or "already casting" && now - _stageTick < WaitMs)
            {
                _nextTick = now + BotRunes.RetryMs;
                doing = hold;

                return true;
            }

            Unready++;
            Why(why);
            Fallbacks++;
            Abort($"{_mage.Name} could not cast Gate Travel: {why}");

            return false;
        }

        _castAt = _mage.Location;
        _mage.Journey?.Finish();
        _spell = new GateTravelSpell(_mage, _entry, scroll);

        if (!_spell.Cast())
        {
            _spell = null;
            _tries++;
            Fizzles++;

            if (_tries >= BotRunes.Tries)
            {
                Fallbacks++;
                Abort("the engine would not begin the cast");

                return false;
            }

            _nextTick = now + BotRunes.RetryMs;
        }

        doing = hold;

        return true;
    }

    /// <summary>The gate this cast opened: one of the engine's travel gates within a tile of the caster, linked to the rune's spot.</summary>
    private bool Find()
    {
        foreach (var gate in _map.GetItemsInRange<GateTravelMoongate>(_mage.Location, 1))
        {
            if (gate is { Deleted: false, LinkedGate: { Deleted: false } other } && BotRunes.Apart(other.Location, _entry.Location) <= 1)
            {
                _gate = gate;
                _far = other.Location;
                _wait = Aside(_far);

                return true;
            }
        }

        return false;
    }

    /// <summary>A tile a few steps off the far gate for the members through to stand on: standing on the gate, a step takes them back.</summary>
    private Point3D Aside(Point3D at)
    {
        ReadOnlySpan<int> offsets = [3, 0, 0, 3, -3, 0, 0, -3, 2, 2, -2, 2, 2, -2, -2, -2];

        for (var i = 0; i < offsets.Length; i += 2)
        {
            var x = at.X + offsets[i];
            var y = at.Y + offsets[i + 1];

            if (BotStep.Settle(_map, x, y, out var z) && _map.CanFit(x, y, z, 16, false, false))
            {
                return new Point3D(x, y, z);
            }
        }

        return at;
    }

    private bool Crossing(long now, out BotDoing doing)
    {
        doing = default;

        if (_gate is not { Deleted: false })
        {
            if (_through.Count == 0)
            {
                Fallbacks++;
                Abort("the gate closed before anybody went through");

                return false;
            }

            // The leader is on the near side with members through: it goes to them. See the class note.
            HalfThrough++;
            To(Stage.Rejoin, now);

            logger.Information(
                "{Mage}'s gate to {Place} closed with {Through} of {Leader}'s company through and the leader on the near side; the leader walks to them",
                _mage.Name,
                Place,
                _through.Count,
                _leader.Name
            );

            return Rejoining(now, out doing);
        }

        // Anybody standing at the gate is put through, whether or not its own beat has come.
        var waiting = 0;
        var members = _squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is not { Deleted: false, Alive: true } self || ReferenceEquals(self, _leader))
            {
                continue;
            }

            // Through and still on the far side, or stepped back through the far gate (a re-station can walk a member onto
            // it) and counted as waiting again.
            if (_through.Contains(self))
            {
                if (self.Map == _map && self.InRange(_far, 12))
                {
                    continue;
                }

                _through.Remove(self);
                Stepped++;
            }

            if (Landed(self))
            {
                _through.Add(self);

                continue;
            }

            if (self.Map == _map && self.InRange(_gate.Location, 1) && Step(self))
            {
                continue;
            }

            waiting++;
        }

        // The leader last: when nobody is left to wait for, or the gate's time is nearly out.
        if (waiting == 0 || now - _stageTick >= CrossMs)
        {
            if (!_leader.InRange(_gate.Location, 1))
            {
                doing = BotDoing.Walk(_map, _gate.Location, BotArrival.Within(1), $"into {_mage.Name}'s gate to {Place}, last");

                return true;
            }

            if (!Step(_leader))
            {
                doing = BotDoing.Work($"stepping into {_mage.Name}'s gate to {Place}");

                return true;
            }

            Finish(waiting);

            return false;
        }

        doing = _leader.InRange(_gate.Location, 1)
            ? BotDoing.Work($"holding {_mage.Name}'s gate to {Place} for {waiting} of the company")
            : BotDoing.Walk(_map, _gate.Location, BotArrival.Within(1), $"to {_mage.Name}'s gate to {Place}");

        return true;
    }

    private bool Rejoining(long now, out BotDoing doing)
    {
        doing = default;

        if (_leader.InRange(_far, 8))
        {
            Rejoined++;
            To(Stage.Over, now);
            _open.Remove(_squad);

            logger.Information("{Leader} has reached the part of its company that went through {Mage}'s gate to {Place}", _leader.Name, _mage?.Name, Place);

            return false;
        }

        if (now - _stageTick >= RejoinMs)
        {
            Abort($"the leader did not reach the members through the gate in {RejoinMs / 60000} minutes");

            return false;
        }

        doing = BotDoing.Walk(_map, _far, BotArrival.Within(6), $"to the company waiting at {Place}");

        return true;
    }

    /// <summary>
    /// Puts one body through the gate by the engine's own use of it (<c>Moongate.EndConfirmation</c>: in range, the same map,
    /// not casting). True when it landed on the far side.
    /// </summary>
    private bool Step(Mobile who)
    {
        if (_gate is not { Deleted: false } || who.Spell != null)
        {
            return false;
        }

        _gate.EndConfirmation(who);

        if (!Landed(who))
        {
            return false;
        }

        _through.Add(who);
        (who as BotMobile)?.Journey?.Finish();

        return true;
    }

    private bool Landed(Mobile who) => who.Map == _map && _far != Point3D.Zero && who.InRange(_far, 2);

    /// <summary>The company is through, the leader last.</summary>
    private void Finish(int left)
    {
        var through = _through.Count - (_through.Contains(_leader) ? 1 : 0);

        Crossings++;
        Through += through + 1;
        Behind += left;

        var saved = Math.Max(0, _from - BotRunes.Apart(_far, _destination));

        Saved += saved;
        To(Stage.Over, Core.TickCount);
        _open.Remove(_squad);

        if (_mage?.Guild != null)
        {
            BotRuneShelf.Used(_mage.Guild.Name, _map, _entry.Location);
        }

        logger.Information(
            "{Leader}'s company went through {Mage}'s gate to {Place}: {Through} and the leader through, {Left} left on the near side, {Saved} tiles of march to {Toward} saved",
            _leader.Name,
            _mage.Name,
            Place,
            through,
            left,
            saved,
            _toward
        );
    }

    private BotDoing Hold(string note) =>
        _leader.InRange(_castAt, 3)
            ? BotDoing.Work(note)
            : BotDoing.Walk(_map, _castAt, BotArrival.Within(2), note);

    private void To(Stage stage, long now)
    {
        _stage = stage;
        _stageTick = now;
    }

    /// <summary>The passage is let go: the company marches on by road from wherever it stands.</summary>
    public void Abort(string why)
    {
        if (_stage == Stage.Over)
        {
            return;
        }

        Aborted++;
        _stage = Stage.Over;
        _open.Remove(_squad);

        if (_spell != null && ReferenceEquals(_mage?.Spell, _spell))
        {
            _spell.Disturb(DisturbType.Hurt);
        }

        _spell = null;

        logger.Information("{Leader}'s gate to {Place} is let go: {Why}", _leader?.Name, Place, why);
    }

    public static string Describe()
    {
        if (Asked == 0)
        {
            return "no company has asked for a gate";
        }

        using var line = Server.Text.ValueStringBuilder.Create(512);

        line.Append(
            $"{Asked} companies asked for a gate: {Begun} begun, {TooNear} were within {BotRunes.LeastSaved} tiles, {NoMage} had nobody able to cast Gate Travel, {NoRune} held no rune within {BotRunes.GateNear} of where they went, {Unready} had a mage that could not cast"
        );

        if (_why.Count > 0)
        {
            line.Append(" (");

            var first = true;

            foreach (var (why, many) in _why)
            {
                line.Append(first ? "" : ", ");
                line.Append($"{why} {many}");
                first = false;
            }

            line.Append(')');
        }

        line.Append(
            $"; {Opened} gates opened ({Fizzles} casts fizzled), {Crossings} companies through with {Through} bodies and {Behind} left on the near side, {HalfThrough} gates closed half through ({Rejoined} rejoined), {Stepped} found back on the near side, {Fallbacks} marched by road instead, {Aborted} let go, {Saved} tiles of march saved"
        );

        return line.ToString();
    }

    public static void Forget()
    {
        _open.Clear();
        _why.Clear();
        Asked = 0;
        TooNear = 0;
        NoMage = 0;
        NoRune = 0;
        Unready = 0;
        Begun = 0;
        Opened = 0;
        Fizzles = 0;
        Fallbacks = 0;
        Crossings = 0;
        Through = 0;
        Behind = 0;
        HalfThrough = 0;
        Rejoined = 0;
        Aborted = 0;
        Saved = 0;
        Stepped = 0;
    }
}
