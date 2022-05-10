using Server.Network;
using Server.Prompts;
using Server.Targeting;

namespace Server.Items;

public enum KeyType
{
    Copper = 0x100E,
    Gold = 0x100F,
    Iron = 0x1010,
    Rusty = 0x1013
}

public interface ILockable
{
    bool Locked { get; set; }
    uint KeyValue { get; set; }
}

public class Key : Item
{
    private string m_Description;
    private uint m_KeyVal;

    [Constructible]
    public Key(uint val = 0) : this(KeyType.Iron, val)
    {
    }

    public Key(KeyType type, uint val = 0, Item link = null) : base((int)type)
    {
        Weight = 1.0;

        MaxRange = 3;
        m_KeyVal = val;
        Link = link;
    }

    public Key(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Description
    {
        get => m_Description;
        set
        {
            m_Description = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxRange { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public uint KeyValue
    {
        get => m_KeyVal;

        set
        {
            m_KeyVal = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Item Link { get; set; }

    public static uint RandomValue() => (uint)(0xFFFFFFFE * Utility.RandomDouble()) + 1;

    public static void RemoveKeys(Mobile m, uint keyValue)
    {
        if (keyValue == 0)
        {
            return;
        }

        RemoveKeys(m.Backpack, keyValue);
        RemoveKeys(m.BankBox, keyValue);
    }

    public static void RemoveKeys(Container cont, uint keyValue)
    {
        if (cont == null || keyValue == 0)
        {
            return;
        }

        var items = cont.FindItemsByType(new[] { typeof(Key), typeof(KeyRing) });

        foreach (var item in items)
        {
            if (item is Key key)
            {
                if (key.KeyValue == keyValue)
                {
                    key.Delete();
                }
            }
            else
            {
                var keyRing = (KeyRing)item;

                keyRing.RemoveKeys(keyValue);
            }
        }
    }

    public static bool ContainsKey(Container cont, uint keyValue)
    {
        if (cont == null)
        {
            return false;
        }

        var items = cont.FindItemsByType(new[] { typeof(Key), typeof(KeyRing) });

        foreach (var item in items)
        {
            if (item is Key key)
            {
                if (key.KeyValue == keyValue)
                {
                    return true;
                }
            }
            else
            {
                var keyRing = (KeyRing)item;

                if (keyRing.ContainsKey(keyValue))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(2); // version

        writer.Write(MaxRange);

        writer.Write(Link);

        writer.Write(m_Description);
        writer.Write(m_KeyVal);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        switch (version)
        {
            case 2:
                {
                    MaxRange = reader.ReadInt();

                    goto case 1;
                }
            case 1:
                {
                    Link = reader.ReadEntity<Item>();

                    goto case 0;
                }
            case 0:
                {
                    if (version < 2 || MaxRange == 0)
                    {
                        MaxRange = 3;
                    }

                    m_Description = reader.ReadString();

                    m_KeyVal = reader.ReadUInt();

                    break;
                }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(501661); // That key is unreachable.
            return;
        }

        Target t;
        int number;

        if (m_KeyVal != 0)
        {
            number = 501662; // What shall I use this key on?
            t = new UnlockTarget(this);
        }
        else
        {
            number = 501663; // This key is a key blank. Which key would you like to make a copy of?
            t = new CopyTarget(this);
        }

        from.SendLocalizedMessage(number);
        from.Target = t;
    }

    public override void GetProperties(ObjectPropertyList list)
    {
        base.GetProperties(list);

        string desc;

        if (m_KeyVal == 0)
        {
            desc = "(blank)";
        }
        else if ((desc = m_Description) == null || (desc = desc.Trim()).Length <= 0)
        {
            desc = null;
        }

        if (desc != null)
        {
            list.Add(desc);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        string desc;

        if (m_KeyVal == 0)
        {
            desc = "(blank)";
        }
        else
        {
            desc = m_Description?.Trim() ?? "";
        }

        if (desc.Length > 0)
        {
            from.NetState.SendMessage(Serial, ItemID, MessageType.Regular, 0x3B2, 3, false, "ENU", "", desc);
        }
    }

    public bool UseOn(Mobile from, ILockable o)
    {
        if (o.KeyValue == KeyValue)
        {
            if (o is BaseDoor door && !door.UseLocks())
            {
                return false;
            }

            o.Locked = !o.Locked;

            if (o is Item item)
            {
                if (o.Locked)
                {
                    item.SendLocalizedMessageTo(from, 1048000); // You lock it.
                }
                else
                {
                    item.SendLocalizedMessageTo(from, 1048001); // You unlock it.
                }

                if (item is LockableContainer cont)
                {
                    if (cont.LockLevel == ILockpickable.MagicLock)
                    {
                        cont.LockLevel = cont.RequiredSkill - 10;
                    }

                    if (cont.TrapType != TrapType.None && cont.TrapOnLockpick)
                    {
                        if (o.Locked)
                        {
                            cont.SendLocalizedMessageTo(from, 501673); // You re-enable the trap.
                        }
                        else
                        {
                            // You disable the trap temporarily.  Lock it again to re-enable it.
                            cont.SendLocalizedMessageTo(from, 501672);
                        }
                    }
                }
            }

            return true;
        }

        return false;
    }

    private class RenamePrompt : Prompt
    {
        private readonly Key m_Key;

        public RenamePrompt(Key key) => m_Key = key;

        public override void OnResponse(Mobile from, string text)
        {
            if (m_Key.Deleted || !m_Key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            m_Key.Description = Utility.FixHtml(text);
        }
    }

    private class UnlockTarget : Target
    {
        private readonly Key m_Key;

        public UnlockTarget(Key key) : base(key.MaxRange, false, TargetFlags.None)
        {
            m_Key = key;
            CheckLOS = false;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Key.Deleted || !m_Key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            int number;

            if (targeted == m_Key)
            {
                number = 501665; // Enter a description for this key.

                from.Prompt = new RenamePrompt(m_Key);
            }
            else if (targeted is ILockable lockable)
            {
                if (m_Key.UseOn(from, lockable))
                {
                    number = -1;
                }
                else
                {
                    number = 501668; // This key doesn't seem to unlock that.
                }
            }
            else
            {
                number = 501666; // You can't unlock that!
            }

            if (number != -1)
            {
                from.SendLocalizedMessage(number);
            }
        }
    }

    private class CopyTarget : Target
    {
        private readonly Key m_Key;

        public CopyTarget(Key key) : base(3, false, TargetFlags.None) => m_Key = key;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Key.Deleted || !m_Key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            int number;

            if (targeted is Key k)
            {
                if (k.m_KeyVal == 0)
                {
                    number = 501675; // This key is also blank.
                }
                else if (from.CheckTargetSkill(SkillName.Tinkering, k, 0, 75.0))
                {
                    number = 501676; // You make a copy of the key.

                    m_Key.Description = k.Description;
                    m_Key.KeyValue = k.KeyValue;
                    m_Key.Link = k.Link;
                    m_Key.MaxRange = k.MaxRange;
                }
                else if (Utility.RandomDouble() <= 0.1) // 10% chance to destroy the key
                {
                    from.SendLocalizedMessage(501677); // You fail to make a copy of the key.

                    number = 501678; // The key was destroyed in the attempt.

                    m_Key.Delete();
                }
                else
                {
                    number = 501677; // You fail to make a copy of the key.
                }
            }
            else
            {
                number = 501688; // Not a key.
            }

            from.SendLocalizedMessage(number);
        }
    }
}
