using Server.Engines.Quests.Haven;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class CannonAddonComponent : AddonComponent
    {
        public CannonAddonComponent(int itemID) : base(itemID) => LootType = LootType.Blessed;

        public CannonAddonComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1076157; // Decorative Cannon

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Addon is CannonAddon addon)
            {
                if (addon.IsRewardItem)
                {
                    list.Add(1076223); // 7th Year Veteran Reward
                }

                list.Add(1076207, addon.Charges); // Remaining Charges: ~1_val~
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class CannonAddon : BaseAddon
    {
        private static readonly int[] m_Effects =
        {
            0x36B0, 0x3728, 0x3709, 0x36FE
        };

        private int m_Charges;
        private bool m_IsRewardItem;

        [Constructible]
        public CannonAddon(CannonDirection direction)
        {
            CannonDirection = direction;

            switch (direction)
            {
                case CannonDirection.North:
                    {
                        AddComponent(new CannonAddonComponent(0xE8D), 0, 0, 0);
                        AddComponent(new CannonAddonComponent(0xE8C), 0, 1, 0);
                        AddComponent(new CannonAddonComponent(0xE8B), 0, 2, 0);

                        break;
                    }
                case CannonDirection.East:
                    {
                        AddComponent(new CannonAddonComponent(0xE96), 0, 0, 0);
                        AddComponent(new CannonAddonComponent(0xE95), -1, 0, 0);
                        AddComponent(new CannonAddonComponent(0xE94), -2, 0, 0);

                        break;
                    }
                case CannonDirection.South:
                    {
                        AddComponent(new CannonAddonComponent(0xE91), 0, 0, 0);
                        AddComponent(new CannonAddonComponent(0xE92), 0, -1, 0);
                        AddComponent(new CannonAddonComponent(0xE93), 0, -2, 0);

                        break;
                    }
                default:
                    {
                        AddComponent(new CannonAddonComponent(0xE8E), 0, 0, 0);
                        AddComponent(new CannonAddonComponent(0xE8F), 1, 0, 0);
                        AddComponent(new CannonAddonComponent(0xE90), 2, 0, 0);

                        break;
                    }
            }
        }

        public CannonAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                var deed = new CannonDeed();
                deed.Charges = m_Charges;
                deed.IsRewardItem = m_IsRewardItem;

                return deed;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CannonDirection CannonDirection { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = value;

                foreach (var c in Components)
                {
                    c.InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;

                foreach (var c in Components)
                {
                    c.InvalidateProperties();
                }
            }
        }

        public override void OnComponentUsed(AddonComponent c, Mobile from)
        {
            if (from.InRange(Location, 2))
            {
                if (m_Charges > 0)
                {
                    from.Target = new InternalTarget(this);
                }
                else
                {
                    if (from.Backpack != null)
                    {
                        var keg = from.Backpack.FindItemByType<PotionKeg>();

                        if (Validate(keg) > 0)
                        {
                            from.SendGump(new InternalGump(this, keg));
                        }
                        else
                        {
                            from.SendLocalizedMessage(
                                1076198
                            ); // You do not have a full keg of explosion potions needed to recharge the cannon.
                        }
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1076766); // That is too far away.
            }
        }

        public int Validate(PotionKeg keg)
        {
            if (keg?.Deleted != false || keg.Held != 100)
            {
                return 0;
            }

            return keg.Type switch
            {
                PotionEffect.ExplosionLesser  => 5,
                PotionEffect.Explosion        => 10,
                PotionEffect.ExplosionGreater => 15,
                _                             => 0
            };
        }

        public void Fill(Mobile from, PotionKeg keg)
        {
            Charges = Validate(keg);

            if (Charges > 0)
            {
                keg.Delete();
                from.SendLocalizedMessage(1076199); // Your cannon is recharged.
            }
            else
            {
                from.SendLocalizedMessage(
                    1076198
                ); // You do not have a full keg of explosion potions needed to recharge the cannon.
            }
        }

        public void DoFireEffect(Point3D target)
        {
            var map = Map;

            if (map == null)
            {
                return;
            }

            Effects.PlaySound(target, map, Utility.RandomList(0x11B, 0x11C, 0x11D));
            Effects.SendLocationEffect(target, map, m_Effects.RandomElement(), 16, 1);

            for (var count = Utility.Random(3); count > 0; count--)
            {
                Point3D location = new Point3D(
                    target.X + Utility.RandomMinMax(-1, 1),
                    target.Y + Utility.RandomMinMax(-1, 1),
                    target.Z
                );
                Effects.SendLocationEffect(location, map, m_Effects.RandomElement(), 16, 1);
            }

            Charges -= 1;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write((int)CannonDirection);
            writer.Write(m_Charges);
            writer.Write(m_IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            CannonDirection = (CannonDirection)reader.ReadInt();
            m_Charges = reader.ReadInt();
            m_IsRewardItem = reader.ReadBool();
        }

        private class InternalTarget : Target
        {
            private readonly CannonAddon m_Cannon;

            public InternalTarget(CannonAddon cannon) : base(12, true, TargetFlags.None) => m_Cannon = cannon;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Cannon?.Deleted != false)
                {
                    return;
                }

                if (targeted is not IPoint3D p)
                {
                    return;
                }

                if (from.InLOS(new Point3D(p)))
                {
                    if (!Utility.InRange(new Point3D(p), m_Cannon.Location, 2))
                    {
                        var allow = false;

                        var x = p.X - m_Cannon.X;
                        var y = p.Y - m_Cannon.Y;

                        switch (m_Cannon.CannonDirection)
                        {
                            case CannonDirection.North:
                                if (y < 0 && x.Abs() <= -y / 3)
                                {
                                    allow = true;
                                }

                                break;
                            case CannonDirection.East:
                                if (x > 0 && y.Abs() <= x / 3)
                                {
                                    allow = true;
                                }

                                break;
                            case CannonDirection.South:
                                if (y > 0 && x.Abs() <= y / 3)
                                {
                                    allow = true;
                                }

                                break;
                            case CannonDirection.West:
                                if (x < 0 && y.Abs() <= -x / 3)
                                {
                                    allow = true;
                                }

                                break;
                        }

                        var loc = new Point3D(p);

                        if (allow && Utility.InRange(loc, m_Cannon.Location, 14))
                        {
                            m_Cannon.DoFireEffect(loc);
                        }
                        else
                        {
                            from.SendLocalizedMessage(1076203); // Target out of range.
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(1076215); // Cannon must be aimed farther away.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1049630); // You cannot see that target!
                }
            }

            protected override void OnTargetOutOfRange(Mobile from, object targeted)
            {
                from.SendLocalizedMessage(1076203); // Target out of range.
            }
        }

        private class InternalGump : Gump
        {
            private readonly CannonAddon m_Cannon;
            private readonly PotionKeg m_Keg;

            public InternalGump(CannonAddon cannon, PotionKeg keg) : base(50, 50)
            {
                m_Cannon = cannon;
                m_Keg = keg;

                Closable = true;
                Disposable = true;
                Draggable = true;
                Resizable = false;

                AddPage(0);

                AddBackground(0, 0, 291, 133, 0x13BE);
                AddImageTiled(5, 5, 280, 100, 0xA40);

                AddHtmlLocalized(
                    9,
                    9,
                    272,
                    100,
                    1076196,
                    cannon.Validate(keg).ToString(),
                    0x7FFF
                ); // You will need a full keg of explosion potions to recharge the cannon.  Your keg will provide ~1_CHARGES~ charges.

                AddButton(5, 107, 0xFB1, 0xFB2, (int)Buttons.Cancel);
                AddHtmlLocalized(40, 109, 100, 20, 1060051, 0x7FFF); // CANCEL

                AddButton(160, 107, 0xFB7, 0xFB8, (int)Buttons.Recharge);
                AddHtmlLocalized(195, 109, 120, 20, 1076197, 0x7FFF); // Recharge
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (m_Cannon?.Deleted == false && info.ButtonID == (int)Buttons.Recharge)
                {
                    m_Cannon.Fill(state.Mobile, m_Keg);
                }
            }

            private enum Buttons
            {
                Cancel,
                Recharge
            }
        }
    }

    public class CannonDeed : BaseAddonDeed, IRewardItem, IRewardOption
    {
        private int m_Charges;

        private CannonDirection m_Direction;
        private bool m_IsRewardItem;

        [Constructible]
        public CannonDeed() => LootType = LootType.Blessed;

        public CannonDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1076195; // A deed for a cannon

        public override BaseAddon Addon => new CannonAddon(m_Direction)
        {
            Charges = m_Charges,
            IsRewardItem = m_IsRewardItem
        };

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;
                InvalidateProperties();
            }
        }

        public void GetOptions(RewardOptionList list)
        {
            list.Add((int)CannonDirection.South, 1075386); // South
            list.Add((int)CannonDirection.East, 1075387);  // East
            list.Add((int)CannonDirection.North, 1075389); // North
            list.Add((int)CannonDirection.West, 1075390);  // West
        }

        public void OnOptionSelected(Mobile from, int option)
        {
            m_Direction = (CannonDirection)option;

            if (!Deleted)
            {
                base.OnDoubleClick(from);
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsRewardItem)
            {
                list.Add(1076223); // 7th Year Veteran Reward
            }

            list.Add(1076207, m_Charges); // Remaining Charges: ~1_val~
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
            {
                return;
            }

            if (IsChildOf(from.Backpack))
            {
                from.CloseGump<RewardOptionGump>();
                from.SendGump(new RewardOptionGump(this));
            }
            else
            {
                from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Charges);
            writer.Write(m_IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Charges = reader.ReadInt();
            m_IsRewardItem = reader.ReadBool();
        }
    }
}
