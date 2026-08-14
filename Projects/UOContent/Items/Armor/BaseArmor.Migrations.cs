using System;
using AMA = Server.Items.ArmorMeditationAllowance;

namespace Server.Items;

public partial class BaseArmor
{
    // PlayerConstructed moved onto Item
    private void MigrateFrom(V9Content content)
    {
        _attributes = content.Attributes ?? AttributesDefaultValue();
        _armorAttributes = content.ArmorAttributes ?? ArmorAttributesDefaultValue();
        _physicalBonus = content.PhysicalBonus ?? 0;
        _fireBonus = content.FireBonus ?? 0;
        _coldBonus = content.ColdBonus ?? 0;
        _poisonBonus = content.PoisonBonus ?? 0;
        _energyBonus = content.EnergyBonus ?? 0;
        _identified = content.Identified;
        _maxHitPoints = content.MaxHitPoints ?? 0;
        _hitPoints = content.HitPoints ?? 0;
        _crafter = content.Crafter;
        _quality = content.Quality ?? ArmorQuality.Regular;
        _durability = content.Durability ?? ArmorDurabilityLevel.Regular;
        _protectionLevel = content.ProtectionLevel ?? ArmorProtectionLevel.Regular;
        _resource = content.Resource ?? DefaultResource;
        _armorBase = content.BaseArmorRating ?? -1;
        _strBonus = content.StrBonus ?? -1;
        _dexBonus = content.DexBonus ?? -1;
        _intBonus = content.IntBonus ?? -1;
        _strReq = content.StrRequirement ?? -1;
        _dexReq = content.DexRequirement ?? -1;
        _intReq = content.IntRequirement ?? -1;
        _meditate = content.MeditationAllowance ?? (AMA)(-1);
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        PlayerConstructed = content.PlayerConstructed;
    }

    private void MigrateFrom(V8Content content)
    {
        _attributes = content.Attributes ?? AttributesDefaultValue();
        _armorAttributes = content.ArmorAttributes ?? ArmorAttributesDefaultValue();
        _physicalBonus = content.PhysicalBonus ?? 0;
        _fireBonus = content.FireBonus ?? 0;
        _coldBonus = content.ColdBonus ?? 0;
        _poisonBonus = content.PoisonBonus ?? 0;
        _energyBonus = content.EnergyBonus ?? 0;
        _identified = content.Identified;
        _maxHitPoints = content.MaxHitPoints ?? 0;
        _hitPoints = content.HitPoints ?? 0;
        Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, content.Crafter);
        _quality = content.Quality ?? ArmorQuality.Regular;
        _durability = content.Durability ?? ArmorDurabilityLevel.Regular;
        _resource = content.RawResource ?? DefaultResource;
        _armorBase = content.BaseArmorRating ?? -1;
        _strBonus = content.StrBonus ?? -1;
        _dexBonus = content.DexBonus ?? -1;
        _intBonus = content.IntBonus ?? -1;
        _strReq = content.StrRequirement ?? -1;
        _dexReq = content.DexRequirement ?? -1;
        _intReq = content.IntRequirement ?? -1;
        _meditate = content.MeditationAllowance ?? (AMA)(-1);
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        PlayerConstructed = content.PlayerConstructed;
    }

    // Version 7 (pre-codegen)
    private void Deserialize(IGenericReader reader, int version)
    {
        var flags = (OldSaveFlag)reader.ReadEncodedInt();

        Attributes = new AosAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.Attributes))
        {
            Attributes.Deserialize(reader);
        }

        ArmorAttributes = new AosArmorAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.ArmorAttributes))
        {
            ArmorAttributes.Deserialize(reader);
        }

        if (GetSaveFlag(flags, OldSaveFlag.PhysicalBonus))
        {
            _physicalBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.FireBonus))
        {
            _fireBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.ColdBonus))
        {
            _coldBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.PoisonBonus))
        {
            _poisonBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.EnergyBonus))
        {
            _energyBonus = reader.ReadEncodedInt();
        }

        _identified = GetSaveFlag(flags, OldSaveFlag.Identified);

        if (GetSaveFlag(flags, OldSaveFlag.MaxHitPoints))
        {
            _maxHitPoints = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.HitPoints))
        {
            _hitPoints = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Crafter))
        {
            Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, reader.ReadEntity<Mobile>());
        }

        if (GetSaveFlag(flags, OldSaveFlag.Quality))
        {
            _quality = (ArmorQuality)reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Durability))
        {
            _durability = (ArmorDurabilityLevel)reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Protection))
        {
            _protectionLevel = (ArmorProtectionLevel)reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Resource))
        {
            _resource = (CraftResource)reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.BaseArmor))
        {
            _armorBase = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.StrBonus))
        {
            _strBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.DexBonus))
        {
            _dexBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.IntBonus))
        {
            _intBonus = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.StrReq))
        {
            _strReq = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.DexReq))
        {
            _dexReq = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.IntReq))
        {
            _intReq = reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.MedAllowance))
        {
            _meditate = (AMA)reader.ReadEncodedInt();
        }

        SkillBonuses = new AosSkillBonuses(this);

        if (GetSaveFlag(flags, OldSaveFlag.SkillBonuses))
        {
            SkillBonuses.Deserialize(reader);
        }

        PlayerConstructed = GetSaveFlag(flags, OldSaveFlag.PlayerConstructed);
    }

    private static bool GetSaveFlag(OldSaveFlag flags, OldSaveFlag toGet) => (flags & toGet) != 0;

    [Flags]
    private enum OldSaveFlag
    {
        None = 0x00000000,
        Attributes = 0x00000001,
        ArmorAttributes = 0x00000002,
        PhysicalBonus = 0x00000004,
        FireBonus = 0x00000008,
        ColdBonus = 0x00000010,
        PoisonBonus = 0x00000020,
        EnergyBonus = 0x00000040,
        Identified = 0x00000080,
        MaxHitPoints = 0x00000100,
        HitPoints = 0x00000200,
        Crafter = 0x00000400,
        Quality = 0x00000800,
        Durability = 0x00001000,
        Protection = 0x00002000,
        Resource = 0x00004000,
        BaseArmor = 0x00008000,
        StrBonus = 0x00010000,
        DexBonus = 0x00020000,
        IntBonus = 0x00040000,
        StrReq = 0x00080000,
        DexReq = 0x00100000,
        IntReq = 0x00200000,
        MedAllowance = 0x00400000,
        SkillBonuses = 0x00800000,
        PlayerConstructed = 0x01000000
    }
}
