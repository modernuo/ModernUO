namespace Server.Items;

public partial class BaseClothing
{
    private void MigrateFrom(V6Content content)
    {
        _resource = content.RawResource ?? DefaultResource;
        _attributes = content.Attributes ?? AttributesDefaultValue();
        _clothingAttributes = content.ClothingAttributes ?? ClothingAttributesDefaultValue();
        _skillBonuses = content.SkillBonuses ?? SkillBonusesDefaultValue();
        _resistances = content.Resistances ?? ResistancesDefaultValue();
        _maxHitPoints = content.MaxHitPoints ?? 0;
        _playerConstructed = content.PlayerConstructed;
        Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, content.Crafter);
        _quality = content.Quality ?? ClothingQuality.Regular;
        _strReq = content.StrRequirement ?? -1;
    }

    // Version 5 (pre-codegen)
    private void Deserialize(IGenericReader reader, int version)
    {
        var flags = (OldSaveFlag)reader.ReadEncodedInt();

        if (GetSaveFlag(flags, OldSaveFlag.Resource))
        {
            _resource = (CraftResource)reader.ReadEncodedInt();
        }
        else
        {
            _resource = DefaultResource;
        }

        Attributes = new AosAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.Attributes))
        {
            Attributes.Deserialize(reader);
        }

        ClothingAttributes = new AosArmorAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.ClothingAttributes))
        {
            ClothingAttributes.Deserialize(reader);
        }

        SkillBonuses = new AosSkillBonuses(this);

        if (GetSaveFlag(flags, OldSaveFlag.SkillBonuses))
        {
            SkillBonuses.Deserialize(reader);
        }

        Resistances = new AosElementAttributes(this);

        if (GetSaveFlag(flags, OldSaveFlag.Resistances))
        {
            Resistances.Deserialize(reader);
        }

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
            _quality = (ClothingQuality)reader.ReadEncodedInt();
        }

        if (GetSaveFlag(flags, OldSaveFlag.StrReq))
        {
            _strReq = reader.ReadEncodedInt();
        }

        PlayerConstructed = GetSaveFlag(flags, OldSaveFlag.PlayerConstructed);
    }
}
