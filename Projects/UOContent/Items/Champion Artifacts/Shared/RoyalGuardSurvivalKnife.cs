using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class RoyalGuardSurvivalKnife : SkinningKnife
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

        public override int LabelNumber => 1094918; // Royal Guard Survival Knife [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
