using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class AegisOfGrace : DragonHelm
{
    [Constructible]
    public AegisOfGrace()
    {
        SkillBonuses.SetValues(0, SkillName.MagicResist, 10.0);

        Attributes.DefendChance = 20;

        ArmorAttributes.SelfRepair = 2;
    }

    public override int LabelNumber => 1075047; // Aegis of Grace

    public override int BasePhysicalResistance => 10;
    public override int BaseFireResistance => 9;
    public override int BaseColdResistance => 7;
    public override int BasePoisonResistance => 7;
    public override int BaseEnergyResistance => 15;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Dragon;
    public override CraftResource DefaultResource => CraftResource.Iron;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowElvesOnly;
}
