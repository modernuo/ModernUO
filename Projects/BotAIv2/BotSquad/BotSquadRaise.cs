using System;
using System.Collections.Generic;
using Server.Items;
using Server.Spells;
using Server.Spells.Eighth;
using Server.Targeting;

namespace Server.BotAI.V2;

/// <summary>
/// <b>The company's half of raising its fallen (02.10.2026).</b> What raises and how is <see cref="BotRaise"/>'s; this is who goes,
/// when, and how the company holds still for it.
///
/// <para>
/// A member that dies leaves the roster in the same second (<c>BotMobile.OnDeath</c> → <c>BotSquads.Leave</c> → <see cref="Detach"/>),
/// so the company keeps its fallen apart, on <see cref="_fallen"/>, for <see cref="BotRaise.HoldMs"/>. On every beat it looks for the
/// member best able to raise each of them — the engine's chance on one try, and the nearer at equal odds — within
/// <see cref="BotRaise.Reach"/>; the leader as much as anybody, because in a company of two the survivor is the leader. That member's
/// walk is written into the same slot a station is (<see cref="BotJourney.Rebase(Map, Point3D, BotArrival, string)"/>), its own
/// work is not advanced while it is at the body (<c>BotWill.Advance</c>), and the company hands it no station, no target and no
/// patient until the body is up or it is given up on: forty seconds, twenty tries, or flight. While the company's enemy stands within
/// <see cref="BotRaise.ClearReach"/> of the body the raising waits, and the revival is held for it.
/// </para>
/// </summary>
public sealed partial class BotSquad
{
    private sealed class FallenOne
    {
        public BotMobile Ghost;

        public long Fell;

        public Point3D Where;

        public Map Map;

        public BotMobile Raiser;

        public BotRaise.Means Means;

        public double Chance;

        public long Claimed;

        /// <summary>Tries by the member at it now, and by everybody.</summary>
        public int Tries;

        public int AllTries;

        /// <summary>When the bandage being wound is done; nought when none is.</summary>
        public long Wound;

        /// <summary>Members given up on for this body.</summary>
        public readonly HashSet<Serial> Passed = [];

        /// <summary>Whether anybody in the company could raise it, ever; and anybody able within reach.</summary>
        public bool EverAble;

        public bool EverNear;

        /// <summary>Whether the fight held it.</summary>
        public bool Waited;

        /// <summary>Whether a raising took and the engine would not stand the body up after all: nothing more is spent on it.</summary>
        public bool Refused;
    }

    private readonly List<FallenOne> _fallen = [];

    /// <summary>Raisings this company has spent. See <see cref="BotRaise.PerCompany"/>.</summary>
    private int _raisings;

    /// <summary>A member left the roster dead: kept apart for raising. See <see cref="Detach"/>.</summary>
    private void KeepFallen(BotMobile ghost)
    {
        if (!BotRaise.Running || Disbanded || ghost is not { Deleted: false } || ghost.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        for (var i = 0; i < _fallen.Count; i++)
        {
            if (ReferenceEquals(_fallen[i].Ghost, ghost))
            {
                return;
            }
        }

        _fallen.Add(new FallenOne { Ghost = ghost, Fell = ghost.FellTick, Where = ghost.Location, Map = map });

        BotRaise.NoteFallen();
        BotRaise.Await(ghost, this);
    }

    /// <summary>Whether this member is at the body of one of the company's fallen. The company hands it nothing else meanwhile.</summary>
    internal bool Raising(Mobile body)
    {
        if (body == null)
        {
            return false;
        }

        for (var i = 0; i < _fallen.Count; i++)
        {
            if (ReferenceEquals(_fallen[i].Raiser, body))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a raising is going on: somebody is at a body. A company so busy is not quiet, and is not one bot.</summary>
    private bool RaiseUnderway
    {
        get
        {
            for (var i = 0; i < _fallen.Count; i++)
            {
                if (_fallen[i].Raiser != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Every fallen member looked at once a beat: let go, waited on, walked to, or raised.</summary>
    private void Raise()
    {
        if (_fallen.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;

        for (var i = _fallen.Count - 1; i >= 0; i--)
        {
            var down = _fallen[i];

            if (Attend(down, now, out var why))
            {
                if (why != null)
                {
                    BotRaise.NoteUnraised(why.Value);
                }

                StepAway(down);
                BotRaise.Done(down.Ghost);
                _fallen.RemoveAt(i);
            }
        }
    }

    /// <summary>A raiser stepping away from a body: its cast, if one of ours is still up, is let go of.</summary>
    private static void StepAway(FallenOne down)
    {
        var raiser = down.Raiser;

        down.Raiser = null;
        down.Tries = 0;
        down.Wound = 0;

        if (raiser is { Deleted: false } && raiser.Spell is ResurrectionSpell spell)
        {
            if (raiser.Target is { } cursor)
            {
                cursor.Cancel(raiser, TargetCancelType.Canceled);
            }
            else
            {
                // Still casting: without this the cursor comes up after the hand-off, nobody aims it, and the spell holds for 30 s.
                spell.Disturb(DisturbType.Hurt);
            }
        }
    }

    /// <summary>
    /// One beat for one fallen member. Returns true when the company is done with it — raised (<paramref name="why"/> null) or let go
    /// (<paramref name="why"/> says why).
    /// </summary>
    private bool Attend(FallenOne down, long now, out BotRaise.Unraised? why)
    {
        why = null;

        var ghost = down.Ghost;

        // Up already, or gone from where it fell: the ordinary revival carried it home, or something else raised it.
        if (ghost is not { Deleted: false } || ghost.Alive || ghost.FellTick != down.Fell || ghost.Map != down.Map
            || ghost.Location != down.Where || now - down.Fell >= BotRaise.HoldMs)
        {
            why = !down.EverAble ? BotRaise.Unraised.NobodyAble
                : !down.EverNear ? BotRaise.Unraised.NobodyNear
                : down.AllTries > 0 ? BotRaise.Unraised.TriedAndFailed
                : down.Waited ? BotRaise.Unraised.FightHeld
                : BotRaise.Unraised.CarriedHome;

            return true;
        }

        if (down.Refused)
        {
            why = BotRaise.Unraised.Ground;

            return true;
        }

        if (BotRaise.Unholy(ghost))
        {
            why = BotRaise.Unraised.Outlawed;

            return true;
        }

        if (!BotRaise.Takes(ghost))
        {
            why = BotRaise.Unraised.Ground;

            return true;
        }

        if (_raisings >= BotRaise.PerCompany || !BotDelveParty.MayRaise(ghost))
        {
            why = BotRaise.Unraised.Spent;

            return true;
        }

        if (down.Raiser != null && !Keeps(down, now))
        {
            down.Passed.Add(down.Raiser.Serial);
            StepAway(down);
            BotRaise.NoteHanded();
        }

        if (down.Raiser == null && !Choose(down, now))
        {
            // Nobody in the company has anything to raise it with: let go on the first look rather than asked again for two minutes.
            if (!down.EverAble && now - down.Fell >= BotRaise.JudgeMs)
            {
                why = BotRaise.Unraised.NobodyAble;

                return true;
            }

            return false;
        }

        return Work(down, now);
    }

    /// <summary>Whether the member at a body is still the one for it.</summary>
    private bool Keeps(FallenOne down, long now)
    {
        var raiser = down.Raiser;

        if (raiser is not { Deleted: false, Alive: true } || raiser.Map != down.Map || !Has(raiser))
        {
            return false;
        }

        // Running, or binding its own wounds, or about to need to: the body waits for somebody else.
        if (raiser.Resolve?.Deed is BotBolt or BotSalve || BotMend.Share(raiser) < DyingShare)
        {
            return false;
        }

        if (now - down.Claimed >= BotRaise.ClaimMs || down.Tries >= BotRaise.MaxTries)
        {
            return false;
        }

        // Mid-cast or mid-bandage the means are already spent or in hand; otherwise it must still have them.
        if (down.Wound != 0 || raiser.Spell is ResurrectionSpell)
        {
            return true;
        }

        return BotRaise.Best(raiser, out _, out _) == down.Means;
    }

    /// <summary>
    /// The member best able to raise this body, within reach and fit to go, set on it. Returns whether one was; says to the revival
    /// whether there is hope either way.
    /// </summary>
    private bool Choose(FallenOne down, long now)
    {
        BotMobile best = null;
        var bestMeans = BotRaise.Means.None;
        var bestChance = 0.0;
        var bestScore = 0.0;
        var bestAway = int.MaxValue;
        var able = false;

        for (var i = 0; i < _members.Count; i++)
        {
            if (_members[i].Self is not BotMobile { Deleted: false, Alive: true } body || ReferenceEquals(body, down.Ghost)
                || body.Map != down.Map || down.Passed.Contains(body.Serial) || Raising(body) || BotRegard.Hostile(body, down.Ghost))
            {
                continue;
            }

            var means = BotRaise.Best(body, out var chance, out _);

            if (means == BotRaise.Means.None)
            {
                continue;
            }

            able = true;

            var away = Math.Max(Math.Abs(body.X - down.Where.X), Math.Abs(body.Y - down.Where.Y));

            if (away > BotRaise.Reach || body.Resolve?.Deed is BotBolt or BotSalve || BotMend.Share(body) < DyingShare)
            {
                continue;
            }

            // A caster whose pool holds the cast now before one that would stand waiting for it: its odds count half while it waits.
            var score = means == BotRaise.Means.Bandage || body.Mana >= BotRaise.Mana ? chance : chance * 0.5;

            if (score < bestScore || score == bestScore && away >= bestAway)
            {
                continue;
            }

            best = body;
            bestMeans = means;
            bestChance = chance;
            bestScore = score;
            bestAway = away;
        }

        down.EverAble |= able;
        down.EverNear |= best != null;

        BotRaise.Looked(down.Ghost, best != null);

        if (best == null)
        {
            return false;
        }

        // <b>Not into the fight that made the body.</b> Raised with ten hits beside what killed it, it is a second corpse: the
        // raising waits while the company's enemy stands within ClearReach of it, and the revival is held for it meanwhile.
        if (FightNear(down))
        {
            return false;
        }

        down.Raiser = best;
        down.Means = bestMeans;
        down.Chance = bestChance;
        down.Claimed = now;
        down.Tries = 0;
        down.Wound = 0;

        return true;
    }

    /// <summary>Whether the company's enemy stands within <see cref="BotRaise.ClearReach"/> of the body; counted as a wait when it does.</summary>
    private bool FightNear(FallenOne down)
    {
        if (Stance != BotSquadStance.Fighting || Focus is not { Deleted: false, Alive: true } foe || foe.Map != down.Map
            || Math.Max(Math.Abs(foe.X - down.Where.X), Math.Abs(foe.Y - down.Where.Y)) > BotRaise.ClearReach)
        {
            return false;
        }

        down.Waited = true;
        BotRaise.NoteFightWait();

        return true;
    }

    /// <summary>
    /// The raiser's beat at the body: the walk to it, then the scroll, the book or the bandage. Returns whether the body is up.
    /// </summary>
    private bool Work(FallenOne down, long now)
    {
        var raiser = down.Raiser;
        var ghost = down.Ghost;

        if (raiser == null)
        {
            return false;
        }

        BotRaise.Looked(ghost, true);

        if (!raiser.InRange(down.Where, 1) || !raiser.InLOS(ghost))
        {
            var journey = raiser.Journey;

            if (journey != null && (!journey.Active || journey.Target != down.Where))
            {
                journey.Rebase(down.Map, down.Where, BotArrival.Beside, "raise");
            }

            return false;
        }

        // The fight came back to the body after the raiser set out: nothing new begun while it is there (a cast or a bandage already
        // under way is finished — it is spent).
        if (down.Wound == 0 && raiser.Spell == null && FightNear(down))
        {
            // Waiting on the fight is not the raiser's failing: its forty seconds start again when the fight leaves.
            down.Claimed = now;

            return false;
        }

        // A ghost is seen by the living only when it manifests (Mobile.CanSee): what a player does to be raised, the bot does too.
        ghost.Warmode = true;

        if (down.Means == BotRaise.Means.Bandage)
        {
            return Wind(down, now, raiser, ghost);
        }

        // The cursor is up: aimed, and the engine's spending read for whether it took. The scroll and the mana are spent only when
        // the cast takes (Spell.CheckSequence); the herbs of a book on every try.
        if (raiser.Target is { } cursor && raiser.Spell is ResurrectionSpell cast)
        {
            var used = cast.Scroll;
            var before = used?.Amount ?? 0;
            var mana = raiser.Mana;

            cursor.Invoke(raiser, ghost);

            var took = used != null
                ? used.Deleted || used.Amount < before
                : raiser.Mana <= mana - BotRaise.Mana / 2;

            down.Tries++;
            down.AllTries++;
            BotRaise.NoteTry(took);

            return took && Stood(down, raiser);
        }

        if (raiser.Spell != null)
        {
            // Its own cast going up, and the cursor comes on a later beat.
            return false;
        }

        if (raiser.Target is { } foreign)
        {
            // A cursor of something else, which nothing of the company's will aim for a member at a body: let go of.
            foreign.Cancel(raiser, TargetCancelType.Canceled);

            return false;
        }

        if (raiser.Mana < BotRaise.Mana)
        {
            BotRaise.NoteManaWait();

            return false;
        }

        var scroll = down.Means == BotRaise.Means.Scroll ? raiser.Backpack?.FindItemByType<ResurrectionScroll>() : null;

        if (down.Means == BotRaise.Means.Scroll && scroll == null)
        {
            down.Tries = BotRaise.MaxTries;

            return false;
        }

        var spell = SpellRegistry.NewSpell(BotRaise.Spell, raiser, scroll);

        if (spell != null && spell.Cast())
        {
            BotRaise.NoteCast();
        }
        else
        {
            // Refused at the start — frozen, a hand the engine will not free — and counted against the member like a miss.
            down.Tries++;
        }

        return false;
    }

    /// <summary>
    /// The bandage: one used at the start, the engine's wait (a heal's and five seconds more), and the engine's roll at the end, with
    /// its skill gains on the raising that takes (<c>BandageContext.EndHeal</c>). Returns whether the body is up.
    /// </summary>
    private bool Wind(FallenOne down, long now, BotMobile raiser, BotMobile ghost)
    {
        if (down.Wound == 0)
        {
            var cloth = raiser.Backpack?.FindItemByType<Bandage>();

            if (cloth == null)
            {
                down.Tries = BotRaise.MaxTries;

                return false;
            }

            cloth.Consume();
            down.Wound = now + BotRaise.BandageMs(raiser);

            return false;
        }

        if (now < down.Wound)
        {
            return false;
        }

        down.Wound = 0;
        down.Tries++;
        down.AllTries++;

        var healing = raiser.Skills[SkillName.Healing].Value;
        var anatomy = raiser.Skills[SkillName.Anatomy].Value;
        var took = raiser.InRange(ghost, Bandage.Range) && raiser.InLOS(ghost) && healing >= 80.0 && anatomy >= 80.0
                   && (healing - 68.0) / 50.0 > Utility.RandomDouble();

        BotRaise.NoteTry(took);

        if (!took)
        {
            return false;
        }

        raiser.CheckSkill(SkillName.Anatomy, 0.0, 120.0);
        raiser.CheckSkill(SkillName.Healing, 0.0, 120.0);

        return Stood(down, raiser);
    }

    /// <summary>Up, its corpse gone through, and back in the company. See <see cref="BotRaise.Rise"/>.</summary>
    private bool Stood(FallenOne down, BotMobile raiser)
    {
        var ghost = down.Ghost;

        if (!BotRaise.Rise(ghost, raiser, down.Means, down.Chance, down.AllTries, this))
        {
            down.Refused = true;

            return false;
        }

        _raisings++;
        down.Raiser = null;

        if (!Disbanded && Map is { } map
            && BotSquads.Enrol(this, ghost, map, Anchor, $"raised where it fell by {raiser.Name}, back in company {Id}", force: true))
        {
            BotRaise.NoteRejoined();
        }

        return true;
    }
}
