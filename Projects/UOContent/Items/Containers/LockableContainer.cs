using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class LockableContainer : TrappableContainer, ILockable, ILockpickable, ICraftable, IShipwreckedItem
{
    public LockableContainer(int itemID) : base(itemID) => MaxLockLevel = 100;

    public override bool TrapOnOpen => !_trapOnLockpick;

    public override bool DisplaysContent => !_rawLocked;

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        if (from.CheckSkill(SkillName.Tinkering, -5.0, 15.0))
        {
            from.SendLocalizedMessage(500636); // Your tinker skill was sufficient to make the item lockable.

            var key = new Key(KeyType.Copper, Key.RandomValue());

            _keyValue = key.KeyValue;
            DropItem(key);

            var tinkering = from.Skills.Tinkering.Value;
            var level = (int)(tinkering * 0.8);

            _requiredSkill = Math.Min(level - 4, 95);
            _maxLockLevel = Math.Min(level + 35, 95);

            // Lock level of 0 means it is not pickable, so change it to -1
            _lockLevel = level == 14 ? -1 : Math.Min(level - 14, 95);
        }
        else
        {
            from.SendLocalizedMessage(500637); // Your tinker skill was insufficient to make the item lockable.
        }

        return 1;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Picker { get; set; }

    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private bool _isShipwreckedItem;

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private bool _trapOnLockpick;

    [SerializableField(2)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _requiredSkill;

    [SerializableField(3)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _maxLockLevel;

    [SerializableField(4)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private uint _keyValue;

    [SerializableField(5)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _lockLevel;

    [SerializableField(6, getter: "private", setter: "private")]
    private bool _rawLocked;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual bool Locked
    {
        get => _rawLocked;
        set
        {
            _rawLocked = value;

            if (_rawLocked)
            {
                Picker = null;
            }

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public virtual void LockPick(Mobile from)
    {
        Locked = false;
        Picker = from;

        if (_trapOnLockpick && ExecuteTrap(from))
        {
            _trapOnLockpick = false;
        }
    }

    public override bool CheckContentDisplay(Mobile from) => !_rawLocked && base.CheckContentDisplay(from);

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
    {
        if (from.AccessLevel < AccessLevel.GameMaster && _rawLocked)
        {
            from.SendLocalizedMessage(501747); // It appears to be locked.
            return false;
        }

        return base.TryDropItem(from, dropped, sendFullMessage);
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (from.AccessLevel < AccessLevel.GameMaster && _rawLocked)
        {
            from.SendLocalizedMessage(501747); // It appears to be locked.
            return false;
        }

        return base.OnDragDropInto(from, item, p);
    }

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject) =>
        base.CheckLift(from, item, ref reject) &&
        (item == this || from.AccessLevel >= AccessLevel.GameMaster || !_rawLocked);

    public override bool CheckItemUse(Mobile from, Item item)
    {
        if (!base.CheckItemUse(from, item))
        {
            return false;
        }

        if (item != this && from.AccessLevel < AccessLevel.GameMaster && _rawLocked)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return false;
        }

        return true;
    }

    public virtual bool CheckLocked(Mobile from)
    {
        if (!_rawLocked)
        {
            return false;
        }

        var inaccessible = from.AccessLevel < AccessLevel.GameMaster;

        int number = inaccessible
            ? 501747 // It appears to be locked.
            : 502502;    // That is locked, but you open it with your godly powers.

        from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number);

        return inaccessible;
    }

    public override void OnTelekinesis(Mobile from)
    {
        if (CheckLocked(from))
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x376A,
                9,
                32,
                5022
            );
            Effects.PlaySound(Location, Map, 0x1F5);
            return;
        }

        base.OnTelekinesis(from);
    }

    public override void OnDoubleClickSecureTrade(Mobile from)
    {
        if (CheckLocked(from))
        {
            return;
        }

        base.OnDoubleClickSecureTrade(from);
    }

    public override void Open(Mobile from)
    {
        if (CheckLocked(from))
        {
            return;
        }

        base.Open(from);
    }

    public override void OnSnoop(Mobile from)
    {
        if (CheckLocked(from))
        {
            return;
        }

        base.OnSnoop(from);
    }

    public override void AddNameProperties(ObjectPropertyList list)
    {
        base.AddNameProperties(list);

        if (_isShipwreckedItem)
        {
            list.Add(1041645); // recovered from a shipwreck
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (_isShipwreckedItem)
        {
            LabelTo(from, 1041645); // recovered from a shipwreck
        }
    }
}
