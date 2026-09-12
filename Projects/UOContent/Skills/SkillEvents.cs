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
    /// Raised once per <see cref="SkillCheck.CheckSkill(Mobile, Skill, object, double)" /> after gains are
    /// applied. Not raised when the skill cap is zero, or when a <c>Mobile_SkillCheck*</c> wrapper
    /// short-circuits (missing skill, too difficult, or no challenge) before reaching <c>CheckSkill</c>.
    /// Fires for every <see cref="Mobile" />, including creatures. Subscribers must not block or allocate.
    /// </summary>
    public static event Action<Mobile, Skill, bool> SkillChecked;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeSkillChecked(Mobile from, Skill skill, bool success) =>
        SkillChecked?.Invoke(from, skill, success);
}
