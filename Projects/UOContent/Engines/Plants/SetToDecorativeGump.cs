using Server.Engines.Help;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Plants;

public class SetToDecorativeGump : StaticGump<SetToDecorativeGump>
{
    private readonly PlantItem _plant;

    public override bool Singleton => true;

    private SetToDecorativeGump(PlantItem plant) : base(20, 20)
    {
        _plant = plant;
    }

    public static void DisplayTo(Mobile from, PlantItem plant)
    {
        if (from?.NetState == null || plant == null || plant.Deleted)
        {
            return;
        }

        from.SendGump(new SetToDecorativeGump(plant));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(50, 50, 200, 150, 0xE10);

        builder.AddItem(25, 45, 0xCEB);
        builder.AddItem(25, 118, 0xCEC);

        builder.AddItem(227, 45, 0xCEF);
        builder.AddItem(227, 118, 0xCF0);

        builder.AddLabel(115, 85, 0x44, "Set plant");
        builder.AddLabel(82, 105, 0x44, "to decorative mode?");

        builder.AddButton(98, 140, 0x47E, 0x480, 1); // Cancel

        builder.AddButton(138, 141, 0xD2, 0xD2, 2); // Help
        builder.AddLabel(143, 141, 0x835, "?");

        builder.AddButton(168, 140, 0x481, 0x483, 3); // Ok
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID == 0 || _plant.Deleted || _plant.PlantStatus != PlantStatus.Stage9)
        {
            return;
        }

        if (info.ButtonID == 3 && !from.InRange(_plant.GetWorldLocation(), 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 500446); // That is too far away.
            return;
        }

        if (!_plant.IsUsableBy(from))
        {
            _plant.LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
            return;
        }

        switch (info.ButtonID)
        {
            case 1: // Cancel
                {
                    ReproductionGump.DisplayTo(from, _plant);
                    break;
                }
            case 2: // Help
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.DecorativeMode);
                    from.SendGump(this);
                    break;
                }
            case 3: // Ok
                {
                    _plant.PlantStatus = PlantStatus.DecorativePlant;
                    // You prune the plant.
                    // This plant will no longer produce resources or seeds, but will require no upkeep.
                    _plant.LabelTo(from, 1053077);
                    break;
                }
        }
    }
}
