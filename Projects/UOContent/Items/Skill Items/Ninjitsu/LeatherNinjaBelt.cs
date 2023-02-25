using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Mobiles;

namespace Server.Items;

[Flippable(0x2790, 0x27DB)]
[SerializationGenerator(0, false)]
public partial class LeatherNinjaBelt : BaseWaist, INinjaWeapon
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Poison _poison;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _poisonCharges;

    [Constructible]
    public LeatherNinjaBelt() : base(0x2790)
    {
        Weight = 1.0;
        Layer = Layer.Waist;
    }

    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public virtual int WrongAmmoMessage => 1063301;    // You can only place shuriken in a ninja belt.
    public virtual int NoFreeHandMessage => 1063299;   // You must have a free hand to throw shuriken.
    public virtual int EmptyWeaponMessage => 1063297;  // You have no shuriken in your ninja belt!
    public virtual int RecentlyUsedMessage => 1063298; // You cannot throw another shuriken yet.
    public virtual int FullWeaponMessage => 1063302;   // You cannot add any more shuriken.

    public virtual int WeaponMinRange => 2;
    public virtual int WeaponMaxRange => 10;

    public virtual int WeaponDamage => Utility.RandomMinMax(3, 5);

    public virtual Type AmmoType => typeof(Shuriken);

    public bool ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public void AttackAnimation(Mobile from, Mobile to)
    {
        if (from.Body.IsHuman)
        {
            from.Animate(from.Mounted ? 26 : 9, 7, 1, true, false, 0);
        }

        from.PlaySound(0x23A);
        from.MovingEffect(to, 0x27AC, 1, 0, false, false);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~

        if (_poison != null && _poisonCharges > 0)
        {
            list.Add(1062412 + _poison.Level, _poisonCharges);
        }
    }

    public override bool OnEquip(Mobile from)
    {
        if (base.OnEquip(from))
        {
            from.SendLocalizedMessage(1070785); // Double click this item each time you wish to throw a shuriken.
            return true;
        }

        return false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        NinjaWeapon.AttemptShoot((PlayerMobile)from, this);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (IsChildOf(from))
        {
            list.Add(new NinjaWeapon.LoadEntry(this, 6222));
            list.Add(new NinjaWeapon.UnloadEntry(this, 6223));
        }
    }
}
