using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
    [Flippable(0x125E, 0x1230)]
    public class GuillotineComponent : AddonComponent
    {
        public GuillotineComponent() : base(0x125E)
        {
        }

        public GuillotineComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1024656; // Guillotine

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

    public class GuillotineAddon : BaseAddon
    {
        [Constructible]
        public GuillotineAddon()
        {
            AddComponent(new GuillotineComponent(), 0, 0, 0);
        }

        public GuillotineAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new GuillotineDeed();

        public override void OnComponentUsed(AddonComponent c, Mobile from)
        {
            if (from.InRange(Location, 2))
            {
                if (Utility.RandomBool())
                {
                    from.Location = Location;

                    Timer.StartTimer(TimeSpan.FromSeconds(0.5), () => Activate(c, from));
                }
                else
                {
                    from.LocalOverheadMessage(
                        MessageType.Regular,
                        0,
                        501777 // Hmm... you suspect that if you used this again, it might hurt.
                    );
                }
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
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
        }

        public virtual void Activate(AddonComponent c, Mobile from)
        {
            if (c.ItemID == 0x125E || c.ItemID == 0x1269 || c.ItemID == 0x1260)
            {
                c.ItemID = 0x1269;
            }
            else
            {
                c.ItemID = 0x1247;
            }

            // blood
            var amount = Utility.RandomMinMax(3, 7);

            for (var i = 0; i < amount; i++)
            {
                var x = c.X + Utility.RandomMinMax(-1, 1);
                var y = c.Y + Utility.RandomMinMax(-1, 1);
                var z = c.Z;

                if (!c.Map.CanFit(x, y, z, 1, false, false))
                {
                    z = c.Map.GetAverageZ(x, y);

                    if (!c.Map.CanFit(x, y, z, 1, false, false))
                    {
                        continue;
                    }
                }

                var blood = new Blood(Utility.RandomMinMax(0x122C, 0x122F));
                blood.MoveToWorld(new Point3D(x, y, z), c.Map);
            }

            if (from.Female)
            {
                from.PlaySound(Utility.RandomMinMax(0x150, 0x153));
            }
            else
            {
                from.PlaySound(Utility.RandomMinMax(0x15A, 0x15D));
            }

            from.LocalOverheadMessage(
                MessageType.Regular,
                0,
                501777
            ); // Hmm... you suspect that if you used this again, it might hurt.
            SpellHelper.Damage(TimeSpan.Zero, from, Utility.Dice(2, 10, 5));

            Timer.StartTimer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 2, () => Deactivate(c));
        }

        private void Deactivate(AddonComponent c)
        {
            c.ItemID = c.ItemID switch
            {
                0x1269 => 0x1260,
                0x1260 => 0x125E,
                0x1247 => 0x1246,
                0x1246 => 0x1230,
                _      => c.ItemID
            };
        }
    }

    public class GuillotineDeed : BaseAddonDeed
    {
        [Constructible]
        public GuillotineDeed() => LootType = LootType.Blessed;

        public GuillotineDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new GuillotineAddon();
        public override int LabelNumber => 1024656; // Guillotine

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
