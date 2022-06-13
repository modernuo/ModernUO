using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class OrcishMachete : ElvenMachete
    {
        [Constructible]
        public OrcishMachete()
        {
            Attributes.BonusInt = -5;
            Attributes.WeaponDamage = 10;
        }

        public override int LabelNumber => 1073534; // Orcish Machete
    }
}
