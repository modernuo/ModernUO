using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Objectives
{
  public class DeliverObjective : BaseObjective
  {
    public DeliverObjective(Type delivery, int amount, TextDefinition name, Type destination)
      : this(delivery, amount, name, destination, true)
    {
    }

    public DeliverObjective(Type delivery, int amount, TextDefinition name, Type destination, bool spawnsDelivery)
    {
      Delivery = delivery;
      Amount = amount;
      Name = name;
      Destination = destination;
      SpawnsDelivery = spawnsDelivery;

      if (MLQuestSystem.Debug && name.Number > 0)
      {
        int itemid = CollectObjective.LabelToItemID(name.Number);

        if (itemid <= 0 || itemid > 0x4000)
          Console.WriteLine("Warning: cliloc {0} is likely giving the wrong item ID", name.Number);
      }
    }

    public Type Delivery{ get; set; }

    public int Amount{ get; set; }

    public TextDefinition Name{ get; set; }

    public Type Destination{ get; set; }

    public bool SpawnsDelivery{ get; set; }

    public virtual void SpawnDelivery(Container pack)
    {
      if (!SpawnsDelivery || pack == null)
        return;

      List<Item> delivery = new List<Item>();

      for (int i = 0; i < Amount; ++i)
      {
        if (!(Activator.CreateInstance(Delivery) is Item item))
          continue;

        delivery.Add(item);

        if (item.Stackable && Amount > 1)
        {
          item.Amount = Amount;
          break;
        }
      }

      foreach (Item item in delivery)
        pack.DropItem(item); // Confirmed: on OSI items are added even if your pack is full
    }

    public override void WriteToGump(Gump g, ref int y)
    {
      string amount = Amount.ToString();

      g.AddHtmlLocalized(98, y, 312, 16, 1072207, 0x15F90, false, false); // Deliver
      g.AddLabel(143, y, 0x481, amount);

      if (Name.Number > 0)
      {
        g.AddHtmlLocalized(143 + amount.Length * 15, y, 190, 18, Name.Number, 0x77BF, false, false);
        g.AddItem(350, y, CollectObjective.LabelToItemID(Name.Number));
      }
      else if (Name.String != null)
      {
        g.AddLabel(143 + amount.Length * 15, y, 0x481, Name.String);
      }

      y += 32;

      g.AddHtmlLocalized(103, y, 120, 16, 1072379, 0x15F90, false, false); // Deliver to
      g.AddLabel(223, y, 0x481, QuesterNameAttribute.GetQuesterNameFor(Destination));

      y += 16;
    }

    public override BaseObjectiveInstance CreateInstance(MLQuestInstance instance)
    {
      return new DeliverObjectiveInstance(this, instance);
    }
  }

  #region Timed

  public class TimedDeliverObjective : DeliverObjective
  {
    public TimedDeliverObjective(TimeSpan duration, Type delivery, int amount, TextDefinition name, Type destination)
      : this(duration, delivery, amount, name, destination, true)
    {
    }

    public TimedDeliverObjective(TimeSpan duration, Type delivery, int amount, TextDefinition name, Type destination,
      bool spawnsDelivery)
      : base(delivery, amount, name, destination, spawnsDelivery)
    {
      Duration = duration;
    }

    public override bool IsTimed => true;
    public override TimeSpan Duration{ get; }
  }

  #endregion

  public class DeliverObjectiveInstance : BaseObjectiveInstance
  {
    public DeliverObjectiveInstance(DeliverObjective objective, MLQuestInstance instance)
      : base(instance, objective)
    {
      Objective = objective;
    }

    public DeliverObjective Objective{ get; set; }

    public bool HasCompleted{ get; set; }

    public override DataType ExtraDataType => DataType.DeliverObjective;

    public virtual bool IsDestination(IQuestGiver quester, Type type)
    {
      Type destType = Objective.Destination;

      return destType?.IsAssignableFrom(type) == true;
    }

    public override bool IsCompleted()
    {
      return HasCompleted;
    }

    public override void OnQuestAccepted()
    {
      Objective.SpawnDelivery(Instance.Player.Backpack);
    }

    // This is VERY similar to CollectObjective.GetCurrentTotal
    private int GetCurrentTotal()
    {
      Container pack = Instance.Player.Backpack;

      if (pack == null)
        return 0;

      Item[] items = pack.FindItemsByType(Objective.Delivery, false); // Note: subclasses are included
      int total = 0;

      foreach (Item item in items)
        total += item.Amount;

      return total;
    }

    public override bool OnBeforeClaimReward()
    {
      PlayerMobile pm = Instance.Player;

      int total = GetCurrentTotal();
      int desired = Objective.Amount;

      if (total < desired)
      {
        pm.SendLocalizedMessage(1074861); // You do not have everything you need!
        pm.SendLocalizedMessage(1074885, $"{total}\t{desired}"); // You have ~1_val~ item(s) but require ~2_val~
        return false;
      }

      return true;
    }

    // TODO: This is VERY similar to CollectObjective.OnClaimReward
    public override void OnClaimReward()
    {
      Container pack = Instance.Player.Backpack;

      if (pack == null)
        return;

      Item[] items = pack.FindItemsByType(Objective.Delivery, false);
      int left = Objective.Amount;

      foreach (Item item in items)
      {
        if (left == 0)
          break;

        if (item.Amount > left)
        {
          item.Consume(left);
          left = 0;
        }
        else
        {
          item.Delete();
          left -= item.Amount;
        }
      }
    }

    public override void OnQuestCancelled()
    {
      OnClaimReward(); // same effect
    }

    public override void OnExpire()
    {
      OnQuestCancelled();

      Instance.Player.SendLocalizedMessage(1074813); // You have failed to complete your delivery.
    }

    public override void WriteToGump(Gump g, ref int y)
    {
      Objective.WriteToGump(g, ref y);

      base.WriteToGump(g, ref y);

      // No extra instance stuff printed for this objective
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(HasCompleted);
    }
  }
}