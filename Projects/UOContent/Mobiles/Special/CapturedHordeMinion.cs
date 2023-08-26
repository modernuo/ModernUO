using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class CapturedHordeMinion : HordeMinion
{
    [Constructible]
    public CapturedHordeMinion() => FightMode = FightMode.None;

    public override bool InitialInnocent => true;

    public override bool CanBeDamaged() => false;
}
