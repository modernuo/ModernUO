namespace Server.Mobiles
{
    public class FireBoss : BaseCreature
    {
        [Constructible]
        public FireBoss() : base(AIType.AI_FireBoss, FightMode.Closest, 10, 3, 0.05, 0.05)
        {
            Body = 172;
            Hue = 1360;

            SetStr(400);
            SetDex(200);
            SetInt(1000);

            SetHits(300000);

            SetDamage(50);

            SetDamageType(ResistanceType.Fire, 100);

            SetResistance(ResistanceType.Fire, 50);

            Fame = 25000;
            Karma = -25000;

            VirtualArmor = 92;
        }
        public override string CorpseName => "fire boss corpse";
        public override string DefaultName => "fire boss";
        public FireBoss(Serial serial) : base(serial)
        {
        }
        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }
        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
