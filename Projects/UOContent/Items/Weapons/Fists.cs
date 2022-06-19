using System;
using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Fists : BaseMeleeWeapon
    {
        public Fists() : base(0)
        {
            Visible = false;
            Movable = false;
            Quality = WeaponQuality.Regular;
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.Disarm;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

        public override int AosStrengthReq => 0;
        public override int AosMinDamage => 1;
        public override int AosMaxDamage => 4;
        public override int AosSpeed => 50;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 0;
        public override int OldMinDamage => 1;
        public override int OldMaxDamage => 8;
        public override int OldSpeed => 30;

        public override int DefHitSound => -1;
        public override int DefMissSound => -1;

        public override SkillName DefSkill => SkillName.Wrestling;
        public override WeaponType DefType => WeaponType.Fists;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Wrestle;

        public static void Initialize()
        {
            Mobile.DefaultWeapon = new Fists();

            EventSink.DisarmRequest += EventSink_DisarmRequest;
            EventSink.StunRequest += EventSink_StunRequest;
        }

        public override double GetDefendSkillValue(Mobile attacker, Mobile defender)
        {
            var wresValue = defender.Skills.Wrestling.Value;
            var anatValue = defender.Skills.Anatomy.Value;
            var evalValue = defender.Skills.EvalInt.Value;
            var incrValue = Math.Min((anatValue + evalValue + 20.0) * 0.5, 120.0);

            return wresValue > incrValue ? wresValue : incrValue;
        }

        private void CheckPreAOSMoves(Mobile attacker, Mobile defender)
        {
            if (attacker.StunReady)
            {
                if (attacker.CanBeginAction<Fists>())
                {
                    if (attacker.Skills.Anatomy.Value >= 80.0 &&
                        attacker.Skills.Wrestling.Value >= 80.0)
                    {
                        if (attacker.Stam >= 15)
                        {
                            attacker.Stam -= 15;

                            if (CheckMove(attacker, SkillName.Anatomy))
                            {
                                StartMoveDelay(attacker);

                                attacker.StunReady = false;

                                attacker.SendLocalizedMessage(1004013); // You successfully stun your opponent!
                                defender.SendLocalizedMessage(1004014); // You have been stunned!

                                defender.Freeze(TimeSpan.FromSeconds(4.0));
                            }
                            else
                            {
                                attacker.SendLocalizedMessage(1004010); // You failed in your attempt to stun.
                                defender.SendLocalizedMessage(1004011); // Your opponent tried to stun you and failed.
                            }
                        }
                        else
                        {
                            attacker.SendLocalizedMessage(1004009); // You are too fatigued to attempt anything.
                        }
                    }
                    else
                    {
                        attacker.SendLocalizedMessage(1004008); // You are not skilled enough to stun your opponent.
                        attacker.StunReady = false;
                    }
                }
            }
            else if (attacker.DisarmReady)
            {
                if (attacker.CanBeginAction<Fists>())
                {
                    if (defender.Player || defender.Body.IsHuman)
                    {
                        if (attacker.Skills.ArmsLore.Value >= 80.0 &&
                            attacker.Skills.Wrestling.Value >= 80.0)
                        {
                            if (attacker.Stam >= 15)
                            {
                                var toDisarm = defender.FindItemOnLayer(Layer.OneHanded);

                                if (toDisarm?.Movable == false)
                                {
                                    toDisarm = defender.FindItemOnLayer(Layer.TwoHanded);
                                }

                                var pack = defender.Backpack;

                                if (pack == null || toDisarm?.Movable == false)
                                {
                                    attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
                                }
                                else if (CheckMove(attacker, SkillName.ArmsLore))
                                {
                                    StartMoveDelay(attacker);

                                    attacker.Stam -= 15;
                                    attacker.DisarmReady = false;

                                    attacker.SendLocalizedMessage(1004006); // You successfully disarm your opponent!
                                    defender.SendLocalizedMessage(1004007); // You have been disarmed!

                                    pack.DropItem(toDisarm);
                                }
                                else
                                {
                                    attacker.Stam -= 15;

                                    attacker.SendLocalizedMessage(1004004); // You failed in your attempt to disarm.
                                    defender.SendLocalizedMessage(1004005); // Your opponent tried to disarm you but failed.
                                }
                            }
                            else
                            {
                                attacker.SendLocalizedMessage(1004003); // You are too fatigued to attempt anything.
                            }
                        }
                        else
                        {
                            attacker.SendLocalizedMessage(1004002); // You are not skilled enough to disarm your opponent.
                            attacker.DisarmReady = false;
                        }
                    }
                    else
                    {
                        attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
                    }
                }
            }
        }

        public override TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)
        {
            if (!Core.AOS)
            {
                CheckPreAOSMoves(attacker, defender);
            }

            return base.OnSwing(attacker, defender);
        }

        /*public override void OnMiss( Mobile attacker, Mobile defender )
        {
          base.PlaySwingAnimation( attacker );
        }*/

        [AfterDeserialization(false)]
        private void OnAfterDeserialization()
        {
            Delete();
        }

        /* Wrestling moves */

        private static bool CheckMove(Mobile m, SkillName other)
        {
            var wresValue = m.Skills.Wrestling.Value;
            var scndValue = m.Skills[other].Value;

            /* 40% chance at 80, 80
             * 50% chance at 100, 100
             * 60% chance at 120, 120
             */

            var chance = (wresValue + scndValue) / 400.0;

            return chance >= Utility.RandomDouble();
        }

        private static bool HasFreeHands(Mobile m)
        {
            var item = m.FindItemOnLayer(Layer.OneHanded);

            return item is null or Spellbook && m.FindItemOnLayer(Layer.TwoHanded) == null;
        }

        private static void EventSink_DisarmRequest(Mobile m)
        {
            if (Core.AOS)
            {
                return;
            }

            if (!DuelContext.AllowSpecialAbility(m, "Disarm", true))
            {
                return;
            }

            var armsValue = m.Skills.ArmsLore.Value;
            var wresValue = m.Skills.Wrestling.Value;

            if (!HasFreeHands(m))
            {
                m.SendLocalizedMessage(1004029); // You must have your hands free to attempt to disarm your opponent.
                m.DisarmReady = false;
            }
            else if (armsValue >= 80.0 && wresValue >= 80.0)
            {
                m.DisruptiveAction();
                m.DisarmReady = !m.DisarmReady;
                m.SendLocalizedMessage(m.DisarmReady ? 1019013 : 1019014);
            }
            else
            {
                m.SendLocalizedMessage(1004002); // You are not skilled enough to disarm your opponent.
                m.DisarmReady = false;
            }
        }

        private static void EventSink_StunRequest(Mobile m)
        {
            if (Core.AOS)
            {
                return;
            }

            if (!DuelContext.AllowSpecialAbility(m, "Stun", true))
            {
                return;
            }

            var anatValue = m.Skills.Anatomy.Value;
            var wresValue = m.Skills.Wrestling.Value;

            if (!HasFreeHands(m))
            {
                m.SendLocalizedMessage(1004031); // You must have your hands free to attempt to stun your opponent.
                m.StunReady = false;
            }
            else if (anatValue >= 80.0 && wresValue >= 80.0)
            {
                m.DisruptiveAction();
                m.StunReady = !m.StunReady;
                m.SendLocalizedMessage(m.StunReady ? 1019011 : 1019012);
            }
            else
            {
                m.SendLocalizedMessage(1004008); // You are not skilled enough to stun your opponent.
                m.StunReady = false;
            }
        }

        private static void StartMoveDelay(Mobile m)
        {
            new MoveDelayTimer(m).Start();
        }

        private class MoveDelayTimer : Timer
        {
            private readonly Mobile m_Mobile;

            public MoveDelayTimer(Mobile m) : base(TimeSpan.FromSeconds(10.0))
            {
                m_Mobile = m;

                m_Mobile.BeginAction<Fists>();
            }

            protected override void OnTick()
            {
                m_Mobile.EndAction<Fists>();
            }
        }
    }
}
