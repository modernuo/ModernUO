using System;

namespace Server.Items
{
    public class ElvenGlasses : BaseArmor
    {
        [Constructible]
        public ElvenGlasses() : base(0x2FB8)
        {
            Weight = 2;
            WeaponAttributes = new AosWeaponAttributes(this);
        }

        public ElvenGlasses(Serial serial) : base(serial)
        {
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

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosWeaponAttributes WeaponAttributes { get; private set; }

        public override void AppendChildNameProperties(ObjectPropertyList list)
        {
            base.AppendChildNameProperties(list);

            int prop;

            if ((prop = WeaponAttributes.HitColdArea) != 0)
            {
                list.Add(1060416, prop.ToString()); // hit cold area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitDispel) != 0)
            {
                list.Add(1060417, prop.ToString()); // hit dispel ~1_val~%
            }

            if ((prop = WeaponAttributes.HitEnergyArea) != 0)
            {
                list.Add(1060418, prop.ToString()); // hit energy area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitFireArea) != 0)
            {
                list.Add(1060419, prop.ToString()); // hit fire area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitFireball) != 0)
            {
                list.Add(1060420, prop.ToString()); // hit fireball ~1_val~%
            }

            if ((prop = WeaponAttributes.HitHarm) != 0)
            {
                list.Add(1060421, prop.ToString()); // hit harm ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLeechHits) != 0)
            {
                list.Add(1060422, prop.ToString()); // hit life leech ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLightning) != 0)
            {
                list.Add(1060423, prop.ToString()); // hit lightning ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLowerAttack) != 0)
            {
                list.Add(1060424, prop.ToString()); // hit lower attack ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLowerDefend) != 0)
            {
                list.Add(1060425, prop.ToString()); // hit lower defense ~1_val~%
            }

            if ((prop = WeaponAttributes.HitMagicArrow) != 0)
            {
                list.Add(1060426, prop.ToString()); // hit magic arrow ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLeechMana) != 0)
            {
                list.Add(1060427, prop.ToString()); // hit mana leech ~1_val~%
            }

            if ((prop = WeaponAttributes.HitPhysicalArea) != 0)
            {
                list.Add(1060428, prop.ToString()); // hit physical area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitPoisonArea) != 0)
            {
                list.Add(1060429, prop.ToString()); // hit poison area ~1_val~%
            }

            if ((prop = WeaponAttributes.HitLeechStam) != 0)
            {
                list.Add(1060430, prop.ToString()); // hit stamina leech ~1_val~%
            }
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
            {
                flags |= toSet;
            }
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            var flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.WeaponAttributes, !WeaponAttributes.IsEmpty);

            writer.Write((int)flags);

            if (GetSaveFlag(flags, SaveFlag.WeaponAttributes))
            {
                WeaponAttributes.Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            var flags = (SaveFlag)reader.ReadInt();

            if (GetSaveFlag(flags, SaveFlag.WeaponAttributes))
            {
                WeaponAttributes = new AosWeaponAttributes(this, reader);
            }
            else
            {
                WeaponAttributes = new AosWeaponAttributes(this);
            }
        }

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            WeaponAttributes = 0x00000001
        }
    }
}
