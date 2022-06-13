using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x1EB8, 0x1EB9)]
    [SerializationGenerator(0, false)]
    public partial class TinkerTools : BaseTool
    {
        [Constructible]
        public TinkerTools() : base(0x1EB8) => Weight = 1.0;

        [Constructible]
        public TinkerTools(int uses) : base(uses, 0x1EB8) => Weight = 1.0;

        public override CraftSystem CraftSystem => DefTinkering.CraftSystem;
    }

    [SerializationGenerator(0, false)]
    public partial class TinkersTools : BaseTool
    {
        [Constructible]
        public TinkersTools() : base(0x1EBC) => Weight = 1.0;

        [Constructible]
        public TinkersTools(int uses) : base(uses, 0x1EBC) => Weight = 1.0;

        public override CraftSystem CraftSystem => DefTinkering.CraftSystem;
    }
}
