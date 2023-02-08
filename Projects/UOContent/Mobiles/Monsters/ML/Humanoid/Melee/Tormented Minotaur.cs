using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TormentedMinotaur : BaseCreature
    {
        [Constructible]
        public TormentedMinotaur() : base(AIType.AI_Melee)
        {
            Body = 262;

            SetStr(822, 930);
            SetDex(401, 415);
            SetInt(128, 138);

            SetHits(4000, 4200);

            SetDamage(16, 30);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 62);
            SetResistance(ResistanceType.Fire, 74);
            SetResistance(ResistanceType.Cold, 54);
            SetResistance(ResistanceType.Poison, 56);
            SetResistance(ResistanceType.Energy, 54);

            SetSkill(SkillName.Wrestling, 110.1, 111.0);
            SetSkill(SkillName.Tactics, 100.7, 102.8);
            SetSkill(SkillName.MagicResist, 104.3, 116.3);

            Fame = 20000;
            Karma = -20000;
        }

        public override string CorpseName => "a tormented minotaur corpse";
        public override string DefaultName => "Tormented Minotaur";
        public override Poison PoisonImmune => Poison.Deadly;
        public override int TreasureMapLevel => 3;
        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 10);
        }

        public override int GetDeathSound() => 0x596;

        public override int GetAttackSound() => 0x597;

        public override int GetIdleSound() => 0x598;

        public override int GetAngerSound() => 0x599;

        public override int GetHurtSound() => 0x59A;
    }
}
