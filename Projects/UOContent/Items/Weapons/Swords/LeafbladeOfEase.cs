using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class LeafbladeOfEase : Leafblade
    {
        [Constructible]
        public LeafbladeOfEase() => WeaponAttributes.UseBestSkill = 1;

        public override int LabelNumber => 1073524; // leafblade of ease
    }
}
