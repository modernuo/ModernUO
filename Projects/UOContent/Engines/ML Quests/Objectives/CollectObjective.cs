using System;
using Server.Gumps;

namespace Server.Engines.MLQuests.Objectives
{
    public class CollectObjective : BaseObjective
    {
        public CollectObjective(int amount = 0, Type type = null, TextDefinition name = null)
        {
            DesiredAmount = amount;
            AcceptedType = type;
            Name = name;

            if (MLQuestSystem.Debug && ShowDetailed && name?.Number > 0)
            {
                var itemid = LabelToItemID(name.Number);

                if (itemid is <= 0 or > 0x4000)
                {
                    Console.WriteLine("Warning: cliloc {0} is likely giving the wrong item ID", name.Number);
                }
            }
        }

        public int DesiredAmount { get; set; }

        public Type AcceptedType { get; set; }

        public TextDefinition Name { get; set; }

        public virtual bool ShowDetailed => true;

        public bool CheckType(Type type) => AcceptedType?.IsAssignableFrom(type) == true;

        public virtual bool CheckItem(Item item) => true;

        public static int LabelToItemID(int label)
        {
            if (label < 1078872)
            {
                return label - 1020000;
            }

            return label - 1078872;
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            if (ShowDetailed)
            {
                var amount = DesiredAmount.ToString();

                g.AddHtmlLocalized(98, y, 350, 16, 1072205, 0x15F90); // Obtain
                g.AddLabel(143, y, 0x481, amount);

                if (Name.Number > 0)
                {
                    g.AddHtmlLocalized(143 + amount.Length * 15, y, 190, 18, Name.Number, 0x77BF);
                    g.AddItem(350, y, LabelToItemID(Name.Number));
                }
                else if (Name.String != null)
                {
                    g.AddLabel(143 + amount.Length * 15, y, 0x481, Name.String);
                }
            }
            else
            {
                if (Name.Number > 0)
                {
                    g.AddHtmlLocalized(98, y, 312, 32, Name.Number, 0x15F90);
                }
                else if (Name.String != null)
                {
                    g.AddLabel(98, y, 0x481, Name.String);
                }
            }

            y += 32;
        }

        public override BaseObjectiveInstance CreateInstance(MLQuestInstance instance) =>
            new CollectObjectiveInstance(this, instance);
    }

    public class TimedCollectObjective : CollectObjective
    {
        public TimedCollectObjective(TimeSpan duration, int amount, Type type, TextDefinition name)
            : base(amount, type, name) =>
            Duration = duration;

        public override bool IsTimed => true;
        public override TimeSpan Duration { get; }
    }

    public class CollectObjectiveInstance : BaseObjectiveInstance
    {
        public CollectObjectiveInstance(CollectObjective objective, MLQuestInstance instance)
            : base(instance, objective) =>
            Objective = objective;

        public CollectObjective Objective { get; set; }

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
                if (ClaimTypePredicate(item) && item.QuestItem && Objective.CheckItem(item))
                {
                    total += item.Amount;
                }
            }

            return total;
        }

        public override bool AllowsQuestItem(Item item, Type type) => Objective.CheckType(type) && Objective.CheckItem(item);

        public override bool IsCompleted() => GetCurrentTotal() >= Objective.DesiredAmount;

        public override void OnQuestCancelled()
        {
            var pm = Instance.Player;
            var pack = pm.Backpack;

            if (pack == null)
            {
                return;
            }

            foreach (var item in pack.FindItems(false))
            {
                // does another quest still need this item? (OSI just unmarks everything)
                if (ClaimTypePredicate(item) &&
                    item.QuestItem && !MLQuestSystem.CanMarkQuestItem(pm, item, Objective.AcceptedType))
                {
                    item.QuestItem = false;
                }
            }
        }

        // Note: subclasses are included
        private bool ClaimTypePredicate(Item item) => Objective.AcceptedType.IsInstanceOfType(item);

        // Should only be called after IsComplete() is checked to be true
        public override void OnClaimReward()
        {
            var pack = Instance.Player.Backpack;

            if (pack == null)
            {
                return;
            }

            // TODO: OSI also counts the item in the cursor?

            var left = Objective.DesiredAmount;

            foreach (var item in pack.EnumerateItemsByType<Item>(false, ClaimTypePredicate))
            {
                if (item.QuestItem && Objective.CheckItem(item))
                {
                    if (left == 0)
                    {
                        return;
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
        }

        public override void OnAfterClaimReward()
        {
            OnQuestCancelled(); // same thing, clear other quest items
        }

        public override void OnExpire()
        {
            OnQuestCancelled();

            // No message
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            Objective.WriteToGump(g, ref y);
            y -= 16;

            if (Objective.ShowDetailed)
            {
                base.WriteToGump(g, ref y);

                g.AddHtmlLocalized(103, y, 120, 16, 3000087, 0x15F90); // Total
                g.AddLabel(223, y, 0x481, GetCurrentTotal().ToString());
                y += 16;

                g.AddHtmlLocalized(103, y, 120, 16, 1074782, 0x15F90); // Return to
                g.AddLabel(223, y, 0x481, QuesterNameAttribute.GetQuesterNameFor(Instance.QuesterType));
                y += 16;
            }
        }
    }
}
