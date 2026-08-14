using System;
using Server.Engines.Craft;

namespace Server.Items;

public partial class BaseWeapon
{
    // PlayerConstructed moved onto Item
    private void MigrateFrom(V10Content content)
    {
        _damageLevel = content.DamageLevel ?? WeaponDamageLevel.Regular;
        _accuracyLevel = content.AccuracyLevel ?? WeaponAccuracyLevel.Regular;
        _durabilityLevel = content.DurabilityLevel ?? WeaponDurabilityLevel.Regular;
        _quality = content.Quality ?? WeaponQuality.Regular;
        _hitPoints = content.HitPoints ?? 0;
        _maxHitPoints = content.MaxHitPoints ?? 0;
        _slayer = content.Slayer ?? SlayerName.None;
        _poison = content.Poison;
        _poisonCharges = content.PoisonCharges ?? 0;
        _crafter = content.Crafter;
        _identified = content.Identified;
        _strRequirement = content.StrRequirement ?? -1;
        _dexRequirement = content.DexRequirement ?? -1;
        _intRequirement = content.IntRequirement ?? -1;
        _minDamage = content.MinDamage ?? -1;
        _maxDamage = content.MaxDamage ?? -1;
        _hitSound = content.HitSound ?? -1;
        _missSound = content.MissSound ?? -1;
        _speed = content.Speed ?? -1;
        _maxRange = content.MaxRange ?? -1;
        _skill = content.Skill ?? (SkillName)(-1);
        _type = content.Type ?? (WeaponType)(-1);
        _animation = content.Animation ?? (WeaponAnimation)(-1);
        _resource = content.Resource ?? CraftResource.Iron;
        _attributes = content.Attributes ?? AttributesDefaultValue();
        _weaponAttributes = content.WeaponAttributes ?? WeaponAttributesDefaultValue();
        PlayerConstructed = content.PlayerConstructed;
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _slayer2 = content.Slayer2 ?? SlayerName.None;
        _aosElementDamages = content.AosElementDamages ?? AosElementAttributesDefaultValue();
        _engravedText = content.EngravedText;
    }

    // Version 9 (pre-codegen)
    private void Deserialize(IGenericReader reader, int version)
    {
        var flags = (OldSaveFlag)reader.ReadInt();

        if (GetSaveFlag(flags, OldSaveFlag.DamageLevel))
        {
            _damageLevel = (WeaponDamageLevel)reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.AccuracyLevel))
        {
            _accuracyLevel = (WeaponAccuracyLevel)reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.DurabilityLevel))
        {
            _durabilityLevel = (WeaponDurabilityLevel)reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Quality))
        {
            _quality = (WeaponQuality)reader.ReadInt();
        }
        else
        {
            _quality = WeaponQuality.Regular;
        }

        if (GetSaveFlag(flags, OldSaveFlag.Hits))
        {
            _hitPoints = reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.MaxHits))
        {
            _maxHitPoints = reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Slayer))
        {
            _slayer = (SlayerName)reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Poison))
        {
            _poison = reader.ReadPoison();
        }

        if (GetSaveFlag(flags, OldSaveFlag.PoisonCharges))
        {
            _poisonCharges = reader.ReadInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.Crafter))
        {
            Timer.DelayCall(crafter => _crafter = crafter?.RawName, reader.ReadEntity<Mobile>());
        }

        if (GetSaveFlag(flags, OldSaveFlag.Identified))
        {
            _identified = version >= 6 || reader.ReadBool();
        }

        if (GetSaveFlag(flags, OldSaveFlag.StrReq))
        {
            _strRequirement = reader.ReadInt();
        }
        else
        {
            _strRequirement = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.DexReq))
        {
            _dexRequirement = reader.ReadInt();
        }
        else
        {
            _dexRequirement = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.IntReq))
        {
            _intRequirement = reader.ReadInt();
        }
        else
        {
            _intRequirement = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.MinDamage))
        {
            _minDamage = reader.ReadInt();
        }
        else
        {
            _minDamage = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.MaxDamage))
        {
            _maxDamage = reader.ReadInt();
        }
        else
        {
            _maxDamage = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.HitSound))
        {
            _hitSound = reader.ReadInt();
        }
        else
        {
            _hitSound = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.MissSound))
        {
            _missSound = reader.ReadInt();
        }
        else
        {
            _missSound = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.Speed))
        {
            if (version < 9)
            {
                _speed = reader.ReadInt();
            }
            else
            {
                _speed = reader.ReadFloat();
            }
        }
        else
        {
            _speed = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.MaxRange))
        {
            _maxRange = reader.ReadInt();
        }
        else
        {
            _maxRange = -1;
        }

        if (GetSaveFlag(flags, OldSaveFlag.Skill))
        {
            _skill = (SkillName)reader.ReadInt();
        }
        else
        {
            _skill = (SkillName)(-1);
        }

        if (GetSaveFlag(flags, OldSaveFlag.Type))
        {
            _type = (WeaponType)reader.ReadInt();
        }
        else
        {
            _type = (WeaponType)(-1);
        }

        if (GetSaveFlag(flags, OldSaveFlag.Animation))
        {
            _animation = (WeaponAnimation)reader.ReadInt();
        }
        else
        {
            _animation = (WeaponAnimation)(-1);
        }

        if (GetSaveFlag(flags, OldSaveFlag.Resource))
        {
            _resource = (CraftResource)reader.ReadInt();
        }
        else
        {
            _resource = CraftResource.Iron;
        }

        Attributes = new AosAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.Attributes))
        {
            Attributes.Deserialize(reader);
        }

        WeaponAttributes = new AosWeaponAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.WeaponAttributes))
        {
            WeaponAttributes.Deserialize(reader);
        }

        PlayerConstructed = GetSaveFlag(flags, OldSaveFlag.PlayerConstructed);

        SkillBonuses = new AosSkillBonuses(this);

        if (GetSaveFlag(flags, OldSaveFlag.SkillBonuses))
        {
            SkillBonuses.Deserialize(reader);
        }

        if (GetSaveFlag(flags, OldSaveFlag.Slayer2))
        {
            _slayer2 = (SlayerName)reader.ReadInt();
        }

        AosElementDamages = new AosElementAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.ElementalDamages))
        {
            AosElementDamages.Deserialize(reader);
        }

        if (GetSaveFlag(flags, OldSaveFlag.EngravedText))
        {
            _engravedText = reader.ReadString();
        }
    }

    private static bool GetSaveFlag(OldSaveFlag flags, OldSaveFlag toGet) => (flags & toGet) != 0;

    [Flags]
    private enum OldSaveFlag
    {
        None = 0x00000000,
        DamageLevel = 0x00000001,
        AccuracyLevel = 0x00000002,
        DurabilityLevel = 0x00000004,
        Quality = 0x00000008,
        Hits = 0x00000010,
        MaxHits = 0x00000020,
        Slayer = 0x00000040,
        Poison = 0x00000080,
        PoisonCharges = 0x00000100,
        Crafter = 0x00000200,
        Identified = 0x00000400,
        StrReq = 0x00000800,
        DexReq = 0x00001000,
        IntReq = 0x00002000,
        MinDamage = 0x00004000,
        MaxDamage = 0x00008000,
        HitSound = 0x00010000,
        MissSound = 0x00020000,
        Speed = 0x00040000,
        MaxRange = 0x00080000,
        Skill = 0x00100000,
        Type = 0x00200000,
        Animation = 0x00400000,
        Resource = 0x00800000,
        Attributes = 0x01000000,
        WeaponAttributes = 0x02000000,
        PlayerConstructed = 0x04000000,
        SkillBonuses = 0x08000000,
        Slayer2 = 0x10000000,
        ElementalDamages = 0x20000000,
        EngravedText = 0x40000000
    }
}
