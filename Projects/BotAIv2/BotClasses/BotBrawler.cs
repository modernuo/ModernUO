namespace Server.BotAI.V2;

/// <summary>
/// Fights with its hands, and is therefore never holding anything it has to put down.
///
/// <para>
/// <b>Free hands is the talent, and it is the quietest strong thing in the nine.</b> Bandaging takes
/// both hands and several seconds, so every armed bot is forbidden to bandage while something is in
/// contact with it — which left a wounded bot in the first version exactly two options, and both were
/// bad: stand still and die, or run, losing the fight and the training. Drinking and casting have the
/// same problem in a milder form. A bot that fights with its fists has never had it. Nothing new is
/// added to give it this advantage: an existing restriction simply does not apply.
/// </para>
///
/// <para>
/// The rest of "very flexible" is its build — the highest Dexterity of any class and the lowest
/// Intelligence, so it swings often and has stamina left to keep stepping. No dodge chance: there is
/// no such mechanic in this era, and inventing one would put this class in the same position as the
/// mana potion, which exists outside the era and had to be justified item by item.
/// </para>
///
/// <para>
/// Two heal potions rather than one, because healing is what it is built around: it stands in contact
/// longer than anything else on the shard, and the bottle is the only healing available while there.
/// </para>
/// </summary>
public sealed class BotBrawler : BotClass
{
    public override string Name => "Brawler";

    public override BotRole Role => BotRole.Melee;

    public override SkillName? MainSkill => SkillName.Wrestling;

    protected override void Defaults()
    {
        // <b>Strength carries a fist, and this class had the least of it.</b> Damage from wrestling in this
        // era comes off Strength, Tactics and Anatomy and off nothing else — there is no item in the world
        // that can add to it — so a brawler built at forty Strength was the weakest fighter on the shard by
        // arithmetic rather than by bad luck. Sixty, which is between the archer and the captain: it is the
        // one build whose whole case is standing in contact, and it has to be able to.
        Str = 60;
        Dex = 40;
        Int = 10;

        // <b>Higher than any other class's opening, and that is the point of a class with one trade.</b>
        // Everybody else spreads its start across a weapon skill it will grow into; a brawler's weapon is
        // its hands and it has nothing else to grow. Tactics and Anatomy are raised with it because in this
        // era they are not support skills for a puncher, they are half of the damage.
        Skills =
        [
            (SkillName.Wrestling, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.Healing, 100.0)
        ];

        // Fists leaned into, paid for out of Tactics and Anatomy (01.10.2026, Patrick's point 7). See BotLeaning.
        Leanings =
        [
            new BotLeaning("brawler", SkillName.Wrestling, 4, 58.0, 72.0),
            BotLeaning.Even(1)
        ];

        HandsAlwaysFree = true;

        PotionLimits[BotPotionKind.Heal] = 2;

        // No weapon and no tools — the only class the granting step hands nothing to hold. What it is given
        // instead goes <em>on</em> the hands: see BotBrawlerGloves for why that is armour and not damage.
        Kit = new BotKit
        {
            Armour = [typeof(BotBrawlerGloves)]
        };
    }
}
