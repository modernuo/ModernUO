using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class RewardBlackDyeTub : DyeTub, IRewardItem
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _isRewardItem;

        [Constructible]
        public RewardBlackDyeTub()
        {
            Hue = DyedHue = 0x0001;
            Redyable = false;
            LootType = LootType.Blessed;
        }

        public override int LabelNumber => 1006008; // Black Dye Tub

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
