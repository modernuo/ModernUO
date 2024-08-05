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

        private static readonly Dictionary<Mobile, ConsecratedWeaponContext> _table = new Dictionary<Mobile, ConsecratedWeaponContext>();

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

                ConsecratedWeaponContext context;

                if (IsUnderEffects(Caster))
                {
                    context = _table[Caster];

                    if (context.Timer != null)
                    {
                        context.Timer.Stop();
                        context.Timer = null;
                    }

                    context.Weapon = weapon;
                }
                else
                {
                    context = new ConsecratedWeaponContext(Caster, weapon);
                }

                weapon.ConsecratedContext = context;
                context.Timer = Timer.DelayCall(duration, RemoveEffects, Caster);

                _table[Caster] = context;

                BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.ConsecrateWeapon, 1151385, 1151386, duration, Caster, string.Format("{0}\t{1}", context.ConsecrateProcChance, context.ConsecrateDamageBonus)));


                //_table.TryGetValue(weapon, out var timer);
                //timer?.Stop();

                //weapon.Consecrated = true;

                //_table[weapon] = timer = new ExpireTimer(weapon, duration);

                //timer.Start();

                //BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.ConsecrateWeapon, 1151385, 1151386, duration, Caster, string.Empty/*string.Format("{0}\t{1}"/*, context.ConsecrateProcChance, context.ConsecrateDamageBonus))*/));
            }

            FinishSequence();
        }

        public static void RemoveEffects(Mobile m)
        {
            if (_table.ContainsKey(m))
            {
                ConsecratedWeaponContext context = _table[m];

                context.Expire();

                _table.Remove(m);
            }
        }

        public static bool IsUnderEffects(Mobile m) => _table.ContainsKey(m);
    }

    public class ConsecratedWeaponContext
    {
        public Mobile Owner { get; private set; }
        public BaseWeapon Weapon { get; set; }

        public Timer Timer { get; set; }

        public int ConsecrateProcChance
        {
            get
            {
                if (Owner.Skills.Chivalry.Value >= 80)
                {
                    return 100;
                }

                return (int)Owner.Skills.Chivalry.Value;
            }
        }

        public int ConsecrateDamageBonus
        {
            get
            {
                double value = Owner.Skills.Chivalry.Value;

                if (value >= 90)
                {
                    return (int)Math.Truncate((value - 90) / 2);
                }

                return 0;
            }
        }

        public ConsecratedWeaponContext(Mobile owner, BaseWeapon weapon)
        {
            Owner = owner;
            Weapon = weapon;
        }

        public void Expire()
        {
            Weapon.ConsecratedContext = null;

            Effects.PlaySound(Weapon.GetWorldLocation(), Weapon.Map, 0x1F8);

            if (Timer != null)
            {
                Timer.Stop();
                Timer = null;
            }
        }
    }
}
