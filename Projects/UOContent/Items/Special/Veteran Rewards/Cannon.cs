using ModernUO.Serialization;
using Server.Engines.Quests.Haven;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CannonAddonComponent : AddonComponent
{
    public CannonAddonComponent(int itemID) : base(itemID) => LootType = LootType.Blessed;

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
}

[SerializationGenerator(1)]
public partial class CannonAddon : BaseAddon
{
    private static readonly int[] m_Effects = { 0x36B0, 0x3728, 0x3709, 0x36FE };

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CannonDirection _cannonDirection;

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

    public override BaseAddonDeed Deed =>
        new CannonDeed
        {
            Charges = _charges,
            IsRewardItem = _isRewardItem
        };

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = value;

            foreach (var c in Components)
            {
                c.InvalidateProperties();
            }

            this.MarkDirty();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem
    {
        get => _isRewardItem;
        set
        {
            _isRewardItem = value;

            foreach (var c in Components)
            {
                c.InvalidateProperties();
            }

            this.MarkDirty();
        }
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (!from.InRange(Location, 2))
        {
            from.SendLocalizedMessage(1076766); // That is too far away.
            return;
        }

        if (_charges > 0)
        {
            from.Target = new InternalTarget(this);
        }
        else if (from.Backpack != null)
        {
            var keg = from.Backpack.FindItemByType<PotionKeg>();

            if (Validate(keg) > 0)
            {
                from.SendGump(new InternalGump(this, keg));
            }
            else
            {
                // You do not have a full keg of explosion potions needed to recharge the cannon.
                from.SendLocalizedMessage(1076198);
            }
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

        if (_charges > 0)
        {
            keg.Delete();
            from.SendLocalizedMessage(1076199); // Your cannon is recharged.
        }
        else
        {
            // You do not have a full keg of explosion potions needed to recharge the cannon.
            from.SendLocalizedMessage(1076198);
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

    private void Deserialize(IGenericReader reader, int version)
    {
        _cannonDirection = (CannonDirection)reader.ReadInt();
        _charges = reader.ReadInt();
        _isRewardItem = reader.ReadBool();
    }

    private class InternalTarget : Target
    {
        private readonly CannonAddon _cannon;

        public InternalTarget(CannonAddon cannon) : base(12, true, TargetFlags.None) => _cannon = cannon;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_cannon?.Deleted != false)
            {
                return;
            }

            if (targeted is not IPoint3D p)
            {
                return;
            }

            if (!from.InLOS(new Point3D(p)))
            {
                from.SendLocalizedMessage(1049630); // You cannot see that target!
                return;
            }

            if (Utility.InRange(new Point3D(p), _cannon.Location, 2))
            {
                from.SendLocalizedMessage(1076215); // Cannon must be aimed farther away.
                return;
            }

            var allow = false;

            var x = p.X - _cannon.X;
            var y = p.Y - _cannon.Y;

            switch (_cannon.CannonDirection)
            {
                case CannonDirection.North:
                    {
                        if (y < 0 && x.Abs() <= -y / 3)
                        {
                            allow = true;
                        }

                        break;
                    }
                case CannonDirection.East:
                    {
                        if (x > 0 && y.Abs() <= x / 3)
                        {
                            allow = true;
                        }

                        break;
                    }
                case CannonDirection.South:
                    {
                        if (y > 0 && x.Abs() <= y / 3)
                        {
                            allow = true;
                        }

                        break;
                    }
                case CannonDirection.West:
                    {
                        if (x < 0 && y.Abs() <= -x / 3)
                        {
                            allow = true;
                        }

                        break;
                    }
            }

            var loc = new Point3D(p);

            if (allow && Utility.InRange(loc, _cannon.Location, 14))
            {
                _cannon.DoFireEffect(loc);
            }
            else
            {
                from.SendLocalizedMessage(1076203); // Target out of range.
            }
        }

        protected override void OnTargetOutOfRange(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(1076203); // Target out of range.
        }
    }

    private class InternalGump : Gump
    {
        private readonly CannonAddon _cannon;
        private readonly PotionKeg _keg;

        public InternalGump(CannonAddon cannon, PotionKeg keg) : base(50, 50)
        {
            _cannon = cannon;
            _keg = keg;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(0, 0, 291, 133, 0x13BE);
            AddImageTiled(5, 5, 280, 100, 0xA40);

            // You will need a full keg of explosion potions to recharge the cannon.  Your keg will provide ~1_CHARGES~ charges.
            AddHtmlLocalized(9, 9, 272, 100, 1076196, cannon.Validate(keg).ToString(), 0x7FFF);

            AddButton(5, 107, 0xFB1, 0xFB2, (int)Buttons.Cancel);
            AddHtmlLocalized(40, 109, 100, 20, 1060051, 0x7FFF); // CANCEL

            AddButton(160, 107, 0xFB7, 0xFB8, (int)Buttons.Recharge);
            AddHtmlLocalized(195, 109, 120, 20, 1076197, 0x7FFF); // Recharge
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (_cannon?.Deleted == false && info.ButtonID == (int)Buttons.Recharge)
            {
                _cannon.Fill(state.Mobile, _keg);
            }
        }

        private enum Buttons
        {
            Cancel,
            Recharge
        }
    }
}

[SerializationGenerator(0)]
public partial class CannonDeed : BaseAddonDeed, IRewardItem, IRewardOption
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    private CannonDirection _direction;

    [Constructible]
    public CannonDeed() => LootType = LootType.Blessed;

    public override int LabelNumber => 1076195; // A deed for a cannon

    public override BaseAddon Addon => new CannonAddon(_direction)
    {
        Charges = _charges,
        IsRewardItem = _isRewardItem
    };

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)CannonDirection.South, 1075386); // South
        list.Add((int)CannonDirection.East, 1075387);  // East
        list.Add((int)CannonDirection.North, 1075389); // North
        list.Add((int)CannonDirection.West, 1075390);  // West
    }

    public void OnOptionSelected(Mobile from, int option)
    {
        _direction = (CannonDirection)option;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076223); // 7th Year Veteran Reward
        }

        list.Add(1076207, _charges); // Remaining Charges: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
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
}
