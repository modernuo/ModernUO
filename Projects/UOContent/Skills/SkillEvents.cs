using System;
using System.Runtime.CompilerServices;

namespace Server.Misc;

/// <summary>
/// Plain C# events raised by the skill system. These are deliberately not
/// <c>CodeGeneratedEvents</c>: generated events dispatch statically inside this assembly, so
/// content in another assembly (external spawners, quest systems) cannot subscribe to them.
/// </summary>
public static class SkillEvents
{
    /// <summary>
    /// Raised once per <see cref="SkillCheck.CheckSkill(Mobile, Skill, object, double)" /> call, after
    /// gains are applied, with the outcome of the check. Not raised when the check is skipped because
    /// the mobile has a skill cap of zero. Runs on the game thread; subscribers must not block and
    /// should not allocate.
    /// </summary>
    public static event Action<Mobile, Skill, bool> SkillChecked;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeSkillChecked(Mobile from, Skill skill, bool success) =>
        SkillChecked?.Invoke(from, skill, success);
}
