using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Systems.FeatureFlags;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(0, false)]
public abstract partial class SmallBOD : BaseBOD
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _amountCur;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Type _type;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _number;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _graphic;

    public SmallBOD(
        int hue, int amountCur, int amountMax, Type type, int number, int graphic, bool requireExeptional,
        BulkMaterialType material
    ) : base(hue, amountMax, requireExeptional, material)
    {
        _type = type;
        _graphic = graphic;
        _amountCur = amountCur;
        _number = number;
    }

    public SmallBOD()
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool Complete => _amountCur == AmountMax;

    public override int LabelNumber => 1045151; // a bulk order deed

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060654); // small bulk order

        if (RequireExceptional)
        {
            list.Add(1045141); // All items must be exceptional.
        }

        if (Material != BulkMaterialType.None)
        {
            list.Add(SmallBODGump.GetMaterialNumberFor(Material)); // All items must be made with x material.
        }

        list.Add(1060656, AmountMax);                      // amount to make: ~1_val~
        list.Add(1060658, $"{_number:#}\t{_amountCur}"); // ~1_val~: ~2_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!(IsChildOf(from.Backpack) || InSecureTrade || RootParent is PlayerVendor))
        {
            from.SendLocalizedMessage(1045156); // You must have the deed in your backpack to use it.
            return;
        }

        if (!ContentFeatureFlags.BulkOrders && from.AccessLevel < AccessLevel.Administrator)
        {
            from.SendMessage(0x22, "Bulk orders are temporarily disabled.");
            return;
        }

        from.SendGump(new SmallBODGump(this));
    }

    public override void OnDoubleClickNotAccessible(Mobile from)
    {
        OnDoubleClick(from);
    }

    public override void OnDoubleClickSecureTrade(Mobile from)
    {
        OnDoubleClick(from);
    }

    public static BulkMaterialType GetMaterial(CraftResource resource)
    {
        return resource switch
        {
            CraftResource.DullCopper    => BulkMaterialType.DullCopper,
            CraftResource.ShadowIron    => BulkMaterialType.ShadowIron,
            CraftResource.Copper        => BulkMaterialType.Copper,
            CraftResource.Bronze        => BulkMaterialType.Bronze,
            CraftResource.Gold          => BulkMaterialType.Gold,
            CraftResource.Agapite       => BulkMaterialType.Agapite,
            CraftResource.Verite        => BulkMaterialType.Verite,
            CraftResource.Valorite      => BulkMaterialType.Valorite,
            CraftResource.SpinedLeather => BulkMaterialType.Spined,
            CraftResource.HornedLeather => BulkMaterialType.Horned,
            CraftResource.BarbedLeather => BulkMaterialType.Barbed,
            _                           => BulkMaterialType.None
        };
    }

    public override void EndCombine(Mobile from, Item item)
    {
        var objectType = item.GetType();

        if (_amountCur >= AmountMax)
        {
            // The maximum amount of requested items have already been combined to this deed.
            from.SendLocalizedMessage(1045166);
            return;
        }
        var armor = item as BaseArmor;
        var clothing = item as BaseClothing;
        var weapon = item as BaseWeapon;


        if (Type == null || objectType != Type && !objectType.IsSubclassOf(Type) || armor == null && clothing == null && weapon == null)
        {
            from.SendLocalizedMessage(1045169); // The item is not in the request.
        }
        else
        {
            var material = GetMaterial(armor?.Resource ?? clothing?.Resource ?? CraftResource.None);

            if (Material >= BulkMaterialType.DullCopper && Material <= BulkMaterialType.Valorite && material != Material)
            {
                from.SendLocalizedMessage(1045168); // The item is not made from the requested ore.
            }
            else if (Material >= BulkMaterialType.Spined && Material <= BulkMaterialType.Barbed && material != Material)
            {
                from.SendLocalizedMessage(1049352); // The item is not made from the requested leather type.
            }
            else if (RequireExceptional && armor?.Quality != ArmorQuality.Exceptional &&
                     clothing?.Quality != ClothingQuality.Exceptional &&
                     weapon?.Quality != WeaponQuality.Exceptional)
            {
                from.SendLocalizedMessage(1045167); // The item must be exceptional.
            }
            else
            {
                item.Delete();
                ++AmountCur;

                from.SendLocalizedMessage(1045170); // The item has been combined with the deed.
                from.SendGump(new SmallBODGump(this));

                if (_amountCur < AmountMax)
                {
                    BeginCombine(from);
                }
            }
        }
    }
}
