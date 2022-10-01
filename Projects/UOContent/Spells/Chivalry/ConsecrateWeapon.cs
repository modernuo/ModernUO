using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Chivalry
{
    public class ConsecrateWeaponSpell : PaladinSpell
    {
        private static readonly SpellInfo _info = new(
            "Consecrate Weapon",
            "Consecrus Arma",
            -1,
            9002
        );

        private static readonly Dictionary<BaseWeapon, ExpireTimer> _table = new();

        public ConsecrateWeaponSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.5);

        public override double RequiredSkill => 15.0;
        public override int RequiredMana => 10;
        public override int RequiredTithing => 10;
        public override int MantraNumber => 1060720; // Consecrus Arma
        public override bool BlocksMovement => false;

        public override void OnCast()
        {
            if (Caster.Weapon is not (BaseWeapon weapon and not Fists))
            {
                Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
            }
            else if (CheckSequence())
            {
                /* Temporarily enchants the weapon the caster is currently wielding.
                 * The type of damage the weapon inflicts when hitting a target will
                 * be converted to the target's worst Resistance type.
                 * Duration of the effect is affected by the caster's Karma and lasts for 3 to 11 seconds.
                 */

                int itemID, soundID;

                switch (weapon.Skill)
                {
                    case SkillName.Macing:
                        {
                            itemID = 0xFB4;
                            soundID = 0x232;
                            break;
                        }
                    case SkillName.Archery:
                        {
                            itemID = 0x13B1;
                            soundID = 0x145;
                            break;
                        }
                    default:
                        {
                            itemID = 0xF5F;
                            soundID = 0x56;
                            break;
                        }
                }

                Caster.PlaySound(0x20C);
                Caster.PlaySound(soundID);
                Caster.FixedParticles(0x3779, 1, 30, 9964, 3, 3, EffectLayer.Waist);

                IEntity from = new Entity(Serial.Zero, new Point3D(Caster.X, Caster.Y, Caster.Z), Caster.Map);
                IEntity to = new Entity(Serial.Zero, new Point3D(Caster.X, Caster.Y, Caster.Z + 50), Caster.Map);
                Effects.SendMovingParticles(
                    from,
                    to,
                    itemID,
                    1,
                    0,
                    false,
                    false,
                    33,
                    3,
                    9501,
                    1,
                    0,
                    EffectLayer.Head,
                    0x100
                );

                var seconds = Math.Clamp(ComputePowerValue(20), 3.0, 11.0);

                var duration = TimeSpan.FromSeconds(seconds);

                _table.TryGetValue(weapon, out var timer);
                timer?.Stop();

                weapon.Consecrated = true;

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
                _weapon.Consecrated = false;
                Effects.PlaySound(_weapon.GetWorldLocation(), _weapon.Map, 0x1F8);
                _table.Remove(_weapon);
            }
        }
    }
}
