using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ProtectorsEssence : Item
{
    [Constructible]
    public ProtectorsEssence() : base(0x23F)
    {
    }

    public override int LabelNumber => 1073159; // Protector's Essence
}
