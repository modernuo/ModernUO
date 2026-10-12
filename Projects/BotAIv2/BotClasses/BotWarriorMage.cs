namespace Server.BotAI.V2;

/// <summary>
/// Plate, a blade, and spells anyway.
///
/// The one class that is defined by what it gives up. Meditation is how a caster gets mana back, and
/// the engine refuses it to anybody in metal — so a fighter who wanted to cast had to choose between
/// armour and being able to cast twice. This class chooses armour and is paid back with mana that
/// arrives on its own, in plate, in a fight, without sitting down.
///
/// Its trickle is worth the same as a mage's staff and does not stack with one: holding a mage's staff
/// buys it nothing. The advantage is not the quantity, it is that its hands are free for a sword while
/// it happens — and a mage that wants a sword has no answer to that at all.
///
/// It is the only class allowed two mana potions, for the same reason it is the only one with the
/// trickle: mana is what it runs out of, and the bottle is the only mana available to anybody who is
/// being hit.
/// </summary>
public sealed class BotWarriorMage : BotClass
{
    public override string Name => "WarriorMage";

    /// <summary>Melee, not caster: it holds the line. Its spells are reported through <see cref="Casts"/>.</summary>
    public override BotRole Role => BotRole.Melee;

    public override bool Casts => true;

    /// <summary>Settled by the roll, like the plain warrior. It is a fighter first.</summary>
    public override SkillName? MainSkill => null;

    private static readonly int[] _bookFirst =
    [
        BotArsenal.SpellHarm,
        BotArsenal.SpellProtection,
        BotArsenal.SpellFireball,
        BotArsenal.SpellLightning
    ];

    /// <summary>
    /// A short list, because it is a fighter first: the direct damage it can throw between swings, and protection so
    /// a blow does not cost the cast. See <see cref="BotClass.BookFirst"/>.
    /// </summary>
    public override int[] BookFirst => _bookFirst;

    protected override void Defaults()
    {
        Str = 40;
        Dex = 25;
        Int = 35;

        Skills =
        [
            (SkillName.Magery, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.Healing, 100.0)
        ];

        // Either half of the class leaned into: the blade (under its option's target of seventy) or the spellbook, the other half
        // keeping at least half its birth value (01.10.2026, Patrick's point 7). See BotLeaning.
        Leanings =
        [
            new BotLeaning("blade", null, 2, 56.0, 66.0, Weapon: true),
            new BotLeaning("spellsword", SkillName.Magery, 2, 56.0, 68.0),
            BotLeaning.Even(1)
        ];

        // Explicit rather than left at the default, because this is the one class for which the answer
        // is interesting: it wears plate, it therefore cannot meditate, and that is the whole premise.
        NeedsMeditation = false;

        IntrinsicManaTrickle = 2;

        PotionLimits[BotPotionKind.Mana] = 2;


        // <b>The one thing it burns that it could not go and get.</b> Reagents are shop goods in this era —
        // nothing grows them — so until 02.09.2026 the only route into a mage's pack was a counter, and a
        // mage with an empty purse was a mage with no way back: the debugger found Quill, Perri and Rowan
        // standing on 2, 8 and 24 gold with the only offer any of thirty-one proposers made being another
        // purchase they could not afford. The Sage has had the woods since it was written and the argument
        // was never about the Sage: it is that a caster short of herbs should have a day's walk as an
        // alternative to a shelf. Longer than the Sage's half hour, because the Sage's trip is its trade and
        // this one is a fallback.
        HerbIntervalMs = 2700000;

        // <b>A handful, not a haul.</b> Given the woods on 02.09.2026 and no amount of its own, a mage took
        // the Sage's trip — which is that class's trade and pays 2 to 5 kinds at 5 to 20 each: Rowan came
        // back with 65 herbs at 23:36 and Perri with 34. A caster that fills its pack in one walk has no
        // reason to buy, order or pay anybody, which quietly undoes the supplier the Gatherer is meant to be
        // and the market Patrick asked for. Five to twelve of one kind, three quarters of an hour apart, is a
        // supplement to the counter rather than a replacement for it.
        ForageYieldMin = 5;
        ForageYieldMax = 12;

        Kit = new BotKit
        {
            Melee = BotArsenal.MeleeWithStaff(70.0),
            Reagents = 30,
            Spells = [BotArsenal.SpellHeal, BotArsenal.SpellMagicArrow]
        };
    }
}
