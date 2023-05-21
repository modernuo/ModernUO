using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastVein : PlagueBeastComponent
{
    private bool _cutting;

    [SerializableField(0, setter: "private")]
    private bool _cut;

    public PlagueBeastVein(int itemID, int hue) : base(itemID, hue) => _cut = false;

    public override bool Scissor(Mobile from, Scissors scissors)
    {
        if (IsAccessibleTo(from))
        {
            if (!_cut && !_cutting)
            {
                _cutting = true;
                Timer.StartTimer(TimeSpan.FromSeconds(3), () => CuttingDone(from));
                // You begin cutting through the vein.
                scissors.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071899);
                return true;
            }

            // This vein has already been cut.
            scissors.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071900);
        }

        return false;
    }

    private void CuttingDone(Mobile from)
    {
        _cutting = false;
        Cut = true;

        ItemID = ItemID == 0x1B1C ? 0x1B1B : 0x1B1C;

        Owner?.PlaySound(0x199);

        if (Organ is PlagueBeastRubbleOrgan organ)
        {
            organ.OnVeinCut(from, this);
        }
    }
}
