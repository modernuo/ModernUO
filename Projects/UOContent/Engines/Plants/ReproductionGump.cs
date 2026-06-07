using Server.Engines.Help;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Plants;

public class ReproductionGump : DynamicGump
{
    private readonly PlantItem _plant;

    public override bool Singleton => true;

    private ReproductionGump(PlantItem plant) : base(20, 20)
    {
        _plant = plant;
    }

    public static void DisplayTo(Mobile from, PlantItem plant)
    {
        if (from?.NetState == null || plant == null || plant.Deleted)
        {
            return;
        }

        from.SendGump(new ReproductionGump(plant));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(50, 50, 200, 150, 0xE10);

        builder.AddImage(60, 90, 0xE17);
        builder.AddImage(120, 90, 0xE17);

        builder.AddImage(60, 145, 0xE17);
        builder.AddImage(120, 145, 0xE17);

        builder.AddItem(45, 45, 0xCEF);
        builder.AddItem(45, 118, 0xCF0);

        builder.AddItem(211, 45, 0xCEB);
        builder.AddItem(211, 118, 0xCEC);

        builder.AddButton(70, 67, 0xD4, 0xD4, 1); // Main menu
        builder.AddItem(57, 65, 0x1600);

        builder.AddLabel(108, 67, 0x835, "Reproduction");

        if (_plant.PlantStatus == PlantStatus.Stage9)
        {
            builder.AddButton(212, 67, 0xD4, 0xD4, 2); // Set to decorative
            builder.AddItem(202, 68, 0xC61);
            builder.AddLabel(216, 66, 0x21, "/");
        }

        builder.AddButton(80, 116, 0xD4, 0xD4, 3); // Pollination
        builder.AddItem(66, 117, 0x1AA2);
        AddPollinationState(ref builder, 106, 116);

        builder.AddButton(128, 116, 0xD4, 0xD4, 4); // Resources
        builder.AddItem(113, 120, 0x1021);
        AddResourcesState(ref builder, 149, 116);

        builder.AddButton(177, 116, 0xD4, 0xD4, 5); // Seeds
        builder.AddItem(160, 121, 0xDCF);
        AddSeedsState(ref builder, 199, 116);

        builder.AddButton(70, 163, 0xD2, 0xD2, 6); // Gather pollen
        builder.AddItem(56, 164, 0x1AA2);

        builder.AddButton(138, 163, 0xD2, 0xD2, 7); // Gather resources
        builder.AddItem(123, 167, 0x1021);

        builder.AddButton(212, 163, 0xD2, 0xD2, 8); // Gather seeds
        builder.AddItem(195, 168, 0xDCF);
    }

    private void AddPollinationState(ref DynamicGumpBuilder builder, int x, int y)
    {
        var system = _plant.PlantSystem;

        if (!system.PollenProducing)
        {
            builder.AddLabel(x, y, 0x35, "-");
        }
        else if (!system.Pollinated)
        {
            builder.AddLabel(x, y, 0x21, "!");
        }
        else
        {
            builder.AddLabel(x, y, 0x3F, "+");
        }
    }

    private void AddResourcesState(ref DynamicGumpBuilder builder, int x, int y)
    {
        var resInfo = PlantResourceInfo.GetInfo(_plant.PlantType, _plant.PlantHue);

        var system = _plant.PlantSystem;
        var totalResources = system.AvailableResources + system.LeftResources;

        if (resInfo == null || totalResources == 0)
        {
            builder.AddLabel(x + 5, y, 0x21, "X");
        }
        else
        {
            builder.AddLabel(
                x,
                y,
                PlantHueInfo.GetInfo(_plant.PlantHue).GumpHue,
                $"{system.AvailableResources}/{totalResources}"
            );
        }
    }

    private void AddSeedsState(ref DynamicGumpBuilder builder, int x, int y)
    {
        var system = _plant.PlantSystem;
        var totalSeeds = system.AvailableSeeds + system.LeftSeeds;

        if (!_plant.Reproduces || totalSeeds == 0)
        {
            builder.AddLabel(x + 5, y, 0x21, "X");
        }
        else
        {
            builder.AddLabel(
                x,
                y,
                PlantHueInfo.GetInfo(system.SeedHue).GumpHue,
                $"{system.AvailableSeeds}/{totalSeeds}"
            );
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID == 0 || _plant.Deleted || _plant.PlantStatus is >= PlantStatus.DecorativePlant or PlantStatus.BowlOfDirt)
        {
            return;
        }

        if (info.ButtonID >= 6 && info.ButtonID <= 8 && !from.InRange(_plant.GetWorldLocation(), 3))
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
            case 1: // Main menu
                {
                    MainPlantGump.DisplayTo(from, _plant);
                    return;
                }
            case 2: // Set to decorative
                {
                    if (_plant.PlantStatus == PlantStatus.Stage9)
                    {
                        SetToDecorativeGump.DisplayTo(from, _plant);
                    }
                    return;
                }
            case 3: // Pollination
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.PollinationState);
                    break;
                }
            case 4: // Resources
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.ResourceProduction);
                    break;
                }
            case 5: // Seeds
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.SeedProduction);
                    break;
                }
            case 6: // Gather pollen
                {
                    if (!_plant.IsCrossable)
                    {
                        _plant.LabelTo(from, 1053050); // You cannot gather pollen from a mutated plant!
                    }
                    else if (!_plant.PlantSystem.PollenProducing)
                    {
                        // You cannot gather pollen from a plant in this stage of development!
                        _plant.LabelTo(from, 1053051);
                    }
                    else if (_plant.PlantSystem.Health < PlantHealth.Healthy)
                    {
                        _plant.LabelTo(from, 1053052); // You cannot gather pollen from an unhealthy plant!
                    }
                    else
                    {
                        from.Target = new PollinateTarget(_plant);
                        from.SendLocalizedMessage(1053054); // Target the plant you wish to cross-pollinate to.
                        return;
                    }

                    break;
                }
            case 7: // Gather resources
                {
                    var resInfo = PlantResourceInfo.GetInfo(_plant.PlantType, _plant.PlantHue);
                    var system = _plant.PlantSystem;

                    if (resInfo == null)
                    {
                        if (_plant.IsCrossable)
                        {
                            _plant.LabelTo(from, 1053056); // This plant has no resources to gather!
                        }
                        else
                        {
                            _plant.LabelTo(from, 1053055); // Mutated plants do not produce resources!
                        }
                    }
                    else if (system.AvailableResources == 0)
                    {
                        _plant.LabelTo(from, 1053056); // This plant has no resources to gather!
                    }
                    else
                    {
                        var resource = resInfo.CreateResource();

                        if (from.PlaceInBackpack(resource))
                        {
                            system.AvailableResources--;
                            _plant.LabelTo(from, 1053059); // You gather resources from the plant.
                        }
                        else
                        {
                            resource.Delete();
                            // You attempt to gather as many resources as you can hold, but your backpack is full.
                            _plant.LabelTo(from, 1053058);
                        }
                    }

                    break;
                }
            case 8: // Gather seeds
                {
                    var system = _plant.PlantSystem;

                    if (!_plant.Reproduces)
                    {
                        _plant.LabelTo(from, 1053060); // Mutated plants do not produce seeds!
                    }
                    else if (system.AvailableSeeds == 0)
                    {
                        _plant.LabelTo(from, 1053061); // This plant has no seeds to gather!
                    }
                    else
                    {
                        var seed = new Seed(system.SeedType, system.SeedHue, true);

                        if (from.PlaceInBackpack(seed))
                        {
                            system.AvailableSeeds--;
                            _plant.LabelTo(from, 1053063); // You gather seeds from the plant.
                        }
                        else
                        {
                            seed.Delete();
                            // You attempt to gather as many seeds as you can hold, but your backpack is full.
                            _plant.LabelTo(from, 1053062);
                        }
                    }

                    break;
                }
        }

        from.SendGump(this);
    }
}
