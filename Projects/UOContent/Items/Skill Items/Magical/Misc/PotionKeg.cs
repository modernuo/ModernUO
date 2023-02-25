using System;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]
public partial class PotionKeg : Item
{
    public static void Initialize()
    {
        TileData.ItemTable[0x1940].Height = 4;
    }

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PotionEffect _type;

    [Constructible]
    public PotionKeg() : base(0x1940) => UpdateWeight();

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Held
    {
        get => _held;
        set
        {
            if (_held != value)
            {
                _held = value;
                UpdateWeight();
                InvalidateProperties();
                this.MarkDirty();
            }
        }
    }

    public override int LabelNumber
    {
        get
        {
            if (_held > 0 && (int)_type >= (int)PotionEffect.Conflagration)
            {
                return 1072658 + (int)_type - (int)PotionEffect.Conflagration;
            }

            return _held > 0 ? 1041620 + (int)_type : 1041641;
        }
    }

    public virtual void UpdateWeight()
    {
        var held = Math.Max(0, Math.Min(_held, 100));

        Weight = 20 + held * 80 / 100;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _type = (PotionEffect)reader.ReadInt();
        _held = reader.ReadInt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetKegLabel() =>
        _held switch
        {
            <= 0  => 502246, // The keg is empty.
            < 5   => 502248, // The keg is nearly empty.
            < 20  => 502249, // The keg is not very full.
            < 30  => 502250, // The keg is about one quarter full.
            < 40  => 502251, // The keg is about one third full.
            < 47  => 502252, // The keg is almost half full.
            < 54  => 502254, // The keg is approximately half full.
            < 70  => 502253, // The keg is more than half full.
            < 80  => 502255, // The keg is about three quarters full.
            < 96  => 502256, // The keg is very full.
            < 100 => 502257, // The liquid is almost to the top of the keg.
            _     => 502258  // The keg is completely full.
        };

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(GetKegLabel());
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, GetKegLabel());
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (_held <= 0)
        {
            from.SendLocalizedMessage(502246); // The keg is empty.
            return;
        }

        var pack = from.Backpack;

        if (pack?.ConsumeTotal(typeof(Bottle)) == true)
        {
            from.SendLocalizedMessage(502242); // You pour some of the keg's contents into an empty bottle...

            var pot = FillBottle();

            if (pack.TryDropItem(from, pot, false))
            {
                from.SendLocalizedMessage(502243); // ...and place it into your backpack.
                from.PlaySound(0x240);

                if (--Held == 0)
                {
                    from.SendLocalizedMessage(502245); // The keg is now empty.
                }
            }
            else
            {
                from.SendLocalizedMessage(502244); // ...but there is no room for the bottle in your backpack.
                pot.Delete();
            }
        }
    }

    public override bool OnDragDrop(Mobile from, Item item)
    {
        if (item is not BasePotion pot)
        {
            from.SendLocalizedMessage(502232); // The keg is not designed to hold that type of object.
            return false;
        }

        var toHold = Math.Min(100 - _held, pot.Amount);

        if (toHold <= 0)
        {
            from.SendLocalizedMessage(502233); // The keg will not hold any more!
            return false;
        }

        if (_held == 0)
        {
            if ((int)pot.PotionEffect >= (int)PotionEffect.Invisibility)
            {
                from.SendLocalizedMessage(502232); // The keg is not designed to hold that type of object.
                return false;
            }

            if (GiveBottle(from, toHold))
            {
                _type = pot.PotionEffect;
                Held = toHold;

                from.PlaySound(0x240);

                from.SendLocalizedMessage(502237); // You place the empty bottle in your backpack.

                pot.Consume(toHold);

                if (!pot.Deleted)
                {
                    pot.Bounce(from);
                }

                return true;
            }

            from.SendLocalizedMessage(502238); // You don't have room for the empty bottle in your backpack.
            return false;
        }

        if (pot.PotionEffect != _type)
        {
            from.SendLocalizedMessage(
                502236
            ); // You decide that it would be a bad idea to mix different types of potions.
            return false;
        }

        if (GiveBottle(from, toHold))
        {
            Held += toHold;

            from.PlaySound(0x240);

            from.SendLocalizedMessage(502237); // You place the empty bottle in your backpack.

            pot.Consume(toHold);

            if (!pot.Deleted)
            {
                pot.Bounce(from);
            }

            return true;
        }

        from.SendLocalizedMessage(502238); // You don't have room for the empty bottle in your backpack.
        return false;
    }

    public static bool GiveBottle(Mobile m, int amount)
    {
        var pack = m.Backpack;

        var bottle = new Bottle(amount);

        if (pack?.TryDropItem(m, bottle, false) != true)
        {
            bottle.Delete();
            return false;
        }

        return true;
    }

    public BasePotion FillBottle() =>
        _type switch
        {
            PotionEffect.Nightsight            => new NightSightPotion(),
            PotionEffect.CureLesser            => new LesserCurePotion(),
            PotionEffect.Cure                  => new CurePotion(),
            PotionEffect.CureGreater           => new GreaterCurePotion(),
            PotionEffect.Agility               => new AgilityPotion(),
            PotionEffect.AgilityGreater        => new GreaterAgilityPotion(),
            PotionEffect.Strength              => new StrengthPotion(),
            PotionEffect.StrengthGreater       => new GreaterStrengthPotion(),
            PotionEffect.PoisonLesser          => new LesserPoisonPotion(),
            PotionEffect.Poison                => new PoisonPotion(),
            PotionEffect.PoisonGreater         => new GreaterPoisonPotion(),
            PotionEffect.PoisonDeadly          => new DeadlyPoisonPotion(),
            PotionEffect.Refresh               => new RefreshPotion(),
            PotionEffect.RefreshTotal          => new TotalRefreshPotion(),
            PotionEffect.HealLesser            => new LesserHealPotion(),
            PotionEffect.Heal                  => new HealPotion(),
            PotionEffect.HealGreater           => new GreaterHealPotion(),
            PotionEffect.ExplosionLesser       => new LesserExplosionPotion(),
            PotionEffect.Explosion             => new ExplosionPotion(),
            PotionEffect.ExplosionGreater      => new GreaterExplosionPotion(),
            PotionEffect.Conflagration         => new ConflagrationPotion(),
            PotionEffect.ConflagrationGreater  => new GreaterConflagrationPotion(),
            PotionEffect.ConfusionBlast        => new ConfusionBlastPotion(),
            PotionEffect.ConfusionBlastGreater => new GreaterConfusionBlastPotion(),
            _                                  => new NightSightPotion()
        };
}
