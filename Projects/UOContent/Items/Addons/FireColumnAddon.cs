using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FireColumnAddon : BaseAddon
    {
        [Constructible]
        public FireColumnAddon(bool bloody = false)
        {
            AddComponent(new AddonComponent(0x3A5), 0, 0, 0);
            AddComponent(new AddonComponent(0x3A5), 0, 0, 5);
            AddComponent(new AddonComponent(0x3A5), 0, 0, 10);
            AddComponent(new AddonComponent(0x3A5), 0, 0, 15);

            AddComponent(new AddonComponent(0x19BB), 0, 0, 21);
            AddComponent(new AddonComponent(0x19AB), 0, 0, 23);

            if (bloody)
            {
                AddComponent(new AddonComponent(0x122B), -2, 0, 0);
                AddComponent(new AddonComponent(0x122E), 0, -2, 0);
                AddComponent(new AddonComponent(0x122D), -1, 1, 0);
                AddComponent(new AddonComponent(0x122F), 1, -1, 0);
                AddComponent(new AddonComponent(0x122D), 0, 1, 0);
                AddComponent(new AddonComponent(0x122A), 1, 0, 0);
                AddComponent(new AddonComponent(0x122B), 2, -1, 0);
                AddComponent(new AddonComponent(0x122B), 0, 2, 0);
                AddComponent(new AddonComponent(0x122E), 1, 1, 0);
            }
        }

        public override bool ShareHue => false;
    }
}
