using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

public delegate void InstrumentPickedCallback(Mobile from, BaseInstrument instrument);

public enum InstrumentQuality
{
    Low,
    Regular,
    Exceptional
}

[SerializationGenerator(4, false)]
public abstract partial class BaseInstrument : Item, ICraftable, ISlayer
{
    private static readonly Dictionary<Mobile, BaseInstrument> _instruments = new();

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafter;

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SlayerName _slayer;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SlayerName _slayer2;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _successSound;

    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _failureSound;

    public BaseInstrument(int itemID, int wellSound, int badlySound) : base(itemID)
    {
        _successSound = wellSound;
        _failureSound = badlySound;
        _usesRemaining = Utility.RandomMinMax(InitMinUses, InitMaxUses);
    }

    public virtual int InitMinUses => 350;
    public virtual int InitMaxUses => 450;

    public virtual TimeSpan ChargeReplenishRate => TimeSpan.FromMinutes(5.0);

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool ReplenishesCharges
    {
        get => _replenishesCharges;
        set
        {
            if (value != _replenishesCharges && value)
            {
                LastReplenished = Core.Now;
            }

            _replenishesCharges = value;
            this.MarkDirty();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LastReplenished
    {
        get => _lastReplenished;
        set
        {
            _lastReplenished = value;
            CheckReplenishUses();
        }
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public InstrumentQuality Quality
    {
        get => _quality;
        set
        {
            UnscaleUses();
            _quality = value;
            InvalidateProperties();
            ScaleUses();
        }
    }

    [SerializableProperty(6)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int UsesRemaining
    {
        get
        {
            CheckReplenishUses();
            return _usesRemaining;
        }
        set
        {
            _usesRemaining = value;
            InvalidateProperties();
        }
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        Quality = (InstrumentQuality)quality;

        if (makersMark)
        {
            Crafter = from?.RawName;
        }

        return quality;
    }

    public void CheckReplenishUses(bool invalidate = true)
    {
        if (!_replenishesCharges || _usesRemaining >= InitMaxUses)
        {
            return;
        }

        if (_lastReplenished + ChargeReplenishRate < Core.Now)
        {
            var timeDifference = Core.Now - _lastReplenished;

            // How rude of TimeSpan to not allow timespan division.
            _usesRemaining = Math.Min(
                _usesRemaining + (int)(timeDifference.Ticks / ChargeReplenishRate.Ticks), InitMaxUses
            );

            // Don't use the property here otherwise you will get an infinite loop.
            _lastReplenished = Core.Now;

            if (invalidate)
            {
                InvalidateProperties();
            }
        }
    }

    public void ScaleUses()
    {
        UsesRemaining = UsesRemaining * GetUsesScalar() / 100;
        // InvalidateProperties();
    }

    public void UnscaleUses()
    {
        UsesRemaining = UsesRemaining * 100 / GetUsesScalar();
    }

    public int GetUsesScalar() => _quality == InstrumentQuality.Exceptional ? 200 : 100;

    public void ConsumeUse(Mobile from)
    {
        // TODO: Confirm what must happen here?

        if (UsesRemaining > 1)
        {
            --UsesRemaining;
        }
        else
        {
            from?.SendLocalizedMessage(502079); // The instrument played its last tune.

            Delete();
        }
    }

    public static BaseInstrument GetInstrument(Mobile from)
    {
        if (_instruments.TryGetValue(from, out var instrument) && instrument.IsChildOf(from.Backpack))
        {
            return instrument;
        }

        _instruments.Remove(from);
        return null;
    }

    public static int GetBardRange(Mobile bard, SkillName skill) => 8 + (int)(bard.Skills[skill].Value / 15);

    public static void PickInstrument(Mobile from, InstrumentPickedCallback callback)
    {
        var instrument = GetInstrument(from);
        if (instrument != null)
        {
            callback?.Invoke(from, instrument);
        }
        else
        {
            from.SendLocalizedMessage(500617); // What instrument shall you play?
            from.BeginTarget(1, false, TargetFlags.None, OnPickedInstrument, callback);
        }
    }

    public static void OnPickedInstrument(Mobile from, object targeted, InstrumentPickedCallback callback)
    {
        if (targeted is not BaseInstrument instrument)
        {
            from.SendLocalizedMessage(500619); // That is not a musical instrument.
        }
        else
        {
            SetInstrument(from, instrument);
            callback?.Invoke(from, instrument);
        }
    }

    public static bool IsMageryCreature(BaseCreature bc) => bc?.AI == AIType.AI_Mage && bc.Skills.Magery.Base > 5.0;

    public static bool IsFireBreathingCreature(BaseCreature bc) => bc?.GetAbility(MonsterAbilityType.FireBreath) != null;

    public static bool IsPoisonImmune(BaseCreature bc) => bc?.PoisonImmune != null;

    public static int GetPoisonLevel(BaseCreature bc) => (bc?.HitPoison?.Level ?? -1) + 1;

    public static double GetBaseDifficulty(Mobile targ)
    {
        /* Difficulty TODO: Add another 100 points for each of the following abilities:
          - Radiation or Aura Damage (Heat, Cold etc.)
          - Summoning Undead
        */

        // Before LBR, the success rate is actually your skill rate.
        // We are going to fudge the numbers so it feels right without having two separate provocation calculations
        // To do this, we should *undo* the 1.6x multiplier on HitsMaxSeed
        var val = targ.HitsMax * (Core.LBR ? 1.6 : 0.625) + targ.StamMax + targ.ManaMax;

        val += targ.SkillsTotal / 10.0;

        if (val > 700)
        {
            val = 700 + (int)((val - 700) * (3.0 / 11));
        }

        var bc = targ as BaseCreature;

        if (IsMageryCreature(bc))
        {
            val += 100;
        }

        if (IsFireBreathingCreature(bc))
        {
            val += 100;
        }

        if (IsPoisonImmune(bc))
        {
            val += 100;
        }

        if (targ is VampireBat or VampireBatFamiliar)
        {
            val += 100;
        }

        val += GetPoisonLevel(bc) * 20;

        val /= 10;

        if (bc?.IsParagon == true)
        {
            val += 40.0;
        }

        if (Core.SE && val > 160.0)
        {
            val = 160.0;
        }

        return val;
    }

    public double GetDifficultyFor(Mobile targ)
    {
        var val = GetBaseDifficulty(targ);

        if (_quality == InstrumentQuality.Exceptional)
        {
            val -= 5.0; // 10%
        }

        if (_slayer != SlayerName.None)
        {
            var entry = SlayerGroup.GetEntryByName(_slayer);

            if (entry != null)
            {
                if (entry.Slays(targ))
                {
                    val -= 10.0; // 20%
                }
                else if (entry.Group.OppositionSuperSlays(targ))
                {
                    val += 10.0; // -20%
                }
            }
        }

        if (_slayer2 != SlayerName.None)
        {
            var entry = SlayerGroup.GetEntryByName(_slayer2);

            if (entry != null)
            {
                if (entry.Slays(targ))
                {
                    val -= 10.0; // 20%
                }
                else if (entry.Group.OppositionSuperSlays(targ))
                {
                    val += 10.0; // -20%
                }
            }
        }

        return val;
    }

    public static void SetInstrument(Mobile from, BaseInstrument item)
    {
        _instruments[from] = item;
    }

    public override void GetProperties(IPropertyList list)
    {
        var oldUses = _usesRemaining;
        CheckReplenishUses(false);

        base.GetProperties(list);

        if (_crafter != null)
        {
            list.Add(1050043, _crafter); // crafted by ~1_NAME~
        }

        if (_quality == InstrumentQuality.Exceptional)
        {
            list.Add(1060636); // exceptional
        }

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~

        if (_replenishesCharges)
        {
            list.Add(1070928); // Replenish Charges
        }

        if (_slayer != SlayerName.None)
        {
            var entry = SlayerGroup.GetEntryByName(_slayer);
            if (entry != null)
            {
                list.Add(entry.Title);
            }
        }

        if (_slayer2 != SlayerName.None)
        {
            var entry = SlayerGroup.GetEntryByName(_slayer2);
            if (entry != null)
            {
                list.Add(entry.Title);
            }
        }

        if (_usesRemaining != oldUses)
        {
            Timer.StartTimer(InvalidateProperties);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        var attrs = new List<EquipInfoAttribute>();

        if (DisplayLootType)
        {
            if (LootType == LootType.Blessed)
            {
                attrs.Add(new EquipInfoAttribute(1038021)); // blessed
            }
            else if (LootType == LootType.Cursed)
            {
                attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }
        }

        if (_quality == InstrumentQuality.Exceptional)
        {
            attrs.Add(new EquipInfoAttribute(1018305 - (int)_quality));
        }

        if (_replenishesCharges)
        {
            attrs.Add(new EquipInfoAttribute(1070928)); // Replenish Charges
        }

        // TODO: Must this support item identification?
        if (_slayer != SlayerName.None)
        {
            var entry = SlayerGroup.GetEntryByName(_slayer);
            if (entry != null)
            {
                attrs.Add(new EquipInfoAttribute(entry.Title));
            }
        }

        if (_slayer2 != SlayerName.None)
        {
            var entry = SlayerGroup.GetEntryByName(_slayer2);
            if (entry != null)
            {
                attrs.Add(new EquipInfoAttribute(entry.Title));
            }
        }

        int number;

        if (Name == null)
        {
            number = LabelNumber;
        }
        else
        {
            LabelTo(from, Name);
            number = 1041000;
        }

        if (attrs.Count == 0 && Crafter == null && Name != null)
        {
            return;
        }

        from.NetState.SendDisplayEquipmentInfo(Serial, number, _crafter, false, attrs);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _replenishesCharges = reader.ReadBool();

        if (_replenishesCharges)
        {
            _lastReplenished = reader.ReadDateTime();
        }

        Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, reader.ReadEntity<Mobile>());

        _quality = (InstrumentQuality)reader.ReadEncodedInt();
        _slayer = (SlayerName)reader.ReadEncodedInt();
        _slayer2 = (SlayerName)reader.ReadEncodedInt();

        UsesRemaining = reader.ReadEncodedInt();

        SuccessSound = reader.ReadEncodedInt();
        FailureSound = reader.ReadEncodedInt();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        CheckReplenishUses(false);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
        else if (from.BeginAction<BaseInstrument>())
        {
            SetInstrument(from, this);

            // Delay of 6 second before being able to play another instrument again
            Timer.StartTimer(TimeSpan.FromSeconds(6), from.EndAction<BaseInstrument>);

            if (CheckMusicianship(from))
            {
                PlayInstrumentWell(from);
            }
            else
            {
                PlayInstrumentBadly(from);
            }
        }
        else
        {
            from.SendLocalizedMessage(500119); // You must wait to perform another action
        }
    }

    public static bool CheckMusicianship(Mobile m)
    {
        m.CheckSkill(SkillName.Musicianship, 0.0, 120.0);

        return m.Skills.Musicianship.Value / 100 > Utility.RandomDouble();
    }

    public void PlayInstrumentWell(Mobile from)
    {
        from.PlaySound(SuccessSound);
    }

    public void PlayInstrumentBadly(Mobile from)
    {
        from.PlaySound(FailureSound);
    }
}
