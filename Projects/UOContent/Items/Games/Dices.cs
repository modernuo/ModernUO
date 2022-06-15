using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Dices : Item, ITelekinesisable
{
    [Constructible]
    public Dices() : base(0xFA7) => Weight = 1.0;

    public void OnTelekinesis(Mobile from)
    {
        Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
        Effects.PlaySound(Location, Map, 0x1F5);

        Roll(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        Roll(from);
    }

    public void Roll(Mobile from)
    {
        PublicOverheadMessage(
            MessageType.Regular,
            0,
            false,
            $"*{from.Name} rolls {Utility.Random(1, 6)}, {Utility.Random(1, 6)}*"
        );
    }
}
