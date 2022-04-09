using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SerpentPillarAddon : BaseAddon
    {
        [Constructible]
        public SerpentPillarAddon()
        {
            AddComponent(new AddonComponent(9020), -2, +1, 0);
            AddComponent(new AddonComponent(9021), -2, +0, 0);
            AddComponent(new AddonComponent(9022), -2, -1, 0);
            AddComponent(new AddonComponent(9023), -2, -2, 0);
            AddComponent(new AddonComponent(9024), -1, -2, 0);
            AddComponent(new AddonComponent(9025), +0, -2, 0);
            AddComponent(new AddonComponent(9026), +1, -2, 0);

            AddComponent(new AddonComponent(9027), -1, +1, 0);
            AddComponent(new AddonComponent(9028), -1, +0, 0);
            AddComponent(new AddonComponent(9029), -1, -1, 0);

            AddComponent(new AddonComponent(9030), +0, +1, 0);
            AddComponent(new AddonComponent(9031), +0, +0, 0);
            AddComponent(new AddonComponent(9032), +0, -1, 0);

            AddComponent(new AddonComponent(9033), +1, +0, 0);
            AddComponent(new AddonComponent(9034), +1, -1, 0);
        }
    }
}
