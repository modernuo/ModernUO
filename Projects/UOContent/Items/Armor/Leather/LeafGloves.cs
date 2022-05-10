using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable]
    [SerializationGenerator(1)]
    public partial class LeafGloves : BaseArmor, IArcaneEquip
    {
        private int _maxArcaneCharges;
        private int _curArcaneCharges;

        [Constructible]
        public LeafGloves() : base(0x2FC6) => Weight = 2.0;

        public override int RequiredRaces => Race.AllowElvesOnly;
        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 40;

        public override int AosStrReq => 10;
        public override int OldStrReq => 10;

        public override int ArmorBase => 13;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

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
                ItemID = 0x26B0; // TODO: Check
            }
            else if (ItemID == 0x26B0)
            {
                ItemID = 0x2FC6;
            }

            if (IsArcane && CurArcaneCharges == 0)
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
                0x2FC6 => 0x317C,
                0x317C => 0x2FC6,
                _      => ItemID
            };
        }
    }
}
