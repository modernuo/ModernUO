using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseCloak : BaseClothing
    {
        public BaseCloak(int itemID, int hue = 0) : base(itemID, Layer.Cloak, hue)
        {
        }
    }

    [Flippable]
    [SerializationGenerator(2, false)]
    public partial class Cloak : BaseCloak, IArcaneEquip
    {
        private int _maxArcaneCharges;
        private int _curArcaneCharges;

        [Constructible]
        public Cloak(int hue = 0) : base(0x1515, hue) => Weight = 5.0;

        [EncodedInt]
        [SerializableField(0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurArcaneCharges
        {
            get => _curArcaneCharges;
            set
            {
                _curArcaneCharges = value;
                this.MarkDirty();
                InvalidateProperties();
                Update();
            }
        }

        [EncodedInt]
        [SerializableField(1)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxArcaneCharges
        {
            get => _maxArcaneCharges;
            set
            {
                _maxArcaneCharges = value;
                this.MarkDirty();
                InvalidateProperties();
                Update();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsArcane => _maxArcaneCharges > 0 && _curArcaneCharges >= 0;

        private void Deserialize(IGenericReader reader, int version)
        {
            if (reader.ReadBool())
            {
                _curArcaneCharges = reader.ReadInt();
                _maxArcaneCharges = reader.ReadInt();
            }
        }

        public void Update()
        {
            if (IsArcane)
            {
                ItemID = 0x26AD;
            }
            else if (ItemID == 0x26AD)
            {
                ItemID = 0x1515;
            }

            if (IsArcane && _curArcaneCharges == 0)
            {
                Hue = 0;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (IsArcane)
            {
                list.Add(1061837, "{0}\t{1}", _curArcaneCharges, _maxArcaneCharges); // arcane charges: ~1_val~ / ~2_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (IsArcane)
            {
                LabelTo(from, 1061837, $"{_curArcaneCharges}\t{_maxArcaneCharges}");
            }
        }

        public void Flip()
        {
            ItemID = ItemID switch
            {
                0x1515 => 0x1530,
                0x1530 => 0x1515,
                _      => ItemID
            };
        }
    }

    [Flippable]
    [SerializationGenerator(0, false)]
    public partial class RewardCloak : BaseCloak, IRewardItem
    {
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _number;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _isRewardItem;

        [Constructible]
        public RewardCloak(int hue = 0, int labelNumber = 0) : base(0x1515, hue)
        {
            Weight = 5.0;
            LootType = LootType.Blessed;

            _number = labelNumber;
        }

        public override int LabelNumber => _number > 0 ? _number : base.LabelNumber;

        public override int BasePhysicalResistance => 3;

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile mobile)
            {
                mobile.VirtualArmorMod += 2;
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile mobile)
            {
                mobile.VirtualArmorMod -= 2;
            }
        }

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && IsRewardItem)
            {
                // X Year Veteran Reward
                list.Add(RewardSystem.GetRewardYearLabel(this, new object[] { Hue, _number }));
            }
        }

        public override bool CanEquip(Mobile m) =>
            base.CanEquip(m) &&
            (!IsRewardItem || RewardSystem.CheckIsUsableBy(m, this, new object[] { Hue, _number }));

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Parent is Mobile mobile)
            {
                mobile.VirtualArmorMod += 2;
            }
        }
    }

    [Flippable(0x230A, 0x2309)]
    [SerializationGenerator(0, false)]
    public partial class FurCape : BaseCloak
    {
        [Constructible]
        public FurCape(int hue = 0) : base(0x230A, hue) => Weight = 4.0;
    }
}
