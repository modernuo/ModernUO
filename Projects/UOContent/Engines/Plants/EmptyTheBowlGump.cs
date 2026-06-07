using Server.Engines.Help;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Plants;

public class EmptyTheBowlGump : DynamicGump
{
    private readonly PlantItem _plant;

    public override bool Singleton => true;

    private EmptyTheBowlGump(PlantItem plant) : base(20, 20)
    {
        _plant = plant;
    }

    public static void DisplayTo(Mobile from, PlantItem plant)
    {
        if (from?.NetState == null || plant == null || plant.Deleted)
        {
            return;
        }

        from.SendGump(new EmptyTheBowlGump(plant));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(50, 50, 200, 150, 0xE10);

        builder.AddItem(45, 45, 0xCEF);
        builder.AddItem(45, 118, 0xCF0);

        builder.AddItem(211, 45, 0xCEB);
        builder.AddItem(211, 118, 0xCEC);

        builder.AddLabel(90, 70, 0x44, "Empty the bowl?");

        builder.AddItem(90, 100, 0x1602);
        builder.AddImage(140, 102, 0x15E1);
        builder.AddItem(160, 100, 0x15FD);

        if (_plant.PlantStatus != PlantStatus.BowlOfDirt && _plant.PlantStatus < PlantStatus.Plant)
        {
            builder.AddItem(156, 130, 0xDCF); // Seed
        }

        builder.AddButton(98, 150, 0x47E, 0x480, 1); // Cancel

        builder.AddButton(138, 151, 0xD2, 0xD2, 2); // Help
        builder.AddLabel(143, 151, 0x835, "?");

        builder.AddButton(168, 150, 0x481, 0x483, 3); // Ok
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID == 0 || _plant.Deleted || _plant.PlantStatus >= PlantStatus.DecorativePlant)
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
                    MainPlantGump.DisplayTo(from, _plant);
                    break;
                }
            case 2: // Help
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.EmptyingBowl);
                    from.SendGump(this);
                    break;
                }
            case 3: // Ok
                {
                    var bowl = new PlantBowl();

                    if (!from.PlaceInBackpack(bowl))
                    {
                        bowl.Delete();

                        _plant.LabelTo(from, 1053047); // You cannot empty a bowl with a full pack!
                        MainPlantGump.DisplayTo(from, _plant);
                        break;
                    }

                    if (_plant.PlantStatus != PlantStatus.BowlOfDirt && _plant.PlantStatus < PlantStatus.Plant)
                    {
                        var seed = new Seed(_plant.PlantType, _plant.PlantHue, _plant.ShowType);

                        if (!from.PlaceInBackpack(seed))
                        {
                            bowl.Delete();
                            seed.Delete();

                            _plant.LabelTo(from, 1053047); // You cannot empty a bowl with a full pack!
                            MainPlantGump.DisplayTo(from, _plant);
                            break;
                        }
                    }

                    _plant.Delete();
                    break;
                }
        }
    }
}
