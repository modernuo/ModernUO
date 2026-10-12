using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a turn at the skillet to anybody carrying one and some meat.
///
/// <para>
/// <b>The tool decides who cooks, not the class name</b> — the same rule the smith and the miner keep, and
/// for the same reason: a bot that picks up a skillet tomorrow is a cook tomorrow, and a list of permitted
/// archetypes is a list that has to be edited every time a class is added.
/// </para>
///
/// <para>
/// <b>Every gate apart, with the denominator, and no bucket called "other".</b> Written that way from the
/// first line this time: three gates went in without counters on the night of 04-05.09.2026 and every one of
/// them made a summary line lie about a mechanism nobody could then see.
/// </para>
/// </summary>
public sealed class BotCook : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCook));

    private static bool _saidNoSystem;

    /// <summary>Bots asked who were carrying a skillet.</summary>
    public static long Asked { get; private set; }

    /// <summary>Answers that went to somebody with no skillet at all. Most of them, and not a refusal.</summary>
    public static long NoKit { get; private set; }

    /// <summary>Cooks with nothing raw in the pack worth putting on.</summary>
    public static long NoMeat { get; private set; }

    /// <summary>Cooks holding meat their skill will not carry a recipe for.</summary>
    public static long Unskilled { get; private set; }

    /// <summary>Cooks with meat and a recipe and no fire anywhere they could carry it to.</summary>
    public static long NoFire { get; private set; }

    /// <summary>Turns at the skillet offered.</summary>
    public static long Offered { get; private set; }

    public string Name => "Cook";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (BotOven.Kit(body) == null)
        {
            NoKit++;

            return null;
        }

        Asked++;

        if (BotOven.System == null)
        {
            if (!_saidNoSystem)
            {
                _saidNoSystem = true;

                logger.Error("The cooking system does not exist yet, so nobody can cook");
            }

            return null;
        }

        var raw = BotOven.Larder(body, out _);

        if (raw == null)
        {
            NoMeat++;

            return null;
        }

        var recipe = BotOven.Choose(body, raw);

        if (recipe?.ItemType == null)
        {
            // Held meat and could not make a meal of it. Counted apart from having no meat, because the two
            // want opposite answers: one is a hunter's problem and the other is a lesson's.
            Unskilled++;

            return null;
        }

        // <b>The fire is looked for last, after the meat and the recipe.</b> Order decides what the summary
        // can say: asked first, it would answer "no fire" for the whole population including the twelve
        // hundred carrying nothing to cook, and the one number that matters — cooks held up by having
        // nowhere to cook — would be buried in it.
        var here = BotOven.AtAHearth(body);
        var hearth = here ? body.Location : BotGround.Hearth(bot, body.Location);

        if (!here && hearth == Point3D.Zero)
        {
            NoFire++;

            return null;
        }

        Offered++;

        return new BotBake(map, hearth, raw, recipe, recipe.ItemType);
    }

    public static string Describe() =>
        Asked == 0
            ? $"nobody has been offered the skillet ({NoKit} answers went to bots with none)"
            : $"{Asked} asked to cook: {Offered} put something on, {NoMeat} had no meat worth cooking, "
              + $"{Unskilled} had meat but no recipe their skill would carry, {NoFire} had both and no fire "
              + $"they could get to (of {BotGround.Hearths.Count} known), {BotBake.Hopped} sent on to another fire from a cold or roadless one; {BotOven.Spared} stacks of raw meat "
              + $"kept back off a corpse for their own pan against {BotOven.Sold} sold on past the cap; {BotMeal.Describe()}";

    public static void Forget()
    {
        _saidNoSystem = false;
        Asked = 0;
        NoKit = 0;
        NoMeat = 0;
        Unskilled = 0;
        NoFire = 0;
        Offered = 0;
        BotOven.Forget();
    }
}
