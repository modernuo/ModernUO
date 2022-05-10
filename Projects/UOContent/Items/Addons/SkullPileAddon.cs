using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SkullPileAddon : BaseAddon
    {
        [Constructible]
        public SkullPileAddon()
        {
            AddComponent(new AddonComponent(6872), 1, 1, 0);
            AddComponent(new AddonComponent(6873), 0, 1, 0);
            AddComponent(new AddonComponent(6874), -1, 1, 0);
            AddComponent(new AddonComponent(6875), 0, 0, 0);
            AddComponent(new AddonComponent(6876), 1, 0, 0);
            AddComponent(new AddonComponent(6877), 1, -1, 0);
            AddComponent(new AddonComponent(6878), 2, -1, 0);
            AddComponent(new AddonComponent(6879), 2, 0, 0);
        }
    }
}
