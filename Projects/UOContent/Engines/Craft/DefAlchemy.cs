using System;
using Server.Items;

namespace Server.Engines.Craft;

public class DefAlchemy : CraftSystem
{
    private static readonly Type typeofPotion = typeof(BasePotion);

    public static void Initialize()
    {
        if (CraftSystem != null)
            return; // Already initialized
        CraftSystem = new DefAlchemy();
    }

    private DefAlchemy() : base(1, 1, 1.25)
    {
    }

    public override SkillName MainSkill => SkillName.Alchemy;

    public override TextDefinition GumpTitle { get; } = 1044001;

    public static CraftSystem CraftSystem { get; private set; }

    public override double GetChanceAtMin(CraftItem item) => 0.0;

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
        if (tool?.Deleted != false || tool.UsesRemaining < 0)
        {
            return 1044038; // You have worn out your tool!
        }

        if (!BaseTool.CheckAccessible(tool, from))
        {
            return 1044263; // The tool must be on your person to use.
        }

        return 0;
    }

    public override void PlayCraftEffect(Mobile from)
    {
        from.PlaySound(0x242);
    }

    public static bool IsPotion(Type type) => typeofPotion.IsAssignableFrom(type);

    public override int PlayEndingEffect(
        Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality,
        bool makersMark, CraftItem item
    )
    {
        if (toolBroken)
        {
            from.SendLocalizedMessage(1044038); // You have worn out your tool
        }

        if (failed)
        {
            if (IsPotion(item.ItemType))
            {
                from.AddToBackpack(new Bottle());
                return 500287; // You fail to create a useful potion.
            }

            return 1044043; // You failed to create the item, and some of your materials are lost.
        }

        from.PlaySound(0x240); // Sound of a filling bottle

        if (IsPotion(item.ItemType))
        {
            if (quality == -1)
            {
                return 1048136; // You create the potion and pour it into a keg.
            }

            return 500279; // You pour the potion into a bottle...
        }

        return 1044154; // You create the item.
    }

    public override void InitCraftList()
    {
        // Refresh Potion
        var index = AddCraft(typeof(RefreshPotion), 1044530, 1044538, -25, 25.0, typeof(BlackPearl), 1044353, 1, 1044361);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(TotalRefreshPotion),
            1044530,
            1044539,
            25.0,
            75.0,
            typeof(BlackPearl),
            1044353,
            5,
            1044361
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Agility Potion
        index = AddCraft(typeof(AgilityPotion), 1044531, 1044540, 15.0, 65.0, typeof(Bloodmoss), 1044354, 1, 1044362);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(GreaterAgilityPotion),
            1044531,
            1044541,
            35.0,
            85.0,
            typeof(Bloodmoss),
            1044354,
            3,
            1044362
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Nightsight Potion
        index = AddCraft(
            typeof(NightSightPotion),
            1044532,
            1044542,
            -25.0,
            25.0,
            typeof(SpidersSilk),
            1044360,
            1,
            1044368
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Heal Potion
        index = AddCraft(typeof(LesserHealPotion), 1044533, 1044543, -25.0, 25.0, typeof(Ginseng), 1044356, 1, 1044364);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(typeof(HealPotion), 1044533, 1044544, 15.0, 65.0, typeof(Ginseng), 1044356, 3, 1044364);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(typeof(GreaterHealPotion), 1044533, 1044545, 55.0, 105.0, typeof(Ginseng), 1044356, 7, 1044364);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Strength Potion
        index = AddCraft(
            typeof(StrengthPotion),
            1044534,
            1044546,
            25.0,
            75.0,
            typeof(MandrakeRoot),
            1044357,
            2,
            1044365
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(GreaterStrengthPotion),
            1044534,
            1044547,
            45.0,
            95.0,
            typeof(MandrakeRoot),
            1044357,
            5,
            1044365
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Poison Potion
        index = AddCraft(
            typeof(LesserPoisonPotion),
            1044535,
            1044548,
            -5.0,
            45.0,
            typeof(Nightshade),
            1044358,
            1,
            1044366
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(typeof(PoisonPotion), 1044535, 1044549, 15.0, 65.0, typeof(Nightshade), 1044358, 2, 1044366);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(GreaterPoisonPotion),
            1044535,
            1044550,
            55.0,
            105.0,
            typeof(Nightshade),
            1044358,
            4,
            1044366
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(DeadlyPoisonPotion),
            1044535,
            1044551,
            90.0,
            140.0,
            typeof(Nightshade),
            1044358,
            8,
            1044366
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Cure Potion
        index = AddCraft(typeof(LesserCurePotion), 1044536, 1044552, -10.0, 40.0, typeof(Garlic), 1044355, 1, 1044363);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(typeof(CurePotion), 1044536, 1044553, 25.0, 75.0, typeof(Garlic), 1044355, 3, 1044363);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(typeof(GreaterCurePotion), 1044536, 1044554, 65.0, 115.0, typeof(Garlic), 1044355, 6, 1044363);
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        // Explosion Potion
        index = AddCraft(
            typeof(LesserExplosionPotion),
            1044537,
            1044555,
            5.0,
            55.0,
            typeof(SulfurousAsh),
            1044359,
            3,
            1044367
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(ExplosionPotion),
            1044537,
            1044556,
            35.0,
            85.0,
            typeof(SulfurousAsh),
            1044359,
            5,
            1044367
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);
        index = AddCraft(
            typeof(GreaterExplosionPotion),
            1044537,
            1044557,
            65.0,
            115.0,
            typeof(SulfurousAsh),
            1044359,
            10,
            1044367
        );
        AddRes(index, typeof(Bottle), 1044529, 1, 500315);

        if (Core.SE)
        {
            index = AddCraft(typeof(SmokeBomb), 1044537, 1030248, 90.0, 120.0, typeof(Eggs), 1044477, 1, 1044253);
            AddRes(index, typeof(Ginseng), 1044356, 3, 1044364);
            SetNeededExpansion(index, Expansion.SE);

            // Conflagration Potions
            index = AddCraft(
                typeof(ConflagrationPotion),
                1044109,
                1072096,
                55.0,
                105.0,
                typeof(GraveDust),
                1023983,
                5,
                1044253
            );
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(
                typeof(GreaterConflagrationPotion),
                1044109,
                1072099,
                65.0,
                115.0,
                typeof(GraveDust),
                1023983,
                10,
                1044253
            );
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            SetNeededExpansion(index, Expansion.SE);
            // Confusion Blast Potions
            index = AddCraft(
                typeof(ConfusionBlastPotion),
                1044109,
                1072106,
                55.0,
                105.0,
                typeof(PigIron),
                1023978,
                5,
                1044253
            );
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(
                typeof(GreaterConfusionBlastPotion),
                1044109,
                1072109,
                65.0,
                115.0,
                typeof(PigIron),
                1023978,
                10,
                1044253
            );
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            SetNeededExpansion(index, Expansion.SE);
        }
    }
}
