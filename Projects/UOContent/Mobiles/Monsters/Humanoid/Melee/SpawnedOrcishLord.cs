using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SpawnedOrcishLord : OrcishLord
    {
        [Constructible]
        public SpawnedOrcishLord()
        {
            var pack = Backpack;

            pack?.Delete();

            NoKillAwards = true;
        }

        public override string CorpseName => "an orcish corpse";

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            c.Delete();
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            NoKillAwards = true;
        }
    }
}
