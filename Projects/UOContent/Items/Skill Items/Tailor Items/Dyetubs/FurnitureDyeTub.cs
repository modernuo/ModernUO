using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(1, false)]
    public partial class FurnitureDyeTub : DyeTub, IRewardItem
    {
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _isRewardItem;

        [Constructible]
        public FurnitureDyeTub() => LootType = LootType.Blessed;

        public override bool AllowDyables => false;
        public override bool AllowFurniture => true;
        public override int TargetMessage => 501019; // Select the furniture to dye.
        public override int FailMessage => 501021;   // That is not a piece of furniture.
        public override int LabelNumber => 1041246;  // Furniture Dye Tub

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
