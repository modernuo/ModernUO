using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class StatuetteDyeTub : DyeTub, IRewardItem
    {
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _isRewardItem;

        [Constructible]
        public StatuetteDyeTub() => LootType = LootType.Blessed;

        public override bool AllowDyables => false;
        public override bool AllowStatuettes => true;
        public override int TargetMessage => 1049777; // Target the statuette to dye
        public override int FailMessage => 1049778;   // You can only dye veteran reward statuettes with this tub.
        public override int LabelNumber => 1049741;   // Reward Statuette Dye Tub
        public override CustomHuePicker CustomHuePicker => CustomHuePicker.LeatherDyeTub;

        public override void OnDoubleClick(Mobile from)
        {
            if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
            {
                return;
            }

            base.OnDoubleClick(from);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && _isRewardItem)
            {
                list.Add(1076221); // 5th Year Veteran Reward
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
