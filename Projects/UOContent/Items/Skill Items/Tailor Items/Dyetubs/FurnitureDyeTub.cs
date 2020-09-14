using Server.Engines.VeteranRewards;

namespace Server.Items
{
    public class FurnitureDyeTub : DyeTub, IRewardItem
    {
        [Constructible]
        public FurnitureDyeTub() => LootType = LootType.Blessed;

        public FurnitureDyeTub(Serial serial) : base(serial)
        {
        }

        public override bool AllowDyables => false;
        public override bool AllowFurniture => true;
        public override int TargetMessage => 501019; // Select the furniture to dye.
        public override int FailMessage => 501021;   // That is not a piece of furniture.
        public override int LabelNumber => 1041246;  // Furniture Dye Tub

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
            {
                return;
            }

            base.OnDoubleClick(from);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && IsRewardItem)
            {
                list.Add(1076217); // 1st Year Veteran Reward
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        IsRewardItem = reader.ReadBool();
                        break;
                    }
            }

            if (LootType == LootType.Regular)
            {
                LootType = LootType.Blessed;
            }
        }
    }
}
