using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

public enum TrapType
{
    None,
    MagicTrap,
    ExplosionTrap,
    DartTrap,
    PoisonTrap
}

[SerializationGenerator(3, false)]
public abstract partial class TrappableContainer : BaseContainer, ITelekinesisable
{
    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _trapLevel;

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private int _trapPower;

    [SerializableField(2)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TrapType _trapType;

    public TrappableContainer(int itemID) : base(itemID)
    {
    }

    public virtual bool TrapOnOpen => true;

    public virtual void OnTelekinesis(Mobile from)
    {
        Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
        Effects.PlaySound(Location, Map, 0x1F5);

        if (TrapOnOpen)
        {
            ExecuteTrap(from);
        }
    }

    private void SendMessageTo(Mobile to, int number, int hue)
    {
        if (Deleted || !to.CanSee(this))
        {
            return;
        }

        to.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, hue, 3, number);
    }

    private void SendMessageTo(Mobile to, string text, int hue)
    {
        if (Deleted || !to.CanSee(this))
        {
            return;
        }

        to.NetState.SendMessage(Serial, ItemID, MessageType.Regular, hue, 3, false, "ENU", "", text);
    }

    public override void Open(Mobile from)
    {
        if (!TrapOnOpen || !ExecuteTrap(from))
        {
            base.Open(from);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _trapLevel = reader.ReadInt();
        _trapPower = reader.ReadInt();
        _trapType = (TrapType)reader.ReadInt();
    }
}
