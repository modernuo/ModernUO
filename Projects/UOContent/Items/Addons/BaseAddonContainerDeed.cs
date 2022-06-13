using System;
using Server.Engines.Craft;
using Server.Multis;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    [Flippable(0x14F0, 0x14EF)]
    public abstract class BaseAddonContainerDeed : Item, ICraftable
    {
        private CraftResource m_Resource;

        public BaseAddonContainerDeed() : base(0x14F0)
        {
            Weight = 1.0;

            if (!Core.AOS)
            {
                LootType = LootType.Newbied;
            }
        }

        public BaseAddonContainerDeed(Serial serial) : base(serial)
        {
        }

        public abstract BaseAddonContainer Addon { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => m_Resource;
            set
            {
                if (m_Resource != value)
                {
                    m_Resource = value;
                    Hue = CraftResources.GetHue(m_Resource);

                    InvalidateProperties();
                }
            }
        }

        public virtual int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            BaseTool tool, CraftItem craftItem, int resHue
        )
        {
            var resourceType = typeRes ?? craftItem.Resources[0].ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            var context = craftSystem.GetContext(from);

            if (context?.DoNotColor == true)
            {
                Hue = 0;
            }

            return quality;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            // version 1
            writer.Write((int)m_Resource);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Resource = version switch
            {
                1 => (CraftResource)reader.ReadInt(),
                _ => m_Resource
            };
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (!CraftResources.IsStandard(m_Resource))
            {
                list.Add(CraftResources.GetLocalizationNumber(m_Resource));
            }
        }

        private class InternalTarget : Target
        {
            private readonly BaseAddonContainerDeed m_Deed;

            public InternalTarget(BaseAddonContainerDeed deed) : base(-1, true, TargetFlags.None)
            {
                m_Deed = deed;

                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                var p = targeted as IPoint3D;
                var map = from.Map;

                if (p == null || map == null || m_Deed.Deleted)
                {
                    return;
                }

                if (m_Deed.IsChildOf(from.Backpack))
                {
                    var addon = m_Deed.Addon;
                    addon.Resource = m_Deed.Resource;

                    SpellHelper.GetSurfaceTop(ref p);

                    BaseHouse house = null;

                    var res = addon.CouldFit(p, map, from, ref house);

                    if (res == AddonFitResult.Valid)
                    {
                        addon.MoveToWorld(new Point3D(p), map);
                    }
                    else if (res == AddonFitResult.Blocked)
                    {
                        from.SendLocalizedMessage(500269); // You cannot build that there.
                    }
                    else if (res == AddonFitResult.NotInHouse)
                    {
                        from.SendLocalizedMessage(500274); // You can only place this in a house that you own!
                    }
                    else if (res == AddonFitResult.DoorsNotClosed)
                    {
                        from.SendMessage("You must close all house doors before placing this.");
                    }
                    else if (res == AddonFitResult.DoorTooClose)
                    {
                        from.SendLocalizedMessage(500271); // You cannot build near the door.
                    }
                    else if (res == AddonFitResult.NoWall)
                    {
                        from.SendLocalizedMessage(500268); // This object needs to be mounted on something.
                    }

                    if (res == AddonFitResult.Valid)
                    {
                        m_Deed.Delete();
                        house.Addons.Add(addon);
                        house.AddSecure(from, addon);
                    }
                    else
                    {
                        addon.Delete();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
            }
        }
    }
}
