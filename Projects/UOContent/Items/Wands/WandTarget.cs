using Server.Items;

namespace Server.Targeting;

public class WandTarget : Target
{
    private readonly BaseWand m_Item;

    public WandTarget(BaseWand item) : base(6, false, TargetFlags.None) => m_Item = item;

    protected override void OnTarget(Mobile from, object targeted)
    {
        m_Item.DoWandTarget(from, targeted);
    }
}