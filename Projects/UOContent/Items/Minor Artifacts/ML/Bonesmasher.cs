using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Bonesmasher : DiamondMace
{
    [Constructible]
    public Bonesmasher()
    {
        ItemID = 0x2D30;
        Hue = 0x482;

        SkillBonuses.SetValues(0, SkillName.Macing, 10.0);

        WeaponAttributes.HitLeechMana = 40;
        WeaponAttributes.SelfRepair = 2;
    }

    public override int LabelNumber => 1075030; // Bonesmasher

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
