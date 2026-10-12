using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Looking after each other, as a module.
///
/// <para>
/// <see cref="BotPhase.World"/> and requires <c>Classes</c> and <c>Will</c>. Nothing else: mending buys
/// nothing and sells nothing, which is why it is the smallest module in the assembly and the one the ladder
/// has been waiting for the longest.
/// </para>
///
/// <para>
/// <b>It hands over two proposers on two different rungs, and the difference between them is the whole
/// design.</b> Mending itself answers <c>Failing</c> — the rung that has existed since the ladder was written
/// and has never had anything on it, so a bot with its health going held on to whatever it was doing. Mending
/// somebody else answers <c>Free</c>, competing with digging and writing on the same arithmetic. The first
/// version had these the other way round and produced bots that called for help they could not join while
/// bleeding to death.
/// </para>
/// </summary>
public sealed class BotMendModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMendModule));

    public override string Name => "Mend";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes", "Will"];

    public override void Start()
    {
        BotMendConfig.Load();

        BotWill.Offer(new BotMedic());
        BotWill.Offer(new BotSurgeon());

        // What a healer does while nobody is hurt yet: stands by one of ours who is fighting. See BotAccompany.
        BotWill.Offer(new BotAttendant());

        // And what a healer does when nobody of ours is fighting anywhere near it: goes to where they are. Six of
        // eleven healers held nothing at 07:1x on 16.09.2026, three hundred tiles from the nearest fight.
        BotWill.Offer(new BotHouseCalls());

        // The other half of looking after yourself, and it belongs here rather than with the hunt: a bot
        // does not have to have gone looking for a fight to be in one. Registered from this module because
        // this module owns <c>Failing</c>, and a rung whose two answers are handed out by two different
        // subsystems is a rung that can be half switched off by accident.
        BotWill.Offer(new BotFugitive());

        // The early flight, for a bot being hit and plainly outmatched before it is failing (build 323). See BotFugitive.
        BotWill.Offer(new BotFugitive(early: true));

        logger.Information(
            "Mending ready: a bot looks after itself below {Hurt:P0} health and stops at {Mended:P0}, spell before cloth; a caster watches {Watch} tiles for somebody worse off",
            BotMend.Hurt,
            BotMend.Mended,
            BotSurgeon.Reach
        );

        logger.Information(
            "Flight ready: on the same rung, a hurt bot runs when what is within {Watch} tiles comes to more than {Bearable:P0} of the strength it has left, and gives up after {GiveUp}s",
            BotBolt.Watch,
            BotFugitive.Bearable,
            BotBolt.GiveUpMs / 1000
        );
    }

    public override void Reset()
    {
        BotMedic.Forget();
        BotMend.Forget();
        BotSurgeon.Forget();
        BotAttendant.Forget();
        BotHouseCalls.Forget();
        BotSalve.Forget();
        BotRetainer.Forget();
        BotFugitive.Forget();
    }
}
