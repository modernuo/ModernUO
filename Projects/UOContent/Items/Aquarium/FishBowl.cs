using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Network;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FishBowl : BaseContainer
    {
        [Constructible]
        public FishBowl() : base(0x241C)
        {
            Hue = 0x47E;
            MaxItems = 1;
        }

        public override int LabelNumber => 1074499; // A fish bowl

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Empty => Items.Count == 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseFish Fish => Empty ? null : Items[0] as BaseFish;

        public override double DefaultWeight => 2.0;

        public override void OnDoubleClick(Mobile from)
        {
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            if (!CheckHold(from, dropped, sendFullMessage, true))
            {
                return false;
            }

            DropItem(dropped);
            return true;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!IsAccessibleTo(from))
            {
                from.SendLocalizedMessage(502436); // That is not accessible.
                return false;
            }

            if (!(dropped is BaseFish))
            {
                from.SendLocalizedMessage(1074836); // The container can not hold that type of object.
                return false;
            }

            if (base.OnDragDrop(from, dropped))
            {
                ((BaseFish)dropped).StopTimer();
                InvalidateProperties();

                return true;
            }

            return false;
        }

        public override bool CheckItemUse(Mobile from, Item item) => item == this && base.CheckItemUse(from, item);

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (item != this)
            {
                reject = LRReason.CannotLift;
                return false;
            }

            return base.CheckLift(from, item, ref reject);
        }

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            if (!Empty)
            {
                var fish = Fish;

                if (fish != null)
                {
                    list.AddLocalized(1074494, fish.LabelNumber); // Contains: ~1_CREATURE~
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            if (!Empty && IsAccessibleTo(from))
            {
                list.Add(new RemoveCreature());
            }
        }

        private class RemoveCreature : ContextMenuEntry
        {
            public RemoveCreature() : base(6242, 3)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (target is not FishBowl { Deleted: false } bowl || !bowl.IsAccessibleTo(from))
                {
                    return;
                }

                var fish = bowl.Fish;

                if (fish == null)
                {
                    return;
                }

                if (fish.IsLockedDown) // for legacy fish bowls
                {
                    from.SendLocalizedMessage(1010449); // You may not use this object while it is locked down.
                }
                else if (!from.PlaceInBackpack(fish))
                {
                    from.SendLocalizedMessage(1074496); // There is no room in your pack for the creature.
                }
                else
                {
                    from.SendLocalizedMessage(1074495); // The creature has been removed from the fish bowl.
                    fish.StartTimer();
                    bowl.InvalidateProperties();
                }
            }
        }
    }
}
