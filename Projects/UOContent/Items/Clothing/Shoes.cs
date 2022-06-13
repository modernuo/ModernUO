using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseShoes : BaseClothing
    {
        public BaseShoes(int itemID, int hue = 0) : base(itemID, Layer.Shoes, hue)
        {
        }

        public override bool Scissor(Mobile from, Scissors scissors)
        {
            if (DefaultResource == CraftResource.None)
            {
                return base.Scissor(from, scissors);
            }

            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }
    }

    [Flippable(0x2307, 0x2308)]
    [SerializationGenerator(0, false)]
    public partial class FurBoots : BaseShoes
    {
        [Constructible]
        public FurBoots(int hue = 0) : base(0x2307, hue) => Weight = 3.0;
    }

    [Flippable(0x170b, 0x170c)]
    [SerializationGenerator(0, false)]
    public partial class Boots : BaseShoes
    {
        [Constructible]
        public Boots(int hue = 0) : base(0x170B, hue) => Weight = 3.0;

        public override CraftResource DefaultResource => CraftResource.RegularLeather;
    }

    [Flippable]
    [SerializationGenerator(2, false)]
    public partial class ThighBoots : BaseShoes, IArcaneEquip
    {
        private int _maxArcaneCharges;
        private int _curArcaneCharges;

        [Constructible]
        public ThighBoots(int hue = 0) : base(0x1711, hue) => Weight = 4.0;

        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        [EncodedInt]
        [SerializableField(0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurArcaneCharges
        {
            get => _curArcaneCharges;
            set
            {
                _curArcaneCharges = value;
                InvalidateProperties();
                Update();
                this.MarkDirty();
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
                InvalidateProperties();
                Update();
                this.MarkDirty();
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

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (IsArcane)
            {
                LabelTo(from, 1061837, $"{_curArcaneCharges}\t{_maxArcaneCharges}");
            }
        }

        public void Update()
        {
            if (IsArcane)
            {
                ItemID = 0x26AF;
            }
            else if (ItemID == 0x26AF)
            {
                ItemID = 0x1711;
            }

            if (IsArcane && CurArcaneCharges == 0)
            {
                Hue = 0;
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (IsArcane)
            {
                list.Add(1061837, $"{_curArcaneCharges}\t{_maxArcaneCharges}"); // arcane charges: ~1_val~ / ~2_val~
            }
        }

        public void Flip()
        {
            ItemID = ItemID switch
            {
                0x1711 => 0x1712,
                0x1712 => 0x1711,
                _      => ItemID
            };
        }
    }

    [Flippable(0x170f, 0x1710)]
    [SerializationGenerator(0, false)]
    public partial class Shoes : BaseShoes
    {
        [Constructible]
        public Shoes(int hue = 0) : base(0x170F, hue) => Weight = 2.0;

        public override CraftResource DefaultResource => CraftResource.RegularLeather;
    }

    [Flippable(0x170d, 0x170e)]
    [SerializationGenerator(0, false)]
    public partial class Sandals : BaseShoes
    {
        [Constructible]
        public Sandals(int hue = 0) : base(0x170D, hue) => Weight = 1.0;

        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override bool Dye(Mobile from, DyeTub sender) => false;
    }

    [Flippable(0x2797, 0x27E2)]
    [SerializationGenerator(0, false)]
    public partial class NinjaTabi : BaseShoes
    {
        [Constructible]
        public NinjaTabi(int hue = 0) : base(0x2797, hue) => Weight = 2.0;
    }

    [Flippable(0x2796, 0x27E1)]
    [SerializationGenerator(0, false)]
    public partial class SamuraiTabi : BaseShoes
    {
        [Constructible]
        public SamuraiTabi(int hue = 0) : base(0x2796, hue) => Weight = 2.0;
    }

    [Flippable(0x2796, 0x27E1)]
    [SerializationGenerator(0, false)]
    public partial class Waraji : BaseShoes
    {
        [Constructible]
        public Waraji(int hue = 0) : base(0x2796, hue) => Weight = 2.0;
    }

    [Flippable(0x2FC4, 0x317A)]
    [SerializationGenerator(0)]
    public partial class ElvenBoots : BaseShoes
    {
        [Constructible]
        public ElvenBoots(int hue = 0) : base(0x2FC4, hue) => Weight = 2.0;

        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override bool Dye(Mobile from, DyeTub sender) => false;
    }
}
