using System.Linq;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The maker of both halves of the armour chain: the seam and the forge.
///
/// <para>
/// <b>Its office was a hundredth of every sale on the auction</b>, paid when <em>anybody</em> traded, so the only
/// way it could earn more was for the population as a whole to make more and sell more. The levy went with the
/// auction on 01.10.2026 (Patrick's order of 30.09: every sale hand to hand, nothing settled from nowhere); what is
/// left is a maker of both halves of the armour chain, paid for its forge and its seam like anybody else.
/// </para>
///
/// <para>
/// <b>Born finished, like the captain, and for the same kind of reason.</b> See
/// <see cref="BotClass.Seasoned"/>: a class that sets it "had better have a reason that is not it would be
/// stronger". This one's is that it is the shard's answer to "nobody here is good enough to make that" — the
/// armourer's own refusal, counted by the hundred on the nights when the population wanted plate and had
/// nothing but leather. An architect still learning its trade is one more bot who cannot make the thing.
/// </para>
///
/// <para>
/// <b>Both trades, and both at Expert, because the two halves of its ambition are one chain.</b> Ore becomes
/// ingots becomes armour, and a bot that could dig but not forge would be handing the bottleneck to somebody
/// else and calling it a job. Tailoring and tinkering come with it a little lower: they are how the rest of
/// the population gets dressed while the forge is busy with mail.
/// </para>
/// </summary>
public sealed class BotArchitect : BotClass
{
    public override string Name => "Architect";

    /// <summary>One on the island at any population (07.10.2026, Patrick). See <see cref="BotClass.Office"/>.</summary>
    public override bool Office => true;

    public override BotRole Role => BotRole.Producer;

    /// <summary>The forge. Mining is the higher number and the smaller half of the trade.</summary>
    public override SkillName? MainSkill => SkillName.Blacksmith;

    /// <summary>Born holding both trades. See the class note for why this one is allowed to.</summary>
    public override bool Seasoned => true;

    /// <summary>Born strong and still with somewhere to go. See BotClass.Seasoning.</summary>
    public override double Seasoning => 0.78;

    /// <summary>
    /// Buys a horse, for the same reason the gatherer does: half its trade is at the far end of a walk.
    ///
    /// <para>
    /// <b>The riding flag went on the class that digs and was not put on the other one that digs.</b> This
    /// bot mines its own ore — see the note above on why both halves of the chain are one class — so the
    /// two hundred and forty tiles out to the cave are its walk as much as the gatherer's, and then it has
    /// to carry the ore back to a forge that is in town. It arguably needs the horse more: the gatherer
    /// banks at the counter it passes, while this one's day is a round trip by construction.
    /// </para>
    /// </summary>
    public override bool Rides => true;

    protected override void Defaults()
    {
        // A smith's build: it swings a hammer all day and carries ore up out of a hole, and it is not
        // expected to win a fight. The intelligence is there because tinkering wants it.
        Str = 65;
        Dex = 20;
        Int = 15;

        Skills =
        [
            (SkillName.Mining, 100.0),
            (SkillName.Blacksmith, 100.0),
            (SkillName.Tailoring, 100.0),
            (SkillName.Tinkering, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Healing, 100.0)
        ];

        Kit = new BotKit
        {
            // Carried because everything on this island does, and trained where a crafter's is: it goes to
            // the forge, not to the graveyard.
            Melee = BotArsenal.Melee(100.0),

            Tools = [typeof(SmithHammer), typeof(Pickaxe), typeof(SewingKit), typeof(TinkerTools)],

            Bandages = 30
        };
    }
}
