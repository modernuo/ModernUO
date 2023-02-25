using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MalletAndChisel : BaseTool
{
    [Constructible]
    public MalletAndChisel() : base(0x12B3) => Weight = 1.0;

    [Constructible]
    public MalletAndChisel(int uses) : base(uses, 0x12B3) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefMasonry.CraftSystem;
}
