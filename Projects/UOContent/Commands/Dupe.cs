using System;
using System.Reflection;
using Server.Items;
using Server.Targeting;
using Server.Utilities;

namespace Server.Commands
{
    public class Dupe
    {
        public static void Initialize()
        {
            CommandSystem.Register("Dupe", AccessLevel.GameMaster, Dupe_OnCommand);
            CommandSystem.Register("DupeInBag", AccessLevel.GameMaster, DupeInBag_OnCommand);
        }

        [Usage("Dupe [amount]")]
        [Description("Dupes a targeted item.")]
        private static void Dupe_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);
            e.Mobile.Target = new DupeTarget(false, amount > 0 ? amount : 1);
            e.Mobile.SendMessage("What do you wish to dupe?");
        }

        [Usage("DupeInBag <count>")]
        [Description("Dupes an item at it's current location (count) number of times.")]
        private static void DupeInBag_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);

            e.Mobile.Target = new DupeTarget(true, amount > 0 ? amount : 1);
            e.Mobile.SendMessage("What do you wish to dupe?");
        }

        public static void CopyProperties(Item dest, Item src)
        {
            PropertyInfo[] props = src.GetType().GetProperties();

            for (int i = 0; i < props.Length; i++)
                try
                {
                    if (props[i].CanRead && props[i].CanWrite) props[i].SetValue(dest, props[i].GetValue(src, null), null);
                }
                catch
                {
                    // Console.WriteLine( "Denied" );
                }
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
                bool done = false;
                if (!(targ is Item))
                {
                    from.SendMessage("You can only dupe items.");
                    return;
                }

                CommandLogging.WriteLine(from, "{0} {1} duping {2} (inBag={3}; amount={4})", from.AccessLevel,
                    CommandLogging.Format(from), CommandLogging.Format(targ), m_InBag, m_Amount);

                Item copy = (Item)targ;
                Container pack = null;

                if (m_InBag)
                {
                    if (copy.Parent is Container cont)
                        pack = cont;
                    else if (copy.Parent is Mobile m)
                        pack = m.Backpack;
                }
                else
                {
                    pack = from.Backpack;
                }

                ConstructorInfo c = ActivatorUtil.GetConstructor(copy.GetType());
                if (c != null)
                {
                    var paramList = c.GetParameters();
                    object[] args = paramList.Length == 0 ? null : new object[paramList.Length];
                    if (args != null) Array.Fill(args, Type.Missing);
                    try
                    {
                        from.SendMessage("Duping {0}...", m_Amount);
                        for (int i = 0; i < m_Amount; i++)
                            if (c.Invoke(args) is Item newItem)
                            {
                                CopyProperties(newItem, copy); // copy.Dupe( item, copy.Amount );
                                copy.OnAfterDuped(newItem);
                                newItem.Parent = null;

                                if (pack != null)
                                    pack.DropItem(newItem);
                                else
                                    newItem.MoveToWorld(from.Location, from.Map);

                                newItem.InvalidateProperties();

                                CommandLogging.WriteLine(from, "{0} {1} duped {2} creating {3}", from.AccessLevel,
                                    CommandLogging.Format(from), CommandLogging.Format(targ),
                                    CommandLogging.Format(newItem));
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

                if (!done) from.SendMessage("Unable to dupe.  Item must have a 0 parameter constructor.");
            }
        }
    }
}
