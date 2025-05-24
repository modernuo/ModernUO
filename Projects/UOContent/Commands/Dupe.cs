using System;
using System.Reflection;
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

    public static bool DoDupe(Item src, int amount, Mobile from = null, Container pack = null)
    {
        from?.SendMessage($"Duping {amount}...");
        var c = src.GetType().GetConstructor(out var paramCount);
        if (c == null)
        {
            from?.SendMessage("Unable to dupe. Item must have a constructor with zero required parameters.");
            return false;
        }

        var args = paramCount == 0 ? null : new object[paramCount];
        if (args != null)
        {
            Array.Fill(args, Type.Missing);
        }

        try
        {

            for (var i = 0; i < amount; i++)
            {
                var newItem = DoDupe(src, c, args, from);
                if (newItem != null)
                {
                    if (pack != null)
                    {
                        pack.DropItem(newItem);
                    }
                    else if (from != null)
                    {
                        newItem.MoveToWorld(from.Location, from.Map);
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Item DoDupe(Item src, ConstructorInfo c, object[] args, Mobile from = null)
    {
        try
        {
            if (c.Invoke(args) is not Item newItem)
            {
                return null;
            }

            src.Dupe(newItem);

            newItem.UpdateTotals();
            newItem.InvalidateProperties();
            newItem.Delta(ItemDelta.Update);

            if (from != null)
            {
                CommandLogging.WriteLine(
                    from,
                    $"{from.AccessLevel} {CommandLogging.Format(from)} duped {CommandLogging.Format(src)} creating {CommandLogging.Format(newItem)}"
                );
            }

            // Recurse for items that have items
            if (newItem.Items.Count > 0)
            {
                for (var j = newItem.Items.Count - 1; j >= 0; j--)
                {
                    var itemToDelete = newItem.Items[j];
                    newItem.RemoveItem(itemToDelete);
                    itemToDelete.Delete();
                }
            }

            for (var j = 0; j < src.Items.Count; j++)
            {
                c = src.Items[j].GetType().GetConstructor(out var paramCount);
                if (c == null)
                {
                    continue;
                }

                args = paramCount == 0 ? null : new object[paramCount];
                if (args != null)
                {
                    Array.Fill(args, Type.Missing);
                }

                var subItem = DoDupe(src.Items[j], c, args, from);
                newItem.AddItem(subItem);
            }

            return newItem;
        }
        catch
        {
            return null;
        }
    }

    private class DupeTarget : Target
    {
        private readonly int _amount;
        private readonly bool _inBag;

        public DupeTarget(bool inbag, int amount) : base(15, false, TargetFlags.None)
        {
            _inBag = inbag;
            _amount = amount;
        }

        protected override void OnTarget(Mobile from, object targ)
        {
            if (targ is not Item copy)
            {
                from.SendMessage("You can only dupe items.");
                return;
            }

            CommandLogging.WriteLine(
                from,
                $"{from.AccessLevel} {CommandLogging.Format(from)} duping {CommandLogging.Format(copy)} (inBag={_inBag}; amount={_amount})"
            );

            Container pack;

            if (_inBag)
            {
                pack = copy.Parent switch
                {
                    Container cont => cont,
                    Mobile m       => m.Backpack,
                    _              => null
                };
            }
            else
            {
                pack = from.Backpack;
            }

            if (DoDupe(copy, _amount, from, pack))
            {
                from.SendMessage("Duping done.");
            }
            else
            {
                from.SendMessage("Duping Error!");
            }
        }
    }
}
