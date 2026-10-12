namespace Server.BotAI.V2;

/// <summary>
/// The plain fighter. Any melee weapon, no talent, no restriction.
///
/// Deliberately the class with nothing special about it, and it is the yardstick the other eight are
/// read against: whenever a talent looks too strong, the question is what it does to a fight this
/// class would have had an even chance in. It also means a shard can be populated entirely with these
/// and still work, which is worth having while the rest is being built.
///
/// Its weapon target sits above Tactics on purpose. A new bot's opening skill points are dealt out to
/// its three highest targets in order, and the first version put Swords and Tactics both at seventy —
/// a tie, broken by whatever order the dictionary happened to enumerate in, so half the warriors on
/// the shard spent their fifty best points on Tactics and could not hit anything.
/// </summary>
public sealed class BotWarrior : BotClass
{
    public override string Name => "Warrior";

    public override BotRole Role => BotRole.Melee;

    /// <summary>Settled by the roll: this class's trade is fighting, and the blade is the roll's business.</summary>
    public override SkillName? MainSkill => null;

    protected override void Defaults()
    {
        Str = 50;
        Dex = 35;
        Int = 15;

        Skills =
        [
            (SkillName.Tactics, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.Healing, 100.0)
        ];

        // <b>A swordsman, a maceman or a fencer by the roll, and how much of one by temperament (01.10.2026, Patrick's point 7:
        // "some may start with more Swordsmanship if they are swordsmen, but put fewer points into other talents").</b> Five in
        // six lean into the weapon the roll handed them, paid for out of Tactics, Anatomy and Healing; one in six is born even.
        Leanings =
        [
            new BotLeaning("blade", null, 5, 56.0, 72.0, Weapon: true),
            BotLeaning.Even(1)
        ];

        Kit = new BotKit
        {
            Melee = BotArsenal.Melee(100.0)
        };
    }
}
