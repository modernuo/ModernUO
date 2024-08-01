using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class RewardSign : Sign, IEngravable
    {
        [SerializableField(0)]
        private string _engravedText;

        [Constructible]
        public RewardSign() : base((SignType)Utility.RandomMinMax(31, 58), Utility.RandomBool() ? SignFacing.North : SignFacing.West)
        {
            Movable = true;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (!string.IsNullOrEmpty(EngravedText))
            {
                list.Add(1072305, EngravedText); // Engraved: ~1_INSCRIPTION~
            }
        }
    }
}
