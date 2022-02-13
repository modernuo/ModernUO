using AMA = Server.Items.ArmorMeditationAllowance;

namespace Server.Items;

public partial class BaseArmor
{
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
        _crafter = content.Crafter?.RawName; // Convert from Mobile -> String via RawName
        _quality = content.Quality ?? ArmorQuality.Regular;
        _durability = content.Durability ?? ArmorDurabilityLevel.Regular;
        _rawResource = content.RawResource ?? DefaultResource;
        _armorBase = content.BaseArmorRating ?? -1;
        _strBonus = content.StrBonus ?? -1;
        _dexBonus = content.DexBonus ?? -1;
        _intBonus = content.IntBonus ?? -1;
        _strReq = content.StrRequirement ?? -1;
        _dexReq = content.DexRequirement ?? -1;
        _intReq = content.IntRequirement ?? -1;
        _meditate = content.MeditationAllowance ?? (AMA)(-1);
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _playerConstructed = content.PlayerConstructed;
    }
}
