using Server.Items;
using Server.Network;

namespace Server.Spells.Sixth
{
    public class MarkSpell : MagerySpell, ISpellTargetingItem
    {
        private static readonly SpellInfo _info = new(
            "Mark",
            "Kal Por Ylem",
            218,
            9002,
            Reagent.BlackPearl,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot
        );

        public MarkSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public void Target(Item item)
        {
            if (item is not RecallRune rune)
            {
                Caster.NetState.SendMessageLocalized(
                    Caster.Serial,
                    Caster.Body,
                    MessageType.Regular,
                    0x3B2,
                    3,
                    501797, // I cannot mark that object.
                    Caster.Name
                );
            }
            else if (!SpellHelper.CheckTravel(Caster, TravelCheckType.Mark, out var failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
            }
            else if (SpellHelper.CheckMulti(Caster.Location, Caster.Map, !Core.AOS))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (!rune.IsChildOf(Caster.Backpack))
            {
                // You must have this rune in your backpack in order to mark it.
                Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1062422);
            }
            else if (CheckSequence())
            {
                rune.Mark(Caster);

                Caster.PlaySound(0x1FA);
                Effects.SendLocationEffect(Caster, 14201, 16);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetItem(this, range: Core.ML ? 10 : 12);
        }

        public override bool CheckCast()
        {
            if (base.CheckCast())
            {
                return true;
            }

            if (!SpellHelper.CheckTravel(Caster, TravelCheckType.Mark, out var failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
                return false;
            }

            return true;
        }
    }
}
