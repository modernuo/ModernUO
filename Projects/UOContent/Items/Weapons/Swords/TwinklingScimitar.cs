namespace Server.Items
{
    [Serializable(0)]
    public partial class TwinklingScimitar : RadiantScimitar
    {
        [Constructible]
        public TwinklingScimitar() => Attributes.DefendChance = 6;

        public override int LabelNumber => 1073544; // twinkling scimitar
    }
}
