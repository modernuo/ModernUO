using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Commands
{
    public static class VisibilityList
    {
        public static void Initialize()
        {
            EventSink.Login += OnLogin;

            CommandSystem.Register("Vis", AccessLevel.Counselor, Vis_OnCommand);
            CommandSystem.Register("VisList", AccessLevel.Counselor, VisList_OnCommand);
            CommandSystem.Register("VisClear", AccessLevel.Counselor, VisClear_OnCommand);
        }

        public static void OnLogin(Mobile m)
        {
            (m as PlayerMobile)?.VisibilityList.Clear();
        }

        [Usage("Vis")]
        [Description("Adds or removes a targeted player from your visibility list.  Anyone on your visibility list will be able to see you at all times, even when you're hidden.")]
        public static void Vis_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                e.Mobile.Target = new VisTarget();
                e.Mobile.SendMessage("Select person to add or remove from your visibility list.");
            }
        }

        [Usage("VisList")]
        [Description("Shows the names of everyone in your visibility list.")]
        public static void VisList_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                var list = pm.VisibilityList;

                if (list.Count > 0)
                {
                    if (list.Count == 1)
                    {
                        pm.SendMessage($"You are visible to {list.Count} mobile:");
                    }
                    else
                    {
                        pm.SendMessage($"You are visible to {list.Count} mobiles:");
                    }

                    for (var i = 0; i < list.Count; ++i)
                    {
                        pm.SendMessage($"#{i + 1}: {list[i].Name}");
                    }
                }
                else
                {
                    pm.SendMessage("Your visibility list is empty.");
                }
            }
        }

        [Usage("VisClear")]
        [Description("Removes everyone from your visibility list.")]
        public static void VisClear_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                var list = new List<Mobile>(pm.VisibilityList);

                pm.VisibilityList.Clear();
                pm.SendMessage("Your visibility list has been cleared.");

                if (list.Count > 0)
                {
                    Span<byte> removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

                    for (var i = 0; i < list.Count; ++i)
                    {
                        var m = list[i];

                        if (!m.CanSee(pm) && Utility.InUpdateRange(m.Location, pm.Location))
                        {
                            OutgoingEntityPackets.CreateRemoveEntity(removeEntity, pm.Serial);
                            m.NetState?.Send(removeEntity);
                        }
                    }
                }
            }
        }

        private class VisTarget : Target
        {
            public VisTarget() : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from is PlayerMobile pm && targeted is Mobile targ)
                {
                    if (targ.AccessLevel <= pm.AccessLevel)
                    {
                        var list = pm.VisibilityList;

                        if (list.Contains(targ))
                        {
                            list.Remove(targ);
                            pm.SendMessage($"{targ.Name} has been removed from your visibility list.");
                        }
                        else
                        {
                            list.Add(targ);
                            pm.SendMessage($"{targ.Name} has been added to your visibility list.");
                        }

                        if (Utility.InUpdateRange(targ.Location, from.Location))
                        {
                            var ns = targ.NetState;

                            if (ns != null)
                            {
                                if (targ.CanSee(pm))
                                {
                                    ns.SendMobileIncoming(targ, pm);

                                    pm.SendOPLPacketTo(ns);

                                    foreach (var item in pm.Items)
                                    {
                                        item.SendOPLPacketTo(ns);
                                    }
                                }
                                else
                                {
                                    ns.SendRemoveEntity(pm.Serial);
                                }
                            }
                        }
                    }
                    else
                    {
                        pm.SendMessage("They can already see you!");
                    }
                }
                else
                {
                    from.SendMessage("Add only mobiles to your visibility list.");
                }
            }
        }
    }
}
