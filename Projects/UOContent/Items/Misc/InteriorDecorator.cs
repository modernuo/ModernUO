using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public enum DecorateCommand
    {
        None,
        Turn,
        Up,
        Down
    }

    public class InteriorDecorator : Item
    {
        private DecorateCommand m_Command;

        [Constructible]
        public InteriorDecorator() : base(0xFC1)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public InteriorDecorator(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DecorateCommand Command
        {
            get => m_Command;
            set
            {
                m_Command = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1041280; // an interior decorator

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Command != DecorateCommand.None)
            {
                list.Add(1018322 + (int)m_Command); // Turn/Up/Down
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

        public override void OnDoubleClick(Mobile from)
        {
            if (!CheckUse(this, from))
            {
                return;
            }

            if (!from.HasGump<InternalGump>())
            {
                from.SendGump(new InternalGump(this));
            }

            if (m_Command != DecorateCommand.None)
            {
                from.Target = new InternalTarget(this);
            }
        }

        public static bool InHouse(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(from);

            return house?.IsCoOwner(from) == true;
        }

        public static bool CheckUse(InteriorDecorator tool, Mobile from)
        {
            /*if (tool.Deleted || !tool.IsChildOf( from.Backpack ))
              from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
            else*/
            if (!InHouse(from))
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
            }
            else
            {
                return true;
            }

            return false;
        }

        private class InternalGump : Gump
        {
            private readonly InteriorDecorator m_Decorator;

            public InternalGump(InteriorDecorator decorator) : base(150, 50)
            {
                m_Decorator = decorator;

                AddBackground(0, 0, 200, 200, 2600);

                AddButton(50, 45, decorator.Command == DecorateCommand.Turn ? 2154 : 2152, 2154, 1);
                AddHtmlLocalized(90, 50, 70, 40, 1018323); // Turn

                AddButton(50, 95, decorator.Command == DecorateCommand.Up ? 2154 : 2152, 2154, 2);
                AddHtmlLocalized(90, 100, 70, 40, 1018324); // Up

                AddButton(50, 145, decorator.Command == DecorateCommand.Down ? 2154 : 2152, 2154, 3);
                AddHtmlLocalized(90, 150, 70, 40, 1018325); // Down
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                var command = info.ButtonID switch
                {
                    1 => DecorateCommand.Turn,
                    2 => DecorateCommand.Up,
                    3 => DecorateCommand.Down,
                    _ => DecorateCommand.None
                };

                if (command != DecorateCommand.None)
                {
                    m_Decorator.Command = command;
                    sender.Mobile.SendGump(new InternalGump(m_Decorator));
                    sender.Mobile.Target = new InternalTarget(m_Decorator);
                }
                else
                {
                    Target.Cancel(sender.Mobile);
                }
            }
        }

        private class InternalTarget : Target
        {
            private readonly InteriorDecorator m_Decorator;

            public InternalTarget(InteriorDecorator decorator) : base(-1, false, TargetFlags.None)
            {
                CheckLOS = false;

                m_Decorator = decorator;
            }

            protected override void OnTargetNotAccessible(Mobile from, object targeted)
            {
                OnTarget(from, targeted);
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item item && CheckUse(m_Decorator, from))
                {
                    var house = BaseHouse.FindHouseAt(from);

                    var isDecorableComponent = false;
                    object addon = null;
                    var count = 0;

                    if (item is AddonComponent component)
                    {
                        count = component.Addon.Components.Count;
                        addon = component.Addon;
                    }
                    else if (item is AddonContainerComponent containerComponent)
                    {
                        count = containerComponent.Addon.Components.Count;
                        addon = containerComponent.Addon;
                    }
                    else if (item is BaseAddonContainer container)
                    {
                        count = container.Components.Count;
                        addon = container;
                    }

                    if (addon != null)
                    {
                        if (count == 1 && Core.SE)
                        {
                            isDecorableComponent = true;
                        }

                        if (m_Decorator.Command == DecorateCommand.Turn)
                        {
                            var attributes =
                                (FlippableAddonAttribute[])addon.GetType()
                                    .GetCustomAttributes(typeof(FlippableAddonAttribute), false);

                            if (attributes.Length > 0)
                            {
                                isDecorableComponent = true;
                            }
                        }
                    }

                    if (house?.IsCoOwner(from) != true)
                    {
                        from.SendLocalizedMessage(502092); // You must be in your house to do this.
                    }
                    else if (item.Parent != null || !house.IsInside(item))
                    {
                        from.SendLocalizedMessage(1042270); // That is not in your house.
                    }
                    else if (!house.HasLockedDownItem(item) && !house.HasSecureItem(item) && !isDecorableComponent)
                    {
                        if (item is AddonComponent && m_Decorator.Command == DecorateCommand.Up)
                        {
                            from.SendLocalizedMessage(1042274); // You cannot raise it up any higher.
                        }
                        else if (item is AddonComponent && m_Decorator.Command == DecorateCommand.Down)
                        {
                            from.SendLocalizedMessage(1042275); // You cannot lower it down any further.
                        }
                        else
                        {
                            from.SendLocalizedMessage(1042271); // That is not locked down.
                        }
                    }
                    else if (item is VendorRentalContract)
                    {
                        from.SendLocalizedMessage(1062491); // You cannot use the house decorator on that object.
                    }
                    else if (item.TotalWeight + item.PileWeight > 100)
                    {
                        from.SendLocalizedMessage(1042272); // That is too heavy.
                    }
                    else
                    {
                        switch (m_Decorator.Command)
                        {
                            case DecorateCommand.Up:
                                Up(item, from);
                                break;
                            case DecorateCommand.Down:
                                Down(item, from);
                                break;
                            case DecorateCommand.Turn:
                                Turn(item, from);
                                break;
                        }
                    }
                }

                from.Target = new InternalTarget(m_Decorator);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Canceled)
                {
                    from.CloseGump<InternalGump>();
                }
            }

            private static void Turn(Item item, Mobile from)
            {
                object addon = null;

                if (item is AddonComponent component)
                {
                    addon = component.Addon;
                }
                else if (item is AddonContainerComponent containerComponent)
                {
                    addon = containerComponent.Addon;
                }
                else if (item is BaseAddonContainer container)
                {
                    addon = container;
                }

                if (addon != null)
                {
                    var aAttributes =
                        (FlippableAddonAttribute[])addon.GetType()
                            .GetCustomAttributes(typeof(FlippableAddonAttribute), false);

                    if (aAttributes.Length > 0)
                    {
                        aAttributes[0].Flip(from, (Item)addon);
                        return;
                    }
                }

                var attributes =
                    (FlippableAttribute[])item.GetType().GetCustomAttributes(typeof(FlippableAttribute), false);

                if (attributes.Length > 0)
                {
                    attributes[0].Flip(item);
                }
                else
                {
                    from.SendLocalizedMessage(1042273); // You cannot turn that.
                }
            }

            private static void Up(Item item, Mobile from)
            {
                var floorZ = GetFloorZ(item);

                if (floorZ > int.MinValue && item.Z < floorZ + 15) // Confirmed : no height checks here
                {
                    item.Location = new Point3D(item.Location.X, item.Location.Y, item.Z + 1);
                }
                else
                {
                    from.SendLocalizedMessage(1042274); // You cannot raise it up any higher.
                }
            }

            private static void Down(Item item, Mobile from)
            {
                var floorZ = GetFloorZ(item);

                if (floorZ > int.MinValue && item.Z > GetFloorZ(item))
                {
                    item.Location = new Point3D(item.Location.X, item.Location.Y, item.Z - 1);
                }
                else
                {
                    from.SendLocalizedMessage(1042275); // You cannot lower it down any further.
                }
            }

            private static int GetFloorZ(Item item)
            {
                var map = item.Map;

                if (map == null)
                {
                    return int.MinValue;
                }

                var tiles = map.Tiles.GetStaticTiles(item.X, item.Y, true);

                var z = int.MinValue;

                for (var i = 0; i < tiles.Length; ++i)
                {
                    var tile = tiles[i];
                    var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                    var top = tile.Z; // Confirmed : no height checks here

                    if (id.Surface && !id.Impassable && top > z && top <= item.Z)
                    {
                        z = top;
                    }
                }

                return z;
            }
        }
    }
}
