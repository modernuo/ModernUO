using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class PyramidAddon : BaseAddon
    {
        [Constructible]
        public PyramidAddon()
        {
            AddComponent(new AddonComponent(1006), 0, 0, 5);

            for (var o = 1; o <= 2; ++o)
            {
                AddComponent(new AddonComponent(1011), -o, -o, (2 - o) * 5);
                AddComponent(new AddonComponent(1012), +o, +o, (2 - o) * 5);
                AddComponent(new AddonComponent(1013), +o, -o, (2 - o) * 5);
                AddComponent(new AddonComponent(1014), -o, +o, (2 - o) * 5);
            }

            for (var o = -1; o <= 1; ++o)
            {
                AddComponent(new AddonComponent(1007), o, 2, 0);
                AddComponent(new AddonComponent(1008), 2, o, 0);
                AddComponent(new AddonComponent(1009), o, -2, 0);
                AddComponent(new AddonComponent(1010), -2, o, 0);
            }

            AddComponent(new AddonComponent(1007), 0, 1, 5);
            AddComponent(new AddonComponent(1008), 1, 0, 5);
            AddComponent(new AddonComponent(1009), 0, -1, 5);
            AddComponent(new AddonComponent(1010), -1, 0, 5);
        }

        public override bool ShareHue => false;
    }
}
