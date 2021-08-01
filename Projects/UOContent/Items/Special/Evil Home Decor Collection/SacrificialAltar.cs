using System;

namespace Server.Items
{
    [FlippableAddon(Direction.South, Direction.East)]
    public class SacrificialAltarAddon : BaseAddonContainer
    {
        private TimerExecutionToken _timerToken;

        [Constructible]
        public SacrificialAltarAddon() : base(0x2A9B)
        {
            Direction = Direction.South;

            AddComponent(new LocalizedContainerComponent(0x2A9A, 1074818), 1, 0, 0);
        }

        public SacrificialAltarAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonContainerDeed Deed => new SacrificialAltarDeed();
        public override int LabelNumber => 1074818; // Sacrificial Altar
        public override int DefaultMaxWeight => 0;
        public override int DefaultGumpID => 0x107;
        public override int DefaultDropSound => 0x42;

        private void StartTimer()
        {
            _timerToken.Cancel();
            Timer.StartTimer(TimeSpan.FromMinutes(3), Empty, out _timerToken);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!base.OnDragDrop(from, dropped))
            {
                return false;
            }

            if (TotalItems >= 50)
            {
                SendLocalizedMessageTo(from, 501478); // The trash is full!  Emptying!
                Empty();
            }
            else
            {
                SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

                StartTimer();
            }

            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!base.OnDragDropInto(from, item, p))
            {
                return false;
            }

            if (TotalItems >= 50)
            {
                SendLocalizedMessageTo(from, 501478); // The trash is full!  Emptying!
                Empty();
            }
            else
            {
                SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

                StartTimer();
            }

            return true;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (Items.Count > 0)
            {
                StartTimer();
            }
        }

        public virtual void Flip(Mobile from, Direction direction)
        {
            switch (direction)
            {
                case Direction.East:
                    ItemID = 0x2A9C;
                    AddComponent(new LocalizedContainerComponent(0x2A9D, 1074818), 0, -1, 0);
                    break;
                case Direction.South:
                    ItemID = 0x2A9B;
                    AddComponent(new LocalizedContainerComponent(0x2A9A, 1074818), 1, 0, 0);
                    break;
            }
        }

        public virtual void Empty()
        {
            if (Items.Count > 0)
            {
                var location = Location;
                location.Z += 10;

                Effects.SendLocationEffect(location, Map, 0x3709, 10, 10, 0x356);
                Effects.PlaySound(location, Map, 0x32E);

                if (Items.Count > 0)
                {
                    for (var i = Items.Count - 1; i >= 0; --i)
                    {
                        if (i >= Items.Count)
                        {
                            continue;
                        }

                        Items[i].Delete();
                    }
                }
            }

            _timerToken.Cancel();
        }
    }

    public class SacrificialAltarDeed : BaseAddonContainerDeed
    {
        [Constructible]
        public SacrificialAltarDeed() => LootType = LootType.Blessed;

        public SacrificialAltarDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddonContainer Addon => new SacrificialAltarAddon();
        public override int LabelNumber => 1074818; // Sacrificial Altar

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
