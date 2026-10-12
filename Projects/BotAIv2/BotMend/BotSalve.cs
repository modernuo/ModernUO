using System;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Patching somebody up — itself or somebody else, by spell if it can and by cloth if it cannot.
///
/// <para>
/// <b>This is the undertaking the ladder has been missing since it was written.</b> The rung for a bot that is
/// losing has always been there and has always had nothing on it, so the brain's answer to failing health was
/// to hold on to whatever the bot was already doing. That was survivable while nothing fought; the moment
/// hunting existed it meant a bot at two hit points going back to a skeleton, which is the first version's
/// worst night in one sentence.
/// </para>
///
/// <para>
/// <b>The same undertaking serves both, and the rung is what makes them different.</b> Mending itself is
/// offered above everything — a bot on the floor does not weigh options. Mending somebody else is offered as
/// ordinary work, competing on the same arithmetic as digging: it pays in real Healing and real Magery, which
/// at five hundred a point is a living, and it pays nothing at all in coin. That ordering is not tidiness. The
/// first version put "shout for help" <em>above</em> "I am dying", so a bot on its last few points announced a
/// company it could not join, found nobody able, and posted it again — dozens of times in a row. Looking after
/// somebody else must never outrank looking after yourself.
/// </para>
///
/// <para>
/// <b>Nothing here is a state that can wait indefinitely.</b> Out of mana, out of cloth, patient healed,
/// patient dead, patient walked away — every one of them ends the undertaking on the same beat it becomes true.
/// A healer standing over a corpse with no bandages is the shape of bug this project keeps finding.
/// </para>
/// </summary>
public sealed class BotSalve : BotDeed
{
    /// <summary>The ledger's key. One kind of work whoever the patient is.</summary>
    public const string Trade = "mend";

    /// <summary>
    /// What mending is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// It produces nothing and earns nothing, so everything it is worth is skill — and that is real: a heal
    /// cast trains Magery and a bandage trains Healing, both by the engine's own check. Thirty puts it above
    /// an errand to the shops and below every trade, which is the right place for looking after each other on
    /// a shard where nobody has yet been paid to do it.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 30.0;

    /// <summary>How long a patch-up is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How often another attempt is made once one has been begun.</summary>
    public static int TryMs { get; set; } = 1500;

    /// <summary>
    /// How much more urgent mending is at death's door than at the threshold.
    ///
    /// <para>
    /// <b>Without this there was a band where nobody healed at all.</b> A bot drops onto <c>Failing</c> at
    /// thirty-five per cent and looks after itself; above seventy it does not want mending; and in between the
    /// estimate was a flat thirty a minute, which loses to a mining trip at forty-five. So a bot at forty per
    /// cent went and dug ore.
    /// </para>
    ///
    /// <para>
    /// Three, meaning a bot on its last legs reckons mending at four times what a barely-scratched one does —
    /// which beats every trade on the shard, and should. It is the same trick used everywhere here rather than a
    /// new mechanism: the number is made to reflect the fact instead of a rung being added to carry it.
    /// </para>
    /// </summary>
    public static double Urgency { get; set; } = 3.0;

    private readonly Mobile _patient;

    private readonly Map _map;

    private readonly Point3D _found;

    private readonly bool _onSelf;

    private readonly SkillName _trains;

    private int _casts;

    private int _cloths;

    private int _draughts;

    private bool _tried;

    private bool _awaiting;

    private long _triedTick;

    private bool _reasoned;

    /// <summary>Whether the patient is waiting for this healer, and when it was last asked. See <see cref="BotAwaitMend"/>.</summary>
    private bool _askedToWait;

    private long _askedTick;

    private bool _askedEver;

    /// <summary>How often a healer walking to a patient asks it again to wait.</summary>
    public static int AskEveryMs { get; set; } = 5000;

    /// <summary>Mendings whose first choice was a spell, for a patient in a fight. See <see cref="BotMend.Embattled"/>.</summary>
    public static long ForFight { get; private set; }

    /// <summary>Mendings whose first choice was cloth, for a patient out of any fight, keeping the mana.</summary>
    public static long ForCalm { get; private set; }

    /// <summary>Mendings whose first choice was cloth because the healer itself was under fire.</summary>
    public static long UnderFire { get; private set; }

    /// <summary>Mendings that began with only one of spell and cloth to hand, so nothing was chosen.</summary>
    public static long Bare { get; private set; }

    /// <summary>Mendings of another ended because the patient turned to robbery, went red, or became somebody it is a crime to help. See <see cref="BotMend.Abetting"/>.</summary>
    public static long Turned { get; private set; }

    public static string Describe() =>
        ForFight + ForCalm + UnderFire + Bare == 0
            ? "no mending has been begun"
            : $"{ForFight + ForCalm + UnderFire + Bare} mendings begun ({Bent} walks to a patient bent into another try): {ForFight} by spell for a patient in a fight, {ForCalm} by cloth out of one, {UnderFire} by cloth with the healer under fire, {Bare} with only one means to hand";

    public static void Forget()
    {
        ForFight = 0;
        ForCalm = 0;
        UnderFire = 0;
        Bare = 0;
        Turned = 0;
    }

    /// <summary>Tells the stopwatch a cast for the patient has begun. Always true, so it reads in line. See <c>BotReflex.Casting</c>.</summary>
    private bool Cast(Mobile body)
    {
        BotReflex.Casting(body, _patient);

        return true;
    }

    /// <summary>Who is being mended, and whether it is the mender itself. See <c>BotReflex.Took</c>.</summary>
    public Mobile Patient => _patient;

    public bool OnSelf => _onSelf;

    public BotSalve(Mobile patient, Map map, bool onSelf, SkillName trains)
    {
        _patient = patient;
        _map = map;
        _found = patient?.Location ?? Point3D.Zero;
        _onSelf = onSelf;
        _trains = trains;
    }

    public override string Kind => Trade;

    /// <summary>A wound to bind, this bot's or somebody else's. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    /// <summary>
    /// <b>Somebody else's wound in a fight will not wait for the dwell (06.10.2026, react14).</b> A mending of another was held
    /// off by the work in hand like any offer: half a minute to two for work freshly taken, and the margin after. Pressing —
    /// so weighed plainly against what the work in hand is worth now — for a patient that is failing, or is under
    /// <c>BotMend.Hurt</c> and in a fight, and only when the healer's hands are empty or hold work that is put down and taken
    /// up again: a mending never throws work away to begin. Never for a bot's own wounds, which the failing rung looks after.
    /// </summary>
    public override bool Pressing(IBotWilful bot)
    {
        if (_onSelf || !BotSurgeon.Calls || _patient is not { Deleted: false, Alive: true } patient)
        {
            return false;
        }

        var held = bot?.Resolve?.Deed;

        if (held != null && (!held.Resumes || bot.Resolve.Paused != null))
        {
            return false;
        }

        return BotLadder.Failing(patient) || BotMend.Share(patient) < BotMend.Hurt && BotMend.Embattled(patient);
    }

    /// <summary>A second mending of the same patient is the same mending: the medic and the surgeon both offer one.</summary>
    public override bool Repeats(BotDeed other) => other is BotSalve salve && salve._patient == _patient;

    /// <summary>
    /// Mending oneself is put down for a flight and taken up again after it, rather than thrown away.
    ///
    /// <para>
    /// A bot bandaging itself in the field is set upon, runs, and comes back still hurt: the bandage is the
    /// next thing it needs, and the flight already winds one on the run. Only for oneself — a patient that
    /// walked off during the flight is a chase, not a resumption.
    /// </para>
    /// </summary>
    public override bool Steadfast => _onSelf;

    /// <summary>
    /// <b>A patient that could not be reached is tried for again (29.09.2026).</b> Ten of thirty-eight mendings on build 300
    /// ended "could not get nearer to {patient}": the walk to a bot in a fight ran out of plans at the crowd round it,
    /// and the healer gave the patient up while standing eight tiles off. A refused walk to a living patient within
    /// twice the casting reach is bent — the next beat asks for the walk again, to where the patient is now — up to
    /// <see cref="MostBends"/> times.
    /// </summary>
    public static int MostBends { get; set; } = 3;

    /// <summary>Refused walks to a patient bent into another try. See <see cref="MostBends"/>.</summary>
    public static long Bent { get; private set; }

    private int _bends;

    public override bool Bend(IBotWilful bot)
    {
        var body = bot?.Self;

        if (_onSelf || body == null || _patient is not { Deleted: false, Alive: true } || _patient.Map != body.Map
            || ++_bends > MostBends || !body.InRange(_patient, BotMend.Cast * 2))
        {
            return false;
        }

        Bent++;

        return true;
    }

    /// <summary>See <see cref="BotDeed.ResumesHurt"/>: a hurt bot is exactly who should go back to bandaging.</summary>
    public override bool ResumesHurt => true;

    /// <summary>Somebody else's bandaging is taken up again after a flight too; only one's own is held against offers. See <see cref="BotDeed.Resumes"/>.</summary>
    public override bool Resumes => true;

    public override void Resumed(IBotWilful bot)
    {
        _tried = false;
        _awaiting = false;
    }

    public override Map Map => _map;

    /// <summary>
    /// Where the patient was found, and for a bot mending itself, where it is standing now.
    ///
    /// <para>
    /// <b>A bot binding its own wounds is not going anywhere, and the place it began is not where the work is.</b> The
    /// work in hand is appraised again at every review, and the one veto that reads a work's place against the bot's own
    /// square is ground closed by deaths lately (<c>BotPeril.Closes</c>, build 48), which spares only the square the bot
    /// stands in - so a bot that had stepped out of the square it began mending in had its own bandaging weighed as a
    /// walk into the killing ground. On build 48, in the war north of Britain on 15.09.2026, Wynn's mend on itself was
    /// weighed at nought at 04:47:22 in the second a fresh one was offered at 32, and was put down "interrupted by mend"
    /// nine times in 47 seconds, seven of them worth nought, against one such drop in the nine sessions before it that
    /// night. Mending somebody else keeps the place the patient was found, which is the decision the healer set out on.
    /// </para>
    /// </summary>
    public override Point3D Where => _onSelf && _patient is { Deleted: false } ? _patient.Location : _found;

    /// <summary>
    /// Worth what the wound is worth: the same prior, multiplied by how far past caring the patient is.
    ///
    /// Live rather than fixed at proposal, and that is right — a patient that got worse while the healer walked
    /// is a more urgent job than the one it set out on.
    /// </summary>
    public override double Expects
    {
        get
        {
            var share = BotMend.Share(_patient);
            var past = Math.Clamp((BotMend.Hurt - share) / Math.Max(0.01, BotMend.Hurt), 0.0, 1.0);

            return Prior * (1.0 + past * Urgency);
        }
    }

    public override double Minutes => WorkMinutes;

    /// <summary>
    /// Magery for a caster, Healing for everybody else, decided by the proposer from what this bot can do.
    ///
    /// Named rather than inferred, like every other undertaking's skill — and it is the entire payment for this
    /// one, which makes getting it right worth a line.
    /// </summary>
    public override SkillName? Trains => _trains;

    public override int Outlay => 0;

    /// <summary>Not a coin either way. Bandages were paid for at a counter long before this.</summary>
    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage
    {
        get
        {
            var who = _onSelf ? "itself" : _patient?.Name ?? "somebody";

            return $"mending {who} ({_casts} casts, {_cloths} bandages, {_draughts} bottles)";
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null)
        {
            return BotDoing.Failed("no body");
        }

        if (_patient == null || _patient.Deleted || !_patient.Alive || _patient.Map != _map)
        {
            return Ending("the patient is past mending");
        }

        if (!_onSelf && BotMend.ShunsOutlaws && BotMend.Abetting(body, _patient) is { } crime)
        {
            Turned++;

            return BotDoing.Done($"{_patient.Name} is {crime}, and mending it would be a crime");
        }

        // <b>Arriving to find nobody hurt is not a failure, and calling it one had a price.</b> Ending()
        // reports Failed whenever nothing was administered — right for a patient who died on the way, wrong
        // for one who got better — and a failure writes caution against the ground under the trade's name.
        // So a healer that walked to somebody who recovered, or whom a second healer reached first, was
        // taught to avoid that spot. The sentence was worse than the ending: "failed at mend — mended" is two
        // words contradicting each other in six characters.
        //
        // Finished, and it took nothing, which the ledger reads as a trade that paid nothing here. That is
        // the honest signal and it needs no caution to carry it.
        if (BotMend.Whole(_patient))
        {
            // Mended by somebody else: a little regard for the healer (07.10.2026, traits17). See BotBiography.Healed.
            if (!_onSelf && _casts + _cloths + _draughts > 0)
            {
                BotBiography.Healed(body, _patient);
            }

            return _casts + _cloths + _draughts > 0
                ? Ending("mended")
                : BotDoing.Done("there was nothing left to mend");
        }

        // A bottle first when it is nearly over, and only then: it is the one mending that works while
        // something is hitting you, and there are two of them.
        if (_onSelf)
        {
            var bottle = BotMend.Draught(body);

            if (bottle != null && BotMend.Swallow(body, bottle))
            {
                _draughts++;

                return BotDoing.Work("drinking");
            }
        }

        // <b>Which means decides how close to stand, and standing too close is what stops the means working.</b>
        // A heal reaches eight tiles; cloth reaches one. Walking to the cloth distance to cast put the healer
        // inside melee range of whatever was hitting the patient, and a caster that is being hit cannot cast.
        // <b>Whose fight it is decides the means.</b> A blow on the healer destroys a cast, so under fire it is
        // cloth whatever the patient is doing. A patient in a fight gets the spell: two seconds from eight tiles,
        // where a bandage is ten seconds standing next to whatever is hitting it. A patient out of any fight gets
        // cloth when the healer has any, and the mana is kept for the next fight — Patrick's order of 15.09.2026.
        var spell = BotMend.Spell(body, _patient);
        var fired = BotMend.UnderFire(bot);
        var embattled = !fired && BotMend.Embattled(_patient);
        var hasCloth = BotMend.Cloth(body) > 0;
        var cloth = fired || spell < 0 || (!embattled && hasCloth);

        if (!_reasoned)
        {
            _reasoned = true;

            if (spell < 0 || !hasCloth)
            {
                Bare++;
            }
            else if (fired)
            {
                UnderFire++;
            }
            else if (embattled)
            {
                ForFight++;
            }
            else
            {
                ForCalm++;
            }
        }

        var near = cloth ? BotMend.Touch : BotMend.Cast;

        if (!_onSelf && !body.InRange(_patient.Location, near))
        {
            // A patient about its own work is asked to stop and wait for the healer. See BotAwaitMend (build 321).
            // Asked again every few seconds until it waits: the first ask can find the patient still at grips (build 335).
            if (!_askedToWait && (!_askedEver || Core.TickCount - _askedTick >= AskEveryMs))
            {
                _askedEver = true;
                _askedTick = Core.TickCount;
                _askedToWait = BotAwaitMend.Ask(body, _patient);
            }

            // Following the patient rather than the place it was standing: hurt things move, usually away
            // from whatever hurt them.
            return BotDoing.Walk(_map, _patient, BotArrival.Within(near), $"to {_patient.Name}");
        }

        // A cast of <em>ours</em> that has come round to its target. This is the click a bot has no client to
        // make.
        //
        // <b>Guarded by having started one.</b> A cursor on a bot is not necessarily this undertaking's: mining
        // puts one up to point at rock, and pointing a harvest target at a wounded friend would be this file
        // reaching into somebody else's work through a field they happen to share.
        if (_awaiting && body.Target != null)
        {
            _awaiting = false;

            if (BotMend.Aim(body, _patient))
            {
                _casts++;
            }

            return BotDoing.Work("healing");
        }

        // Mid-cast. The engine is holding the delay and movement already knows to stand still for it.
        if (body.Spell != null)
        {
            return BotDoing.Work("casting");
        }

        if (_tried && Core.TickCount - _triedTick < TryMs)
        {
            return BotDoing.Work("healing");
        }

        _tried = true;
        _triedTick = Core.TickCount;

        // <b>Cloth under fire, spell out of it.</b> Not a preference: a blow destroys a cast outright and only
        // makes a bandage slip. Out of a fight the order is the other way round for three reasons of its own —
        // two seconds against ten, mana against money, and herbs a caster walks to town for anyway.
        if (BotMend.Winding(body))
        {
            return BotDoing.Work("bandaging");
        }

        if (cloth)
        {
            if (BotMend.Wind(body, _patient))
            {
                _cloths++;

                return BotDoing.Work("bandaging");
            }

            // Out of cloth. A cast under fire is mostly wasted, but wasted beats nothing at all.
            var last = BotMend.Spell(body, _patient);

            if (last >= 0 && BotMend.Begin(body, last) && Cast(body))
            {
                _awaiting = true;

                return BotDoing.Work("casting");
            }
        }
        else
        {
            // The spell chosen above, this beat: asking again would build the spell a second time for the same answer.
            if (spell >= 0 && BotMend.Begin(body, spell) && Cast(body))
            {
                _awaiting = true;

                return BotDoing.Work("casting");
            }

            if (BotMend.Wind(body, _patient))
            {
                _cloths++;

                return BotDoing.Work("bandaging");
            }
        }

        // No mana, no herbs, no cloth. Ending rather than waiting: what is missing is bought at a counter, and
        // the errand that does that is somebody else's to offer.
        return Ending("nothing left to mend with");
    }

    /// <summary>
    /// Over, and never as a failure when something was actually done.
    ///
    /// <b>A failure marks the place with caution</b>, and the place a bot mends itself is wherever it was
    /// standing when it got hurt — usually its own work. Teaching the ledger that the mine is dangerous because
    /// a bot bandaged itself at the mouth of it would be the mining trip paying for the fight it survived.
    /// </summary>
    private BotDoing Ending(string why) =>
        _casts + _cloths + _draughts > 0
            ? BotDoing.Done($"{why} after {_casts} casts, {_cloths} bandages and {_draughts} bottles")
            : BotDoing.Failed(why);
}
