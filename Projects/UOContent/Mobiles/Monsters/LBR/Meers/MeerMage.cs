using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MeerMage : BaseCreature
    {
        private static readonly Dictionary<Mobile, TimerExecutionToken> m_Table = new();

        private DateTime m_NextAbilityTime;

        [Constructible]
        public MeerMage() : base(AIType.AI_Mage, FightMode.Evil)
        {
            Body = 770;

            SetStr(171, 200);
            SetDex(126, 145);
            SetInt(276, 305);

            SetHits(103, 120);

            SetDamage(24, 26);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 50);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 70.1, 80.0);
            SetSkill(SkillName.Meditation, 85.1, 95.0);
            SetSkill(SkillName.MagicResist, 80.1, 100.0);
            SetSkill(SkillName.Tactics, 70.1, 90.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 8000;
            Karma = 8000;

            VirtualArmor = 16;

            m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));
        }

        public override string CorpseName => "a meer's corpse";
        public override string DefaultName => "a meer mage";

        public override bool AutoDispel => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override bool CanRummageCorpses => true;
        public override int TreasureMapLevel => 3;

        public override bool InitialInnocent => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.MedScrolls, 2);
            // TODO: Daemon bone ...
        }

        public override int GetHurtSound() => 0x14D;

        public override int GetDeathSound() => 0x314;

        public override int GetAttackSound() => 0x75;

        public override void OnThink()
        {
            if (Core.Now >= m_NextAbilityTime)
            {
                var combatant = Combatant;

                if (combatant != null && combatant.Map == Map && combatant.InRange(this, 12) && IsEnemy(combatant) &&
                    !UnderEffect(combatant))
                {
                    m_NextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(20, 30));

                    if (combatant is BaseCreature bc)
                    {
                        if (bc.Controlled && bc.ControlMaster?.Deleted == false && bc.ControlMaster.Alive)
                        {
                            if (bc.ControlMaster.Map == Map && bc.ControlMaster.InRange(this, 12) &&
                                !UnderEffect(bc.ControlMaster))
                            {
                                Combatant = combatant = bc.ControlMaster;
                            }
                        }
                    }

                    if (Utility.RandomDouble() < .1)
                    {
                        int[][] coord =
                        {
                            new[] { -4, -6 }, new[] { 4, -6 }, new[] { 0, -8 }, new[] { -5, 5 }, new[] { 5, 5 }
                        };

                        for (var i = 0; i < 5; i++)
                        {
                            var x = combatant.X + coord[i][0];
                            var y = combatant.Y + coord[i][1];

                            var loc = new Point3D(x, y, combatant.Map.GetAverageZ(x, y));

                            if (!combatant.Map.CanSpawnMobile(loc))
                            {
                                continue;
                            }

                            var rabid = i switch
                            {
                                0 => (BaseCreature)new EnragedRabbit(this),
                                1 => new EnragedHind(this),
                                2 => new EnragedHart(this),
                                3 => new EnragedBlackBear(this),
                                _ => new EnragedEagle(this)
                            };

                            rabid.FocusMob = combatant;
                            rabid.MoveToWorld(loc, combatant.Map);
                        }

                        Say(
                            1071932
                        ); // Creatures of the forest, I call to thee!  Aid me in the fight against all that is evil!
                    }
                    else if (combatant.Player)
                    {
                        Say(true, "I call a plague of insects to sting your flesh!");

                        var count = 0;
                        Timer.StartTimer(
                            TimeSpan.FromSeconds(0.5),
                            TimeSpan.FromSeconds(7.0),
                            () => DoEffect(combatant, count++),
                            out var timerToken
                        );

                        m_Table[combatant] = timerToken;
                    }
                }
            }

            base.OnThink();
        }

        public static bool UnderEffect(Mobile m) => m_Table.ContainsKey(m);

        public static void StopEffect(Mobile m, bool message)
        {
            if (m_Table.Remove(m, out var timer))
            {
                if (message)
                {
                    m.PublicOverheadMessage(
                        MessageType.Emote,
                        m.SpeechHue,
                        true,
                        "* The open flame begins to scatter the swarm of insects *"
                    );
                }

                timer.Cancel();
            }
        }

        private void DoEffect(Mobile m, int count)
        {
            if (!m.Alive)
            {
                StopEffect(m, false);
                return;
            }

            if (m.FindItemOnLayer<Torch>(Layer.TwoHanded)?.Burning == true)
            {
                StopEffect(m, true);
                return;
            }

            if (count % 4 == 0)
            {
                m.LocalOverheadMessage(
                    MessageType.Emote,
                    m.SpeechHue,
                    true,
                    "* The swarm of insects bites and stings your flesh! *"
                );
                m.NonlocalOverheadMessage(
                    MessageType.Emote,
                    m.SpeechHue,
                    true,
                    $"* {m.Name} is stung by a swarm of insects *"
                );
            }

            m.FixedParticles(0x91C, 10, 180, 9539, EffectLayer.Waist);
            m.PlaySound(0x00E);
            m.PlaySound(0x1BC);

            AOS.Damage(m, this, Utility.RandomMinMax(30, 40) - (Core.AOS ? 0 : 10), 100, 0, 0, 0, 0);

            if (!m.Alive)
            {
                StopEffect(m, false);
            }
        }
    }
}
