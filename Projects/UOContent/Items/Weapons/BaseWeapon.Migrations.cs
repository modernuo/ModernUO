namespace Server.Items;

public abstract partial class BaseWeapon
{
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
        _playerConstructed = content.PlayerConstructed;
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _slayer2 = content.Slayer2 ?? SlayerName.None;
        _aosElementDamages = content.AosElementDamages ?? AosElementAttributesDefaultValue();
        _engravedText = content.EngravedText;
        _extendedWeaponAttributes = ExtendedWeaponAttributesDefaultValue();
        _negativeAttributes = NegativeAttributesDefaultValue();
    }

    private void MigrateFrom(V11Content content)
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
        _playerConstructed = content.PlayerConstructed;
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _slayer2 = content.Slayer2 ?? SlayerName.None;
        _aosElementDamages = content.AosElementDamages ?? AosElementAttributesDefaultValue();
        _engravedText = content.EngravedText;
        _extendedWeaponAttributes = content.ExtendedWeaponAttributes ?? ExtendedWeaponAttributesDefaultValue();
        _negativeAttributes = NegativeAttributesDefaultValue();
    }

    private void MigrateFrom(V12Content content)
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
        _playerConstructed = content.PlayerConstructed;
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _slayer2 = content.Slayer2 ?? SlayerName.None;
        _aosElementDamages = content.AosElementDamages ?? AosElementAttributesDefaultValue();
        _engravedText = content.EngravedText;
        _extendedWeaponAttributes = content.ExtendedWeaponAttributes ?? ExtendedWeaponAttributesDefaultValue();
        _negativeAttributes = content.NegativeAttributes ?? NegativeAttributesDefaultValue();
        _unwieldyOriginalWeight = -1;
    }

    private void MigrateFrom(V13Content content)
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
        _playerConstructed = content.PlayerConstructed;
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _slayer2 = content.Slayer2 ?? SlayerName.None;
        _aosElementDamages = content.AosElementDamages ?? AosElementAttributesDefaultValue();
        _engravedText = content.EngravedText;
        _extendedWeaponAttributes = content.ExtendedWeaponAttributes ?? ExtendedWeaponAttributesDefaultValue();
        _negativeAttributes = content.NegativeAttributes ?? NegativeAttributesDefaultValue();
        _unwieldyOriginalWeight = content.UnwieldyOriginalWeight ?? -1;
        _absorptionAttributes = AbsorptionAttributesDefaultValue();
    }
}
