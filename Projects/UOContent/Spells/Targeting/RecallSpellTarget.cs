using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Spells;

public class RecallSpellTarget : Target
{
    private readonly IRecallSpell _spell;
    private readonly bool _toBoat;

    public RecallSpellTarget(IRecallSpell spell, bool toBoat = true) : base(spell.TargetRange, false, TargetFlags.None)
    {
        _spell = spell;
        _toBoat = toBoat;
        _spell.Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501029); // Select Marked item.
    }

    protected override void OnTarget(Mobile from, object o)
    {
        if (o is RecallRune rune)
        {
            if (!rune.Marked)
            {
                from.SendLocalizedMessage(501805); // That rune is not yet marked.
                return;
            }

            _spell.Effect(rune.Target, rune.TargetMap, true);
        }
        else if (o is Runebook runebook)
        {
            var e = runebook.Default;

            if (e == null)
            {
                from.SendLocalizedMessage(502354); // Target is not marked.
                return;
            }

            _spell.Effect(e.Location, e.Map, true);
        }
        else if (_toBoat && o is Key key && key.KeyValue != 0 && key.Link is BaseBoat boat && !boat.Deleted &&
                 boat.CheckKey(key.KeyValue))
        {
            _spell.Effect(boat.GetMarkedLocation(), boat.Map, false);
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

    protected override void OnTargetFinish(Mobile from) => _spell?.FinishSequence();
}
