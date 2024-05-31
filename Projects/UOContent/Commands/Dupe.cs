using System;
using Server.Items;
using Server.Targeting;

namespace Server.Commands;

public static class Dupe
{
    public static void Configure()
    {
        CommandSystem.Register("Dupe", AccessLevel.GameMaster, Dupe_OnCommand);
        CommandSystem.Register("DupeInBag", AccessLevel.GameMaster, DupeInBag_OnCommand);
    }

    [Usage("Dupe [amount]")]
    [Description("Dupes a targeted item.")]
    private static void Dupe_OnCommand(CommandEventArgs e)
    {
        var amount = 1;
        if (e.Length >= 1)
        {
            amount = e.GetInt32(0);
        }

        e.Mobile.Target = new DupeTarget(false, amount > 0 ? amount : 1);
        e.Mobile.SendMessage("What do you wish to dupe?");
    }

    [Usage("DupeInBag <count>")]
    [Description("Dupes an item at it's current location (count) number of times.")]
    private static void DupeInBag_OnCommand(CommandEventArgs e)
    {
        var amount = 1;
        if (e.Length >= 1)
        {
            amount = e.GetInt32(0);
        }

        e.Mobile.Target = new DupeTarget(true, amount > 0 ? amount : 1);
        e.Mobile.SendMessage("What do you wish to dupe?");
    }

    private class DupeTarget : Target
    {
        private readonly int m_Amount;
        private readonly bool m_InBag;

        public DupeTarget(bool inbag, int amount)
            : base(15, false, TargetFlags.None)
        {
            m_InBag = inbag;
            m_Amount = amount;
        }

        protected override void OnTarget(Mobile from, object targ)
        {
            var done = false;
            if (!(targ is Item))
            {
                from.SendMessage("You can only dupe items.");
                return;
            }

            CommandLogging.WriteLine(
                from,
                $"{from.AccessLevel} {CommandLogging.Format(from)} duping {CommandLogging.Format(targ)} (inBag={m_InBag}; amount={m_Amount})"
            );

            var copy = (Item)targ;
            Container pack = null;

            if (m_InBag)
            {
                pack = copy.Parent switch
                {
                    Container cont => cont,
                    Mobile m       => m.Backpack,
                    _              => pack
                };
            }
            else
            {
                pack = from.Backpack;
            }

            var c = copy.GetType().GetConstructor(out var paramCount);
            if (c != null)
            {
                var args = paramCount == 0 ? null : new object[paramCount];
                if (args != null)
                {
                    Array.Fill(args, Type.Missing);
                }

                try
                {
                    from.SendMessage($"Duping {m_Amount}...");
                    for (var i = 0; i < m_Amount; i++)
                    {
                        if (c.Invoke(args) is Item newItem)
                        {
                            copy.Dupe(newItem);

                            if (pack != null)
                            {
                                pack.DropItem(newItem);
                            }
                            else
                            {
                                newItem.MoveToWorld(from.Location, from.Map);
                            }

                            newItem.UpdateTotals();
                            newItem.InvalidateProperties();
                            newItem.Delta(ItemDelta.Update);

                            CommandLogging.WriteLine(
                                from,
                                $"{from.AccessLevel} {CommandLogging.Format(from)} duped {CommandLogging.Format(targ)} creating {CommandLogging.Format(newItem)}"
                            );
                        }
                    }

                    from.SendMessage("Done");
                    done = true;
                }
                catch
                {
                    from.SendMessage("Error!");
                    return;
                }
            }

            if (!done)
            {
                from.SendMessage("Unable to dupe. Item must have a constructor with zero required parameters.");
            }
        }
    }
}
