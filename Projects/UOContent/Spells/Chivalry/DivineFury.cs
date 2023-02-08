using System;
using System.Collections.Generic;

namespace Server.Spells.Chivalry;

public class DivineFurySpell : PaladinSpell
{
    private static readonly SpellInfo _info = new(
        "Divine Fury",
        "Divinum Furis",
        -1,
        9002
    );

    private static readonly Dictionary<Mobile, InternalTimer> _table = new();

    public DivineFurySpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

    public override double RequiredSkill => 25.0;
    public override int RequiredMana => 15;
    public override int RequiredTithing => 10;
    public override int MantraNumber => 1060722; // Divinum Furis
    public override bool BlocksMovement => false;

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.PlaySound(0x20F);
            Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);
            Caster.FixedParticles(0x376A, 1, 31, 9961, 1160, 0, EffectLayer.Waist);
            Caster.FixedParticles(0x37C4, 1, 31, 9502, 43, 2, EffectLayer.Waist);

            Caster.Stam = Caster.StamMax;

            RemoveTimer(Caster);

            var delay = TimeSpan.FromSeconds(Math.Clamp(ComputePowerValue(10), 7, 24));

            var timer = Core.HS && Caster.Skills.Chivalry.Value >= 120 && Caster.Karma >= 10000
                ? new InternalTimer(Caster, delay, 15, 20, 15, 10)
                : new InternalTimer(Caster, delay, 10, 10, 10, 20);
            timer.Start();

            _table[Caster] = timer;

            Caster.Delta(MobileDelta.WeaponDamage);

            if (Core.HS) // Publish 71 with boost
            {
                BuffInfo.AddBuff(
                    Caster,
                    new BuffInfo(
                        BuffIcon.DivineFury,
                        1060589,
                        1150218,
                        delay,
                        Caster,
                        $"{timer.AttackBonus}\t{timer.DamageBonus}\t{timer.WeaponSpeed}\t{timer.DefendMalus}"
                    )
                );
            }
            else
            {
                BuffInfo.AddBuff(
                    Caster,
                    new BuffInfo(BuffIcon.DivineFury, 1060589, 1075634, delay, Caster)
                );
            }
        }

        FinishSequence();
    }

    public static int GetAttackBonus(Mobile m) => _table.TryGetValue(m, out var timer) ? timer.AttackBonus : 0;

    public static int GetDamageBonus(Mobile m) => _table.TryGetValue(m, out var timer) ? timer.DamageBonus : 0;

    public static int GetWeaponSpeed(Mobile m) => _table.TryGetValue(m, out var timer) ? timer.WeaponSpeed : 0;

    public static int GetDefendMalus(Mobile m) => _table.TryGetValue(m, out var timer) ? timer.DefendMalus : 0;

    public static void GetBonuses(
        Mobile m, out int attackBonus, out int damageBonus, out int weaponSpeed, out int defendMalus
    )
    {
        if (_table.TryGetValue(m, out var timer))
        {
            damageBonus = timer.DamageBonus;
            weaponSpeed = timer.WeaponSpeed;
            attackBonus = timer.AttackBonus;
            defendMalus = timer.DefendMalus;
            return;
        }

        damageBonus = 0;
        weaponSpeed = 0;
        attackBonus = 0;
        defendMalus = 0;
    }

    private static void RemoveTimer(Mobile m)
    {
        if (_table.Remove(m, out var timer))
        {
            timer.Stop();
        }
    }

    public static void StopDivineFury(Mobile m)
    {
        RemoveTimer(m);
        m.Delta(MobileDelta.WeaponDamage);
        m.PlaySound(0xF8);
        BuffInfo.RemoveBuff(m, BuffIcon.DivineFury);
    }

    public static bool UnderEffect(Mobile m) => _table.ContainsKey(m);

    private class InternalTimer : Timer
    {
        private Mobile _mobile;
        public int DamageBonus { get; }
        public int WeaponSpeed { get; }
        public int AttackBonus { get; }
        public int DefendMalus { get; }

        public InternalTimer(Mobile m, TimeSpan delay, int attackBonus, int damageBonus, int weaponSpeed, int defendMalus)
            : base(delay)
        {
            DamageBonus = damageBonus;
            WeaponSpeed = weaponSpeed;
            AttackBonus = attackBonus;
            DefendMalus = defendMalus;
            _mobile = m;
        }

        protected override void OnTick() => StopDivineFury(_mobile);
    }
}
