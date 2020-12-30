using System;
using Server.Network;

namespace Server.Items
{
    public class BaseShield : BaseArmor
    {
        public BaseShield(int itemID) : base(itemID)
        {
        }

        public BaseShield(Serial serial) : base(serial)
        {
        }

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

        public override double ArmorRating
        {
            get
            {
                var m = Parent as Mobile;
                var ar = base.ArmorRating;

                if (m != null)
                {
                    return m.Skills.Parry.Value * ar / 200.0 + 1.0;
                }

                return ar;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                if (this is Aegis)
                {
                    return;
                }

                // The 15 bonus points to resistances are not applied to shields on OSI.
                PhysicalBonus = 0;
                FireBonus = 0;
                ColdBonus = 0;
                PoisonBonus = 0;
                EnergyBonus = 0;
            }
        }

        public override int OnHit(BaseWeapon weapon, int damage)
        {
            if (Core.AOS)
            {
                if (ArmorAttributes.SelfRepair > Utility.Random(10))
                {
                    HitPoints += 2;
                }
                else
                {
                    var halfArmor = ArmorRating / 2.0;
                    var absorbed = (int)(halfArmor + halfArmor * Utility.RandomDouble());

                    if (absorbed < 2)
                    {
                        absorbed = 2;
                    }

                    int wear;

                    if (weapon.Type == WeaponType.Bashing)
                    {
                        wear = absorbed / 2;
                    }
                    else
                    {
                        wear = Utility.Random(2);
                    }

                    if (wear > 0 && MaxHitPoints > 0)
                    {
                        if (HitPoints >= wear)
                        {
                            HitPoints -= wear;
                            wear = 0;
                        }
                        else
                        {
                            wear -= HitPoints;
                            HitPoints = 0;
                        }

                        if (wear > 0)
                        {
                            if (MaxHitPoints > wear)
                            {
                                MaxHitPoints -= wear;

                                if (Parent is Mobile mobile)
                                {
                                    mobile.LocalOverheadMessage(
                                        MessageType.Regular,
                                        0x3B2,
                                        1061121
                                    ); // Your equipment is severely damaged.
                                }
                            }
                            else
                            {
                                Delete();
                            }
                        }
                    }
                }

                return 0;
            }

            if (!(Parent is Mobile owner))
            {
                return damage;
            }

            var ar = ArmorRating;
            var chance = (owner.Skills.Parry.Value - ar * 2.0) / 100.0;

            if (chance < 0.01)
            {
                chance = 0.01;
            }

            /*
            FORMULA: Displayed AR = ((Parrying Skill * Base AR of Shield) � 200) + 1

            FORMULA: % Chance of Blocking = parry skill - (shieldAR * 2)

            FORMULA: Melee Damage Absorbed = (AR of Shield) / 2 | Archery Damage Absorbed = AR of Shield
            */
            if (owner.CheckSkill(SkillName.Parry, chance))
            {
                damage -= Math.Min(damage, weapon.Skill == SkillName.Archery ? (int)ar : (int)(ar / 2.0));

                owner.FixedEffect(0x37B9, 10, 16);

                if (Utility.Random(100) < 25) // 25% chance to lower durability
                {
                    var wear = Utility.Random(2);

                    if (wear > 0 && MaxHitPoints > 0)
                    {
                        if (HitPoints >= wear)
                        {
                            HitPoints -= wear;
                            wear = 0;
                        }
                        else
                        {
                            wear -= HitPoints;
                            HitPoints = 0;
                        }

                        if (wear > 0)
                        {
                            if (MaxHitPoints > wear)
                            {
                                MaxHitPoints -= wear;

                                ((Mobile)Parent).LocalOverheadMessage(
                                    MessageType.Regular,
                                    0x3B2,
                                    1061121
                                ); // Your equipment is severely damaged.
                            }
                            else
                            {
                                Delete();
                            }
                        }
                    }
                }
            }

            return damage;
        }
    }
}
