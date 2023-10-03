using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class APersonalLetterAddressedToAhie : TransientItem
{
    [Constructible]
    public APersonalLetterAddressedToAhie() : base(0x14ED, TimeSpan.FromMinutes(30)) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073128; // A personal letter addressed to: Ahie

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
