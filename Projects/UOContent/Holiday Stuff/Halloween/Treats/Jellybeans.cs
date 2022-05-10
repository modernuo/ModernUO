using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class JellyBeans : CandyCane
    {
        [Constructible]
        public JellyBeans(int amount = 1) : base(0x468C) => Stackable = true;

        public override int LabelNumber => 1096932; /* jellybeans */
    }
}
