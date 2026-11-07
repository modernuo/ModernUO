using System;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// A property test split by where it may run. <see cref="Worker" /> is a necessary condition made
/// only of leaves that touch no game state and run no user code, so a search worker can apply it
/// without ever rejecting a true match. <see cref="Loop" /> is the full test, or null when every
/// leaf is worker-safe and <see cref="Worker" /> is already exact.
/// </summary>
public sealed record SearchPredicate(Func<object, bool> Worker, Func<object, bool> Loop)
{
    public static readonly SearchPredicate Never = new(static _ => false, null);

    public bool NeedsLoop => Loop != null;
}
