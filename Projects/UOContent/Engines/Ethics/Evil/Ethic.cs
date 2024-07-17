using ModernUO.Serialization;
using Server.Factions;

namespace Server.Ethics.Evil;

[SerializationGenerator(0, false)]
public sealed partial class EvilEthic : Ethic
{
    public override EthicDefinition Definition => new(
        0x455,
        "Evil",
        "(Evil)",
        "I am evil incarnate",
        [
            new UnholySense(),
            new UnholyItem(),
            new SummonFamiliar(),
            new VileBlade(),
            new Blight(),
            new UnholyShield(),
            new UnholySteed(),
            new UnholyWord()
        ]
    );

    public EvilEthic()
    {
        if (!RegisterEthic(this))
        {
            Delete();
        }
    }

    public override bool IsEligible(Mobile mob) => Faction.Find(mob) is Minax or Shadowlords;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!RegisterEthic(this))
        {
            Delete();
        }
    }
}
