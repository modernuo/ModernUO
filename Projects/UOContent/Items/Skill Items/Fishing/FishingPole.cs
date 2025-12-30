using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Harvest;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FishingPole : Item
{
    [Constructible]
    public FishingPole() : base(0x0DC0) => Layer = Layer.TwoHanded;

    public override double DefaultWeight => 8.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from))
        {
            var loc = GetWorldLocation();

            if (!from.InLOS(loc) || !from.InRange(loc, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that
                return;
            }
        }

        Fishing.System.BeginHarvesting(from, this);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        BaseHarvestTool.AddContextMenuEntries(from, this, ref list, Fishing.System);
    }

    public override bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
    {
        if (base.CheckConflictingLayer(m, item, layer))
        {
            return true;
        }

        if (layer == Layer.OneHanded)
        {
            m.SendLocalizedMessage(500214); // You already have something in both hands.
            return true;
        }

        return false;
    }
}
