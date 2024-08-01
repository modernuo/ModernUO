using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class AnimalPheromone : Item
{
    [Constructible]
    public AnimalPheromone()
        : base( 0x182F )
    {
    }

    public override int LabelNumber => 1071200; //  animal pheromone
}
