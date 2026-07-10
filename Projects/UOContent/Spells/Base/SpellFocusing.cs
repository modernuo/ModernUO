using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells;

public static class SpellFocusing
{
    public static void Configure()
    {
        EventSink.Logout += Clear;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void Clear(Mobile mobile)
    {
        if (mobile == null)
        {
            return;
        }

        var items = mobile.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is IAosItem item)
            {
                item.Attributes.ResetSpellFocusing();
            }
        }
    }

    public static bool TryGetDamageOffset(Spell spell, Mobile caster, Mobile target, out int offset)
    {
        offset = 0;

        if (!Core.AOS || spell?.SpellFocusingEligible != true || caster?.Alive != true || target?.Alive != true)
        {
            return false;
        }

        var items = caster.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is IAosItem { Attributes.SpellFocusing: not 0 } item)
            {
                offset = item.Attributes.GetSpellFocusingOffset(caster, target);
                return true;
            }
        }

        return false;
    }
}