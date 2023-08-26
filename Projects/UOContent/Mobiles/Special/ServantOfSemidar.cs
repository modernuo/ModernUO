using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class ServantOfSemidar : BaseCreature
{
    [Constructible]
    public ServantOfSemidar() : base(AIType.AI_Melee, FightMode.None) => Body = 0x26;

    public override string DefaultName => "a Servant of Semidar";

    public override bool DisallowAllMoves => true;

    public override bool InitialInnocent => true;

    public override bool CanBeDamaged() => false;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1005494); // enslaved
    }
}
