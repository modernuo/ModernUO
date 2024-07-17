using ModernUO.Serialization;
using Server.Factions;

namespace Server.Ethics.Hero;

[SerializationGenerator(0, false)]
public sealed partial class HeroEthic : Ethic
{
    public override EthicDefinition Definition => new(
        0x482,
        "Hero",
        "(Hero)",
        "I will defend the virtues",
        [
            new HolySense(),
            new HolyItem(),
            new SummonFamiliar(),
            new HolyBlade(),
            new Bless(),
            new HolyShield(),
            new HolySteed(),
            new HolyWord()
        ]
    );

    public HeroEthic()
    {
        if (!RegisterEthic(this))
        {
            Delete();
        }
    }

    public override bool IsEligible(Mobile mob) => mob.Kills < 5 && Faction.Find(mob) is TrueBritannians or CouncilOfMages;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!RegisterEthic(this))
        {
            Delete();
        }
    }
}
