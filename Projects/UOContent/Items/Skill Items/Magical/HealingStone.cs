using System;
using ModernUO.Serialization;
using Server.Engines.ConPVP;
using Server.Spells;
using Server.Spells.Mysticism;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HealingStone : Item
{
    public const int ItemID = 0x4078;
    public const int FullRechargeSeconds = 15;
    public const int UseCooldownSeconds = 2;

    [SerializableField(0)]
    private Mobile _ownerMobile;

    [SerializableField(1)]
    [InvalidateProperties]
    private int _storedLifeForce;

    [SerializableField(2)]
    private int _maximumLifeForce;

    [SerializableField(3)]
    [InvalidateProperties]
    private int _healingAvailable;

    [SerializableField(4)]
    private int _maximumHealing;

    [SerializableField(5)]
    private DateTime _rechargeStarted;

    [Constructible]
    public HealingStone(Mobile owner, int lifeForce, int maxHealing) : base(ItemID)
    {
        _ownerMobile = owner;
        _maximumLifeForce = Math.Max(0, lifeForce);
        _storedLifeForce = _maximumLifeForce;
        _maximumHealing = Math.Max(1, maxHealing);
        _healingAvailable = _maximumHealing;
        _rechargeStarted = Core.Now;

        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;

    public override string DefaultName => "a healing stone";

    public override bool Nontransferable => true;

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Owner => _ownerMobile;

    [CommandProperty(AccessLevel.GameMaster)]
    public int LifeForce
    {
        get => _storedLifeForce;
        set
        {
            _storedLifeForce = Math.Clamp(value, 0, _maximumLifeForce);
            InvalidateProperties();

            if (_storedLifeForce <= 0)
            {
                Delete();
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxLifeForce => _maximumLifeForce;

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxHealing => _maximumHealing;

    [CommandProperty(AccessLevel.GameMaster)]
    public int AvailableHealing
    {
        get
        {
            Replenish();
            return _healingAvailable;
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1115274, _storedLifeForce);
    }

    public override void OnDoubleClick(Mobile from) => TryUse(from);

    internal bool TryUseForTests(Mobile from) => TryUse(from);

    public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) => false;

    public override bool DropToWorld(Mobile from, Point3D p)
    {
        Delete();
        return false;
    }

    public override void OnAfterDelete()
    {
        _ownerMobile = null;
        base.OnAfterDelete();
    }

    public static int GetCureCost(int poisonLevel) => Math.Min(120, Math.Max(0, poisonLevel * 25));

    public static double GetCureChance(double mysticism, double supportSkill, int poisonLevel)
    {
        var effectiveSkill = (mysticism + supportSkill) / 2.0;
        return (10000 + effectiveSkill * 75 - (poisonLevel + 1) * 2000) / 100.0;
    }

    internal static void OnPotionHealed(Mobile from)
    {
        var stone = from?.Backpack?.FindItemByType<HealingStone>();
        stone?.ResetHealingAfterPotion();
    }

    private bool TryUse(Mobile from)
    {
        if (Deleted || from == null || from.Deleted)
        {
            return false;
        }

        if (from != _ownerMobile)
        {
            from.SendMessage("Only the mystic who created this stone can use it.");
            return false;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(502138); // That is too far away for you to use.
            return false;
        }

        if (!BasePotion.HasFreeHand(from))
        {
            from.SendLocalizedMessage(1080116); // You must have a free hand to use a Healing Stone.
            return false;
        }

        if (!from.Poisoned && from.Hits >= from.HitsMax)
        {
            from.SendLocalizedMessage(1049547); // You are already at full health.
            return false;
        }

        if (MortalStrike.IsWounded(from))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x22, 1005000); // You can not heal yourself in your current state.
            return false;
        }

        if (!from.CanBeginAction<HealingStone>())
        {
            from.SendLocalizedMessage(1095172); // You must wait a few seconds before using another Healing Stone.
            return false;
        }

        from.BeginAction<HealingStone>();
        Timer.StartTimer(TimeSpan.FromSeconds(UseCooldownSeconds), from.EndAction<HealingStone>);

        if (from.Poisoned)
        {
            TryCure(from);
            return true;
        }

        Replenish();

        var toHeal = Math.Min(_healingAvailable, Math.Min(_storedLifeForce, from.HitsMax - from.Hits));
        if (toHeal <= 0)
        {
            from.SendLocalizedMessage(1115264); // Your healing stone does not have enough energy to remove the poison.
            return true;
        }

        var oldHits = from.Hits;
        SpellHelper.Heal(toHeal, from, _ownerMobile);
        var healed = Math.Max(0, from.Hits - oldHits);

        _storedLifeForce -= healed;
        _healingAvailable = Math.Min(1, _maximumHealing);
        _rechargeStarted = Core.Now;
        InvalidateProperties();

        from.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
        from.PlaySound(0x202);

        if (_storedLifeForce <= 0)
        {
            from.SendLocalizedMessage(1115266); // The healing stone has used up all its energy and has been destroyed.
            Delete();
        }

        return true;
    }

    private void TryCure(Mobile from)
    {
        var poisonLevel = Poison.IncreaseLevel(from.Poison).Level;
        var cost = GetCureCost(poisonLevel);

        if (_maximumLifeForce < cost)
        {
            from.SendLocalizedMessage(1115265); // Your Mysticism, Focus, or Imbuing Skills are not enough to use the heal stone to cure yourself.
            return;
        }

        if (_storedLifeForce < cost)
        {
            from.SendLocalizedMessage(1115264); // Your healing stone does not have enough energy to remove the poison.
            LifeForce -= cost / 3;
            return;
        }

        var chanceToCure = GetCureChance(
            MysticSpell.GetBaseSkill(from),
            Math.Max(from.Skills.Focus.Value, from.Skills.Imbuing.Value),
            poisonLevel
        );

        if (chanceToCure > Utility.Random(100) && from.CurePoison(_ownerMobile))
        {
            from.SendLocalizedMessage(500231); // You feel cured of poison!
            from.FixedEffect(0x373A, 10, 15);
            from.PlaySound(0x1E0);
            LifeForce -= cost;
        }
        else
        {
            from.SendMessage("The Healing Stone failed to cure your poison.");
            LifeForce -= cost / 3;
        }
    }

    private void ResetHealingAfterPotion()
    {
        _healingAvailable = Math.Min(1, _maximumHealing);
        _rechargeStarted = Core.Now;
        InvalidateProperties();
    }

    private void Replenish()
    {
        if (_healingAvailable >= _maximumHealing)
        {
            return;
        }

        var elapsed = Core.Now - _rechargeStarted;
        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        var recovered = (int)(elapsed.TotalSeconds * _maximumHealing / FullRechargeSeconds);
        if (recovered <= 0)
        {
            return;
        }

        _healingAvailable = Math.Min(_maximumHealing, _healingAvailable + recovered);
        _rechargeStarted = _healingAvailable >= _maximumHealing
            ? Core.Now
            : _rechargeStarted.AddSeconds(recovered * (double)FullRechargeSeconds / _maximumHealing);
        InvalidateProperties();
    }
}
