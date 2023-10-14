using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0)]
public partial class EminosKatanaChest : WoodenChest
{
    [Constructible]
    public EminosKatanaChest()
    {
        Movable = false;
        ItemID = 0xE42;

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

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile player && player.InRange(GetWorldLocation(), 2))
        {
            var qs = player.Quest;

            if (qs is EminosUndertakingQuest)
            {
                if (EminosUndertakingQuest.HasLostEminosKatana(from))
                {
                    Item katana = new EminosKatana();

                    if (!player.PlaceInBackpack(katana))
                    {
                        katana.Delete();
                        // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                        player.SendLocalizedMessage(1046260);
                    }
                }
                else
                {
                    QuestObjective obj = qs.FindObjective<HallwayWalkObjective>();

                    if (obj?.Completed == false)
                    {
                        Item katana = new EminosKatana();

                        if (player.PlaceInBackpack(katana))
                        {
                            GenerateTreasure();
                            obj.Complete();
                        }
                        else
                        {
                            katana.Delete();
                            // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                            player.SendLocalizedMessage(1046260);
                        }
                    }
                }
            }
        }

        base.OnDoubleClick(from);
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

        if (from is PlayerMobile player && player.Quest is EminosUndertakingQuest)
        {
            var obj = player.Quest.FindObjective<HallwayWalkObjective>();
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
        if (from is PlayerMobile player && player.Quest is EminosUndertakingQuest)
        {
            var obj = player.Quest.FindObjective<HallwayWalkObjective>();
            if (obj != null)
            {
                obj.StolenTreasure = true;
            }
        }
    }
}
