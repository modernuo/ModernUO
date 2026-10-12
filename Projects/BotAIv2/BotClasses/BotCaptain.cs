namespace Server.BotAI.V2;

/// <summary>
/// The one bot on the shard that exists for the others rather than for itself.
///
/// <para>
/// <b>Born finished, and that is the whole of what makes it different at birth.</b> Every other class starts
/// a trade rather than holding one — <c>BotMobile.Learn</c> hands out fifty, thirty and twenty points to a
/// class's top three skills and nothing else, on purpose, because a population that begins at grandmaster
/// has nothing left to do and nothing to show a watcher. The captain is the exception the shard needs: it
/// cannot lead a company through ground that killed somebody while it is still learning which end of a bow
/// to hold, and it cannot teach a skill it does not have. So it arrives at Expert — see
/// <see cref="BotClass.Seasoned"/> — and that single fact is what buys it both of its offices.
/// </para>
///
/// <para>
/// <b>Both ranges, and the second one is not an apology.</b> <see cref="BotWarriorArcher"/> shoots and keeps
/// a dagger for when that stops working; the dagger is trained forty points below the bow and is, in its own
/// words, "an admission, not a second trade". The captain carries a broadsword at the same Expert standing
/// as its bow, and the difference shows in one moment: when something closes, an archer's whole case for
/// existing is that it is somewhere else, and a captain's is that it is exactly here. See
/// <see cref="Closes"/> — that flag is the class's entire combat identity, and everything else about the way
/// it fights is the shard's ordinary code.
/// </para>
///
/// <para>
/// <b>Expert and no further, which is a ceiling on its teaching before it is a ceiling on itself.</b> A
/// captain may only train a student up to its own standing in the skill, so the two numbers are one number:
/// there is no separate "teaching cap" constant to drift out of step with what the captain actually knows.
/// That is deliberate — a rule with two numbers on the same shelf is the defect this project keeps finding,
/// and the cheapest way not to have it is not to have the second number.
/// </para>
/// </summary>
public sealed class BotCaptain : BotClass
{
    public override string Name => "Captain";

    /// <summary>One on the island at any population (07.10.2026, Patrick). See <see cref="BotClass.Office"/>.</summary>
    public override bool Office => true;

    /// <summary>
    /// Ranged, because that is what it opens with and what the standoff arithmetic reads.
    ///
    /// The role is a statement about distance rather than about damage, and the captain's answer to distance
    /// is an archer's until the distance runs out.
    /// </summary>
    public override BotRole Role => BotRole.Ranged;

    /// <summary>The bow. The sword is equal to it in skill and second to it in order.</summary>
    public override SkillName? MainSkill => SkillName.Archery;

    /// <summary>Calls companies together for places. The one class that may.</summary>
    public override bool Leads => true;

    /// <summary>Born holding its trade rather than starting it. The one class that does.</summary>
    public override bool Seasoned => true;

    /// <summary>Born strong and still with somewhere to go. See BotClass.Seasoning.</summary>
    public override double Seasoning => 0.78;

    /// <summary>Draws steel rather than giving ground. The one class that does.</summary>
    public override bool Closes => true;

    protected override void Defaults()
    {
        // Heavier than an archer and lighter than a brawler: it has to survive the moment it chooses to
        // stand in, and it has to be able to walk a company across half a map to get there.
        Str = 80;
        Dex = 75;
        Int = 25;

        // <b>Declared in full rather than left to the ladder, and both weapon skills are named here.</b>
        // Ordinarily a class leaves its weapon skill out and lets the roll settle it — the captain's roll
        // cannot settle anything, because it is going to hold both. Archery leads because it is what the
        // fight opens with; the sword is one point behind so that the highest skill, and therefore the
        // title over its head, is the one it is known for.
        Skills =
        [
            (SkillName.Archery, 100.0),
            (SkillName.Swords, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.Healing, 100.0)
        ];

        Kit = new BotKit
        {
            Ranged = BotArsenal.Bow(100.0),

            // A broadsword rather than a dagger, at the bow's own standing. This is the line that separates
            // the captain from the hybrid archer.
            Sidearm = new BotWeaponOption(typeof(Server.Items.Broadsword), SkillName.Swords, 77.0),

            // It expects to be the last one standing in a bad square, and it expects to be patching other
            // people up on the way home.
            Bandages = 50
        };
    }
}
