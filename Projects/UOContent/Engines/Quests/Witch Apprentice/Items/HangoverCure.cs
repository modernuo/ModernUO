using ModernUO.Serialization;

namespace Server.Engines.Quests.Hag;

[SerializationGenerator(0, false)]
public partial class HangoverCure : Item
{
    [EncodedInt]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _uses;

    [Constructible]
    public HangoverCure() : base(0xE2B)
    {
        Hue = 0x2D;

        _uses = 20;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1055060; // Grizelda's Extra Strength Hangover Cure

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            SendLocalizedMessageTo(from, 1042038); // You must have the object in your backpack to use it.
            return;
        }

        if (Uses > 0)
        {
            from.PlaySound(0x2D6);
            from.SendLocalizedMessage(501206); // An awful taste fills your mouth.

            if (from.BAC > 0)
            {
                from.BAC = 0;
                from.SendLocalizedMessage(501204); // You are now sober!
            }

            Uses--;
        }
        else
        {
            Delete();
            from.SendLocalizedMessage(501201); // There wasn't enough left to have any effect.
        }
    }
}
