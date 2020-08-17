using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Ninja
{
  public class EminosKatanaChest : WoodenChest
  {
    [Constructible]
    public EminosKatanaChest()
    {
      Movable = false;
      ItemID = 0xE42;

      GenerateTreasure();
    }

    public EminosKatanaChest(Serial serial) : base(serial)
    {
    }

    public override bool IsDecoContainer => false;

    private void GenerateTreasure()
    {
      for (int i = Items.Count - 1; i >= 0; i--)
        Items[i].Delete();

      for (int i = 0; i < 75; i++)
        DropItem(
          Utility.Random(10) switch
          {
            0 => new GoldBracelet(),
            1 => new GoldRing(),
            _ => Loot.RandomGem() // 2
          }
        );
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (from is PlayerMobile player && player.InRange(GetWorldLocation(), 2))
      {
        QuestSystem qs = player.Quest;

        if (qs is EminosUndertakingQuest)
        {
          if (EminosUndertakingQuest.HasLostEminosKatana(from))
          {
            Item katana = new EminosKatana();

            if (!player.PlaceInBackpack(katana))
            {
              katana.Delete();
              player.SendLocalizedMessage(
                1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
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
                player.SendLocalizedMessage(
                  1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
              }
            }
          }
        }
      }

      base.OnDoubleClick(from);
    }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight) => false;

    public override bool CheckItemUse(Mobile from, Item item) => item == this;

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    {
      if (from.AccessLevel >= AccessLevel.GameMaster)
        return true;

      if (from is PlayerMobile player && player.Quest is EminosUndertakingQuest)
      {
        HallwayWalkObjective obj = player.Quest.FindObjective<HallwayWalkObjective>();
        if (obj?.StolenTreasure == true)
          from.SendLocalizedMessage(
            1063247); // The guard is watching you carefully!  It would be unwise to remove another item from here.
        else
          return true;
      }

      return false;
    }

    public override void OnItemLifted(Mobile from, Item item)
    {
      if (from is PlayerMobile player && player.Quest is EminosUndertakingQuest)
      {
        HallwayWalkObjective obj = player.Quest.FindObjective<HallwayWalkObjective>();
        if (obj != null)
          obj.StolenTreasure = true;
      }
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}
