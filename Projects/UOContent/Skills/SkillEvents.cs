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
    /// Raised once per <see cref="SkillCheck.CheckSkill(Mobile, Skill, object, double)" /> call, after gains
    /// are applied, with the outcome of the check.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not every skill check reaches <c>CheckSkill</c>, so this event does not observe them all. The four
    /// <c>Mobile_SkillCheck*</c> wrappers that <see cref="Mobile" /> dispatches through return before calling
    /// it when the mobile has no such skill, when the check is too difficult
    /// (<c>value &lt; minSkill</c>, or <c>chance &lt; 0.0</c>) and when the check is no challenge
    /// (<c>value &gt;= maxSkill</c>, <c>minSkill &gt;= maxSkill</c>, or <c>chance &gt;= 1.0</c>). A
    /// trivially successful check by a high-skill mobile therefore never raises this event even though it
    /// returns <c>true</c>. Every skill check in this assembly reaches <c>CheckSkill</c> through one of those
    /// wrappers, combat included. A caller that invokes <c>CheckSkill</c> directly always raises the event,
    /// except for the <c>Skills.Cap == 0</c> early return, where no check is rolled.
    /// </para>
    /// <para>
    /// Raised for every <see cref="Mobile" />, not only players: pets and monsters roll skill checks in
    /// combat too, so a subscriber that only cares about players should filter on <c>PlayerMobile</c>.
    /// <c>CheckSkill</c> does not test <see cref="Mobile.Deleted" />, so a subscriber must not assume
    /// <c>from</c> is still in the world.
    /// </para>
    /// <para>
    /// Runs on the game thread; subscribers must not block and should not allocate.
    /// </para>
    /// </remarks>
    public static event Action<Mobile, Skill, bool> SkillChecked;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeSkillChecked(Mobile from, Skill skill, bool success) =>
        SkillChecked?.Invoke(from, skill, success);
}
