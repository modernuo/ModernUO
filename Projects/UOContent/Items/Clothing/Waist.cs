using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseWaist : BaseClothing
    {
        public BaseWaist(int itemID, int hue = 0) : base(itemID, Layer.Waist, hue)
        {
        }
    }

    [Flippable(0x153b, 0x153c)]
    [SerializationGenerator(0, false)]
    public partial class HalfApron : BaseWaist
    {
        [Constructible]
        public HalfApron(int hue = 0) : base(0x153b, hue) => Weight = 2.0;
    }

    [Flippable(0x27A0, 0x27EB)]
    [SerializationGenerator(0, false)]
    public partial class Obi : BaseWaist
    {
        [Constructible]
        public Obi(int hue = 0) : base(0x27A0, hue) => Weight = 1.0;
    }

    [Flippable(0x2B68, 0x315F)]
    [SerializationGenerator(0)]
    public partial class WoodlandBelt : BaseWaist
    {
        [Constructible]
        public WoodlandBelt(int hue = 0) : base(0x2B68, hue) => Weight = 4.0;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        public override bool Scissor(Mobile from, Scissors scissors)
        {
            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }
    }
}
