using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CrateForSledge : TransientItem
{
    [Constructible]
    public CrateForSledge() : base(0x1FFF, TimeSpan.FromHours(1)) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074520; // Crate for Sledge

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
