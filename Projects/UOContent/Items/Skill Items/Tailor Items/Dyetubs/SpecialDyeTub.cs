using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class SpecialDyeTub : DyeTub, IRewardItem
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _isRewardItem;

        [Constructible]
        public SpecialDyeTub() => LootType = LootType.Blessed;

        public override CustomHuePicker CustomHuePicker => CustomHuePicker.SpecialDyeTub;
        public override int LabelNumber => 1041285; // Special Dye Tub

        public override void OnDoubleClick(Mobile from)
        {
            if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
            {
                return;
            }

            base.OnDoubleClick(from);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && _isRewardItem)
            {
                list.Add(1076217); // 1st Year Veteran Reward
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            if (LootType == LootType.Regular)
            {
                LootType = LootType.Blessed;
            }
        }
    }
}
