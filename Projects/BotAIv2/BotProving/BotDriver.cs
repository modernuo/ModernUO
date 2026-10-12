using System;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// One double's hands in a fight: the bot's own reflexes, beat by beat, against whatever it is set on. Shared by the
/// single fight (<see cref="BotTrial"/>) and the company's (<see cref="BotRoomTrial"/>), so the two measure the same bot.
///
/// <para>
/// In the order a bot reaches for things: a spell of its own coming round to its target; a bottle when poisoned or low
/// (<see cref="BotMend.Draught"/>); a healing spell for a caster that has one, a bandage underneath for anybody, when under
/// <see cref="BotMend.Hurt"/>; the blade drawn when a class that closes has something on top of it and a dry bow put away
/// (<c>BotArms.Suit</c>, <c>BotArms.Check</c>); a step closer when it cannot reach; the best attack spell it can throw
/// (<see cref="BotStrike"/>). The swing itself is the engine's, on Combatant and Warmode.
/// </para>
/// </summary>
public sealed class BotDriver
{
    public BotDriver(BotStandIn me, long now)
    {
        Me = me;
        _castTick = now - BotStrike.CastMs;
        _stepTick = now - BotProving.StepMs;
    }

    public BotStandIn Me { get; }

    public int Casts { get; private set; }

    public int Heals { get; private set; }

    public int Bandages { get; private set; }

    public int Bottles { get; private set; }

    public int Steps { get; private set; }

    private bool _aiming;

    private bool _healing;

    private long _castTick;

    private long _stepTick;

    /// <summary>One beat of this double against this foe.</summary>
    public void Answer(Mobile foe, long now)
    {
        var me = Me;

        if (me is not { Deleted: false, Alive: true } || foe is not { Deleted: false, Alive: true })
        {
            return;
        }

        // A cast of ours has come round to its target: the click a bot has no client to make.
        if (_aiming && me.Target != null)
        {
            _aiming = false;

            if (_healing ? BotMend.Aim(me, me) : BotStrike.Aim(me, foe))
            {
                if (_healing)
                {
                    Heals++;
                }
                else
                {
                    Casts++;
                }
            }

            return;
        }

        var bottle = BotMend.Draught(me);

        if (bottle != null && BotMend.Swallow(me, bottle))
        {
            Bottles++;
        }

        var hurt = BotMend.Share(me) < BotMend.Hurt || me.Poisoned;

        // A caster heals itself by spell when it has one; everybody else winds a bandage on, which goes on underneath
        // whatever else it does.
        if (hurt && me.Spell == null && now - _castTick >= BotStrike.CastMs)
        {
            var mend = BotMend.Spell(me, me);

            if (mend >= 0 && BotMend.Begin(me, mend))
            {
                _aiming = true;
                _healing = true;
                _castTick = now;

                return;
            }
        }

        if (hurt && !BotMend.Winding(me) && BotMend.Cloth(me) > 0 && BotMend.Wind(me, me))
        {
            Bandages++;
        }

        // The hand follows the fight as the bot's does.
        var near = me.InRange(foe.Location, BotSlay.TooClose);
        var dry = me.Weapon is BaseRanged { AmmoType: { } ammo } && (me.Backpack?.GetAmount(ammo) ?? 0) <= 0;

        if (me.Closes || dry)
        {
            me.Draw(melee: dry || near);
        }
        else if (me.Shoots && me.Weapon is not BaseRanged)
        {
            // A shooter caught with a blade in its hand — copied the moment its quiver ran dry — brings the bow back up when
            // there is something to fire, as the bot does (BotArms.Check, "restrung"). Aric the archer fought an orcish mage
            // with a dagger at 02:45 on 26.09.2026 with a bow and arrows in its pack.
            me.Draw(melee: false);
        }

        me.Warmode = true;
        me.Combatant = foe;

        var casting = BotStrike.Can(me);
        var armed = casting && BotStrike.Best(me) >= 0;
        var reach = Math.Max(me.Weapon?.MaxRange ?? 1, armed ? BotStrike.Range : 0);

        if (!me.InRange(foe.Location, reach) || !me.InLOS(foe))
        {
            // One step a beat, the pace a running bot keeps. The field is flat and empty on purpose; nothing here needs a
            // road, and a bow that is walking does not shoot.
            if (now - _stepTick >= BotProving.StepMs)
            {
                _stepTick = now;

                var direction = me.GetDirectionTo(foe, true);
                var before = me.Location;

                me.Direction = direction;

                if (me.Move(direction) && me.Location != before)
                {
                    Steps++;
                }
            }

            return;
        }

        if (!casting || me.Spell != null || now - _castTick < BotStrike.CastMs)
        {
            return;
        }

        var spell = BotStrike.Best(me);

        if (spell >= 0 && BotStrike.Begin(me, spell))
        {
            _aiming = true;
            _healing = false;
            _castTick = now;
        }
    }
}
