using ModernUO.Serialization;
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

[SerializationGenerator(0, false)]
public partial class Key : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxRange;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Item _link;

    [SerializableField(2)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _description;

    [SerializableField(3)]
    [InvalidateProperties]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private uint _keyValue;

    [Constructible]
    public Key(uint val = 0) : this(KeyType.Iron, val)
    {
    }

    public Key(KeyType type, uint val = 0, Item link = null) : base((int)type)
    {
        Weight = 1.0;

        _maxRange = 3;
        _keyValue = val;
        _link = link;
    }

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

        foreach (var item in cont.EnumerateItems())
        {
            if (item is Key key)
            {
                if (key.KeyValue == keyValue)
                {
                    key.Delete();
                }
            }
            else if (item is KeyRing keyRing)
            {
                keyRing.RemoveKey(keyValue);
            }
        }
    }

    public static bool ContainsKey(Container cont, uint keyValue)
    {
        if (cont == null)
        {
            return false;
        }

        foreach (var item in cont.FindItems())
        {
            if (item is Key key)
            {
                if (key.KeyValue == keyValue)
                {
                    return true;
                }
            }
            else if (item is KeyRing keyRing)
            {
                if (keyRing.ContainsKey(keyValue))
                {
                    return true;
                }
            }
        }

        return false;
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

        if (_keyValue != 0)
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

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        string desc;

        desc = _keyValue == 0 ? "(blank)" : _description.DefaultIfNullOrEmpty(null)?.Trim();

        if (desc != null)
        {
            list.Add(desc);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        var desc = _keyValue == 0 ? "(blank)" : _description.DefaultIfNullOrEmpty(null)?.Trim();

        if (desc != null)
        {
            from.NetState.SendMessage(Serial, ItemID, MessageType.Regular, 0x3B2, 3, false, "ENU", "", desc);
        }
    }

    public bool UseOn(Mobile from, ILockable o)
    {
        if (o.KeyValue != KeyValue)
        {
            return false;
        }

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

    private class RenamePrompt : Prompt
    {
        private Key _key;

        public RenamePrompt(Key key) => _key = key;

        public override void OnResponse(Mobile from, string text)
        {
            if (_key.Deleted || !_key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            _key.Description = Utility.FixHtml(text);
        }
    }

    private class UnlockTarget : Target
    {
        private Key _key;

        public UnlockTarget(Key key) : base(key.MaxRange, false, TargetFlags.None)
        {
            _key = key;
            CheckLOS = false;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_key.Deleted || !_key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            int number;

            if (targeted == _key)
            {
                number = 501665; // Enter a description for this key.

                from.Prompt = new RenamePrompt(_key);
            }
            else if (targeted is ILockable lockable)
            {
                if (_key.UseOn(from, lockable))
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
        private Key _key;

        public CopyTarget(Key key) : base(3, false, TargetFlags.None) => _key = key;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_key.Deleted || !_key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            int number;

            if (targeted is Key k)
            {
                if (k._keyValue == 0)
                {
                    number = 501675; // This key is also blank.
                }
                else if (from.CheckTargetSkill(SkillName.Tinkering, k, 0, 75.0))
                {
                    number = 501676; // You make a copy of the key.

                    _key.Description = k.Description;
                    _key.KeyValue = k.KeyValue;
                    _key.Link = k.Link;
                    _key.MaxRange = k.MaxRange;
                }
                else if (Utility.RandomDouble() < 0.1) // 10% chance to destroy the key
                {
                    from.SendLocalizedMessage(501677); // You fail to make a copy of the key.

                    number = 501678; // The key was destroyed in the attempt.

                    _key.Delete();
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
