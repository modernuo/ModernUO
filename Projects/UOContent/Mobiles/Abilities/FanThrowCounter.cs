namespace Server.Mobiles;

public class FanThrowCounter : MonsterAbilitySingleTarget
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.FanThrow;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;
    public override double ChanceToTrigger => 0.2;

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        /* Fan Throw
         * Effect:
         * - To: "0x57D4F5B"
         * - ItemId: "0x27A3"
         * - ItemIdName: "Tessen"
         * - FromLocation: "(992 299, 24)"
         * - ToLocation: "(992 308, 22)"
         * - Speed: "10"
         * - Duration: "0"
         * - FixedDirection: "False"
         * - Explode: "False"
         * - Hue: "0x0"
         * - Render: "0x0"
         * Damage: 50-65
         */
        Effects.SendMovingEffect(
            defender.Location,
            defender.Map,
            0x27A3,
            source.Location,
            defender.Location,
            10,
            0
        );

        source.DoHarmful(defender);
        AOS.Damage(defender, source, Utility.RandomMinMax(50, 65), 100, 0, 0, 0, 0);
    }

    protected override bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        base.CanEffectTarget(trigger, source, defender) && !defender.InRange(source, 1);
}
