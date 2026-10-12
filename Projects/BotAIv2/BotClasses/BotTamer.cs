namespace Server.BotAI.V2;

/// <summary>
/// A bow, and a beast beside it.
///
/// <para>
/// <b>Patrick's third addition of the night of 29.09.2026: "the bots have knowledge of taming domestic and wild animals,
/// and the animals help them in a fight."</b> The tamer is the class that knows most of it: Animal Taming to bring a wolf
/// or a bear to heel, Animal Lore to know what it is looking at, Veterinary to bind the beast's wounds, and a bow to fight
/// beside it from a distance. Every other class that lists Animal Taming among its targets tames what its skill allows —
/// a dog, a horse — and the same errand serves them (<c>BotTame</c>).
/// </para>
///
/// <para>
/// <b>Ranged by role, because that is what a tamer is in a fight.</b> The beast closes and holds; the tamer shoots. The
/// class's own strength is measured like everybody's (<c>BotThreat.Power</c>) and does not count its beast, so a tamer
/// alone is offered what an archer is offered; the beast is what makes it come back.
/// </para>
/// </summary>
public sealed class BotTamer : BotClass
{
    public override string Name => "Tamer";

    public override BotRole Role => BotRole.Ranged;

    public override SkillName? MainSkill => SkillName.AnimalTaming;

    protected override void Defaults()
    {
        Str = 35;
        Dex = 45;
        Int = 25;

        Skills =
        [
            (SkillName.AnimalTaming, 100.0),
            (SkillName.AnimalLore, 100.0),
            (SkillName.Veterinary, 100.0),
            (SkillName.Archery, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Healing, 60.0)
        ];

        // The tamer's own craft leaned into, or the bow the roll handed it (01.10.2026, Patrick's point 7). See BotLeaning.
        Leanings =
        [
            new BotLeaning("beastmaster", SkillName.AnimalTaming, 3, 58.0, 70.0),
            new BotLeaning("marksman", null, 1, 56.0, 66.0, Weapon: true),
            BotLeaning.Even(1)
        ];

        CritChancePerSkill = 0.001;
        CritMultiplier = 3;

        Kit = new BotKit
        {
            Ranged = BotArsenal.Bow(100.0),

            Sidearm = BotArsenal.Sidearm(40.0)
        };
    }
}
