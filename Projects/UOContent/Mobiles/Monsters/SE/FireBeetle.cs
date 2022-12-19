using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Mobiles
{
    [Forge]
    [SerializationGenerator(0,false)]
    public partial class FireBeetle : BaseMount
    {
        public override string DefaultName => "a fire beetle";

        [Constructible]
        public FireBeetle() : base(0xA9, 0x3E95, AIType.AI_Melee)
        {
            SetStam(100);
            SetStr(300);
            SetDex(65, 100);
            SetInt(500);

            SetHits(200);

            SetDamage(7, 20);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Fire, 100);

            SetResistance(ResistanceType.Physical, 40);
            SetResistance(ResistanceType.Fire, 70, 75);
            SetResistance(ResistanceType.Cold, 10);
            SetResistance(ResistanceType.Poison, 30);
            SetResistance(ResistanceType.Energy, 30);

            SetSkill(SkillName.MagicResist, 90.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 4000;
            Karma = -4000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 93.9;

            PackItem(new SulfurousAsh(Utility.RandomMinMax(16, 25)));
            PackItem(new IronIngot(2));

            Hue = 0x489;
        }

        public override string CorpseName => "a fire beetle corpse";
        public override bool SubdueBeforeTame => true; // Must be beaten into submission
        public override bool StatLossAfterTame => true;
        public virtual double BoostedSpeed => 0.1;
        public override bool ReduceSpeedWithDamage => false;

        public override int Meat => 16;
        public override FoodType FavoriteFood => FoodType.Meat;

        public override void OnHarmfulSpell(Mobile from)
        {
            if (!Controlled && ControlMaster == null)
            {
                CurrentSpeed = BoostedSpeed;
            }
        }

        public override void OnCombatantChange()
        {
            if (Combatant == null && !Controlled && ControlMaster == null)
            {
                CurrentSpeed = PassiveSpeed;
            }
        }

        public override bool OverrideBondingReqs() => true;

        public override int GetAngerSound() => 0x21D;

        public override int GetIdleSound() => 0x21D;

        public override int GetAttackSound() => 0x162;

        public override int GetHurtSound() => 0x163;

        public override int GetDeathSound() => 0x21D;

        public override double GetControlChance(Mobile m, bool useBaseSkill = false) => 1.0;

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            Hue = 0x489;
        }
    }
}
