using ModernUO.Serialization;

namespace Server.Items;

[TypeAlias("Server.Items.TelekinisisScroll")]
[SerializationGenerator(0, false)]
public partial class TelekinesisScroll : SpellScroll
{
    [Constructible]
    public TelekinesisScroll(int amount = 1) : base(20, 0x1F41, amount)
    {
    }
}
