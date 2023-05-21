using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class JacobsPickaxe : Pickaxe
{
    // TODO: Recharges 1 use every 5 minutes.  Doesn't break when it reaches 0, you get a system message "You must wait a moment for it to recharge" 1072306 if you attempt to use it with no uses remaining.

    [Constructible]
    public JacobsPickaxe()
    {
        UsesRemaining = 20;
        LootType = LootType.Blessed;

        SkillBonuses.SetValues(0, SkillName.Mining, 10.0);
    }

    public override int LabelNumber => 1077758; // Jacob's Pickaxe
}
