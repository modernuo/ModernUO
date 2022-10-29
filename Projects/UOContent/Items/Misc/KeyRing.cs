using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(1)]
public partial class KeyRing : Item
{
    private const int MaxKeys = 20; // List capacity will be 32

    [SerializableField(0)]
    private List<Key> _keys;

    [Constructible]
    public KeyRing() : base(0x1011)
    {
        Weight = 1.0; // They seem to have no weight on OSI ?!
        _keys = new List<Key>();
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
            return false;
        }

        if (dropped is not Key key || key.KeyValue == 0)
        {
            from.SendLocalizedMessage(501689); // Only non-blank keys can be put on a keyring.
            return false;
        }

        if (Keys.Count >= MaxKeys)
        {
            from.SendLocalizedMessage(1008138); // This keyring is full.
            return false;
        }

        Add(key);
        from.SendLocalizedMessage(501691); // You put the key on the keyring.

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
            return;
        }

        from.SendLocalizedMessage(501680); // What do you want to unlock?
        from.Target = new InternalTarget(this);
    }

    public override void OnDelete()
    {
        base.OnDelete();

        foreach (var key in Keys)
        {
            key.Delete();
        }

        this.Clear(_keys);
    }

    public void Add(Key key)
    {
        key.Internalize();
        this.Add(_keys, key);

        UpdateItemID();
    }

    public void Open(Mobile from)
    {
        if (Parent is not Container cont)
        {
            return;
        }

        for (var i = _keys.Count - 1; i >= 0; i--)
        {
            var key = _keys[i];

            if (!key.Deleted && !cont.TryDropItem(from, key, true))
            {
                break;
            }

            this.RemoveAt(_keys, i);
        }

        UpdateItemID();
    }

    public void RemoveKey(uint keyValue)
    {
        for (var i = _keys.Count - 1; i >= 0; i--)
        {
            var key = _keys[i];

            if (key.KeyValue == keyValue)
            {
                key.Delete();
                this.RemoveAt(_keys, i);
            }
        }

        UpdateItemID();
    }

    public bool ContainsKey(uint keyValue)
    {
        foreach (var key in _keys)
        {
            if (key.KeyValue == keyValue)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateItemID()
    {
        ItemID = _keys.Count switch
        {
            < 1 => 0x1011,
            < 3 => 0x1769,
            < 5 => 0x176A,
            _   => 0x176B
        };
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _keys = reader.ReadEntityList<Key>();
    }

    private class InternalTarget : Target
    {
        private KeyRing _keyRing;

        public InternalTarget(KeyRing keyRing) : base(-1, false, TargetFlags.None) => _keyRing = keyRing;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_keyRing.Deleted || !_keyRing.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
                return;
            }

            if (_keyRing == targeted)
            {
                _keyRing.Open(from);
                from.SendLocalizedMessage(501685); // You open the keyring.
            }
            else if (targeted is ILockable o)
            {
                foreach (var key in _keyRing.Keys)
                {
                    if (key.UseOn(from, o))
                    {
                        return;
                    }
                }

                from.SendLocalizedMessage(1008140); // You do not have a key for that.
            }
            else
            {
                from.SendLocalizedMessage(501666); // You can't unlock that!
            }
        }
    }
}
