using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HolySword : Longsword
{
    [Constructible]
    public HolySword()
    {
        Hue = 0x482;
        LootType = LootType.Blessed;

        Slayer = SlayerName.Silver;

        Attributes.WeaponDamage = 40;
        WeaponAttributes.SelfRepair = 10;
        WeaponAttributes.LowerStatReq = 100;
        WeaponAttributes.UseBestSkill = 1;
    }

    public override int LabelNumber => 1062921; // The Holy Sword

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
