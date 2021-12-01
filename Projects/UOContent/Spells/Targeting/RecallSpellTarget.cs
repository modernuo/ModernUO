using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Spells
{
    public class RecallSpellTarget : Target
    {
        private readonly IRecallSpell _spell;
        private readonly bool m_ToBoat;

        public RecallSpellTarget(IRecallSpell spell, bool toBoat = true) : base(Core.ML ? 10 : 12, false, TargetFlags.None)
        {
            _spell = spell;
            m_ToBoat = toBoat;
            _spell.Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501029); // Select Marked item.
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is RecallRune rune)
            {
                if (rune.Marked)
                {
                    _spell.Effect(rune.Target, rune.TargetMap, true);
                }
                else
                {
                    from.SendLocalizedMessage(501805); // That rune is not yet marked.
                }
            }
            else if (o is Runebook runebook)
            {
                var e = runebook.Default;

                if (e != null)
                {
                    _spell.Effect(e.Location, e.Map, true);
                }
                else
                {
                    from.SendLocalizedMessage(502354); // Target is not marked.
                }
            }
            else if (m_ToBoat && o is Key key && key.KeyValue != 0 && key.Link is BaseBoat boat)
            {
                if (!boat.Deleted && boat.CheckKey(key.KeyValue))
                {
                    _spell.Effect(boat.GetMarkedLocation(), boat.Map, false);
                }
                else
                {
                    from.NetState.SendMessageLocalized(
                        from.Serial,
                        from.Body,
                        MessageType.Regular,
                        0x3B2,
                        3,
                        502357, // I can not recall from that object.
                        from.Name
                    );
                }
            }
            else if (o is HouseRaffleDeed deed && deed.ValidLocation())
            {
                _spell.Effect(deed.PlotLocation, deed.PlotFacet, true);
            }
            else
            {
                from.NetState.SendMessageLocalized(
                    from.Serial,
                    from.Body,
                    MessageType.Regular,
                    0x3B2,
                    3,
                    502357, // I can not recall from that object.
                    from.Name
                );
            }
        }

        protected override void OnNonlocalTarget(Mobile from, object o)
        {
        }

        protected override void OnTargetFinish(Mobile from)
        {
            _spell?.FinishSequence();
        }
    }
}
