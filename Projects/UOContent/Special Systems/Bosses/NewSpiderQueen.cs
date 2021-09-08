using System;
using System.Collections;
using System.Collections.Generic;
using EvolutionPetSystem;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Special_Systems.EvolutionPetSystem.Abilities;
using Server.Targeting;

namespace Server.Mobiles
{
    [CorpseName("a spider queen corpse")]
    public class NewSpiderQueen : BaseCreature
    {
        public override WeaponAbility GetWeaponAbility()
        {
            return WeaponAbility.DoubleStrike;
        }

        [Constructible]
        public NewSpiderQueen() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.1)
        {
            Name = "a spider queen";
            Body = 0xAD;
            BaseSoundID = 0x388; // TODO: validate

            SetStr(796, 825);
            SetDex(86, 105);
            SetInt(436, 475);

            SetHits(3000, 3800);

            SetDamage(16, 22);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 60, 70);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.EvalInt, 30.1, 40.0);
            SetSkill(SkillName.Magery, 30.1, 40.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 92.5);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 24;

            PackItem(new SpidersSilk(200));
            PackItem(new LesserPoisonPotion());
            PackItem(new LesserPoisonPotion());
            if (Utility.RandomDouble() < 0.25)
                PackItem(new CureScroll());
        }

        public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            if (from != null && from != this)
            {
                if (from is PlayerMobile)
                {
                    PlayerMobile p_PlayerMobile = from as PlayerMobile;
                    Item weapon1 = p_PlayerMobile.FindItemOnLayer(Layer.OneHanded);
                    Item weapon2 = p_PlayerMobile.FindItemOnLayer(Layer.TwoHanded);

                    if (weapon1 != null)
                    {
                        if (weapon1 is BaseBashing)
                        {
                            damage *= 2;
                        }
                        else if (weapon1 is BaseStaff)
                        {
                            damage *= 4;
                        }
                        else
                        {
                            damage += 0;
                        }
                    }
                    else if (weapon2 != null)
                    {
                        if (weapon2 is BaseBashing)
                        {
                            damage *= 2;
                        }
                        else if (weapon2 is BaseStaff)
                        {
                            damage *= 4;
                        }
                        else
                        {
                            damage += 0;
                        }
                    }
                }
            }
        }

        public override int GetIdleSound() { return 1605; }
        public override int GetAngerSound() { return 1602; }
        public override int GetHurtSound() { return 1604; }
        public override int GetDeathSound() { return 1603; }

        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Arachnid; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }

        private DateTime m_NextAttack;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);
            c.DropItem(new SpiderDust());

            if (0.10 > Utility.RandomDouble())
            {
                switch (Utility.Random(2))
                {
                    case 0:
                        c.DropItem(new SpiderEgg());
                        break;
                    case 1:
                        c.DropItem(new RageAbilityScroll());
                        break;
                    default:
                        c.DropItem(new SpiderEgg());
                        break;


                }
            }

            
        }

        public override void OnActionCombat()
        {
            Mobile combatant = Combatant;

            if (combatant == null || combatant.Deleted || combatant.Map != Map || !InRange(combatant, 12) || !CanBeHarmful(combatant) || !InLOS(combatant))
                return;

            if (DateTime.Now >= m_NextAttack)
            {
                SandAttack(combatant);
                m_NextAttack = DateTime.Now + TimeSpan.FromSeconds(10.0 + (10.0 * Utility.RandomDouble()));
            }
        }

        public void SandAttack(Mobile m)
        {
            DoHarmful(m);

            m.FixedParticles(0x36B0, 10, 25, 9540, 2413, 0, EffectLayer.Waist);

            new InternalTimer(m, this).Start();
        }

        public void SpawnGiantSpider(Mobile m)
        {
            Map map = this.Map;

            if (map == null)
                return;

            DreadSpider spawned = new DreadSpider();

            spawned.Team = this.Team;

            bool validLocation = false;
            Point3D loc = this.Location;

            for (int j = 0; !validLocation && j < 10; ++j)
            {
                int x = X + Utility.Random(3) - 1;
                int y = Y + Utility.Random(3) - 1;
                int z = map.GetAverageZ(x, y);

                if (validLocation = map.CanFit(x, y, this.Z, 16, false, false))
                    loc = new Point3D(x, y, Z);
                else if (validLocation = map.CanFit(x, y, z, 16, false, false))
                    loc = new Point3D(x, y, z);
            }

            spawned.MoveToWorld(loc, map);
            spawned.Combatant = m;
        }

        public void EatGiantSpider()
        {
            ArrayList toEat = new ArrayList();

            foreach (Mobile m in this.GetMobilesInRange(2))
            {
                if (m is DreadSpider)
                    toEat.Add(m);
            }

            if (toEat.Count > 0)
            {
                PlaySound(Utility.Random(0x3B, 2)); // Eat sound

                foreach (Mobile m in toEat)
                {
                    Hits += (m.Hits / 2);
                    m.Delete();
                }
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (this.Hits > (this.HitsMax / 4))
            {
                if (0.25 >= Utility.RandomDouble())
                    SpawnGiantSpider(attacker);
            }
            else if (0.25 >= Utility.RandomDouble())
            {
                EatGiantSpider();
            }
        }

        private class InternalTimer : Timer
        {
            private Mobile m_Mobile, m_From;

            public InternalTimer(Mobile m, Mobile from) : base(TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_From = from;

            }

            protected override void OnTick()
            {
                m_Mobile.PlaySound(0x4CF);
                AOS.Damage(m_Mobile, m_From, Utility.RandomMinMax(1, 50), 90, 10, 0, 0, 0);
            }
        }

        public override void OnCarve(Mobile from, Corpse corpse, Item with)
        {
            if (corpse.Carved == false)
            {
                base.OnCarve(from, corpse, with);

                corpse.AddCarvedItem(new Gold(Utility.RandomMinMax(97, 158)), from);

                from.SendMessage("You carve up some gold.");
                corpse.Carved = true;
            }
        }

        public NewSpiderQueen(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
