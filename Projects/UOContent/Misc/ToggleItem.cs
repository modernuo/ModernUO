using ModernUO.Serialization;
using Server.Commands.Generic;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ToggleItem : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _inactiveItemId;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _activeItemId;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _playersCanToggle;

    [Constructible]
    public ToggleItem(int inactiveItemID, int activeItemID, bool playersCanToggle = false) : base(inactiveItemID)
    {
        Movable = false;

        _inactiveItemId = inactiveItemID;
        _activeItemId = activeItemID;
        _playersCanToggle = playersCanToggle;
    }

    public static void Configure()
    {
        TargetCommands.Register(new ToggleCommand());
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            Toggle();
        }
        else if (!PlayersCanToggle)
        {
            return;
        }

        if (from.InRange(GetWorldLocation(), 1))
        {
            Toggle();
        }
        else
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
    }

    public void Toggle()
    {
        ItemID = ItemID == _activeItemId ? _inactiveItemId : _activeItemId;
        Visible = ItemID != 0x1;
    }

    public class ToggleCommand : BaseCommand
    {
        public ToggleCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllItems;
            Commands = ["Toggle"];
            ObjectTypes = ObjectTypes.Items;
            Usage = "Toggle";
            Description = "Toggles a targeted ToggleItem.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is ToggleItem item)
            {
                item.Toggle();
                AddResponse("The item has been toggled.");
            }
            else
            {
                LogFailure("That is not a ToggleItem.");
            }
        }
    }
}
