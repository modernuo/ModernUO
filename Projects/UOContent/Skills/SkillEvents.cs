using System;
using System.Runtime.CompilerServices;

namespace Server.Misc;

/// <summary>
/// Skill system events. Plain C# events so other assemblies can subscribe; generated events cannot be
/// subscribed across assemblies.
/// </summary>
public static class SkillEvents
{
    /// <summary>
    /// Raised once per skill attempt from the four <c>Mobile_SkillCheck*</c> handlers with the attempt's
    /// outcome, including attempts the handler resolves without a roll (too difficult, no challenge).
    /// Not raised when the mobile lacks the skill. Fires for every <see cref="Mobile" />, including
    /// creatures. Subscribers must not block or allocate.
    /// </summary>
    public static event Action<Mobile, Skill, bool> SkillUsed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeSkillUsed(Mobile from, Skill skill, bool success) =>
        SkillUsed?.Invoke(from, skill, success);
}
