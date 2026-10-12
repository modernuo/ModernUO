using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot the chance to look after itself, and it is the only thing on the rung that says so.
///
/// <para>
/// <b>This fills the rung the ladder has always had and never had anything on.</b> <c>Failing</c> exists
/// because a bot whose health is going does not weigh options — but with no proposer answering it, the brain's
/// only available answer was to hold on to whatever the bot was already doing. That was harmless while nothing
/// on the shard fought. The day hunting arrived it became "go back to the skeleton", and the first version
/// spent a night proving where that ends: four hundred and forty-three deaths, a hundred and four of them one
/// bot getting up in the same tile every half minute.
/// </para>
///
/// <para>
/// <b>Above everything, including helping anybody else.</b> That ordering is a measured defect, not taste: the
/// first version put the call for help above failing health, so a bot on its last few points announced a
/// company it could not join, found nobody able to come, and posted the same call again — dozens of times over.
/// Flight and self-repair outrank the social.
/// </para>
/// </summary>
public sealed class BotMedic : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMedic));

    private static bool _saidNoMeans;

    public string Name => "Medic";

    /// <summary>The rung this was written for. Nothing else answers it.</summary>
    public BotStanding Rung => BotStanding.Failing;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || !BotMend.Wants(body))
        {
            return null;
        }

        // <b>Nothing is bound up while something is standing over you, by order of 24.08.2026.</b>
        //
        // The engine does not forbid it: a blow past the disturb threshold calls BandageContext.Slip, which
        // costs two per cent of the success chance and four points of the healing, and that is all — which is
        // precisely why the code here used to reach for cloth rather than a spell under fire, since a cast is
        // interrupted outright. The trade was defensible and it is not the one wanted. A bot winding a
        // bandage is a bot standing still in front of whatever is hitting it, buying a fraction of a heal per
        // blow taken, and the answer to being hit is to get away from it and say so — see BotCry — not to
        // stand there dressing the wound while it is reopened.
        //
        // Said as "nothing hostile near", not "nothing is targeting me": a creature two tiles away that has
        // not swung yet will swing during the bandage.
        Asked++;

        if (BotThreat.Anything(body, BotMend.Peril))
        {
            Hostile++;

            var foe = BotThreat.Hunter(body, BotMend.Peril);

            if (foe != null)
            {
                BotCry.Raise(body, foe);
            }

            return null;
        }

        var spell = BotMend.Spell(body, body);

        if (spell >= 0)
        {
            Spelled++;

            return new BotSalve(body, map, onSelf: true, SkillName.Magery);
        }

        if (BotMend.Cloth(body) > 0)
        {
            Bandaged++;

            return new BotSalve(body, map, onSelf: true, SkillName.Healing);
        }

        Dry++;

        Missing(body);

        return null;
    }

    /// <summary>
    /// Every gate apart, and the denominator with them.
    ///
    /// <para>
    /// <b>The one question the consumables trade turns on, and nothing anywhere could answer it.</b> The
    /// shopper's tally said the population was "most often short of Bandage (9637 times)" in four hours,
    /// which is a count of beats and not of bots — a handful that cannot buy any counts thousands of times,
    /// and so does a whole population. So "the healers are dry" and "one healer is dry and asks often" were
    /// the same figure, and there was no honest way to decide whether this shard needs somebody making
    /// bandages out of cloth. <see cref="Dry"/> is a count of bots that were hurt, safe, out of mana and out
    /// of cloth — which is a bot about to die of it.
    /// </para>
    /// </summary>
    public static long Asked { get; private set; }

    /// <summary>Hurt, but something was standing over it. Not a want of supplies.</summary>
    public static long Hostile { get; private set; }

    public static long Spelled { get; private set; }

    public static long Bandaged { get; private set; }

    /// <summary>Hurt, safe, and nothing to mend with. The number that says whether bandages are scarce.</summary>
    public static long Dry { get; private set; }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been hurt and left to look after itself"
            : $"{Asked} looked after themselves: {Spelled} had the mana, {Bandaged} had the cloth, "
              + $"{Hostile} had something standing over them, {Dry} were hurt and safe with neither";

    public static void ForgetCounts()
    {
        Asked = 0;
        Hostile = 0;
        Spelled = 0;
        Bandaged = 0;
        Dry = 0;
    }

    private static void Missing(Mobile body)
    {
        if (_saidNoMeans)
        {
            return;
        }

        _saidNoMeans = true;

        // Once, by name. A bot that cannot mend itself will hold whatever it was doing while it dies, and in a
        // log that is indistinguishable from a bot that is busy.
        logger.Error(
            "{Name} is hurt and has neither the mana, the herbs nor the cloth to do anything about it",
            body.Name
        );
    }

    /// <summary>Lets the complaint be made again after a world reload.</summary>
    public static void Forget() => _saidNoMeans = false;
}
