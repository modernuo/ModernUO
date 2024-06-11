using Server.Gumps;
using Server.Network;

namespace Server.Items;

public interface IGumpToggleItem
{
    public bool IsLockedDown { get; }
    public bool TurnedOn { get; set; }
}

public sealed class TurnOnGump : TurnOnOffGump<TurnOnGump>
{
    public TurnOnGump(IGumpToggleItem item) : base(item)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        base.BuildLayout(ref builder);

        builder.AddHtmlLocalized(45, 20, 300, 35, 1011034); // Activate this item
    }
}

public sealed class TurnOffGump : TurnOnOffGump<TurnOffGump>
{
    public TurnOffGump(IGumpToggleItem item) : base(item)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        base.BuildLayout(ref builder);

        builder.AddHtmlLocalized(45, 20, 300, 35, 1011035); // Deactivate this item
    }
}

public abstract class TurnOnOffGump<T> : StaticGump<T> where T : TurnOnOffGump<T>
{
    protected readonly IGumpToggleItem _item;

    public TurnOnOffGump(IGumpToggleItem item) : base(150, 200) => _item = item;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(0, 0, 300, 150, 0xA28);

        builder.AddButton(40, 53, 0xFA5, 0xFA7, 1);
        builder.AddHtmlLocalized(80, 55, 65, 35, 1011036); // OKAY

        builder.AddButton(150, 53, 0xFA5, 0xFA7, 0);
        builder.AddHtmlLocalized(190, 55, 100, 35, 1011012); // CANCEL
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID != 1)
        {
            from.SendLocalizedMessage(502694); // Cancelled action.
            return;
        }

        var newValue = !_item.TurnedOn;
        _item.TurnedOn = newValue;

        if (newValue && !_item.IsLockedDown)
        {
            from.SendLocalizedMessage(502693); // Remember, this only works when locked down.
        }
    }
}
