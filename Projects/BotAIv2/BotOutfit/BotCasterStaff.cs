using ModernUO.Serialization;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The staff a caster leans on. One type, coloured by whoever it was made for.
///
/// <para>
/// <b>It exists because the spellbook was in the wrong hand.</b> A book has never had to be held to
/// cast from, only carried — so the first version's casters spent their lives with the weapon slot
/// occupied by the one item that gained nothing from being there, waving a book at skeletons. The
/// staff costs a caster nothing it was using: it is the classic caster's weapon, it is two-handed and
/// therefore takes only a hand that was already wasted, and it pays back the one thing a caster
/// actually runs out of.
/// </para>
///
/// <para>
/// <b>The strength requirement is the whole of an old bug.</b> A quarterstaff asks for thirty Strength
/// and a mage is built with twenty-five, so the engine refused the staff, it went quietly into the
/// pack, and the spellbook took the hand instead. Every mage on that shard carried the staff it was
/// given and never once equipped it — the item worked perfectly and was never used. A caster's stave is
/// not a soldier's weapon, so it does not ask a soldier's strength.
/// </para>
///
/// <para>
/// <b>What it gives back is not stored here.</b> The class says that — <c>StaffManaTrickle</c>, two for
/// the mage and four for the healer — because the warrior-mage gets the same trickle from no item at
/// all, and the rule that they do not stack has to live somewhere that can see both. An item that knew
/// its own payout would be a second place to look.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotCasterStaff : QuarterStaff
{
    /// <summary>Below the weakest build in the nine, so no caster is ever refused its own staff.</summary>
    private const int CasterStrRequirement = 10;

    [Constructible]
    public BotCasterStaff()
    {
        Name = "a caster's staff";
        StrRequirement = CasterStrRequirement;
    }
}
