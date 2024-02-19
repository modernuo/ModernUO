using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HagCauldron : BaseAddon
{
    [Constructible]
    public HagCauldron()
    {
        AddonComponent pot;
        pot = new AddonComponent(2420);
        AddComponent(pot, 0, 0, 0); // pot w/ support

        AddonComponent fire;
        fire = new AddonComponent(4012); // fire pit
        fire.Light = LightType.Circle150;
        AddComponent(fire, 0, 0, 0);
    }
}
