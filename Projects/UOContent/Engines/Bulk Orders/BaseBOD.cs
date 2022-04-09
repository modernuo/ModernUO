using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Engines.BulkOrders
{
    [SerializationGenerator(1)]
    public abstract partial class BaseBOD : Item
    {
        public BaseBOD(int hue, int amountMax, bool requireExeptional, BulkMaterialType material) : this()
        {
            Hue = hue;
            _amountMax = amountMax;
            _requireExceptional = requireExeptional;
            _material = material;
        }

        public BaseBOD() : base(Core.AOS ? 0x2258 : 0x14EF)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public abstract bool Complete { get; }

        [SerializableField(0)]
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _amountMax;

        [SerializableField(1)]
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _requireExceptional;

        [SerializableField(2)]
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private BulkMaterialType _material;

        public static BulkMaterialType GetRandomMaterial(BulkMaterialType start, double[] chances)
        {
            var random = Utility.RandomDouble();

            for (var i = 0; i < chances.Length; ++i)
            {
                if (random < chances[i])
                {
                    return i == 0 ? BulkMaterialType.None : start + (i - 1);
                }

                random -= chances[i];
            }

            return BulkMaterialType.None;
        }

        public abstract RewardGroup GetRewardGroup();

        public abstract int ComputeGold();
        public abstract int ComputeFame();
        public abstract void EndCombine(Mobile from, Item item);

        public virtual void GetRewards(out Item reward, out int gold, out int fame)
        {
            gold = ComputeGold();
            fame = ComputeFame();

            var rewards = ComputeRewards(false);

            reward = rewards.RandomElement()?.Construct();
        }

        public virtual List<RewardItem> ComputeRewards(bool full)
        {
            var rewardGroup = GetRewardGroup();

            var list = new List<RewardItem>();

            if (full)
            {
                for (var i = 0; i < rewardGroup?.Items.Length; ++i)
                {
                    var reward = rewardGroup.Items[i];

                    if (reward != null)
                    {
                        list.Add(reward);
                    }
                }
            }
            else
            {
                var reward = rewardGroup.AcquireItem();

                if (reward != null)
                {
                    list.Add(reward);
                }
            }

            return list;
        }

        public virtual void BeginCombine(Mobile from)
        {
            if (Complete)
            {
                // The maximum amount of requested items have already been combined to this deed.
                from.SendLocalizedMessage(1045166);
            }
            else
            {
                from.Target = new BODTarget(this);
            }
        }

        [AfterDeserialization(false)]
        private void AfterDeserialization()
        {
            if (Parent == null && Map == Map.Internal && Location == Point3D.Zero)
            {
                Delete();
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            AmountMax = reader.ReadInt();
            RequireExceptional = reader.ReadBool();
            Material = (BulkMaterialType)reader.ReadInt();

            Timer.StartTimer(AfterDeserialization);
        }
    }
}
