using ModernUO.Serialization;
using System;
using Server.Engines.Quests;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Ferret : BaseCreature
    {
        private static readonly string[] m_Vocabulary =
        {
            "dook",
            "dook dook",
            "dook dook dook!"
        };

        private bool m_CanTalk;

        [Constructible]
        public Ferret() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0x117;

            SetStr(41, 48);
            SetDex(55);
            SetInt(75);

            SetHits(45, 50);

            SetDamage(7, 9);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 50);
            SetResistance(ResistanceType.Fire, 10, 14);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 21, 25);
            SetResistance(ResistanceType.Energy, 20, 25);

            SetSkill(SkillName.MagicResist, 4.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 4.0);

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -21.3;

            m_CanTalk = true;
        }

        public override string CorpseName => "a ferret corpse";
        public override string DefaultName => "a ferret";

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.Fish;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m is Ferret ferret && ferret.InRange(this, 3) && ferret.Alive)
            {
                Talk(ferret);
            }
        }

        public void Talk()
        {
            Talk(null);
        }

        public void Talk(Ferret to)
        {
            if (m_CanTalk)
            {
                if (to != null)
                {
                    QuestSystem.FocusTo(this, to);
                }

                Say(m_Vocabulary.RandomElement());

                if (to != null && Utility.RandomBool())
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 8)), to.Talk);
                }

                m_CanTalk = false;

                Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(20, 30)), ResetCanTalk);
            }
        }

        private void ResetCanTalk()
        {
            m_CanTalk = true;
        }

        [AfterDeserialization]
        public void AfterDeserialization()
        {
            m_CanTalk = true;
        }
    }
}
