using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MetallicLeatherDyeTub : LeatherDyeTub
    {
        [Constructible]
        public MetallicLeatherDyeTub() => LootType = LootType.Blessed;

        public override CustomHuePicker CustomHuePicker => null;

        public override int LabelNumber => 1153495; // Metallic Leather ...

        public override bool MetallicHues => true;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && IsRewardItem)
            {
                list.Add(1076221); // 5th Year Veteran Reward
            }
        }
    }
}
