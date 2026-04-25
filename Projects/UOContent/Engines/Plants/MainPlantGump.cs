using System;
using System.Runtime.CompilerServices;
using Server.Engines.Help;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Engines.Plants;

public class MainPlantGump : DynamicGump
{
    private readonly PlantItem _plant;

    public override bool Singleton => true;

    private MainPlantGump(PlantItem plant) : base(20, 20)
    {
        _plant = plant;
    }

    public static void DisplayTo(Mobile from, PlantItem plant)
    {
        if (from?.NetState == null || plant == null || plant.Deleted)
        {
            return;
        }

        from.SendGump(new MainPlantGump(plant));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        DrawBackground(ref builder);
        DrawPlant(ref builder);

        builder.AddButton(71, 67, 0xD4, 0xD4, 1); // Reproduction menu
        builder.AddItem(59, 68, 0xD08);

        var system = _plant.PlantSystem;

        builder.AddButton(71, 91, 0xD4, 0xD4, 2); // Infestation
        builder.AddItem(8, 96, 0x372);
        AddPlus(ref builder, 95, 92, system.Infestation);

        builder.AddButton(71, 115, 0xD4, 0xD4, 3); // Fungus
        builder.AddItem(58, 115, 0xD16);
        AddPlus(ref builder, 95, 116, system.Fungus);

        builder.AddButton(71, 139, 0xD4, 0xD4, 4); // Poison
        builder.AddItem(59, 143, 0x1AE4);
        AddPlus(ref builder, 95, 140, system.Poison);

        builder.AddButton(71, 163, 0xD4, 0xD4, 5); // Disease
        builder.AddItem(55, 167, 0x1727);
        AddPlus(ref builder, 95, 164, system.Disease);

        builder.AddButton(209, 67, 0xD2, 0xD2, 6); // Water
        builder.AddItem(193, 67, 0x1F9D);
        AddPlusMinus(ref builder, 196, 67, system.Water);

        builder.AddButton(209, 91, 0xD4, 0xD4, 7); // Poison potion
        builder.AddItem(201, 91, 0xF0A);
        AddLevel(ref builder, 196, 91, system.PoisonPotion);

        builder.AddButton(209, 115, 0xD4, 0xD4, 8); // Cure potion
        builder.AddItem(201, 115, 0xF07);
        AddLevel(ref builder, 196, 115, system.CurePotion);

        builder.AddButton(209, 139, 0xD4, 0xD4, 9); // Heal potion
        builder.AddItem(201, 139, 0xF0C);
        AddLevel(ref builder, 196, 139, system.HealPotion);

        builder.AddButton(209, 163, 0xD4, 0xD4, 10); // Strength potion
        builder.AddItem(201, 163, 0xF09);
        AddLevel(ref builder, 196, 163, system.StrengthPotion);

        builder.AddImage(48, 47, 0xD2);
        AddLevel(ref builder, 54, 47, (int)_plant.PlantStatus);

        builder.AddImage(232, 47, 0xD2);
        AddGrowthIndicator(ref builder, 239, 47);

        builder.AddButton(48, 183, 0xD2, 0xD2, 11); // Help
        builder.AddLabel(54, 183, 0x835, "?");

        builder.AddButton(232, 183, 0xD4, 0xD4, 12); // Empty the bowl
        builder.AddItem(219, 180, 0x15FD);
    }

    private static void DrawBackground(ref DynamicGumpBuilder builder)
    {
        builder.AddBackground(50, 50, 200, 150, 0xE10);

        builder.AddItem(45, 45, 0xCEF);
        builder.AddItem(45, 118, 0xCF0);

        builder.AddItem(211, 45, 0xCEB);
        builder.AddItem(211, 118, 0xCEC);
    }

    private void DrawPlant(ref DynamicGumpBuilder builder)
    {
        var status = _plant.PlantStatus;

        if (status < PlantStatus.FullGrownPlant)
        {
            builder.AddImage(110, 85, 0x589);

            builder.AddItem(122, 94, 0x914);
            builder.AddItem(135, 94, 0x914);
            builder.AddItem(120, 112, 0x914);
            builder.AddItem(135, 112, 0x914);

            if (status >= PlantStatus.Stage2)
            {
                builder.AddItem(127, 112, 0xC62);
            }

            if (status is PlantStatus.Stage3 or PlantStatus.Stage4)
            {
                builder.AddItem(129, 85, 0xC7E);
            }

            if (status >= PlantStatus.Stage4)
            {
                builder.AddItem(121, 117, 0xC62);
                builder.AddItem(133, 117, 0xC62);
            }

            if (status >= PlantStatus.Stage5)
            {
                builder.AddItem(110, 100, 0xC62);
                builder.AddItem(140, 100, 0xC62);
                builder.AddItem(110, 130, 0xC62);
                builder.AddItem(140, 130, 0xC62);
            }

            if (status >= PlantStatus.Stage6)
            {
                builder.AddItem(105, 115, 0xC62);
                builder.AddItem(145, 115, 0xC62);
                builder.AddItem(125, 90, 0xC62);
                builder.AddItem(125, 135, 0xC62);
            }
        }
        else
        {
            var typeInfo = PlantTypeInfo.GetInfo(_plant.PlantType);
            var hueInfo = PlantHueInfo.GetInfo(_plant.PlantHue);

            // The large images for these trees trigger a client crash, so use a smaller, generic tree.
            if (_plant.PlantType is PlantType.CypressTwisted or PlantType.CypressStraight)
            {
                builder.AddItem(130 + typeInfo.OffsetX, 96 + typeInfo.OffsetY, 0x0CCA, hueInfo.Hue);
            }
            else
            {
                builder.AddItem(130 + typeInfo.OffsetX, 96 + typeInfo.OffsetY, typeInfo.ItemID, hueInfo.Hue);
            }
        }

        if (status != PlantStatus.BowlOfDirt)
        {
            var message = _plant.PlantSystem.GetLocalizedHealth();

            switch (_plant.PlantSystem.Health)
            {
                case PlantHealth.Dying:
                    {
                        builder.AddItem(92, 167, 0x1B9D);
                        builder.AddItem(161, 167, 0x1B9D);

                        builder.AddHtmlLocalized(136, 167, 42, 20, message, 0xFC00);
                        break;
                    }
                case PlantHealth.Wilted:
                    {
                        builder.AddItem(91, 164, 0x18E6);
                        builder.AddItem(161, 164, 0x18E6);

                        builder.AddHtmlLocalized(132, 167, 42, 20, message, 0xC207);
                        break;
                    }
                case PlantHealth.Healthy:
                    {
                        builder.AddItem(96, 168, 0xC61);
                        builder.AddItem(162, 168, 0xC61);

                        builder.AddHtmlLocalized(129, 167, 42, 20, message, 0x8200);
                        break;
                    }
                case PlantHealth.Vibrant:
                    {
                        builder.AddItem(93, 162, 0x1A99);
                        builder.AddItem(162, 162, 0x1A99);

                        builder.AddHtmlLocalized(129, 167, 42, 20, message, 0x83E0);
                        break;
                    }
            }
        }
    }

    private static void AddPlus(ref DynamicGumpBuilder builder, int x, int y, int value)
    {
        switch (value)
        {
            case 1:
                {
                    builder.AddLabel(x, y, 0x35, "+");
                    break;
                }
            case 2:
                {
                    builder.AddLabel(x, y, 0x21, "+");
                    break;
                }
        }
    }

    private static void AddPlusMinus(ref DynamicGumpBuilder builder, int x, int y, int value)
    {
        switch (value)
        {
            case 0:
                {
                    builder.AddLabel(x, y, 0x21, "-");
                    break;
                }
            case 1:
                {
                    builder.AddLabel(x, y, 0x35, "-");
                    break;
                }
            case 3:
                {
                    builder.AddLabel(x, y, 0x35, "+");
                    break;
                }
            case 4:
                {
                    builder.AddLabel(x, y, 0x21, "+");
                    break;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddLevel(ref DynamicGumpBuilder builder, int x, int y, int value) =>
        builder.AddLabel(x, y, 0x835, $"{value}");

    private void AddGrowthIndicator(ref DynamicGumpBuilder builder, int x, int y)
    {
        if (!_plant.IsGrowable)
        {
            return;
        }

        switch (_plant.PlantSystem.GrowthIndicator)
        {
            default:
            case PlantGrowthIndicator.None:
            case PlantGrowthIndicator.InvalidLocation:
                {
                    builder.AddLabel(x, y, 0x21, "!");
                    break;
                }
            case PlantGrowthIndicator.NotHealthy:
                {
                    builder.AddLabel(x, y, 0x21, "-");
                    break;
                }
            case PlantGrowthIndicator.Delay:
                {
                    builder.AddLabel(x, y, 0x35, "-");
                    break;
                }
            case PlantGrowthIndicator.Grown:
                {
                    builder.AddLabel(x, y, 0x3, "+");
                    break;
                }
            case PlantGrowthIndicator.DoubleGrown:
                {
                    builder.AddLabel(x, y, 0x3F, "+");
                    break;
                }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID == 0 || _plant.Deleted || _plant.PlantStatus >= PlantStatus.DecorativePlant)
        {
            return;
        }

        if ((info.ButtonID >= 6 && info.ButtonID <= 10 || info.ButtonID == 12) &&
            !from.InRange(_plant.GetWorldLocation(), 3))
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
            case 1: // Reproduction menu
                {
                    if (_plant.PlantStatus > PlantStatus.BowlOfDirt)
                    {
                        ReproductionGump.DisplayTo(from, _plant);
                    }
                    else
                    {
                        from.SendLocalizedMessage(1061885); // You need to plant a seed in the bowl first.
                        from.SendGump(this);
                    }
                    break;
                }
            case 2: // Infestation
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.InfestationLevel);
                    from.SendGump(this);
                    break;
                }
            case 3: // Fungus
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.FungusLevel);
                    from.SendGump(this);
                    break;
                }
            case 4: // Poison
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.PoisonLevel);
                    from.SendGump(this);
                    break;
                }
            case 5: // Disease
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.DiseaseLevel);
                    from.SendGump(this);
                    break;
                }
            case 6: // Water
                {
                    BaseBeverage bev = null;

                    foreach (var beverage in from.Backpack.FindItemsByType<BaseBeverage>())
                    {
                        if (beverage.IsEmpty && beverage.Pourable && beverage.Content == BeverageType.Water)
                        {
                            bev = beverage;
                            break;
                        }
                    }

                    if (bev == null)
                    {
                        from.Target = new PlantPourTarget(_plant);

                        // Target the container you wish to use to water the ~1_val~.
                        from.SendLocalizedMessage(1060808, $"#{_plant.GetLocalizedPlantStatus()}");
                    }
                    else
                    {
                        _plant.Pour(from, bev);
                    }

                    from.SendGump(this);
                    break;
                }
            case 7: // Poison potion
                {
                    AddPotion(from, PotionEffect.PoisonGreater, PotionEffect.PoisonDeadly);
                    break;
                }
            case 8: // Cure potion
                {
                    AddPotion(from, PotionEffect.CureGreater);
                    break;
                }
            case 9: // Heal potion
                {
                    AddPotion(from, PotionEffect.HealGreater);
                    break;
                }
            case 10: // Strength potion
                {
                    AddPotion(from, PotionEffect.StrengthGreater);
                    break;
                }
            case 11: // Help
                {
                    from.NetState.SendDisplayHelpTopic(HelpTopic.PlantGrowing);
                    from.SendGump(this);
                    break;
                }
            case 12: // Empty the bowl
                {
                    EmptyTheBowlGump.DisplayTo(from, _plant);
                    break;
                }
        }
    }

    private void AddPotion(Mobile from, params PotionEffect[] effects)
    {
        var item = GetPotion(from, effects);

        if (item != null)
        {
            _plant.Pour(from, item);
        }
        else
        {
            if (_plant.ApplyPotion(effects[0], true, out var message))
            {
                from.SendLocalizedMessage(1061884); // You don't have any strong potions of that type in your pack.

                from.Target = new PlantPourTarget(_plant);

                // Target the container you wish to use to water the ~1_val~.
                from.SendLocalizedMessage(1060808, $"#{_plant.GetLocalizedPlantStatus()}");

                return;
            }

            _plant.LabelTo(from, message);
        }

        from.SendGump(this);
    }

    public static Item GetPotion(Mobile from, PotionEffect[] effects)
    {
        if (from.Backpack == null)
        {
            return null;
        }

        foreach (var item in from.Backpack.FindItems())
        {
            if (item is BasePotion potion)
            {
                if (Array.IndexOf(effects, potion.PotionEffect) >= 0)
                {
                    return potion;
                }
            }
            else if (item is PotionKeg keg && keg.Held > 0 && Array.IndexOf(effects, keg.Type) >= 0)
            {
                return keg;
            }
        }

        return null;
    }
}
