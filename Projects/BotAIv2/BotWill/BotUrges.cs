using System;

namespace Server.BotAI.V2;

/// <summary>
/// The two things a bot feels, and why there are only two.
///
/// <para>
/// <b>Wanting things is not what drives this population — getting better at something is.</b> The first
/// version made every motive a shortage, and shortages get filled: by the end of an hour thirty-eight of
/// fifty-one bots were patrolling with their drive stuck at 0.62. That was not a defect in the arithmetic.
/// They had run out of things to want. So the value of work here is the <em>rate</em> at which it produces
/// skill and money — see <see cref="BotYield"/> — which cannot be satisfied and put away, because it falls
/// to nothing on its own as the work stops teaching the bot anything. That is the mechanism, and this file
/// is only what is left over.
/// </para>
///
/// <para>
/// <b>Boredom is what is left over on the empty side.</b> It is the one thing that grows while nothing
/// happens, so it cannot be settled and forgotten, and it exists for the case the ledger cannot price: a bot
/// with nothing on offer at all. It does not compete with work — it changes how work is chosen, by making a
/// bored bot discount the thing it has been doing over and over and by shortening the margin it demands
/// before trying something else.
/// </para>
///
/// <para>
/// <b>Need is what is left over on the full side, and it is a fact rather than a feeling</b>: how short the
/// purse is of what this bot was about to try to do. Not a comfort line — the first version compared every
/// purse against a flat 250 while handing every bot 100 at birth, so the entire population read as short of
/// money from its first second, and a signal that is on for everybody always is not a signal.
/// </para>
/// </summary>
public sealed class BotUrges
{
    /// <summary>
    /// How much boredom an idle minute adds. At a tenth, ten minutes of nothing to do takes a bot from
    /// content to fed up, which is about the pace at which a person watching the shard notices.
    /// </summary>
    public static double BoredomPerMinute { get; set; } = 0.10;

    /// <summary>How much boredom a hundred gold-equivalent of takings lifts.</summary>
    public static double ReliefPerHundred { get; set; } = 0.25;

    /// <summary>Where boredom starts changing behaviour rather than only being reported.</summary>
    public static double Restless { get; set; } = 0.5;

    private long _stampTick;

    /// <summary>
    /// Whether the clock has ever been read.
    ///
    /// A flag rather than "is the stamp still zero", and that is this shard's rule rather than taste: on
    /// some hosts the tick count is a pass-through of the physical machine's uptime counter, so it starts
    /// enormous and can wrap negative. Zero is a legitimate reading, which makes it useless as "never".
    /// </summary>
    private bool _stamped;

    private long _barrenTick;

    /// <summary>
    /// How tired of doing nothing this bot is, from nought to one.
    /// </summary>
    public double Boredom { get; private set; }

    /// <summary>
    /// How short this bot is of the money its own plans need, from nought to one. Recomputed from what is on
    /// offer every time the auction runs, and zero when nothing on offer costs anything.
    /// </summary>
    public double Need { get; private set; }

    /// <summary>
    /// Whether the last look for work found nothing at all.
    ///
    /// <para>
    /// The number that matters most for judging the <em>world</em> rather than the bot. If takings are the
    /// measure of work, then a bot with nothing worth doing is not a bot with a broken motive — it is a shard
    /// with nothing left on it that this bot can profit from, and that is a content problem wearing the
    /// costume of an AI problem. The first version could not tell the two apart at all.
    /// </para>
    /// </summary>
    public bool IsBarren { get; private set; }

    /// <summary>
    /// Minutes since this was last asked, and the clock is moved on. Elapsed time rather than a count of
    /// beats, so nothing here changes meaning when the population's beat is retuned.
    /// </summary>
    public double Since(long now)
    {
        if (!_stamped)
        {
            _stamped = true;
            _stampTick = now;

            return 0.0;
        }

        var minutes = (now - _stampTick) / 60000.0;

        _stampTick = now;

        return minutes > 0.0 ? minutes : 0.0;
    }

    /// <summary>Nothing is happening and nothing is being done about it. Boredom rises.</summary>
    public void Idle(double minutes) =>
        Boredom = Math.Clamp(Boredom + minutes * BoredomPerMinute, 0.0, 1.0);

    /// <summary>
    /// Something is being done about it. Boredom holds exactly where it is — deliberately, and it is not the
    /// same as relief.
    ///
    /// <para>
    /// A bot that has been walking to a mine for two minutes is neither entertained nor getting more bored;
    /// it is waiting to find out. Relief comes from the takings when the work settles, which is what makes
    /// unprofitable busyness fail to comfort a bot. The first version's patrol relieved boredom, so a bot
    /// with nothing to do had something to do about it, and it never came back.
    /// </para>
    /// </summary>
    public void Held(double minutes)
    {
    }

    /// <summary>Takings arrived. Boredom falls in proportion to what they came to.</summary>
    public void Paid(double worth)
    {
        if (worth <= 0.0)
        {
            return;
        }

        Boredom = Math.Clamp(Boredom - worth / 100.0 * ReliefPerHundred, 0.0, 1.0);
    }

    /// <summary>
    /// Need, from the purse and from the largest outlay anything on offer wanted. Called by the auction,
    /// which is the only place both figures are known at once.
    /// </summary>
    public void Weigh(int wealth, int outlay) =>
        Need = outlay <= 0 ? 0.0 : Math.Clamp(1.0 - (double)wealth / outlay, 0.0, 1.0);

    /// <summary>The last look found nothing worth doing. The clock starts on the first such look.</summary>
    public void Barren(long now)
    {
        if (IsBarren)
        {
            return;
        }

        IsBarren = true;
        _barrenTick = now;
    }

    /// <summary>Something was taken on. Whatever the drought was, it is over.</summary>
    public void Fruitful() => IsBarren = false;

    /// <summary>How long this bot has had nothing worth doing, in minutes. Zero when it has something.</summary>
    public double BarrenMinutes(long now) =>
        IsBarren ? Math.Max(0.0, (now - _barrenTick) / 60000.0) : 0.0;

    /// <summary>Whether boredom has reached the point where it changes what the bot picks.</summary>
    public bool IsRestless => Boredom >= Restless;

    public override string ToString() => $"boredom {Boredom:F2}, need {Need:F2}";
}
