namespace Server.Items
{
    [Serializable(0, false)]
    public partial class AquariumFood : Item
    {
        [Constructible]
        public AquariumFood() : base(0xEFC)
        {
        }

        public override int LabelNumber => 1074819; // Aquarium food
    }
}
