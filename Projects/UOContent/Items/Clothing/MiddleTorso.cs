using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseMiddleTorso : BaseClothing
    {
        public BaseMiddleTorso(int itemID, int hue = 0) : base(itemID, Layer.MiddleTorso, hue)
        {
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1541, 0x1542)]
    public partial class BodySash : BaseMiddleTorso
    {
        [Constructible]
        public BodySash(int hue = 0) : base(0x1541, hue) => Weight = 1.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x153d, 0x153e)]
    public partial class FullApron : BaseMiddleTorso
    {
        [Constructible]
        public FullApron(int hue = 0) : base(0x153d, hue) => Weight = 4.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1f7b, 0x1f7c)]
    public partial class Doublet : BaseMiddleTorso
    {
        [Constructible]
        public Doublet(int hue = 0) : base(0x1F7B, hue) => Weight = 2.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1ffd, 0x1ffe)]
    public partial class Surcoat : BaseMiddleTorso
    {
        [Constructible]
        public Surcoat(int hue = 0) : base(0x1FFD, hue) => Weight = 6.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1fa1, 0x1fa2)]
    public partial class Tunic : BaseMiddleTorso
    {
        [Constructible]
        public Tunic(int hue = 0) : base(0x1FA1, hue) => Weight = 5.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x2310, 0x230F)]
    public partial class FormalShirt : BaseMiddleTorso
    {
        [Constructible]
        public FormalShirt(int hue = 0) : base(0x2310, hue) => Weight = 1.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1f9f, 0x1fa0)]
    public partial class JesterSuit : BaseMiddleTorso
    {
        [Constructible]
        public JesterSuit(int hue = 0) : base(0x1F9F, hue) => Weight = 4.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x27A1, 0x27EC)]
    public partial class JinBaori : BaseMiddleTorso
    {
        [Constructible]
        public JinBaori(int hue = 0) : base(0x27A1, hue) => Weight = 3.0;
    }
}
