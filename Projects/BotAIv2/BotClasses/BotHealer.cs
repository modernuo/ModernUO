using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The green staff. Spends its mana on other people, and is equipped accordingly.
///
/// Its staff gives back twice what a mage's does, and the reason is the difference between the two
/// jobs rather than generosity: a mage that runs dry has stopped fighting, and a healer that runs dry
/// has stopped being the reason anybody else is still standing. It gets the better staff on the same
/// argument that gives a smith the better hammer.
///
/// It brews faster than everybody else — seven minutes against ten — and that is its only advantage
/// at the mortar. Brewing itself is open to anyone with the skill and the reagents; what the healer has
/// is the shorter wait and the reason to care.
///
/// Like the mage it refuses metal, and for the same mechanical reason: meditation is where most of its
/// mana comes from and the engine will not allow it in plate.
/// </summary>
public sealed class BotHealer : BotClass
{
    public override string Name => "Healer";

    public override BotRole Role => BotRole.Medic;

    public override bool Casts => true;

    /// <summary>Healing, not Magery. Its spells support the trade; the bandages are the trade.</summary>
    public override SkillName? MainSkill => SkillName.Healing;

    /// <summary>
    /// Only when something has laid hands on it. See <see cref="BotClass.DefendsOnly"/>, whose note says it is
    /// for a medic — and for twelve days no medic said it.
    ///
    /// <para>
    /// <b>The rule outlived the only class that used it.</b> The King's Rangers' healer carried this line, word
    /// for word the whole of what being the healer meant, and when the rangers were cut on 02.09.2026 it went
    /// with them. <c>BotHunter</c> still asks the question and nothing has answered yes since. Measured on the
    /// morning of 14.09.2026: healers spent 26% of their working minutes looking for a fight and 5% in one,
    /// against 10% on their own trade, which is a fifth fighter with a green staff. A flag nobody sets reads
    /// exactly like a rule that holds.
    /// </para>
    /// </summary>
    public override bool DefendsOnly => Defends;

    /// <summary>
    /// Whether healers keep the medic's rule. A dial rather than a constant so the difference it makes can be
    /// measured in both directions on a running shard.
    /// </summary>
    public static bool Defends { get; set; } = true;

    private static readonly int[] _bookFirst =
    [
        BotArsenal.SpellCure,
        BotArsenal.SpellProtection,
        BotArsenal.SpellGreaterHeal,
        BotArsenal.SpellArchCure,
        BotArsenal.SpellArchProtection,
        BotArsenal.SpellResurrection
    ];

    /// <summary>Cures, protection and the greater heals before anything cheaper. See <see cref="BotClass.BookFirst"/>.</summary>
    public override int[] BookFirst => _bookFirst;

    protected override void Defaults()
    {
        Str = 25;
        Dex = 30;
        Int = 45;

        Skills =
        [
            (SkillName.Healing, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.Magery, 100.0),
            (SkillName.Meditation, 100.0),
            (SkillName.Alchemy, 100.0)
        ];

        // The bandage leaned into, paid for out of Anatomy and Magery (01.10.2026, Patrick's point 7). See BotLeaning.
        Leanings =
        [
            new BotLeaning("mender", SkillName.Healing, 4, 58.0, 72.0),
            BotLeaning.Even(1)
        ];

        NeedsMeditation = true;

        // <b>A healer walks out for the herbs its heals spend — Patrick's order of 15.09.2026.</b> Half an hour
        // between trips, against the sage's quarter: the sage feeds the market and this feeds one spellbook. What it
        // picks and what it keeps are BotHerbs' business, which reads the role.
        HerbIntervalMs = 1800000;

        StaffManaTrickle = 4;
        StaffHue = 0x48F;

        BrewIntervalMs = 420000;

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
            Reagents = 30,
            Spells = [BotArsenal.SpellHeal, BotArsenal.SpellCure, BotArsenal.SpellMagicArrow]
        };
    }
}
