namespace Server.BotAI.V2;

/// <summary>
/// The one bot on the shard that is not trying to make a living.
///
/// <para>
/// <b>Every other class is an answer to "how does this bot get by".</b> A miner digs because ore sells, a
/// tailor sews because armour is wanted, a captain patrols because the auction happens to offer it and it
/// has nothing it would rather do — and all of them are weighed in gold a minute, which is the one honest
/// currency this shard has. The Baron is the deliberate exception, and he is only interesting <em>as</em> an
/// exception: he is paid nothing, keeps nothing, gives away everything a fight drops, and the two pieces of
/// work he will take are the two the arithmetic would never choose. What replaces the wage is
/// <see cref="Grieves"/> — the ground that has killed people and has not been dealt with.
/// </para>
///
/// <para>
/// <b>Sworn, which is a harder statement than "he prefers his own work".</b> Preference is a number, and a
/// number loses: pricing his hunt high enough to beat a rescue would have been a thumb on the auction's
/// scale, and pricing it honestly would have left him mining. So he is not offered ordinary work at all —
/// see <see cref="BotClass.Sworn"/> — and the four trades he may take are named there rather than implied
/// by a score. This is the only class that says it, and it should stay the only one: a shard where several
/// classes cannot be offered work is a shard whose auction has stopped being the thing that decides.
/// </para>
///
/// <para>
/// <b>Born finished, like the captain and for a harder reason.</b> He walks into ground that has already
/// killed somebody, with five bots behind him who came because he asked, and he is the one standing between
/// the ground and them. A Baron learning which end of a halberd to hold would be five bots' worth of funeral.
/// Ninety-five across the four skills that decide a melee fight in this era — the blade, the tactics behind
/// it, the anatomy that makes it hurt and the resistance that keeps a caster from ending him at range — and
/// healing beside them, because the company he raises is going to need patching and he is the one who never
/// leaves.
/// </para>
///
/// <para>
/// <b>He leads, and that flag is nearly inert on him.</b> <see cref="BotClass.Leads"/> is read by a patrol
/// offer, a lectern and a scouting party, and none of the three is ever put to him: all are on the free rung
/// and all are outside his sworn list, so the auction never asks. He walks unknown ground under his own
/// office instead — see <c>BotWarden</c>, which pays nobody and goes alone. It is set because it is true — he calls companies
/// together for places, which is the whole of what the flag means — and leaving it false would have made the
/// one class that most obviously leads the one class that does not claim to.
/// </para>
/// </summary>
public sealed class BotBaron : BotClass
{
    public override string Name => "Baron";

    /// <summary>One on the island at any population (07.10.2026, Patrick). See <see cref="BotClass.Office"/>.</summary>
    public override bool Office => true;

    public override BotRole Role => BotRole.Melee;

    public override SkillName? MainSkill => SkillName.Swords;

    /// <summary>Calls companies together for places, as a captain does.</summary>
    public override bool Leads => true;

    /// <summary>Born holding his trade. See the note above: a learning Baron is a company's funeral.</summary>
    public override bool Seasoned => true;

    /// <summary>
    /// What he is content by, in place of a wage and an empty afternoon.
    ///
    /// See <c>BotMobile.Mood</c>: boredom and need are the ordinary two halves, and both of them are wrong
    /// for a bot that is paid nothing on purpose. Need would read nought for ever because he buys almost
    /// nothing, and boredom would climb for ever because relief comes from being paid — so the ordinary
    /// arithmetic would have shown him as miserable while he worked and contented while he stood still,
    /// which is exactly backwards.
    /// </summary>
    public override bool Grieves => true;

    /// <summary>
    /// Takes no share of what the company kills. Everything off every corpse is divided among the five who
    /// came, and that division is the whole of what he is offering them.
    /// </summary>
    public override bool Unpaid => true;

    /// <summary>
    /// The trades he will take, and nothing else on the shard.
    ///
    /// <para>
    /// Two of them are his own: the harrowing of a square that has killed people, and the walk he takes
    /// through the town when no square has. <c>Shopper</c> is the errand that keeps him able to do the first
    /// two — bandages and bottles — and it is here because "he needs no money" is a statement about wages,
    /// not about supplies. <c>Mind</c> is on the list because it is not work: it is the door his own
    /// reasoning comes through, and a Baron without it would be a thinking bot that cannot act on a thought.
    /// </para>
    ///
    /// <para>
    /// <b><c>Armoury</c> was on this list for half an hour and had to come off, which is worth writing down
    /// rather than quietly deleting.</b> It buys attack scrolls, the order asked for scrolls, and it looked
    /// obviously right. It is not: a scroll is cast, casting wants Magery, and this build has none — so the
    /// very first thing he did on the shard was walk to a shop, spend twenty-two gold and be told "has
    /// HarmScroll but the book would not take it". An errand that can only ever fail is worse than an errand
    /// that is missing, because it looks like provision.
    /// </para>
    /// </summary>
    /// <summary>
    /// The trades this class may be offered at all. See the class note: a whitelist, not a preference.
    ///
    /// <para>
    /// <b>"Warden" was added on 27.08.2026 and the omission is worth writing down, because a sworn list
    /// fails silently by construction.</b> The rounds were built, registered and reckoned, and the Baron was
    /// never asked once — 862 answers went to bots that are not Barons and not one to the Baron, while his
    /// harrowing, on the same rung and registered two lines above, was asked twenty times. Nothing was
    /// broken: <c>BotWill.Sworn</c> skips a proposer this class may not take before it is ever called, so a
    /// new office for a sworn class is inert until it is named here. Any office added to this class in
    /// future has to be added here in the same breath, or it will look exactly like code that does not run.
    /// </para>
    /// </summary>
    public override string[] Sworn => ["Baron", "Warden", "Undertaker", "Stroll", "Shopper", "Mind"];

    protected override void Defaults()
    {
        // A captain's budget, spent differently: the captain splits it between drawing a bow and standing in
        // contact, and the Baron does only the second. Ninety-five carries gold plate — the engine asks sixty
        // of a cuirass on this era — and sixty-five of dexterity is what is left of a fighter's speed once a
        // full suit has taken its eight points off the chest and more off the rest.
        Str = 95;
        Dex = 65;
        Int = 20;

        // <b>Swordsmanship is declared here as well as on the weapon, and that is deliberate duplication.</b>
        // The kit's own roll is what sets the skill of whatever the bot actually holds, and the class list is
        // what the birth line reads back and what the title is computed from — so a weapon skill left out of
        // this list is a master swordsman whose own paperwork does not mention it. Both numbers are ninety-five
        // and there is nowhere for them to drift apart to: the second one is the first one's source.
        Skills =
        [
            (SkillName.Swords, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Anatomy, 100.0),
            (SkillName.MagicResist, 100.0),
            (SkillName.Healing, 100.0)
        ];

        Kit = new BotKit
        {
            // One option rather than a list, so the roll has nothing to decide. Every other class rolls
            // because which blade it swings is genuinely open; the halberd is not a weapon this bot happened
            // to be issued, it is part of what makes him recognisable across a field.
            Melee = [new BotWeaponOption(typeof(BotBaronHalberd), SkillName.Swords, 95.0)],

            // The whole suit and the cloak, worn at birth and bound. See BotRegalia for why they are types of
            // their own rather than plate out of the ordinary catalogue.
            Armour =
            [
                typeof(BotBaronHelm),
                typeof(BotBaronGorget),
                typeof(BotBaronArms),
                typeof(BotBaronGloves),
                typeof(BotBaronChest),
                typeof(BotBaronLegs),
                typeof(BotBaronCloak)
            ],

            // He is the last one standing in every square he walks into and the one who patches the rest on
            // the way home. Ninety-five in Healing with twenty bandages would be a surgeon with no thread.
            Bandages = 100
        };

        // <b>What "he orders the bottles he needs" comes to.</b> Three heals rather than the shard's default
        // of one, and two cures: he is the bot who stands in contact in every fight his company has, he never
        // withdraws, and a single bottle is one bad thirty seconds. They are bought by BotShopper off the same
        // list birth hands out, so this one number is the whole of the change.
        PotionLimits[BotPotionKind.Heal] = 3;
        PotionLimits[BotPotionKind.Cure] = 2;

        // Ten thousand, by order, kept up whenever it falls below a tenth of that. It is not a wage and it is
        // not savings: see BotStipend for why the one bot on the shard who is given money is also the one
        // whose money never competes with anybody's.
        Stipend = 10000;
    }
}
