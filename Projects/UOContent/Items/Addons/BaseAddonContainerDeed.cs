using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Multis;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

[Flippable(0x14F0, 0x14EF)]
[SerializationGenerator(2)]
public abstract partial class BaseAddonContainerDeed : Item, ICraftable
{
    public BaseAddonContainerDeed() : base(0x14F0)
    {
        if (!Core.AOS)
        {
            LootType = LootType.Newbied;
        }
    }

    public override double DefaultWeight => 1.0;

    public abstract BaseAddonContainer Addon { get; }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
        get => _resource;
        set
        {
            if (_resource != value)
            {
                _resource = value;
                Hue = CraftResources.GetHue(_resource);

                InvalidateProperties();
                this.MarkDirty();
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

    private void Deserialize(IGenericReader reader, int version)
    {
        _resource = (CraftResource)reader.ReadInt();
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

        if (!CraftResources.IsStandard(_resource))
        {
            list.Add(CraftResources.GetLocalizationNumber(_resource));
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

            if (!m_Deed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

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
    }
}
