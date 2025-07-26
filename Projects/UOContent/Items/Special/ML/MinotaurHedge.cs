using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MinotaurHedge : Item
{
    [Constructible]
    public MinotaurHedge() : base(Utility.Random(3215, 4))
    {
    }

    public override double DefaultWeight => 1.0;

    public override string DefaultName => "minotaur hedge";
}
