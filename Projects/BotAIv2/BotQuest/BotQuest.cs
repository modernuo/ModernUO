using System;

namespace Server.BotAI.V2;

/// <summary>What an errand on the board asks for.</summary>
public enum BotQuestKind
{
    /// <summary>Kill so many of a creature, near a place or anywhere.</summary>
    Kill,

    /// <summary>Bring so many of a thing to a place.</summary>
    Gather,

    /// <summary>Go and stand at a place, so that its square is looked at and counted.</summary>
    Scout
}

/// <summary>
/// One errand on the board: who posted it, what it asks, what it pays, and who holds it. See <see cref="BotQuests"/>.
/// </summary>
public sealed class BotQuest
{
    public int Id { get; init; }

    public BotQuestKind Kind { get; init; }

    /// <summary>The creature or the thing, by its type's name; null for a scout.</summary>
    public string What { get; init; }

    public Type Type { get; init; }

    public int Amount { get; init; }

    /// <summary>How much is done: kills made, or the amount once the goods are handed over, or one once the place is stood at.</summary>
    public int Done { get; set; }

    public Map Map { get; init; }

    /// <summary>Where: the place to kill near (or Zero for anywhere), the place to deliver to, or the place to stand at.</summary>
    public Point3D Where { get; init; }

    /// <summary>The reward, held by the board from the moment of posting. See <see cref="BotQuests.Post"/>.</summary>
    public int Held { get; set; }

    /// <summary>Who posted it: the door, a watcher, or the clock.</summary>
    public string By { get; init; }

    public long PostedTick { get; set; }

    public BotMobile Taker { get; set; }

    /// <summary>When it was taken, or when the taker last made progress on it. See <see cref="BotQuests.HoldMs"/>.</summary>
    public long TakenTick { get; set; }

    /// <summary>
    /// Takers that let it go without a thing done for it: no kill, no delivery, no look. Not kept across a restart. See
    /// <see cref="BotQuests.MostLetGo"/>.
    /// </summary>
    public int LetGo { get; set; }

    /// <summary>Why the last of them let it go, for the line that takes the errand down.</summary>
    public string LetGoBecause { get; set; }

    /// <summary>Who let it go last, and when, whatever was done for it. See <see cref="BotQuests.RetakeMs"/>.</summary>
    public BotMobile LastTaker { get; set; }

    public long LetGoTick { get; set; }

    public bool Open => Taker == null;

    public bool Fulfilled => Kind == BotQuestKind.Kill ? Done >= Amount : Done > 0;

    public string Tell() =>
        Kind switch
        {
            BotQuestKind.Kill => $"#{Id}: kill {Amount} {What}{Near()}, {Done} down, {Held}gp ({By}){Holder()}",
            BotQuestKind.Gather => $"#{Id}: bring {Amount} {What} to ({Where.X}, {Where.Y}), {Held}gp ({By}){Holder()}",
            _ => $"#{Id}: scout ({Where.X}, {Where.Y}), {Held}gp ({By}){Holder()}"
        };

    private string Near() => Where == Point3D.Zero ? " anywhere" : $" near ({Where.X}, {Where.Y})";

    private string Holder() => Taker is { Deleted: false } ? $", in {Taker.Name}'s hands" : "";
}
