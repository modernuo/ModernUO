namespace Server.BotAI.V2;

/// <summary>
/// Offers the Baron his own town to walk through.
///
/// <para>
/// <b>Always available, and that is a deliberate choice against the tidier alternative.</b> The obvious way
/// to write the pair was to make this the negative of <see cref="BotHarrower"/> — offer the walk only when
/// there is no ground to harrow — and the two errands would then never compete, which would have made both
/// prices unfalsifiable and left the Baron's mind with a menu of exactly one item every time it was asked.
/// A mind with one option is not thinking; it is being told. Offered together, there is a real question in
/// front of him — go now with the five who happen to be standing here, or walk and be asked again in four
/// minutes — and the arithmetic answers it by default while the mind is allowed to disagree and be measured.
/// </para>
///
/// <para>
/// The anchor is the bank counter rather than a list of towns. Towns are not a thing this shard keeps; a
/// counter is, it is found by the same survey everything else uses, and it is within a few streets of
/// everywhere a person would look for somebody.
/// </para>
/// </summary>
public sealed class BotStroll : IBotProposer
{
    public string Name => "Stroll";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    /// <summary>Asked of somebody who is not a Baron. Not a refusal — nearly every answer is this.</summary>
    public static long NotABaron { get; private set; }

    public static long Held { get; private set; }

    /// <summary>No counter has been surveyed yet, so nothing here knows where a town is.</summary>
    public static long Townless { get; private set; }

    public static long Offered { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (body is not BotMobile { Class: BotBaron })
        {
            NotABaron++;

            return null;
        }

        Asked++;

        // A company is somewhere to be. Offering a walk to a Baron who is leading one would be offering him
        // the chance to leave five bots standing in a wood.
        if (bot is not IBotSquadMember { Squad: null })
        {
            Held++;

            return null;
        }

        // <b>The town at home, not the nearest counter to wherever he stands.</b> On 16.09.2026 at 18:06, after a
        // muster at the north gate failed, the nearest counter to the Baron lay by the plague beast's bog; he walked
        // its corners, fled and mended in the beast's reach, and died at 18:14 on no harrow at all. The stroll is
        // "where you are when no ground is standing", and that is the town.
        var town = BotGround.Counter(map, BotPopulation.Where);

        if (town == Point3D.Zero)
        {
            Townless++;

            return null;
        }

        Offered++;

        return new BotRounds(map, town);
    }

    public static string Describe() =>
        Asked == 0
            ? $"no Baron has ever been offered a walk ({NotABaron} answers went to bots that are not Barons)"
            : $"{Asked} times a Baron was asked: {Offered} were offered a town, {Held} were leading a company, {Townless} had no counter surveyed to walk around; {BotRounds.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NotABaron = 0;
        Held = 0;
        Townless = 0;
        Offered = 0;

        BotRounds.Forget();
    }
}
