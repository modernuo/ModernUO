using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public abstract partial class BaseSuit : Item
{
    public BaseSuit(AccessLevel level, int hue, int itemID) : base(itemID)
    {
        Hue = hue;
        Weight = 1.0;
        Movable = false;
        LootType = LootType.Newbied;
        Layer = Layer.OuterTorso;

        _accessLevel = level;
    }

    [SerializableProperty(0)]
    public AccessLevel AccessLevel
    {
        get => _accessLevel;
        set
        {
            var oldAccessLevel = _accessLevel;
            _accessLevel = value;
            InvalidateProperties();
            this.MarkDirty();

            OnAccessLevelChanged(oldAccessLevel, _accessLevel);
        }
    }

    public virtual void OnAccessLevelChanged(AccessLevel oldAccessLevel, AccessLevel accessLevel)
    {
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        AccessLevel = (AccessLevel)reader.ReadInt();
    }

    public bool Validate()
    {
        if (RootParent is not Mobile mobile || mobile.AccessLevel >= AccessLevel)
        {
            return true;
        }

        Delete();
        return false;
    }

    public override void OnSingleClick(Mobile from)
    {
        if (Validate())
        {
            base.OnSingleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Validate())
        {
            base.OnDoubleClick(from);
        }
    }

    public override bool VerifyMove(Mobile from) => from.AccessLevel >= AccessLevel;

    public override bool OnEquip(Mobile from)
    {
        if (from.AccessLevel < AccessLevel)
        {
            from.SendMessage("You may not wear this.");
            return false;
        }

        return true;
    }
}
