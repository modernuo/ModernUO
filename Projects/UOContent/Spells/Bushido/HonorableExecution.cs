using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Spells.Bushido;

public class HonorableExecution : SamuraiMove
{
    private static readonly Dictionary<Mobile, HonorableExecutionTimer> _table = new();

    public override int BaseMana => 0;
    public override double RequiredSkill => 25.0;

    // You better kill your enemy with your next hit or you'll be rather sorry...
    public override TextDefinition AbilityMessage { get; } = 1063122;

    public override double GetDamageScalar(Mobile attacker, Mobile defender) =>
        // TODO: 20 -> Perfection
        1.0 + attacker.Skills.Bushido.Value * 20 / 10000;

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
        if (!Validate(attacker) || !CheckMana(attacker, true))
        {
            return;
        }

        ClearCurrentMove(attacker);
        RemovePenalty(attacker);

        HonorableExecutionTimer timer;

        if (!defender.Alive)
        {
            attacker.FixedParticles(0x373A, 1, 17, 0x7E2, EffectLayer.Waist);

            var bushido = attacker.Skills.Bushido.Value;
            bushido *= bushido;

            attacker.Hits += 20 + (int)(bushido / 480.0);

            var swingBonus = Math.Max(1, (int)(bushido / 720.0));

            timer = new HonorableExecutionTimer(TimeSpan.FromSeconds(20.0), attacker, swingBonus: swingBonus);

            (attacker as PlayerMobile)?.AddBuff(
                new BuffInfo(
                    BuffIcon.HonorableExecution,
                    1060595,
                    1153807,
                    TimeSpan.FromSeconds(20.0),
                    $"{swingBonus}"
                )
            );
        }
        else
        {
            attacker.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, "PhysicalResistHonorableExecution", -40));
            attacker.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, "FireResistHonorableExecution", -40));
            attacker.AddResistanceMod(new ResistanceMod(ResistanceType.Cold, "ColdResistHonorableExecution", -40));
            attacker.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, "PoisonResistHonorableExecution", -40));
            attacker.AddResistanceMod(new ResistanceMod(ResistanceType.Energy, "EnergyResistHonorableExecution", -40));

            var resSpells = attacker.Skills.MagicResist.Value;

            if (resSpells > 0.0)
            {
                attacker.AddSkillMod(new DefaultSkillMod(SkillName.MagicResist, "MagicResistHonorableExecution", true, -resSpells));
            }

            timer = new HonorableExecutionTimer(TimeSpan.FromSeconds(7.0), attacker, penalty: true);

            if (Core.HS)
            {
                (attacker as PlayerMobile)?.AddBuff(
                    new BuffInfo(
                        BuffIcon.HonorableExecution,
                        1060595,
                        1153808,
                        TimeSpan.FromSeconds(7.0),
                        $"{resSpells}\t{40}\t{40}\t{40}\t{40}\t{40}"
                    )
                );
            }
            else
            {
                (attacker as PlayerMobile)?.AddBuff(
                    new BuffInfo(BuffIcon.HonorableExecution, 1060595, TimeSpan.FromSeconds(7.0))
                );
            }
        }

        _table[attacker] = timer;
        timer.Start();

        attacker.Delta(MobileDelta.WeaponDamage);
        CheckGain(attacker);
    }

    public static int GetSwingBonus(Mobile target) => _table.TryGetValue(target, out var info) ? info._swingBonus : 0;

    public static bool IsUnderPenalty(Mobile target) => _table.TryGetValue(target, out var info) && info._penalty;

    public static void RemovePenalty(Mobile target)
    {
        if (_table.Remove(target, out var timer))
        {
            timer.Clear();
        }
    }

    private class HonorableExecutionTimer : Timer
    {
        public readonly Mobile _from;
        public readonly bool _penalty;
        public readonly int _swingBonus;

        public HonorableExecutionTimer(TimeSpan duration, Mobile from, int swingBonus = 0, bool penalty = false) : base(duration)
        {
            _from = from;
            _swingBonus = swingBonus;
            _penalty = penalty;
        }

        protected override void OnTick()
        {
            _from?.Delta(MobileDelta.WeaponDamage);
            RemovePenalty(_from);
        }

        public void Clear()
        {
            Stop();

            _from.RemoveResistanceMod("PhysicalResistHonorableExecution");
            _from.RemoveResistanceMod("FireResistHonorableExecution");
            _from.RemoveResistanceMod("ColdResistHonorableExecution");
            _from.RemoveResistanceMod("PoisonResistHonorableExecution");
            _from.RemoveResistanceMod("EnergyResistHonorableExecution");
            _from.RemoveSkillMod("MagicResistHonorableExecution");
        }
    }
}
