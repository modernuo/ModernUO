using ModernUO.Serialization;
using Server.Multis;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x14F0, 0x14EF)]
    public abstract partial class BaseAddonDeed : Item
    {
        private CraftResource m_Resource;

        public BaseAddonDeed() : base(0x14F0)
        {
            Weight = 1.0;

            if (!Core.AOS)
            {
                LootType = LootType.Newbied;
            }
        }

        public abstract BaseAddon Addon { get; }

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

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        private class InternalTarget : Target
        {
            private readonly BaseAddonDeed m_Deed;

            public InternalTarget(BaseAddonDeed deed) : base(-1, true, TargetFlags.None)
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

                if (!m_Deed.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else
                {
                    var addon = m_Deed.Addon;

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
                    }
                    else
                    {
                        addon.Delete();
                    }
                }
            }
        }
    }
}
