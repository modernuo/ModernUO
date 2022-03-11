namespace Server.Mobiles
{
    public class ServantOfSemidar : BaseCreature
    {
        [Constructible]
        public ServantOfSemidar() : base(AIType.AI_Melee, FightMode.None) => Body = 0x26;

        public ServantOfSemidar(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a Servant of Semidar";

        public override bool DisallowAllMoves => true;

        public override bool InitialInnocent => true;

        public override bool CanBeDamaged() => false;

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1005494); // enslaved
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
