using System;

namespace Server.Items;

public partial class BaseTalisman
{
    private void Deserialize(IGenericReader reader, int version)
    {
        var flags = (OldSaveFlag)reader.ReadEncodedInt();

        Attributes = new AosAttributes(this);
        if (GetOldSaveFlag(flags, OldSaveFlag.Attributes))
        {
            Attributes.Deserialize(reader);
        }

        SkillBonuses = new AosSkillBonuses(this);
        if (GetOldSaveFlag(flags, OldSaveFlag.SkillBonuses))
        {
            SkillBonuses.Deserialize(reader);
        }

        // Backward compatibility
        if (GetOldSaveFlag(flags, OldSaveFlag.Owner))
        {
            BlessedFor = reader.ReadEntity<Mobile>();
        }

        _protection = new TalismanAttribute();
        if (GetOldSaveFlag(flags, OldSaveFlag.Protection))
        {
            _protection.Deserialize(reader);
        }

        _killer = new TalismanAttribute();
        if (GetOldSaveFlag(flags, OldSaveFlag.Killer))
        {
            _killer.Deserialize(reader);
        }

        _summoner = new TalismanAttribute();
        if (GetOldSaveFlag(flags, OldSaveFlag.Summoner))
        {
            _summoner.Deserialize(reader);
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.Removal))
        {
            _removal = (TalismanRemoval)reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.OldKarmaLoss))
        {
            Attributes.IncreasedKarmaLoss = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.Skill))
        {
            _skill = (SkillName)reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.SuccessBonus))
        {
            _successBonus = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.ExceptionalBonus))
        {
            _exceptionalBonus = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.MaxCharges))
        {
            _maxCharges = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.Charges))
        {
            _charges = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.MaxChargeTime))
        {
            _maxChargeTime = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.ChargeTime))
        {
            _chargeTime = reader.ReadEncodedInt();
        }

        if (GetOldSaveFlag(flags, OldSaveFlag.Slayer))
        {
            _slayer = (TalismanSlayerName)reader.ReadEncodedInt();
        }

        _blessed = GetOldSaveFlag(flags, OldSaveFlag.Blessed);
    }

    private static bool GetOldSaveFlag(OldSaveFlag flags, OldSaveFlag toGet) => (flags & toGet) != 0;

    [Flags]
    private enum OldSaveFlag
    {
        None = 0x00000000,
        Attributes = 0x00000001,
        SkillBonuses = 0x00000002,
        Owner = 0x00000004,
        Protection = 0x00000008,
        Killer = 0x00000010,
        Summoner = 0x00000020,
        Removal = 0x00000040,
        OldKarmaLoss = 0x00000080,
        Skill = 0x00000100,
        SuccessBonus = 0x00000200,
        ExceptionalBonus = 0x00000400,
        MaxCharges = 0x00000800,
        Charges = 0x00001000,
        MaxChargeTime = 0x00002000,
        ChargeTime = 0x00004000,
        Blessed = 0x00008000,
        Slayer = 0x00010000
    }
}
