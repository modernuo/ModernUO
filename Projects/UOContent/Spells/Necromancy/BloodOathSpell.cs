using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy;

public class BloodOathSpell : NecromancerSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Blood Oath",
        "In Jux Mani Xen",
        203,
        9031,
        Reagent.DaemonBlood
    );

    // Keyed by BOTH participants (caster and target) -> shared timer, so the oath resolves and
    // removes from either side. Required so the death/delete events can break it from either mobile.
    private static readonly Dictionary<Mobile, ExpireTimer> _table = new();

    public BloodOathSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

    public override double RequiredSkill => 20.0;
    public override int RequiredMana => 13;

    public void Target(Mobile m)
    {
        if (m == null)
        {
            Caster.SendLocalizedMessage(1060508); // You can't curse that.
        }
        // only PlayerMobile and BaseCreature implement blood oath checking
        else if (Caster == m || m is not (PlayerMobile or BaseCreature))
        {
            Caster.SendLocalizedMessage(1060508); // You can't curse that.
        }
        else if (_table.ContainsKey(Caster))
        {
            Caster.SendLocalizedMessage(1061607); // You are already bonded in a Blood Oath.
        }
        else if (_table.ContainsKey(m))
        {
            if (m.Player)
            {
                Caster.SendLocalizedMessage(1061608); // That player is already bonded in a Blood Oath.
            }
            else
            {
                Caster.SendLocalizedMessage(1061609); // That creature is already bonded in a Blood Oath.
            }
        }
        else if (CheckHSequence(m))
        {
            SpellHelper.Turn(Caster, m);

            /* Temporarily creates a dark pact between the caster and the target.
             * Any damage dealt by the target to the caster is increased, but the target receives the same amount of damage.
             * The effect lasts for ((Spirit Speak skill level - target's Resist Magic skill level) / 8) + 8 seconds.
             *
             * NOTE: The in-game tooltip (and UOGuide) display /80 due to a fixed-point bug.
             * The actual OSI formula is /8, matching RunUO/ServUO.
             */

            m.Spell?.OnCasterHurt();

            Caster.PlaySound(0x175);

            Caster.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
            Caster.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);

            m.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
            m.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);

            var duration = TimeSpan.FromSeconds(GetDurationSeconds(GetDamageSkill(Caster), GetResistSkill(m)));
            m.CheckSkill(SkillName.MagicResist, 0.0, 120.0); // Skill check for gain

            RegisterOath(Caster, m, duration);

            HarmfulSpell(m);
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }

    // ((Spirit Speak - Resisting Spells) / 8) + 8 seconds.
    internal static double GetDurationSeconds(double damageSkill, double resistSkill) =>
        (damageSkill - resistSkill) / 8 + 8;

    // The attacker takes the original (un-bonused) damage reflected back. Publish 48 lets the
    // attacker's Resisting Spells reduce the reflected damage, but only against creature casters.
    internal static int ComputeReflectedDamage(int originalDamage, double attackerMagicResist, bool applyResistMitigation)
    {
        if (!applyResistMitigation)
        {
            return originalDamage;
        }

        // ((Resisting Spells * 10) / 20) + 10 = percentage of damage resisted
        var resisted = (attackerMagicResist * 0.5 + 10) / 100;
        return (int)(originalDamage * (1.0 - resisted));
    }

    internal static void RegisterOath(Mobile caster, Mobile target, TimeSpan duration)
    {
        var timer = new ExpireTimer(caster, target, duration);
        _table[caster] = timer;
        _table[target] = timer;
        timer.Start();

        (caster as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.BloodOathCaster, 1075659, duration, target.Name));
        (target as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.BloodOathCurse, 1075661, duration, caster.Name));
    }

    public static bool RemoveCurse(Mobile m)
    {
        if (m == null || !_table.TryGetValue(m, out var timer))
        {
            return false;
        }

        var caster = timer.Caster;
        var target = timer.Target;

        timer.Stop();
        _table.Remove(caster);
        _table.Remove(target);

        caster.SendLocalizedMessage(1061620); // Your Blood Oath has been broken.
        target.SendLocalizedMessage(1061620); // Your Blood Oath has been broken.

        (caster as PlayerMobile)?.RemoveBuff(BuffIcon.BloodOathCaster);
        (target as PlayerMobile)?.RemoveBuff(BuffIcon.BloodOathCurse);

        return true;
    }

    public static Mobile GetBloodOath(Mobile m) =>
        m != null && _table.TryGetValue(m, out var timer) && timer.Target == m ? timer.Caster : null;

    // Death or deletion of either participant breaks the oath immediately. RemoveCurse resolves the
    // shared timer from either the caster or the target key, so a single call per mobile is enough.
    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void OnCurseEnds(Mobile m) => RemoveCurse(m);

    private class ExpireTimer : Timer
    {
        public Mobile Caster { get; }
        public Mobile Target { get; }

        // Single-shot: fire once when the oath expires. Death or deletion of either party is
        // handled separately by the OnCurseEnds event handler.
        public ExpireTimer(Mobile caster, Mobile target, TimeSpan delay) : base(delay)
        {
            Caster = caster;
            Target = target;
        }

        protected override void OnTick() => RemoveCurse(Target);
    }
}
