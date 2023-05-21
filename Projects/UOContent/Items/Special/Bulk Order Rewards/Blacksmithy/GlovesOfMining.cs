using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
[Flippable(0x13c6, 0x13ce)]
public partial class LeatherGlovesOfMining : BaseGlovesOfMining
{
    [Constructible]
    public LeatherGlovesOfMining(int bonus) : base(bonus, 0x13C6) => Weight = 1;

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 3;

    public override int InitMinHits => 30;
    public override int InitMaxHits => 40;

    public override int AosStrReq => 20;
    public override int OldStrReq => 10;

    public override int ArmorBase => 13;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override int LabelNumber => 1045122; // leather blacksmith gloves of mining
}

[SerializationGenerator(0, false)]
[Flippable(0x13d5, 0x13dd)]
public partial class StuddedGlovesOfMining : BaseGlovesOfMining
{
    [Constructible]
    public StuddedGlovesOfMining(int bonus) : base(bonus, 0x13D5) => Weight = 2;

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 4;

    public override int InitMinHits => 35;
    public override int InitMaxHits => 45;

    public override int AosStrReq => 25;
    public override int OldStrReq => 25;

    public override int ArmorBase => 16;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override int LabelNumber => 1045123; // studded leather blacksmith gloves of mining
}

[SerializationGenerator(0, false)]
[Flippable(0x13eb, 0x13f2)]
public partial class RingmailGlovesOfMining : BaseGlovesOfMining
{
    [Constructible]
    public RingmailGlovesOfMining(int bonus) : base(bonus, 0x13EB) => Weight = 1;

    public override int BasePhysicalResistance => 3;
    public override int BaseFireResistance => 3;
    public override int BaseColdResistance => 1;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 3;

    public override int InitMinHits => 40;
    public override int InitMaxHits => 50;

    public override int AosStrReq => 40;
    public override int OldStrReq => 20;

    public override int OldDexBonus => -1;

    public override int ArmorBase => 22;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Ringmail;

    public override int LabelNumber => 1045124; // ringmail blacksmith gloves of mining
}

[SerializationGenerator(0, false)]
public abstract partial class BaseGlovesOfMining : BaseArmor
{
    private SkillMod _skillMod;

    public BaseGlovesOfMining(int bonus, int itemID) : base(itemID)
    {
        _bonus = bonus;

        // TODO: Color weighted by rarity?
        Hue = CraftResources.GetRandomResource(CraftResource.DullCopper, CraftResource.Valorite)?.Hue ?? 0;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Bonus
    {
        get => _bonus;
        set
        {
            _bonus = value;
            InvalidateProperties();
            this.MarkDirty();

            if (_bonus == 0)
            {
                _skillMod?.Remove();

                _skillMod = null;
            }
            else if (_skillMod == null && Parent is Mobile mobile)
            {
                _skillMod = new DefaultSkillMod(SkillName.Mining, "MiningGloves", true, _bonus);
                mobile.AddSkillMod(_skillMod);
            }
            else if (_skillMod != null)
            {
                _skillMod.Value = _bonus;
            }
        }
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (_bonus != 0 && parent is Mobile mobile)
        {
            _skillMod?.Remove();

            _skillMod = new DefaultSkillMod(SkillName.Mining, "MiningGloves", true, _bonus);
            mobile.AddSkillMod(_skillMod);
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        _skillMod?.Remove();
        _skillMod = null;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_bonus != 0)
        {
            list.Add(1062005, _bonus); // mining bonus +~1_val~
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_bonus != 0 && Parent is Mobile mobile)
        {
            _skillMod = new DefaultSkillMod(SkillName.Mining, "MiningGloves", true, _bonus);
            mobile.AddSkillMod(_skillMod);
        }
    }
}
