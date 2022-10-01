using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Necromancy
{
    public class CurseWeaponSpell : NecromancerSpell
    {
        private static readonly SpellInfo _info = new(
            "Curse Weapon",
            "An Sanct Gra Char",
            203,
            9031,
            Reagent.PigIron
        );

        private static readonly Dictionary<BaseWeapon, ExpireTimer> _table = new();

        public CurseWeaponSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.75);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => 7;

        public override void OnCast()
        {
            if (Caster.Weapon is not BaseWeapon weapon || weapon is Fists)
            {
                Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
            }
            else if (CheckSequence())
            {
                /* Temporarily imbues a weapon with a life draining effect.
                 * Half the damage that the weapon inflicts is added to the necromancer's health.
                 * The effects lasts for (Spirit Speak skill level / 34) + 1 seconds.
                 *
                 * NOTE: Above algorithm is fixed point, should be :
                 * (Spirit Speak skill level / 3.4) + 1
                 *
                 * TODO: What happens if you curse a weapon then give it to someone else? Should they get the drain effect?
                 */

                Caster.PlaySound(0x387);
                Caster.FixedParticles(0x3779, 1, 15, 9905, 32, 2, EffectLayer.Head);
                Caster.FixedParticles(0x37B9, 1, 14, 9502, 32, 5, (EffectLayer)255);
                Timer.StartTimer(TimeSpan.FromSeconds(0.75), () => Caster.PlaySound(0xFA));

                var duration = TimeSpan.FromSeconds(Caster.Skills.SpiritSpeak.Value / 3.4 + 1.0);

                _table.TryGetValue(weapon, out var timer);
                timer?.Stop();

                weapon.Cursed = true;
                _table[weapon] = timer = new ExpireTimer(weapon, duration);

                timer.Start();
            }

            FinishSequence();
        }

        private class ExpireTimer : Timer
        {
            private BaseWeapon _weapon;

            public ExpireTimer(BaseWeapon weapon, TimeSpan delay) : base(delay) => _weapon = weapon;

            protected override void OnTick()
            {
                _weapon.Cursed = false;
                Effects.PlaySound(_weapon.GetWorldLocation(), _weapon.Map, 0xFA);
                _table.Remove(_weapon);
            }
        }
    }
}
