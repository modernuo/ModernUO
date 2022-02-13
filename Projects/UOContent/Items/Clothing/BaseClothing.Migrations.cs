namespace Server.Items;

public partial class BaseClothing
{
    private void MigrateFrom(V6Content content)
    {
        _rawResource = content.RawResource ?? DefaultResource;
        _attributes = content.Attributes ?? AttributesDefaultValue();
        _clothingAttributes = content.ClothingAttributes ?? ClothingAttributesDefaultValue();
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _resistances = content.Resistances ?? ResistancesDefaultValue();
        _maxHitPoints = content.MaxHitPoints ?? 0;
        _playerConstructed = content.PlayerConstructed;
        _crafter = content.Crafter?.RawName; // Convert from Mobile -> String via RawName
        _quality = content.Quality ?? ClothingQuality.Regular;
        _strReq = content.StrRequirement ?? -1;
    }
}
