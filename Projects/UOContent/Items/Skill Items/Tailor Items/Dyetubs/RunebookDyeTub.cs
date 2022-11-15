using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class RunebookDyeTub : DyeTub, IRewardItem
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _isRewardItem;

        [Constructible]
        public RunebookDyeTub() => LootType = LootType.Blessed;

        public override bool AllowDyables => false;
        public override bool AllowRunebooks => true;
        public override int TargetMessage => 1049774; // Target the runebook or runestone to dye
        public override int FailMessage => 1049775;   // You can only dye runestones or runebooks with this tub.
        public override int LabelNumber => 1049740;   // Runebook Dye Tub
        public override CustomHuePicker CustomHuePicker => CustomHuePicker.LeatherDyeTub;

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
                list.Add(1076220); // 4th Year Veteran Reward
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
