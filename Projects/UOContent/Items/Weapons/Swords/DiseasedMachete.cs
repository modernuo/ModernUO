using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class DiseasedMachete : ElvenMachete
    {
        [Constructible]
        public DiseasedMachete() => WeaponAttributes.HitPoisonArea = 25;

        public override int LabelNumber => 1073536; // Diseased Machete
    }
}
