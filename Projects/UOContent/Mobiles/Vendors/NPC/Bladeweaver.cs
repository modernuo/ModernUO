using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Bladeweaver : BaseVendor
{
    private readonly List<SBInfo> m_SBInfos = new();

    [Constructible]
    public Bladeweaver() : base("the bladeweaver")
    {
        SetSkill(SkillName.Throwing, 85.0, 100.0);
        SetSkill(SkillName.Tactics, 85.0, 100.0);
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override NpcGuild NpcGuild => NpcGuild.WarriorsGuild;

    public override void InitSBInfo()
    {
        m_SBInfos.Add(new SBBladeweaverWeapon());
    }

    public override void InitOutfit()
    {
        AddItem(new GargishLeatherArmsType1());
        AddItem(new GargishLeatherChestType1());
        AddItem(new GargishLeatherLegsType1());
        AddItem(new Boomerang());

        PackGold(100, 200);
    }
}
