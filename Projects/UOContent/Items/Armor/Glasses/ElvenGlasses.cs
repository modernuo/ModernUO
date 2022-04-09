using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ElvenGlasses : BaseArmor
    {
        [Constructible]
        public ElvenGlasses() : base(0x2FB8)
        {
            Weight = 2;
            _weaponAttributes = new AosWeaponAttributes(this);
        }

        public override int LabelNumber => 1032216; // elven glasses

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 36;
        public override int InitMaxHits => 48;

        public override int AosStrReq => 45;
        public override int OldStrReq => 40;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;
        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

        [SerializableField(0, setter: "private")]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster, canModify: true)]")]
        public AosWeaponAttributes _weaponAttributes;

        [SerializableFieldSaveFlag(0)]
        private bool ShouldSerializeWeaponAttributes() => !_weaponAttributes.IsEmpty;

        [SerializableFieldDefault(0)]
        private AosWeaponAttributes WeaponAttributesDefaultValue() => new(this);

        public override void AppendChildNameProperties(ObjectPropertyList list)
        {
            base.AppendChildNameProperties(list);

            int prop;

            if ((prop = _weaponAttributes.HitColdArea) != 0)
            {
                list.Add(1060416, prop.ToString()); // hit cold area ~1_val~%
            }

            if ((prop = _weaponAttributes.HitDispel) != 0)
            {
                list.Add(1060417, prop.ToString()); // hit dispel ~1_val~%
            }

            if ((prop = _weaponAttributes.HitEnergyArea) != 0)
            {
                list.Add(1060418, prop.ToString()); // hit energy area ~1_val~%
            }

            if ((prop = _weaponAttributes.HitFireArea) != 0)
            {
                list.Add(1060419, prop.ToString()); // hit fire area ~1_val~%
            }

            if ((prop = _weaponAttributes.HitFireball) != 0)
            {
                list.Add(1060420, prop.ToString()); // hit fireball ~1_val~%
            }

            if ((prop = _weaponAttributes.HitHarm) != 0)
            {
                list.Add(1060421, prop.ToString()); // hit harm ~1_val~%
            }

            if ((prop = _weaponAttributes.HitLeechHits) != 0)
            {
                list.Add(1060422, prop.ToString()); // hit life leech ~1_val~%
            }

            if ((prop = _weaponAttributes.HitLightning) != 0)
            {
                list.Add(1060423, prop.ToString()); // hit lightning ~1_val~%
            }

            if ((prop = _weaponAttributes.HitLowerAttack) != 0)
            {
                list.Add(1060424, prop.ToString()); // hit lower attack ~1_val~%
            }

            if ((prop = _weaponAttributes.HitLowerDefend) != 0)
            {
                list.Add(1060425, prop.ToString()); // hit lower defense ~1_val~%
            }

            if ((prop = _weaponAttributes.HitMagicArrow) != 0)
            {
                list.Add(1060426, prop.ToString()); // hit magic arrow ~1_val~%
            }

            if ((prop = _weaponAttributes.HitLeechMana) != 0)
            {
                list.Add(1060427, prop.ToString()); // hit mana leech ~1_val~%
            }

            if ((prop = _weaponAttributes.HitPhysicalArea) != 0)
            {
                list.Add(1060428, prop.ToString()); // hit physical area ~1_val~%
            }

            if ((prop = _weaponAttributes.HitPoisonArea) != 0)
            {
                list.Add(1060429, prop.ToString()); // hit poison area ~1_val~%
            }

            if ((prop = _weaponAttributes.HitLeechStam) != 0)
            {
                list.Add(1060430, prop.ToString()); // hit stamina leech ~1_val~%
            }
        }
    }
}
