namespace Server.Items
{
    public class RoyalGuardSurvivalKnife : SkinningKnife
    {
        [Constructible]
        public RoyalGuardSurvivalKnife()
        {
            Attributes.SpellChanneling = 1;
            Attributes.Luck = 140;
            Attributes.EnhancePotions = 25;

            WeaponAttributes.UseBestSkill = 1;
            WeaponAttributes.LowerStatReq = 50;
        }

        public RoyalGuardSurvivalKnife(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094918; // Royal Guard Survival Knife [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

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
