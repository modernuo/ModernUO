using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Mobiles;

namespace Server.Items;

[Flippable(0x27AA, 0x27F5)]
[SerializationGenerator(0, false)]
public partial class Fukiya : Item, INinjaWeapon
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
    public Fukiya() : base(0x27AA)
    {
        Weight = 4.0;
        Layer = Layer.OneHanded;
    }

    public virtual int WrongAmmoMessage => 1063329;    // You can only load fukiya darts
    public virtual int NoFreeHandMessage => 1063327;   // You must have a free hand to use a fukiya.
    public virtual int EmptyWeaponMessage => 1063325;  // You have no fukiya darts!
    public virtual int RecentlyUsedMessage => 1063326; // You are already using that fukiya.
    public virtual int FullWeaponMessage => 1063330;   // You can only load fukiya darts

    public virtual int WeaponMinRange => 0;
    public virtual int WeaponMaxRange => 6;

    public virtual int WeaponDamage => Utility.RandomMinMax(4, 6);

    public Type AmmoType => typeof(FukiyaDarts);

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public void AttackAnimation(Mobile from, Mobile to)
    {
        if (from.Body.IsHuman && !from.Mounted)
        {
            from.Animate(33, 2, 1, true, true, 0);
        }

        from.PlaySound(0x223);
        from.MovingEffect(to, 0x2804, 5, 0, false, false);
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

    public override void OnDoubleClick(Mobile from)
    {
        NinjaWeapon.AttemptShoot((PlayerMobile)from, this);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (IsChildOf(from))
        {
            list.Add(new NinjaWeapon.LoadEntry(this, 6224));
            list.Add(new NinjaWeapon.UnloadEntry(this, 6225));
        }
    }
}
