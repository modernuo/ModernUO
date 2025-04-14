using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Targeting;

namespace Server.Commands.Generic
{
    public class DesignInsertCommand : BaseCommand
    {
        public enum DesignInsertResult
        {
            Valid,
            InvalidItem,
            NotInHouse,
            OutsideHouseBounds
        }

        public DesignInsertCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Single | CommandSupport.Area;
            Commands = new[] { "DesignInsert" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "DesignInsert [allItems=false]";
            Description = "Inserts multiple targeted items into a customizable house's design.";
        }

        public static void Configure()
        {
            TargetCommands.Register(new DesignInsertCommand());
        }

        public static DesignInsertResult ProcessInsert(Item item, bool staticsOnly, out HouseFoundation house)
        {
            house = null;

            if (item is null or BaseMulti or HouseSign || staticsOnly && item is not Static)
            {
                return DesignInsertResult.InvalidItem;
            }

            house = BaseHouse.FindHouseAt(item) as HouseFoundation;

            if (house == null)
            {
                return DesignInsertResult.NotInHouse;
            }

            var x = item.X - house.X;
            var y = item.Y - house.Y;
            var z = item.Z - house.Z;

            if (!TryInsertIntoState(house.CurrentState, item.ItemID, x, y, z))
            {
                return DesignInsertResult.OutsideHouseBounds;
            }

            TryInsertIntoState(house.DesignState, item.ItemID, x, y, z);
            item.Delete();

            return DesignInsertResult.Valid;
        }

        private static bool TryInsertIntoState(DesignState state, int itemID, int x, int y, int z)
        {
            var mcl = state.Components;

            if (x < mcl.Min.X || y < mcl.Min.Y || x > mcl.Max.X || y > mcl.Max.Y)
            {
                return false;
            }

            mcl.Add(itemID, x, y, z);
            state.OnRevised();

            return true;
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            Target t = new DesignInsertTarget(new List<HouseFoundation>(), e.Length < 1 || !e.GetBoolean(0));
            t.Invoke(e.Mobile, obj);
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            var from = e.Mobile;
            from.SendGump(
                new InsertObjectsNoticeGump(
                    list.Count,
                    okay => OnConfirmCallback(from, okay, list, e.Length < 1 || !e.GetBoolean(0))
                )
            );
            AddResponse("Awaiting confirmation...");
        }

        private class InsertObjectsNoticeGump : StaticWarningGump<InsertObjectsNoticeGump>
        {
            public override int Width => 420;
            public override int Height => 280;
            public override string Content { get; }

            public InsertObjectsNoticeGump(int count, Action<bool> callback) : base(callback) =>
                Content = $"You are about to insert {count} objects. This cannot be undone without a full server revert.<br><br>Continue?";
        }

        private void OnConfirmCallback(Mobile from, bool okay, List<object> list, bool staticsOnly)
        {
            var flushToLog = false;

            if (okay)
            {
                using var foundations = PooledRefQueue<HouseFoundation>.Create();
                flushToLog = list.Count > 20;

                for (var i = 0; i < list.Count; ++i)
                {
                    var result = ProcessInsert(list[i] as Item, staticsOnly, out var house);

                    switch (result)
                    {
                        case DesignInsertResult.Valid:
                            {
                                AddResponse("The item has been inserted into the house design.");

                                if (!foundations.Contains(house))
                                {
                                    foundations.Enqueue(house);
                                }

                                break;
                            }
                        case DesignInsertResult.InvalidItem:
                            {
                                LogFailure("That cannot be inserted.");
                                break;
                            }
                        case DesignInsertResult.NotInHouse:
                        case DesignInsertResult.OutsideHouseBounds:
                            {
                                LogFailure("That item is not inside a customizable house.");
                                break;
                            }
                    }
                }

                while (foundations.Count > 0)
                {
                    foundations.Dequeue().Delta(ItemDelta.Update);
                }
            }
            else
            {
                AddResponse("Command aborted.");
            }

            Flush(from, flushToLog);
        }

        private class DesignInsertTarget : Target
        {
            private readonly List<HouseFoundation> m_Foundations;
            private readonly bool m_StaticsOnly;

            public DesignInsertTarget(List<HouseFoundation> foundations, bool staticsOnly)
                : base(-1, false, TargetFlags.None)
            {
                m_Foundations = foundations;
                m_StaticsOnly = staticsOnly;
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (m_Foundations.Count != 0)
                {
                    from.SendMessage("Your changes have been committed. Updating...");

                    foreach (var house in m_Foundations)
                    {
                        house.Delta(ItemDelta.Update);
                    }
                }
            }

            protected override void OnTarget(Mobile from, object obj)
            {
                var result = ProcessInsert(obj as Item, m_StaticsOnly, out var house);

                switch (result)
                {
                    case DesignInsertResult.Valid:
                        {
                            if (m_Foundations.Count == 0)
                            {
                                from.SendMessage(
                                    "The item has been inserted into the house design. Press ESC when you are finished."
                                );
                            }
                            else
                            {
                                from.SendMessage("The item has been inserted into the house design.");
                            }

                            if (!m_Foundations.Contains(house))
                            {
                                m_Foundations.Add(house);
                            }

                            break;
                        }
                    case DesignInsertResult.InvalidItem:
                        {
                            from.SendMessage("That cannot be inserted. Try again.");
                            break;
                        }
                    case DesignInsertResult.NotInHouse:
                    case DesignInsertResult.OutsideHouseBounds:
                        {
                            from.SendMessage("That item is not inside a customizable house. Try again.");
                            break;
                        }
                }

                from.Target = new DesignInsertTarget(m_Foundations, m_StaticsOnly);
            }
        }
    }
}
