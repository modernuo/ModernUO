using System.Linq;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The captain's opposite number, for the half of the population a captain cannot teach.
///
/// <para>
/// <b>A school for fighters existed and a school for casters did not, and the gap was not an oversight so
/// much as an arithmetic one.</b> <c>BotDrill</c> takes students who are <c>Melee or Ranged</c>, because its
/// master is a captain and a captain can only teach what it knows. A mage was therefore the one build on the
/// shard that could never be taught anything by anybody — and it is also the build whose value depends most
/// on what it knows, because everything it can do is rationed: mana, reagents, and a book that has to be
/// bought or written before it holds a single thing worth throwing.
/// </para>
///
/// <para>
/// <b>It teaches up to its own standing and no further, which is one number rather than two.</b> The same
/// rule the captain lives by, and for the same reason stated there: a separate teaching cap is a second
/// number on one shelf, and this project keeps paying for those. What it knows is what it can hand on.
/// </para>
///
/// <para>
/// <b>One lectern, and the captain has first claim on it.</b> The school holds one master at a time, so a
/// sage that finds the field taken does not teach — see <c>BotSchool.Master</c>. That is not deference, it is
/// the field being a place: two masters calling classes on the same plot would have the students standing in
/// two rings and hearing neither.
/// </para>
///
/// <para>
/// <b>Four circles at birth, and that is what makes it a teacher rather than a student.</b> A caster's book
/// is normally three spells and a long youth of buying scrolls; this one arrives knowing the first four
/// circles outright, so it can write any of them for anybody who asks and can demonstrate any of them to
/// anybody it teaches. The book is bound to it like every other book on the shard — a caster that loses its
/// book is not a caster and cannot earn the price of another.
/// </para>
/// </summary>
public sealed class BotSage : BotClass
{
    /// <summary>The last spell id of the fourth circle. Circles are eight spells each, counted from nought.</summary>
    private const int FourthCircle = 31;

    public override string Name => "Sage";

    /// <summary>One on the island at any population (07.10.2026, Patrick). See <see cref="BotClass.Office"/>.</summary>
    public override bool Office => true;

    public override BotRole Role => BotRole.Caster;

    public override SkillName? MainSkill => SkillName.Magery;

    /// <summary>Born holding its trade. It cannot teach what it does not have.</summary>
    public override bool Seasoned => true;

    /// <summary>Born strong and still with somewhere to go. See BotClass.Seasoning.</summary>
    public override double Seasoning => 0.78;

    /// <summary>Opens a class for casters, as a captain does for fighters. The one class that may.</summary>
    public override bool Tutors => true;

    public override bool Casts => true;

    protected override void Defaults()
    {
        // A caster's build, and the intelligence is not decoration: it is the pool every one of its offices
        // is paid out of — the spells it throws, the scrolls it writes and the demonstrations it teaches by.
        Str = 25;
        Dex = 20;
        Int = 55;

        Skills =
        [
            (SkillName.Magery, 100.0),
            (SkillName.EvalInt, 100.0),
            (SkillName.Inscribe, 100.0),
            (SkillName.Alchemy, 100.0),
            (SkillName.Meditation, 100.0)
        ];

        // <b>The best staff on the shard, and it is not decoration.</b> A caster's staff pays back the one
        // thing a caster runs out of — see BotCasterStaff — and the sage is the bot whose every office is
        // paid out of that pool: the spells it throws, the scrolls it writes, and the demonstrations it
        // teaches by. Five against the mage's two and the healer's four.
        //
        // It also settles a question that has nothing to do with mana. A class with no weapon in its kit is
        // a class whose hands are free at birth, and the first thing that fits a free hand goes into it: the
        // sage was issued a skinning knife with the rest of the population's tools and stood in fights
        // holding it. Two hands full of staff is the honest answer, and BotOutfit no longer offers a hand to
        // a tool at all.
        StaffManaTrickle = 5;

        // Its own colour, so a watcher can tell the two lecterns apart at a glance.
        StaffHue = 0x4AA;

        // Half an hour. Long enough that the trip is an event rather than a supply line, short enough that a
        // shard whose shelves have nothing is never more than half an hour from casting again.
        HerbIntervalMs = 1800000;

        Kit = new BotKit
        {
            // <b>The robe, which is dress and not armour, and that is why it had to be issued here.</b>
            // BotHarness surveys the craft systems for BaseArmor and covers six layers, none of them the
            // outer torso — so a robe cannot enter the catalogue and cannot be ordered from it, whatever a
            // caster is willing to pay. It stops nothing and is not meant to: this era gives a robe no
            // armour rating at all. What it is is the one garment that reads as a mage from across a
            // street, worn from birth and bound like everything else issued, so nobody sells it at a
            // counter with a full pack.
            Armour = [typeof(Robe)],
            Staff = true,

            // Herbs enough to be worth asking for a lesson from: a teacher that runs dry in the middle of
            // the second demonstration has taught the first one only.
            Reagents = 60,

            // The first four circles, whole. Written out of the count rather than listed one by one, so
            // that "four circles" stays a fact somebody can check rather than thirty-two literals to trust.
            Spells = Enumerable.Range(0, FourthCircle + 1).ToArray(),

            Bandages = 20
        };
    }
}
