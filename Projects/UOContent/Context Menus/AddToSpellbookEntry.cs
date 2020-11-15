using Server.Items;
using Server.Targeting;

namespace Server.ContextMenus
{
    public class AddToSpellbookEntry : ContextMenuEntry
    {
        public AddToSpellbookEntry() : base(6144, 3)
        {
        }

        public override void OnClick()
        {
            if (Owner.From.CheckAlive() && Owner.Target is SpellScroll scroll)
            {
                Owner.From.Target = new InternalTarget(scroll);
            }
        }

        private class InternalTarget : Target
        {
            private readonly SpellScroll m_Scroll;

            public InternalTarget(SpellScroll scroll) : base(3, false, TargetFlags.None) => m_Scroll = scroll;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Spellbook book)
                {
                    if (from.CheckAlive() && !m_Scroll.Deleted && m_Scroll.Movable && m_Scroll.Amount >= 1 &&
                        m_Scroll.CheckItemUse(from))
                    {
                        var type = Spellbook.GetTypeForSpell(m_Scroll.SpellID);

                        if (type != book.SpellbookType)
                        {
                        }
                        else if (book.HasSpell(m_Scroll.SpellID))
                        {
                            from.SendLocalizedMessage(500179); // That spell is already present in that spellbook.
                        }
                        else
                        {
                            var val = m_Scroll.SpellID - book.BookOffset;

                            if (val >= 0 && val < book.BookCount)
                            {
                                book.Content |= (ulong)1 << val;

                                m_Scroll.Consume();
                                from.SendSound(0x249, book.GetWorldLocation());
                            }
                        }
                    }
                }
            }
        }
    }
}
