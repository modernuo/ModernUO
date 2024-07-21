namespace Server.ContextMenus;

public class PaperdollEntry : ContextMenuEntry
{
    public PaperdollEntry() : base(6123, 18)
    {
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        if (target is Mobile mobile && mobile.CanPaperdollBeOpenedBy(from))
        {
            mobile.DisplayPaperdollTo(from);
        }
    }
}
