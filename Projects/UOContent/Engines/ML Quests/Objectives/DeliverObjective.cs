using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Logging;
using Server.Utilities;

namespace Server.Engines.MLQuests.Objectives
{
    public class DeliverObjective : BaseObjective
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(DeliverObjective));

        public DeliverObjective(Type delivery, int amount, TextDefinition name, Type destination, bool spawnsDelivery = true)
        {
            Delivery = delivery;
            Amount = amount;
            Name = name;
            Destination = destination;
            SpawnsDelivery = spawnsDelivery;

            if (MLQuestSystem.Debug && name.Number > 0)
            {
                var itemid = CollectObjective.LabelToItemID(name.Number);

                if (itemid is <= 0 or > 0x4000)
                {
                    logger.Warning("Cliloc {Number} is likely giving the wrong item ID", name.Number);
                }
            }
        }

        public Type Delivery { get; set; }

        public int Amount { get; set; }

        public TextDefinition Name { get; set; }

        public Type Destination { get; set; }

        public bool SpawnsDelivery { get; set; }

        public virtual void SpawnDelivery(Container pack)
        {
            if (!SpawnsDelivery || pack == null)
            {
                return;
            }

            var delivery = new List<Item>();

            for (var i = 0; i < Amount; ++i)
            {
                var item = Delivery.CreateEntityInstance<Item>();

                if (item != null)
                {
                    delivery.Add(item);

                    if (item.Stackable && Amount > 1)
                    {
                        item.Amount = Amount;
                        break;
                    }
                }
            }

            foreach (var item in delivery)
            {
                pack.DropItem(item); // Confirmed: on OSI items are added even if your pack is full
            }
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            var amount = Amount.ToString();

            g.AddHtmlLocalized(98, y, 312, 16, 1072207, 0x15F90); // Deliver
            g.AddLabel(143, y, 0x481, amount);

            if (Name.Number > 0)
            {
                g.AddHtmlLocalized(143 + amount.Length * 15, y, 190, 18, Name.Number, 0x77BF);
                g.AddItem(350, y, CollectObjective.LabelToItemID(Name.Number));
            }
            else if (Name.String != null)
            {
                g.AddLabel(143 + amount.Length * 15, y, 0x481, Name.String);
            }

            y += 32;

            g.AddHtmlLocalized(103, y, 120, 16, 1072379, 0x15F90); // Deliver to
            g.AddLabel(223, y, 0x481, QuesterNameAttribute.GetQuesterNameFor(Destination));

            y += 16;
        }

        public override BaseObjectiveInstance CreateInstance(MLQuestInstance instance) =>
            new DeliverObjectiveInstance(this, instance);
    }

    public class TimedDeliverObjective : DeliverObjective
    {
        public TimedDeliverObjective(
            TimeSpan duration, Type delivery, int amount, TextDefinition name, Type destination,
            bool spawnsDelivery = true
        )
            : base(delivery, amount, name, destination, spawnsDelivery) =>
            Duration = duration;

        public override bool IsTimed => true;
        public override TimeSpan Duration { get; }
    }

    public class DeliverObjectiveInstance : BaseObjectiveInstance
    {
        public DeliverObjectiveInstance(DeliverObjective objective, MLQuestInstance instance)
            : base(instance, objective) =>
            Objective = objective;

        public DeliverObjective Objective { get; set; }

        public bool HasCompleted { get; set; }

        public override DataType ExtraDataType => DataType.DeliverObjective;

        public virtual bool IsDestination(IQuestGiver quester, Type type)
        {
            var destType = Objective.Destination;

            return destType?.IsAssignableFrom(type) == true;
        }

        public override bool IsCompleted() => HasCompleted;

        public override void OnQuestAccepted()
        {
            Objective.SpawnDelivery(Instance.Player.Backpack);
        }

        // This is VERY similar to CollectObjective.GetCurrentTotal
        private int GetCurrentTotal()
        {
            var pack = Instance.Player.Backpack;

            if (pack == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var item in pack.FindItems(false))
            {
                if (ClaimTypePredicate(item))
                {
                    total += item.Amount;
                }
            }

            return total;
        }

        public override bool OnBeforeClaimReward()
        {
            var pm = Instance.Player;

            var total = GetCurrentTotal();
            var desired = Objective.Amount;

            if (total < desired)
            {
                pm.SendLocalizedMessage(1074861);                        // You do not have everything you need!
                pm.SendLocalizedMessage(1074885, $"{total}\t{desired}"); // You have ~1_val~ item(s) but require ~2_val~
                return false;
            }

            return true;
        }

        // Note: subclasses are included
        private bool ClaimTypePredicate(Item item) => Objective.Delivery.IsInstanceOfType(item);

        // TODO: This is VERY similar to CollectObjective.OnClaimReward
        public override void OnClaimReward()
        {
            var pack = Instance.Player.Backpack;

            if (pack == null)
            {
                return;
            }

            var left = Objective.Amount;

            foreach (var item in pack.EnumerateItemsByType<Item>(false, ClaimTypePredicate))
            {
                if (left == 0)
                {
                    break;
                }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(HasCompleted);
        }
    }
}
