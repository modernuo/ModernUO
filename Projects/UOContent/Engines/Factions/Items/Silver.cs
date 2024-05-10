using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(0, false)]
public partial class Silver : Item
{
    [Constructible]
    public Silver() : this(1)
    {
    }

    [Constructible]
    public Silver(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public Silver(int amount) : base(0xEF0)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.02;

    public override int GetDropSound() =>
        Amount switch
        {
            <= 1 => 0x2E4,
            <= 5 => 0x2E5,
            _    => 0x2E6
        };
}
