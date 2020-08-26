using System.Text.Json;
using Server.Json;
using Server.Spells.Sixth;

namespace Server.Regions
{
    public class MondainRegion : NoTravelSpellsAllowedRegion
    {
        public MondainRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
        {
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (m.Player && s is MarkSpell)
            {
                m.SendLocalizedMessage(501802); // Thy spell doth not appear to work...
                return false;
            }

            return base.OnBeginSpellCast(m, s);
        }
    }
}
