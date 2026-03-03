using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RandomTalisman : BaseTalisman
{
    [Constructible]
    public RandomTalisman() : base(GetRandomItemID())
    {
        Summoner = GetRandomSummoner();

        if (Summoner.IsEmpty)
        {
            Removal = GetRandomRemoval();

            if (Removal != TalismanRemoval.None)
            {
                MaxCharges = GetRandomCharges();
                MaxChargeTime = 1200;
            }
        }
        else
        {
            MaxCharges = Utility.RandomMinMax(10, 50);

            if (Summoner.IsItem)
            {
                MaxChargeTime = 60;
            }
            else
            {
                MaxChargeTime = 1800;
            }
        }

        Blessed = GetRandomBlessed();
        Slayer = GetRandomSlayer();
        Protection = GetRandomProtection();
        Killer = GetRandomKiller();
        Skill = GetRandomSkill();
        ExceptionalBonus = GetRandomExceptional();
        SuccessBonus = GetRandomSuccessful();
        Charges = MaxCharges;
    }
}
