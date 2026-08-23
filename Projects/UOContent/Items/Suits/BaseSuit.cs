using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public abstract partial class BaseSuit : Item
{
    public BaseSuit(AccessLevel level, int hue, int itemID) : base(itemID)
    {
        Hue = hue;
        Movable = false;
        LootType = LootType.Newbied;
        Layer = Layer.OuterTorso;

        _accessLevel = level;
    }

    public override double DefaultWeight => 1.0;

    [SerializableField(0, fieldChanged: nameof(OnAccessLevelChanged))]
    [InvalidateProperties]
    private AccessLevel _accessLevel;

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
