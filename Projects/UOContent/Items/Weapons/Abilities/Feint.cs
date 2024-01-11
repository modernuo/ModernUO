using System;
using System.Collections.Generic;

namespace Server.Items;

/// <summary>
///     Gain a defensive advantage over your primary opponent for a short time.
/// </summary>
public class Feint : WeaponAbility
{
    public static Dictionary<Mobile, FeintTimer> Registry { get; } = new();

    public override int BaseMana => 30;
    public override bool RequiresSecondarySkill(Mobile from) => true;

    public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
    {
        if (!Validate(attacker) || !CheckMana(attacker, true))
        {
            return;
        }

        if (Registry.Remove(defender, out var timer))
        {
            timer.Stop();
        }

        ClearCurrentAbility(attacker);

        attacker.SendLocalizedMessage(1063360); // You baffle your target with a feint!
        defender.SendLocalizedMessage(1063361); // You were deceived by an attacker's feint!

        attacker.FixedParticles(0x3728, 1, 13, 0x7F3, 0x962, 0, EffectLayer.Waist);

        var skill = Math.Max(attacker.Skills.Ninjitsu.Value, attacker.Skills.Bushido.Value);

        // 20-50 % decrease in damage taken for 6 seconds
        timer = new FeintTimer(
            attacker,
            defender,
            (int)(20.0 + 3.0 * (skill - 50.0) / 7.0)
        );

        timer.Start();
        Registry.Add(defender, timer);

        // TODO: Add buff icon (Publish 100)
    }

    public static bool GetDamageReduction(Mobile attacker, Mobile defender, out int damageReduction)
    {
        if (Registry.TryGetValue(attacker, out var timer) && timer.Defender == defender)
        {
            damageReduction = timer.DamageReduction;
            return true;
        }

        damageReduction = 0;
        return false;
    }

    public class FeintTimer : Timer
    {
        private readonly Mobile _attacker;
        public Mobile Defender { get; }

        public FeintTimer(Mobile attacker, Mobile defender, int damageReduction) : base(TimeSpan.FromSeconds(6.0))
        {
            _attacker = attacker;
            Defender = defender;
            DamageReduction = damageReduction;
        }

        public int DamageReduction { get; }

        protected override void OnTick() => Registry.Remove(_attacker);
    }
}
