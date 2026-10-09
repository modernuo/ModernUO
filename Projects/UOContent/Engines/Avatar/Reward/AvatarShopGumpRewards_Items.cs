using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.Avatar;

public static partial class RewardFactory
{
    public static List<IReward> CreateItemRewards(PlayerMobile from, PlayerContext context) =>
    [
        // Currency
        ItemReward.Create(ONE_THOUSAND_GOLD, true, () => new Gold(500), 500, graphicOverride: AvatarShopGump.GOLD_STACK_ITEM_ID),
        ItemReward.Create(5 * ONE_THOUSAND_GOLD, true, () => new Gold(5000), 5000, graphicOverride: AvatarShopGump.GOLD_STACK_ITEM_ID),

        // Resources
        ItemReward.Create(2 * ONE_HUNDRED_GOLD, true, () => new IronIngot(50), 50)
            .WithDescription("A handful of ingots to get you started."),
        ItemReward.Create(ONE_HUNDRED_GOLD, true, () => new Cloth(50), 50)
            .WithDescription("A handful of cloth to get you started."),
        ItemReward.Create(5 * TEN_GOLD, true, () => new Bottle(10), 10)
            .WithDescription("A handful of bottles to get you started."),

        // Tools
        ItemReward.Create(5 * ONE_HUNDRED_GOLD, true, () => new TinkerTools()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new SmithHammer()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new Saw()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new SewingKit()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new Hatchet()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new Shovel()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new FishingPole()),
        ItemReward.Create(5 * TEN_GOLD, true, () => new Scissors()),

        // Equipment
        ItemReward.Create(5 * ONE_HUNDRED_GOLD, true, () => new BookOfChivalry()),
        ItemReward.Create(5 * ONE_HUNDRED_GOLD, true, () => new NecromancerSpellbook())
            .WithName("Necromancer Spellbook (Empty)"),
        ItemReward.Create(5 * ONE_HUNDRED_GOLD, true, () => new Spellbook()).WithName("Mage's Spellbook (Empty)"),
        ItemReward.Create(5 * ONE_HUNDRED_GOLD, true, () => new SpellweavingBook())
            .WithName("Spellweaving Book (Empty)"),

        // Utility
        ItemReward.Create(
                5 * ONE_HUNDRED_GOLD,
                true,
                () =>
                {
                    var bag = new Bag();
                    bag.DropItem(new Scissors());
                    bag.DropItem(new Bandage(50));
                    return bag;
                }
            )
            .WithName("Healer's Kit")
            .WithDescription("Contains scissors and bandages."),
        ItemReward.Create(
                5 * ONE_HUNDRED_GOLD,
                true,
                () =>
                {
                    var bag = new Bag();
                    bag.DropItem(new CurePotion { Amount = 10 });
                    bag.DropItem(new HealPotion { Amount = 10 });
                    bag.DropItem(new RefreshPotion { Amount = 10 });
                    return bag;
                }
            )
            .WithName("Warrior's Potion Bag")
            .WithDescription("Contains healing, cure, and refresh potions."),
        ItemReward.Create(ONE_THOUSAND_GOLD, true, () => new BagOfReagents())
            .WithName("Bag of Reagents")
            .WithDescription("Contains magery reagents for spells."),
        ItemReward.Create(5 * ONE_HUNDRED_GOLD, true, () => new BagOfNecroReagents())
            .WithName("Bag of Necro Reagents")
            .WithDescription("Contains necromancy reagents for spells."),
        ItemReward.Create(10 * ONE_THOUSAND_GOLD, true, () => new SmallBoatDeed()).WithDescription("Hit the seas sailing!")
    ];
}
