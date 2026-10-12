namespace Server.BotAI.V2;

/// <summary>
/// The bow and nothing else, and the only class that can triple a hit.
///
/// No sidearm on purpose. Handing it a knife would make it a worse warrior-archer, and the interesting
/// thing about a pure archer is that closing with it is supposed to work — its answer to something in
/// its face is distance, which it has to earn every few seconds by stepping back and shooting again.
///
/// <para>
/// <b>The critical.</b> One shot in ten at grandmaster, one in thirty at birth, three times damage
/// when it lands. Scaled by Archery rather than granted whole because a talent handed out at birth is
/// not something a bot can work towards, and wanting to get better is the only motive this project
/// has — the same argument that makes a rank raise a target rather than satisfy it.
/// </para>
///
/// <para>
/// It has to be visible to whatever decides whether a fight is winnable. Danger in this project is
/// toughness times damage, and an archer that quietly does more damage than its numbers say will keep
/// declining fights it would have won. That is a defect waiting in the decision layer, not here, and
/// it is written down so it is not discovered from a log.
/// </para>
/// </summary>
public sealed class BotArcher : BotClass
{
    public override string Name => "Archer";

    public override BotRole Role => BotRole.Ranged;

    public override SkillName? MainSkill => SkillName.Archery;

    protected override void Defaults()
    {
        Str = 35;
        Dex = 50;
        Int = 15;

        Skills =
        [
            (SkillName.Tactics, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.Healing, 100.0)
        ];

        // A marksman five times in six, the bow paid for out of the rest; Archery is the skill this class is judged by, so it
        // never gives a point to anything else (01.10.2026, Patrick's point 7). See BotLeaning.
        Leanings =
        [
            new BotLeaning("marksman", null, 5, 58.0, 74.0, Weapon: true),
            BotLeaning.Even(1)
        ];

        // A thousandth per point: 3% at the thirty a novice starts with, 10% at a hundred.
        CritChancePerSkill = 0.001;
        CritMultiplier = 3;

        Kit = new BotKit
        {
            Ranged = BotArsenal.Bow(100.0),

            // <b>A blade for when the quiver is empty, and its absence was worth measuring.</b> This class
            // was issued a bow and nothing else, so an archer that ran out of arrows had literally nothing
            // in its hands that could hurt anything: on the morning of 04.09.2026 five archers between them
            // failed sixty-seven fights in ten minutes with "100% of it left and not a scratch in 45s",
            // against a mongbat and an ettin alike — the flat signature of no swing rather than a bad one.
            // Nobody on this shard fletches, the shopkeepers carry few, and the population was born with
            // about nineteen hundred arrows between thirteen shooters, so running dry is not an accident,
            // it is the arithmetic. The warrior-archer beside this class has had a sidearm all along.
            Sidearm = BotArsenal.Sidearm(40.0)
        };
    }
}
