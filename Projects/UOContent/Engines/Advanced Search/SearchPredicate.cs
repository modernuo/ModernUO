using System;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// A property test split by where it may run. <see cref="Worker" /> keeps only worker-safe leaves, a
/// necessary condition that never rejects a true match. <see cref="Loop" /> is the full test, or null
/// when <see cref="Worker" /> is already exact.
/// </summary>
public sealed record SearchPredicate(Func<object, bool> Worker, Func<object, bool> Loop)
{
    public static readonly SearchPredicate Never = new(static _ => false, null);

    public bool NeedsLoop => Loop != null;
}
