using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GamblingStone : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _gamblePot = 2500;

    [Constructible]
    public GamblingStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x56;
    }

    public override string DefaultName => "a gambling stone";

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add($"Jackpot: {_gamblePot}gp");
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);
        LabelTo(from, $"Jackpot: {_gamblePot}gp");
    }

    public override void OnDoubleClick(Mobile from)
    {
        var pack = from.Backpack;

        if (pack?.ConsumeTotal(typeof(Gold), 250) == true)
        {
            _gamblePot += 150;
            InvalidateProperties();

            var roll = Utility.Random(1200);

            if (roll == 0) // Jackpot
            {
                const int maxCheck = 1000000;

                from.SendMessage(0x35, $"You win the {_gamblePot}gp jackpot!");

                while (_gamblePot > maxCheck)
                {
                    from.AddToBackpack(new BankCheck(maxCheck));

                    _gamblePot -= maxCheck;
                }

                from.AddToBackpack(new BankCheck(_gamblePot));

                _gamblePot = 2500;
            }
            else if (roll <= 20) // Chance for a regbag
            {
                from.SendMessage(0x35, "You win a bag of reagents!");
                from.AddToBackpack(new BagOfReagents());
            }
            else if (roll <= 40) // Chance for gold
            {
                from.SendMessage(0x35, "You win 1500gp!");
                from.AddToBackpack(new BankCheck(1500));
            }
            else if (roll <= 100) // Another chance for gold
            {
                from.SendMessage(0x35, "You win 1000gp!");
                from.AddToBackpack(new BankCheck(1000));
            }
            else // Loser!
            {
                from.SendMessage(0x22, "You lose!");
            }
        }
        else
        {
            from.SendMessage(0x22, $"You need at least 250gp in your backpack to use this.");
        }
    }
}
