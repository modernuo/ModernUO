using System;
using Server.Spells;

namespace Server.Items
{
    [Serializable(0)]
    public abstract partial class BaseStaffHide : Item
    {
        public virtual bool CastHide => false;
        public virtual bool CastArea => false;

        public BaseStaffHide(int hue, int itemID = 0x1ECD) : base(itemID)
        {
            Stackable = false;
            Hue = hue;
            Weight = 1.0;
            LootType = LootType.Blessed;
            Visible = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
            }
            else if (CastHide)
            {
                new HideSpell(this, from).Cast();
            }
            else if (CastArea)
            {
                new CastAreaSpell(this, from).Cast();
            }
            else if (from.CanBeginAction<BaseStaffHide>())
            {
                HideEffects(from);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add("(Staff Only)");
        }

        public virtual void HideEffects(Mobile from)
        {
            from.BeginAction<BaseStaffHide>();
        }

        public virtual void OnEndHideEffects(Mobile from)
        {
            from.EndAction<BaseStaffHide>();
        }

        public override bool IsAccessibleTo(Mobile from) => from.AccessLevel >= AccessLevel.GameMaster;

        public override void OnDoubleClickNotAccessible(Mobile from)
        {
            if (from.AccessLevel != AccessLevel.Player)
            {
                base.OnDoubleClickNotAccessible(from);
                return;
            }

            from.SendMessage("It vanishes without a trace.");
            Delete();
        }

        private class HideSpell : Spell
        {
            private static readonly SpellInfo _spellInfo = new("Staff Hide", "", Type.EmptyTypes);

            private readonly Mobile _from;
            private readonly BaseStaffHide _item;

            public HideSpell(BaseStaffHide item, Mobile from) : base(from, null, _spellInfo)
            {
                _from = from;
                _item = item;
            }

            public override bool ClearHandsOnCast => false;
            public override bool RevealOnCast => false;
            public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

            public override TimeSpan GetCastRecovery() => TimeSpan.Zero;

            public override TimeSpan GetCastDelay() => TimeSpan.FromSeconds(1.0);

            public override int GetMana() => 0;

            public override bool ConsumeReagents() => false;

            public override bool CheckFizzle() => false;

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable) => false;

            public override void OnDisturb(DisturbType type, bool message)
            {
            }

            public override void OnCast()
            {
                FinishSequence();
                if (_item is { Deleted: false })
                {
                    _item.HideEffects(_from);
                }
            }
        }

        private class CastAreaSpell : Spell
        {
            private static readonly SpellInfo _spellInfo = new("Staff Hide", "", 263, Type.EmptyTypes);

            private readonly Mobile _from;
            private readonly BaseStaffHide _item;

            public CastAreaSpell(BaseStaffHide item, Mobile from) : base(from, null, _spellInfo)
            {
                _from = from;
                _item = item;
            }

            public override bool ClearHandsOnCast => false;
            public override bool RevealOnCast => false;
            public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

            public override TimeSpan GetCastRecovery() => TimeSpan.Zero;

            public override TimeSpan GetCastDelay() => TimeSpan.FromSeconds(1.0);

            public override int GetMana() => 0;

            public override bool ConsumeReagents() => false;

            public override bool CheckFizzle() => false;

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable) => false;

            public override void OnDisturb(DisturbType type, bool message)
            {
            }

            public override void OnCast()
            {
                FinishSequence();
                if (_item is { Deleted: false })
                {
                    _item.HideEffects(_from);
                }
            }
        }
    }
}
