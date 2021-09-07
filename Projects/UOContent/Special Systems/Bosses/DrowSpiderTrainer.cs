using System;
using System.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Spells;
using Server.Targeting;

namespace Server.Mobiles
{
    public class DrowSpiderTrainer : BaseCreature
    {
        public static TimeSpan TalkDelay = TimeSpan.FromSeconds(20.0); //the delay between talks is 20 seconds
        public DateTime m_NextTalk;

        [Constructible]
        public DrowSpiderTrainer() : base(AIType.AI_Melee, FightMode.Closest, 12, 1, 0.175, 0.350)
        {
            Name = NameList.RandomName("elven female");
            Title = "the spider trainer";
            Female = true;
            Body = 606;
            Hue = 33840;
            HairItemID = 12240;
            HairHue = 1153;

            SetStr(147, 205);
            SetDex(97, 114);
            SetInt(54, 147);

            SetHits(225, 350);

            SetDamage(1, 4);

            SetSkill(SkillName.MagicResist, 43.4, 60.2);
            SetSkill(SkillName.Ninjitsu, 100.0);
            SetSkill(SkillName.Tactics, 45.6, 54.4);
            SetSkill(SkillName.Wrestling, 50.7, 59.6);

            Circlet circlet = new Circlet();
            BaseRunicTool.ApplyAttributesTo(circlet, 5, 5, 35);
            circlet.Hue = 1157;
            circlet.Movable = true;
            AddItem(circlet);

            WoodlandGorget gorget = new WoodlandGorget();
            BaseRunicTool.ApplyAttributesTo(gorget, 5, 5, 35);
            gorget.Hue = 2075;
            gorget.Movable = true;
            AddItem(gorget);

            Cloak cloak = new Cloak();
            cloak.Movable = true;
            AddItem(cloak);

            StuddedBustierArms bustier = new StuddedBustierArms();
            BaseRunicTool.ApplyAttributesTo(bustier, 5, 5, 35);
            bustier.Hue = 2075;
            bustier.Movable = true;
            AddItem(bustier);

            DragonGloves gloves = new DragonGloves();
            gloves.Hue = 2075;
            gloves.Movable = true;
            AddItem(gloves);

            WildStaff staff = new WildStaff();
            BaseRunicTool.ApplyAttributesTo(staff, 5, 5, 35);
            staff.Hue = 2704;
            staff.Quality = WeaponQuality.Exceptional;
            staff.Movable = true;
            AddItem(staff);

            WoodlandBelt belt = new WoodlandBelt();
            
            belt.Hue = 2075;
            belt.Movable = true;
            AddItem(belt);

            ThighBoots boots = new ThighBoots();
            
            boots.Hue = 1883;
            boots.Movable = true;
            AddItem(boots);

            PackGold(212, 315);

            Fame = 2000;
            Karma = -2000;

            m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.HighScrolls);
            AddLoot(LootPack.Gems, 3);
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }

        public override void OnDamagedBySpell(Mobile from)
        {
            if (from != null && from.Alive && 0.5 > Utility.RandomDouble())
            {
                ThrowLightningBolt(from);
                DoSpecialAbility(from);
                Animate(31, 5, 1, true, false, 0);
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.5 >= Utility.RandomDouble())
            {
                DoSpecialAbility(attacker);
                Animate(31, 5, 1, true, false, 0);
            }

            if (0.5 >= Utility.RandomDouble())
            {
                ThrowLightningBolt(attacker);
                Animate(31, 5, 1, true, false, 0);
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (0.5 >= Utility.RandomDouble())
            {
                DoSpecialAbility(defender);
                Animate(31, 5, 1, true, false, 0);
            }

            if (0.5 >= Utility.RandomDouble())
            {
                ThrowLightningBolt(defender);
                Animate(31, 5, 1, true, false, 0);
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (DateTime.Now >= m_NextTalk && InRange(m, 10) && InLOS(m) && m is PlayerMobile && !m.Hidden && m.Combatant != null) // check if it's time to talk & mobile in range & in los.
            {
                ThrowLightningBolt(m);

                RangePerception = 300;
                this.Combatant = m;

                m_NextTalk = DateTime.Now + TalkDelay; // set next talk time 
                switch (Utility.Random(15))
                {
                    case 0:
                        Say("Take this!");
                        break;
                    case 1:
                        Say("Booyah!");
                        break;
                    case 2:
                        Say("Light 'em up, baby!");
                        break;
                    case 3:
                        Say("You wanna a piece of me! Try this!");
                        break;
                    case 4:
                        Say("Shock treatment!");
                        break;
                    case 5:
                        Say("Eat this!");
                        break;
                    case 6:
                        Say("Lightning Bolt!");
                        break;
                    case 7:
                        Say("PWHOOSH!");
                        break;
                    case 8:
                        Say("Game over!");
                        break;
                    case 9:
                        Say("Swallow this!");
                        break;
                    case 10:
                        Say("Groovy!");
                        break;
                    case 11:
                        Say("It's time to stop!");
                        break;
                    case 12:
                        Say("BOOM!");
                        break;
                    case 13:
                        Say("Hah!");
                        break;
                    case 14:
                        Say("Hiiyah!");
                        break;
                };
            }
        }

        //<*************//Summon Spider Minions
        public void DoSpecialAbility(Mobile target)
        {
            if (0.10 >= Utility.RandomDouble()) // 10% chance to spawn spider minions
                SpawnMobiles(target);
        }

        public void SpawnMobiles(Mobile target)
        {
            Map map = this.Map;

            if (map == null)
                return;

            int red = 0;

            foreach (Mobile m in this.GetMobilesInRange(10))
            {
                if (m is DreadSpider)
                    ++red;
            }

            if (red < 5)
            {
                PlaySound(0x51A);

                int newblue = Utility.RandomMinMax(1, 2);

                for (int i = 0; i < newblue; ++i)
                {
                    BaseCreature yellow;

                    switch (Utility.Random(2))
                    {
                        default:
                        case 0: yellow = new DreadSpider(); break;
                        case 1: yellow = new DreadSpider(); break;
                    }

                    yellow.Team = this.Team;

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

                    yellow.MoveToWorld(loc, map);
                    yellow.Combatant = target;
                }
            }
        }
        //<*************//end summon spider minions

        //////////////////////////////////////////////////// Throw Lightning Bolt ////////////////////////////////////////////////////

        #region Randomize
        private static int[] m_ItemID = new int[]
        {
                        13920
        };

        public static int GetRandomItemID()
        {
            return Utility.RandomList(m_ItemID);
        }

        private DateTime m_NextLightningBolt;
        private int m_Thrown;

        public override void OnActionCombat()
        {
            Mobile combatant = Combatant;

            if (combatant == null || combatant.Deleted || combatant.Map != Map || !InRange(combatant, 12) || !CanBeHarmful(combatant) || !InLOS(combatant))
                return;

            if (DateTime.Now >= m_NextLightningBolt)
            {
                ThrowLightningBolt(combatant);

                m_Thrown++;

                if (0.75 >= Utility.RandomDouble() && (m_Thrown % 2) == 1) // 75% chance to quickly throw another lightning bolt
                    m_NextLightningBolt = DateTime.Now + TimeSpan.FromSeconds(10.0);
                else
                    m_NextLightningBolt = DateTime.Now + TimeSpan.FromSeconds(15.0 + (10.0 * Utility.RandomDouble())); // 15-25 seconds
            }
        }

        public void ThrowLightningBolt(Mobile m)
        {
            this.MovingEffect(m, Utility.RandomList(m_ItemID), 10, 0, false, false);
            this.DoHarmful(m);
            this.PlaySound(0x20A); // energy bolt

            new InternalTimer(m, this).Start();
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
                m_Mobile.BoltEffect(0x480);
                m_Mobile.PlaySound(0x5CE); // lightning strike
                m_Mobile.Hits -= (Utility.Random(5, 15));
            }
        }
        #endregion

        private DateTime m_NextAbilityTime;

        public override void OnThink()
        {
            if (DateTime.Now >= m_NextAbilityTime)
            {
                DreadSpider toBuff = null;

                foreach (Mobile m in this.GetMobilesInRange(8))
                {
                    if (m is DreadSpider && IsFriend(m) && m.Combatant != null && CanBeBeneficial(m) && m.CanBeginAction(typeof(DrowSpiderTrainer)) && InLOS(m))
                    {
                        toBuff = (DreadSpider)m;
                        break;
                    }
                }

                if (toBuff != null)
                {
                    if (CanBeBeneficial(toBuff) && toBuff.BeginAction(typeof(DrowSpiderTrainer)))
                    {
                        m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));

                        toBuff.Say(true, "Oooga Booga!");
                        this.Say(true, "Purge this jungle of the uncleansed!");

                        DoBeneficial(toBuff);

                        object[] state = new object[] { toBuff, toBuff.HitsMaxSeed, toBuff.RawStr, toBuff.RawDex };

                        SpellHelper.Turn(this, toBuff);

                        int toScale = toBuff.HitsMaxSeed;

                        if (toScale > 0)
                        {
                            toBuff.HitsMaxSeed += AOS.Scale(toScale, 100);
                            toBuff.Hits += AOS.Scale(toScale, 100);
                        }

                        toScale = toBuff.RawStr;

                        if (toScale > 0)
                            toBuff.RawStr += AOS.Scale(toScale, 10);

                        toScale = toBuff.RawDex;

                        if (toScale > 0)
                        {
                            toBuff.RawDex += AOS.Scale(toScale, 10);
                            toBuff.Stam += AOS.Scale(toScale, 100);
                        }

                        toBuff.Hits = toBuff.Hits;
                        toBuff.Stam = toBuff.Stam;

                        toBuff.FixedParticles(0x375A, 10, 15, 5017, EffectLayer.Waist);
                        toBuff.PlaySound(0x1EE);

                        Timer.DelayCall(TimeSpan.FromSeconds(20.0), () => Unbuff(state));
                    }
                }
                else
                {
                    m_NextAbilityTime = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
                }
            }

            base.OnThink();
        }

        private void Unbuff(object state)
        {
            object[] states = (object[])state;

            DreadSpider toDebuff = (DreadSpider)states[0];

            toDebuff.EndAction(typeof(DrowSpiderTrainer));

            if (toDebuff.Deleted)
                return;

            toDebuff.HitsMaxSeed = (int)states[1];
            toDebuff.RawStr = (int)states[2];
            toDebuff.RawDex = (int)states[3];

            toDebuff.Hits = toDebuff.Hits;
            toDebuff.Stam = toDebuff.Stam;
        }

        public DrowSpiderTrainer(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
