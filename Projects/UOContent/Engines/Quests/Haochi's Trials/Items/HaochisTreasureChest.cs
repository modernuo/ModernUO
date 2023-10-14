using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class HaochisTreasureChest : WoodenFootLocker
{
    [Constructible]
    public HaochisTreasureChest()
    {
        Movable = false;

        GenerateTreasure();
    }

    public override bool IsDecoContainer => false;

    private void GenerateTreasure()
    {
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            Items[i].Delete();
        }

        for (var i = 0; i < 75; i++)
        {
            DropItem(
                Utility.Random(10) switch
                {
                    0 => new GoldBracelet(),
                    1 => new GoldRing(),
                    _ => Loot.RandomGem() // 2
                }
            );
        }
    }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight) =>
        false;

    public override bool CheckItemUse(Mobile from, Item item) => item == this;

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (from is PlayerMobile player && player.Quest is HaochisTrialsQuest)
        {
            var obj = player.Quest.FindObjective<FifthTrialIntroObjective>();
            if (obj?.StolenTreasure == true)
            {
                // The guard is watching you carefully!  It would be unwise to remove another item from here.
                from.SendLocalizedMessage(1063247);
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public override void OnItemLifted(Mobile from, Item item)
    {
        if (from is PlayerMobile player && player.Quest is HaochisTrialsQuest)
        {
            var obj = player.Quest.FindObjective<FifthTrialIntroObjective>();
            if (obj != null)
            {
                obj.StolenTreasure = true;
            }
        }

        Timer.StartTimer(TimeSpan.FromMinutes(2.0), GenerateTreasure);
    }
}
