namespace Server.ContextMenus;

public class OpenBackpackEntry : ContextMenuEntry
{
    public OpenBackpackEntry() : base(6145)
    {
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        from.Use(from.Backpack);
    }
}
