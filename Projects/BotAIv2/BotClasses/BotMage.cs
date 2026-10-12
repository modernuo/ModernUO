using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// A spellbook, a blue staff, and no metal.
///
/// <para>
/// <b>The staff is in the hands because the book never needed to be.</b> A spellbook has only ever had
/// to be carried, not held — so the first version's mage spent its life with its weapon slot occupied
/// by the one item that gained nothing from being there, waving a book at skeletons. The staff costs it
/// nothing it was using and pays back the one thing a caster actually runs out of.
/// </para>
///
/// <para>
/// <b>No plate, and the restriction is real rather than flavour.</b> Meditation is this class's main
/// supply of mana and the engine refuses it in metal. That is also the whole of the difference between
/// this class and the warrior-mage: one keeps meditation and gives up armour, the other keeps armour
/// and is handed mana instead.
/// </para>
///
/// <para>
/// Brewing is not its privilege — anybody with the skill and the reagents may brew, and may brew
/// whatever they like. What this class has is the disposition: Alchemy in what it wants, reagents in
/// its pack from birth, and a reason to be near both.
/// </para>
///
/// <para>
/// <b>Inscribe at eighty, and it is what this class does for a living.</b> Without it a mage has no work
/// at all — no pickaxe, no sewing kit, and therefore nothing any proposer can offer it but a trip to a
/// shop, which by design pays nothing. With it the mage has the one trade on this shard whose output no
/// shopkeeper sells: the engine's mage vendors stock the first three circles and stop, so every scroll
/// from the fourth circle up exists only because somebody wrote it.
/// </para>
///
/// <para>
/// Eighty rather than the seventy-five the eighth circle asks for, because the engine gives a nil chance
/// at exactly the minimum: at seventy-five the top circle is not hard, it is impossible. At eighty it is
/// a one-in-ten attempt that burns the blank and the reagents nine times out of ten — which is what makes
/// an eighth-circle scroll worth what somebody will pay for it.
/// </para>
/// </summary>
public sealed class BotMage : BotClass
{
    public override string Name => "Mage";

    public override BotRole Role => BotRole.Caster;

    public override bool Casts => true;

    public override SkillName? MainSkill => SkillName.Magery;

    private static readonly int[] _bookFirst =
    [
        BotArsenal.SpellHarm,
        BotArsenal.SpellFireball,
        BotArsenal.SpellLightning,
        BotArsenal.SpellProtection,
        BotArsenal.SpellEnergyBolt
    ];

    /// <summary>
    /// The spells it fights with before anything cheaper. Magic arrow comes in the kit; the next direct damage of each
    /// circle, and protection so a blow does not cost the cast, come before curses. See <see cref="BotClass.BookFirst"/>.
    /// </summary>
    public override int[] BookFirst => _bookFirst;

    protected override void Defaults()
    {
        Str = 25;
        Dex = 25;
        Int = 50;

        // Magery first on purpose: it ties with Inscribe at eighty, and equal targets break towards
        // whichever the class names first — so the fifty opening points go to the skill this class is
        // named after rather than to the trade it works at.
        Skills =
        [
            (SkillName.Magery, 100.0),
            (SkillName.Inscribe, 100.0),
            (SkillName.Meditation, 100.0),
            (SkillName.Alchemy, 100.0),
            (SkillName.Wrestling, 100.0)
        ];

        // The spellbook leaned into, or the quill. Magery is what this class is judged by and never pays for the quill, so a scribe
        // is paid for out of Meditation alone and leans only a little (01.10.2026, Patrick's point 7). See BotLeaning.
        Leanings =
        [
            new BotLeaning("archmage", SkillName.Magery, 4, 58.0, 72.0),
            new BotLeaning("scribe", SkillName.Inscribe, 1, 36.0, 40.0),
            BotLeaning.Even(1)
        ];

        NeedsMeditation = true;

        StaffManaTrickle = 2;
        StaffHue = 0x48D;

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
            Spells = [BotArsenal.SpellHeal, BotArsenal.SpellMagicArrow, BotArsenal.SpellWeaken]
        };
    }
}
