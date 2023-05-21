using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RoseInAVase : Item /* TODO: when dye tub changes are implemented, furny dyable this */
{
    [Constructible]
    public RoseInAVase() : base(0x0EB0)
    {
        Hue = 0x20;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1023760; // A Rose in a Vase	1023760
}
