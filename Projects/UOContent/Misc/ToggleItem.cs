using Server.Commands.Generic;

namespace Server.Items
{
    public class ToggleItem : Item
    {
        [Constructible]
        public ToggleItem(int inactiveItemID, int activeItemID, bool playersCanToggle = false)
            : base(inactiveItemID)
        {
            Movable = false;

            InactiveItemID = inactiveItemID;
            ActiveItemID = activeItemID;
            PlayersCanToggle = playersCanToggle;
        }

        public ToggleItem(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int InactiveItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ActiveItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PlayersCanToggle { get; set; }

        public static void Initialize()
        {
            TargetCommands.Register(new ToggleCommand());
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                Toggle();
            }
            else if (PlayersCanToggle)
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    Toggle();
                }
                else
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }
            }
        }

        public void Toggle()
        {
            ItemID = ItemID == ActiveItemID ? InactiveItemID : ActiveItemID;
            Visible = ItemID != 0x1;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(InactiveItemID);
            writer.Write(ActiveItemID);
            writer.Write(PlayersCanToggle);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            InactiveItemID = reader.ReadInt();
            ActiveItemID = reader.ReadInt();
            PlayersCanToggle = reader.ReadBool();
        }

        public class ToggleCommand : BaseCommand
        {
            public ToggleCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new[] { "Toggle" };
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
}
