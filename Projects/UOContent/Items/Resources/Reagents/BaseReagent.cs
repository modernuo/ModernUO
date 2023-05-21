using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseReagent : Item
{
    public BaseReagent(int itemID, int amount = 1) : base(itemID)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
}