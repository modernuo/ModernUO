using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Spells.Second;
using Server.Targeting;
using Server.Utilities;

namespace Server.Items;

public enum TalismanRemoval
{
    None = 0,
    Ward = 390,
    Damage = 404,
    Curse = 407,
    Wildfire = 2843
}

[SerializationGenerator(1, false)]
public partial class BaseTalisman : Item, IAosItem
{
    private static readonly int[] _itemIDs =
    {
        0x2F58, 0x2F59, 0x2F5A, 0x2F5B
    };

    private static readonly Type[] _summons =
    {
        typeof(SummonedAntLion),
        typeof(SummonedCow),
        typeof(SummonedLavaSerpent),
        typeof(SummonedOrcBrute),
        typeof(SummonedFrostSpider),
        typeof(SummonedPanther),
        typeof(SummonedDoppleganger),
        typeof(SummonedGreatHart),
        typeof(SummonedBullFrog),
        typeof(SummonedArcticOgreLord),
        typeof(SummonedBogling),
        typeof(SummonedBakeKitsune),
        typeof(SummonedSheep),
        typeof(SummonedSkeletalKnight),
        typeof(SummonedWailingBanshee),
        typeof(SummonedChicken),
        typeof(SummonedVorpalBunny),

        typeof(Board),
        typeof(IronIngot),
        typeof(Bandage)
    };

    private static readonly int[] _summonLabels =
    {
        1075211, // Ant Lion
        1072494, // Cow
        1072434, // Lava Serpent
        1072414, // Orc Brute
        1072476, // Frost Spider
        1029653, // Panther
        1029741, // Doppleganger
        1018292, // great hart
        1028496, // bullfrog
        1018227, // arctic ogre lord
        1029735, // Bogling
        1030083, // bake-kitsune
        1018285, // sheep
        1018239, // skeletal knight
        1072399, // Wailing Banshee
        1072459, // Chicken
        1072401, // Vorpal Bunny

        1015101, // Boards
        1044036, // Ingots
        1023817  // clean bandage
    };

    private static readonly Type[] _killers =
    {
        typeof(OrcBomber), typeof(OrcBrute), typeof(SewerRat), typeof(Rat), typeof(GiantRat),
        typeof(Ratman), typeof(RatmanArcher), typeof(GiantSpider), typeof(FrostSpider), typeof(GiantBlackWidow),
        typeof(DreadSpider), typeof(SilverSerpent), typeof(DeepSeaSerpent), typeof(GiantSerpent), typeof(Snake),
        typeof(IceSnake), typeof(IceSerpent), typeof(LavaSerpent), typeof(LavaSnake), typeof(Yamandon),
        typeof(StrongMongbat), typeof(Mongbat), typeof(VampireBat), typeof(Lich), typeof(EvilMage),
        typeof(LichLord), typeof(EvilMageLord), typeof(SkeletalMage), typeof(KhaldunZealot), typeof(AncientLich),
        typeof(JukaMage), typeof(MeerMage), typeof(Beetle), typeof(DeathwatchBeetle), typeof(RuneBeetle),
        typeof(FireBeetle), typeof(DeathwatchBeetleHatchling), typeof(Bird), typeof(Chicken), typeof(Eagle),
        typeof(TropicalBird), typeof(Phoenix), typeof(DesertOstard), typeof(FrenziedOstard), typeof(ForestOstard),
        typeof(Crane), typeof(SnowLeopard), typeof(IceFiend), typeof(FrostOoze), typeof(FrostTroll),
        typeof(IceElemental), typeof(SnowElemental), typeof(GiantIceWorm), typeof(LadyOfTheSnow), typeof(FireElemental),
        typeof(FireSteed), typeof(HellHound), typeof(HellCat), typeof(PredatorHellCat), typeof(LavaLizard),
        typeof(FireBeetle), typeof(Cow), typeof(Bull), typeof(Gaman) // , typeof( Minotaur)
        // TODO Meraktus, Tormented Minotaur, Minotaur
    };

    private static readonly int[] _killerLabels =
    {
        1072413, 1072414, 1072418, 1072419, 1072420,
        1072421, 1072423, 1072424, 1072425, 1072426,
        1072427, 1072428, 1072429, 1072430, 1072431,
        1072432, 1072433, 1072434, 1072435, 1072438,
        1072440, 1072441, 1072443, 1072444, 1072445,
        1072446, 1072447, 1072448, 1072449, 1072450,
        1072451, 1072452, 1072453, 1072454, 1072455,
        1072456, 1072457, 1072458, 1072459, 1072461,
        1072462, 1072465, 1072468, 1072469, 1072470,
        1072473, 1072474, 1072477, 1072478, 1072479,
        1072480, 1072481, 1072483, 1072485, 1072486,
        1072487, 1072489, 1072490, 1072491, 1072492,
        1072493, 1072494, 1072495, 1072498
    };

    private static readonly SkillName[] _skills =
    {
        SkillName.Alchemy,
        SkillName.Blacksmith,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Cooking,
        SkillName.Fletching,
        SkillName.Inscribe,
        SkillName.Tailoring,
        SkillName.Tinkering
    };

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private AosAttributes _attributes;

    [SerializableFieldSaveFlag(0)]
    public bool ShouldSerializeAttributes() => !_attributes.IsEmpty;

    [SerializableFieldDefault(0)]
    private AosAttributes AttributesDefaultValue() => new(this);

    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private AosSkillBonuses _skillBonuses;

    [SerializableFieldSaveFlag(1)]
    public bool ShouldSerializeSkillBonuses() => !_skillBonuses.IsEmpty;

    [SerializableFieldDefault(1)]
    private AosSkillBonuses SkillBonusesDefaultValue() => new(this);

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private TalismanAttribute _protection;

    [SerializableFieldSaveFlag(2)]
    public bool ShouldSerializeProtection() => !_protection.IsEmpty;

    [SerializableFieldDefault(2)]
    private TalismanAttribute ProtectionDefaultValue() => new();

    [InvalidateProperties]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private TalismanAttribute _killer;

    [SerializableFieldSaveFlag(3)]
    public bool ShouldSerializeKiller() => !_killer.IsEmpty;

    [SerializableFieldDefault(3)]
    private TalismanAttribute KillerDefaultValue() => new();

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private TalismanAttribute _summoner;

    [SerializableFieldSaveFlag(4)]
    public bool ShouldSerializeSummoner() => !_summoner.IsEmpty;

    [SerializableFieldDefault(4)]
    private TalismanAttribute SummonerDefaultValue() => new();

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TalismanRemoval _removal;

    [SerializableFieldSaveFlag(5)]
    public bool ShouldSerializeRemoval() => _removal != TalismanRemoval.None;

    [InvalidateProperties]
    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillName _skill;

    [SerializableFieldSaveFlag(6)]
    public bool ShouldSerializeSkill() => (int)_skill != 0;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _successBonus;

    [SerializableFieldSaveFlag(7)]
    public bool ShouldSerializeSuccessBonus() => _successBonus != 0;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _exceptionalBonus;

    [SerializableFieldSaveFlag(8)]
    public bool ShouldSerializeExceptionalBonus() => _exceptionalBonus != 0;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxCharges;

    [SerializableFieldSaveFlag(9)]
    public bool ShouldSerializeMaxCharges() => _maxCharges != 0;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(11)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxChargeTime;

    [SerializableFieldSaveFlag(11)]
    public bool ShouldSerializeMaxChargeTime() => _maxChargeTime != 0;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(12)]
    private int _chargeTime;

    [SerializableFieldSaveFlag(12)]
    public bool ShouldSerializeChargeTime() => _chargeTime != 0;

    [InvalidateProperties]
    [SerializableField(13)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _blessed;

    [SerializableFieldSaveFlag(13)]
    public bool ShouldSerializeBlessed() => _blessed;

    [InvalidateProperties]
    [SerializableField(14)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TalismanSlayerName _slayer;

    [SerializableFieldSaveFlag(14)]
    public bool ShouldSerializeSlayer() => _slayer != TalismanSlayerName.None;

    private Mobile _creature;


    private TimerExecutionToken _timerToken;

    public BaseTalisman() : this(GetRandomItemID())
    {
    }

    public BaseTalisman(int itemID) : base(itemID)
    {
        Layer = Layer.Talisman;
        Weight = 1.0;

        _protection = new TalismanAttribute();
        _killer = new TalismanAttribute();
        _summoner = new TalismanAttribute();
        Attributes = new AosAttributes(this);
        SkillBonuses = new AosSkillBonuses(this);
    }

    public override int LabelNumber => 1071023; // Talisman
    public virtual bool ForceShowName => false; // used to override default summoner/removal name

    [SerializableProperty(10)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = value;

            if (_chargeTime > 0)
            {
                StartTimer();
            }

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableFieldSaveFlag(10)]
    public bool ShouldSerializeCharges() => _charges != 0;

    public static void Initialize()
    {
        CommandSystem.Register("RandomTalisman", AccessLevel.GameMaster, RandomTalisman_OnCommand);
    }

    [Usage("RandomTalisman <count>"), Description("Generates random talismans in your backpack.")]
    public static void RandomTalisman_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        var count = e.GetInt32(0);

        for (var i = 0; i < count; i++)
        {
            m.AddToBackpack(Loot.RandomTalisman());
        }
    }

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is not BaseTalisman talisman)
        {
            return;
        }

        talisman._summoner = new TalismanAttribute(_summoner);
        talisman._protection = new TalismanAttribute(_protection);
        talisman._killer = new TalismanAttribute(_killer);
        talisman.Attributes = new AosAttributes(newItem, Attributes);
        talisman.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
    }

    public override bool CanEquip(Mobile from)
    {
        if (BlessedFor != null && BlessedFor != from)
        {
            from.SendLocalizedMessage(1010437); // You are not the owner.
            return false;
        }

        return base.CanEquip(from);
    }

    public override void OnAdded(IEntity parent)
    {
        if (parent is Mobile from)
        {
            SkillBonuses.AddTo(from);
            Attributes.AddStatBonuses(from);

            if (_blessed && BlessedFor == null)
            {
                BlessedFor = from;
                LootType = LootType.Blessed;
            }

            if (_chargeTime > 0)
            {
                _chargeTime = _maxChargeTime;
                StartTimer();
            }
        }

        InvalidateProperties();
    }

    public override void OnRemoved(IEntity parent)
    {
        if (parent is Mobile from)
        {
            SkillBonuses.Remove();
            Attributes.RemoveStatBonuses(from);

            if (_creature?.Deleted == false)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(_creature.Location, _creature.Map, EffectItem.DefaultDuration),
                    0x3728,
                    8,
                    20,
                    5042
                );
                Effects.PlaySound(_creature, 0x201);

                _creature.Delete();
            }

            StopTimer();
        }

        InvalidateProperties();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Talisman != this)
        {
            from.SendLocalizedMessage(502641); // You must equip this item to use it.
            return;
        }

        if (_chargeTime > 0)
        {
            // You must wait ~1_val~ seconds for this to recharge.
            from.SendLocalizedMessage(1074882, _chargeTime.ToString());
            return;
        }

        if (_charges == 0 && _maxCharges > 0)
        {
            from.SendLocalizedMessage(1042544); // This item is out of charges.
            return;
        }

        var type = GetSummoner();

        if (_summoner?.IsEmpty == false)
        {
            type = _summoner.Type;
        }

        if (type != null)
        {
            IEntity entity;

            try
            {
                entity = type.CreateInstance<IEntity>();
            }
            catch
            {
                entity = null;
            }

            if (entity is Item item)
            {
                var count = 1;

                if (_summoner?.Amount > 1)
                {
                    if (item.Stackable)
                    {
                        item.Amount = _summoner.Amount;
                    }
                    else
                    {
                        count = _summoner.Amount;
                    }
                }

                if (from.Backpack == null || count * item.Weight > from.Backpack.MaxWeight ||
                    from.Backpack.Items.Count + count > from.Backpack.MaxItems)
                {
                    from.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
                    item.Delete();
                    return;
                }

                for (var i = 0; i < count; i++)
                {
                    from.PlaceInBackpack(item);

                    if (i + 1 < count)
                    {
                        item = type.CreateInstance<Item>();
                    }
                }

                if (item is Board)
                {
                    from.SendLocalizedMessage(1075000); // You have been given some wooden boards.
                }
                else if (item is IronIngot)
                {
                    from.SendLocalizedMessage(1075001); // You have been given some ingots.
                }
                else if (item is Bandage)
                {
                    from.SendLocalizedMessage(1075002); // You have been given some clean bandages.
                }
                else if (_summoner?.Name != null)
                {
                    from.SendLocalizedMessage(
                        1074853, // You have been given ~1_name~
                        _summoner.Name.Number > 0 ? $"#{_summoner.Name}" : _summoner.Name.String
                    );
                }
            }
            else if (entity is BaseCreature mob)
            {
                if (_creature?.Deleted == false)
                {
                    from.SendLocalizedMessage(1074270); // You have too many followers to summon another one.
                    mob.Delete();
                    return;
                }

                if (BaseCreature.Summon(mob, from, from.Location, mob.BaseSoundID, TimeSpan.FromMinutes(10)))
                {
                    Effects.SendLocationParticles(
                        EffectItem.Create(mob.Location, mob.Map, EffectItem.DefaultDuration),
                        0x3728,
                        1,
                        10,
                        0x26B6
                    );

                    mob.Summoned = false;
                    mob.ControlOrder = OrderType.Friend;

                    _creature = mob;
                }
            }
            else
            {
                entity?.Delete();
            }

            OnAfterUse(from);
        }

        if (_removal != TalismanRemoval.None)
        {
            from.Target = new TalismanTarget(this);
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (ForceShowName)
        {
            base.AddNameProperty(list);
        }
        else if (_summoner?.IsEmpty == false)
        {
            var name = _summoner?.Name;
            if (name?.Number > 0)
            {
                list.Add(1072400, name.Number); // Talisman of ~1_name~ Summoning
            }
            else
            {
                list.Add(1072400, name?.String ?? "Unknown"); // Talisman of ~1_name~ Summoning
            }
        }
        else if (_removal != TalismanRemoval.None)
        {
            list.AddLocalized(1072389, 1072000 + (int)_removal); // Talisman of ~1_name~
        }
        else
        {
            base.AddNameProperty(list);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Blessed)
        {
            if (BlessedFor != null)
            {
                // Owned by ~1_name~
                list.Add(1072304,BlessedFor.Name.DefaultIfNullOrEmpty("Unnamed Warrior"));
            }
            else
            {
                list.Add(1072304, "Nobody"); // Owned by ~1_name~
            }
        }

        if (Parent is Mobile && _maxChargeTime > 0)
        {
            if (_chargeTime > 0)
            {
                list.Add(1074884, _chargeTime); // Charge time left: ~1_val~
            }
            else
            {
                list.Add(1074883); // Fully Charged
            }
        }

        list.Add(1075085); // Requirement: Mondain's Legacy

        if (_killer?.IsEmpty == false && _killer.Amount > 0)
        {
            // ~1_NAME~ Killer: +~2_val~%
            list.Add(1072388, $"{_killer.Name ?? "Unknown"}\t{_killer.Amount}");
        }

        if (_protection?.IsEmpty == false && _protection.Amount > 0)
        {
            // ~1_NAME~ Protection: +~2_val~%
            list.Add(1072387, $"{_protection.Name ?? "Unknown"}\t{_protection.Amount}");
        }

        if (_exceptionalBonus != 0)
        {
            // ~1_NAME~ Exceptional Bonus: ~2_val~%
            list.Add(1072395,$"{AosSkillBonuses.GetLabel(_skill):#}\t{_exceptionalBonus}");
        }

        if (_successBonus != 0)
        {
            // ~1_NAME~ Bonus: ~2_val~%
            list.Add(1072394,$"{AosSkillBonuses.GetLabel(_skill):#}\t{_successBonus}");
        }

        SkillBonuses.GetProperties(list);

        int prop;

        if ((prop = Attributes.WeaponDamage) != 0)
        {
            list.Add(1060401, prop); // damage increase ~1_val~%
        }

        if ((prop = Attributes.DefendChance) != 0)
        {
            list.Add(1060408, prop); // defense chance increase ~1_val~%
        }

        if ((prop = Attributes.BonusDex) != 0)
        {
            list.Add(1060409, prop); // dexterity bonus ~1_val~
        }

        if ((prop = Attributes.EnhancePotions) != 0)
        {
            list.Add(1060411, prop); // enhance potions ~1_val~%
        }

        if ((prop = Attributes.CastRecovery) != 0)
        {
            list.Add(1060412, prop); // faster cast recovery ~1_val~
        }

        if ((prop = Attributes.CastSpeed) != 0)
        {
            list.Add(1060413, prop); // faster casting ~1_val~
        }

        if ((prop = Attributes.AttackChance) != 0)
        {
            list.Add(1060415, prop); // hit chance increase ~1_val~%
        }

        if ((prop = Attributes.BonusHits) != 0)
        {
            list.Add(1060431, prop); // hit point increase ~1_val~
        }

        if ((prop = Attributes.BonusInt) != 0)
        {
            list.Add(1060432, prop); // intelligence bonus ~1_val~
        }

        if ((prop = Attributes.LowerManaCost) != 0)
        {
            list.Add(1060433, prop); // lower mana cost ~1_val~%
        }

        if ((prop = Attributes.LowerRegCost) != 0)
        {
            list.Add(1060434, prop); // lower reagent cost ~1_val~%
        }

        if ((prop = Attributes.Luck) != 0)
        {
            list.Add(1060436, prop); // luck ~1_val~
        }

        if ((prop = Attributes.BonusMana) != 0)
        {
            list.Add(1060439, prop); // mana increase ~1_val~
        }

        if ((prop = Attributes.RegenMana) != 0)
        {
            list.Add(1060440, prop); // mana regeneration ~1_val~
        }

        if (Attributes.NightSight != 0)
        {
            list.Add(1060441); // night sight
        }

        if ((prop = Attributes.ReflectPhysical) != 0)
        {
            list.Add(1060442, prop); // reflect physical damage ~1_val~%
        }

        if ((prop = Attributes.RegenStam) != 0)
        {
            list.Add(1060443, prop); // stamina regeneration ~1_val~
        }

        if ((prop = Attributes.RegenHits) != 0)
        {
            list.Add(1060444, prop); // hit point regeneration ~1_val~
        }

        if (Attributes.SpellChanneling != 0)
        {
            list.Add(1060482); // spell channeling
        }

        if ((prop = Attributes.SpellDamage) != 0)
        {
            list.Add(1060483, prop); // spell damage increase ~1_val~%
        }

        if ((prop = Attributes.BonusStam) != 0)
        {
            list.Add(1060484, prop); // stamina increase ~1_val~
        }

        if ((prop = Attributes.BonusStr) != 0)
        {
            list.Add(1060485, prop); // strength bonus ~1_val~
        }

        if ((prop = Attributes.WeaponSpeed) != 0)
        {
            list.Add(1060486, prop); // swing speed increase ~1_val~%
        }

        if (Core.ML && (prop = Attributes.IncreasedKarmaLoss) != 0)
        {
            list.Add(1075210, prop); // Increased Karma Loss ~1val~%
        }

        if (_maxCharges > 0)
        {
            list.Add(1060741, _charges); // charges: ~1_val~
        }

        if (_slayer != TalismanSlayerName.None)
        {
            list.Add(1072503 + (int)_slayer);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Parent is Mobile m)
        {
            Attributes.AddStatBonuses(m);
            SkillBonuses.AddTo(m);

            if (_chargeTime > 0)
            {
                StartTimer();
            }
        }
    }

    public override void OnDelete()
    {
        _creature?.Delete(); // Will stop the unsummon timer on OnAfterDelete()
    }

    public virtual void OnAfterUse(Mobile m)
    {
        _chargeTime = _maxChargeTime;

        if (_charges > 0 && _maxCharges > 0)
        {
            _charges -= 1;
        }

        if (_chargeTime > 0)
        {
            StartTimer();
        }

        InvalidateProperties();
    }

    public virtual Type GetSummoner() => null;

    public virtual void SetSummoner(Type type, TextDefinition name)
    {
        _summoner = new TalismanAttribute(type, name);
    }

    public virtual void SetProtection(Type type, TextDefinition name, int amount)
    {
        _protection = new TalismanAttribute(type, name, amount);
    }

    public virtual void SetKiller(Type type, TextDefinition name, int amount)
    {
        _killer = new TalismanAttribute(type, name, amount);
    }

    public virtual void StartTimer()
    {
        if (!_timerToken.Running)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), Slice, out _timerToken);
        }
    }

    public virtual void StopTimer()
    {
        _timerToken.Cancel();
    }

    public virtual void Slice()
    {
        if (Deleted)
        {
            StopTimer();
            return;
        }

        if (_chargeTime - 10 > 0)
        {
            _chargeTime -= 10;
        }
        else
        {
            _chargeTime = 0;

            StopTimer();
        }

        InvalidateProperties();
    }

    public static int GetRandomItemID() => _itemIDs.RandomElement();

    public static Type GetRandomSummonType() => _summons.RandomElement();

    public static TalismanAttribute GetRandomSummoner()
    {
        if (Utility.RandomDouble() < 0.975)
        {
            return new TalismanAttribute();
        }

        var num = Utility.Random(_summons.Length);

        return num > 14
            ? new TalismanAttribute(_summons[num], _summonLabels[num], 10)
            : new TalismanAttribute(_summons[num], _summonLabels[num]);
    }

    public static TalismanRemoval GetRandomRemoval()
    {
        if (Utility.RandomDouble() < 0.65)
        {
            return (TalismanRemoval)Utility.RandomList(390, 404, 407);
        }

        return TalismanRemoval.None;
    }

    public static TalismanAttribute GetRandomKiller() => GetRandomKiller(true);

    public static TalismanAttribute GetRandomKiller(bool includingNone)
    {
        if (includingNone && Utility.RandomBool())
        {
            return new TalismanAttribute();
        }

        var num = Utility.Random(_killers.Length);

        return new TalismanAttribute(_killers[num], _killerLabels[num], Utility.RandomMinMax(10, 100));
    }

    public static TalismanAttribute GetRandomProtection() => GetRandomProtection(true);

    public static TalismanAttribute GetRandomProtection(bool includingNone)
    {
        if (includingNone && Utility.RandomBool())
        {
            return new TalismanAttribute();
        }

        var num = Utility.Random(_killers.Length);

        return new TalismanAttribute(_killers[num], _killerLabels[num], Utility.RandomMinMax(5, 60));
    }

    public static SkillName GetRandomSkill() => _skills.RandomElement();

    public static int GetRandomExceptional()
    {
        if (Utility.RandomDouble() < 0.3)
        {
            var num = 40 - Math.Log(Utility.RandomMinMax(7, 403)) * 5;

            return (int)Math.Round(num);
        }

        return 0;
    }

    public static int GetRandomSuccessful()
    {
        if (Utility.RandomDouble() < 0.75)
        {
            var num = 40 - Math.Log(Utility.RandomMinMax(7, 403)) * 5;

            return (int)Math.Round(num);
        }

        return 0;
    }

    public static bool GetRandomBlessed() => Utility.RandomDouble() < 0.02;

    public static TalismanSlayerName GetRandomSlayer() => Utility.RandomDouble() < 0.01
        ? (TalismanSlayerName)Utility.RandomMinMax(1, 9)
        : TalismanSlayerName.None;

    public static int GetRandomCharges() => Utility.RandomBool() ? Utility.RandomMinMax(10, 50) : 0;

    private class TalismanTarget : Target
    {
        private readonly BaseTalisman m_Talisman;

        public TalismanTarget(BaseTalisman talisman)
            : base(12, false, TargetFlags.Beneficial) =>
            m_Talisman = talisman;

        protected override void OnTarget(Mobile from, object o)
        {
            if (m_Talisman?.Deleted != false)
            {
                return;
            }

            if (from.Talisman != m_Talisman)
            {
                from.SendLocalizedMessage(502641); // You must equip this item to use it.
                return;
            }

            if (o is not Mobile target)
            {
                from.SendLocalizedMessage(1046439); // That is not a valid target.
                return;
            }

            if (m_Talisman.ChargeTime > 0)
            {
                from.SendLocalizedMessage(
                    1074882, // You must wait ~1_val~ seconds for this to recharge.
                    m_Talisman.ChargeTime.ToString()
                );
                return;
            }

            if (m_Talisman.Charges == 0 && m_Talisman.MaxCharges > 0)
            {
                from.SendLocalizedMessage(1042544); // This item is out of charges.
                return;
            }

            switch (m_Talisman.Removal)
            {
                case TalismanRemoval.Curse:
                    target.PlaySound(0xF6);
                    target.PlaySound(0x1F7);
                    target.FixedParticles(0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head);

                    IEntity mfrom = new Entity(
                        Serial.Zero,
                        new Point3D(target.X, target.Y, target.Z - 10),
                        from.Map
                    );
                    IEntity mto = new Entity(Serial.Zero, new Point3D(target.X, target.Y, target.Z + 50), from.Map);
                    Effects.SendMovingParticles(
                        mfrom,
                        mto,
                        0x2255,
                        1,
                        0,
                        false,
                        false,
                        13,
                        3,
                        9501,
                        1,
                        0,
                        EffectLayer.Head,
                        0x100
                    );

                    var mod = target.GetStatMod("[Magic] Str Curse");
                    if (mod?.Offset < 0)
                    {
                        target.RemoveStatMod("[Magic] Str Curse");
                    }

                    mod = target.GetStatMod("[Magic] Dex Curse");
                    if (mod?.Offset < 0)
                    {
                        target.RemoveStatMod("[Magic] Dex Curse");
                    }

                    mod = target.GetStatMod("[Magic] Int Curse");
                    if (mod?.Offset < 0)
                    {
                        target.RemoveStatMod("[Magic] Int Curse");
                    }

                    target.Paralyzed = false;

                    EvilOmenSpell.EndEffect(target);
                    StrangleSpell.RemoveCurse(target);
                    CorpseSkinSpell.RemoveCurse(target);
                    CurseSpell.RemoveEffect(target);

                    BuffInfo.RemoveBuff(target, BuffIcon.Clumsy);
                    BuffInfo.RemoveBuff(target, BuffIcon.FeebleMind);
                    BuffInfo.RemoveBuff(target, BuffIcon.Weaken);
                    BuffInfo.RemoveBuff(target, BuffIcon.MassCurse);

                    target.SendLocalizedMessage(1072408); // Any curses on you have been lifted

                    if (target != from)
                    {
                        from.SendLocalizedMessage(1072409); // Your targets curses have been lifted
                    }

                    break;
                case TalismanRemoval.Damage:
                    target.PlaySound(0x201);
                    Effects.SendLocationParticles(
                        EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                        0x3728,
                        1,
                        13,
                        0x834,
                        0,
                        0x13B2,
                        0
                    );

                    BleedAttack.EndBleed(target, true);
                    MortalStrike.EndWound(target);

                    BuffInfo.RemoveBuff(target, BuffIcon.Bleed);
                    BuffInfo.RemoveBuff(target, BuffIcon.MortalStrike);

                    target.SendLocalizedMessage(1072405); // Your lasting damage effects have been removed!

                    if (target != from)
                    {
                        from.SendLocalizedMessage(1072406); // Your Targets lasting damage effects have been removed!
                    }

                    break;
                case TalismanRemoval.Ward:
                    target.PlaySound(0x201);
                    Effects.SendLocationParticles(
                        EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                        0x3728,
                        1,
                        13,
                        0x834,
                        0,
                        0x13B2,
                        0
                    );

                    MagicReflectSpell.EndReflect(target);
                    ReactiveArmorSpell.EndArmor(target);
                    ProtectionSpell.EndProtection(target);

                    target.SendLocalizedMessage(1072402); // Your wards have been removed!

                    if (target != from)
                    {
                        from.SendLocalizedMessage(1072403); // Your target's wards have been removed!
                    }

                    break;
                case TalismanRemoval.Wildfire:
                    // TODO
                    break;
            }

            m_Talisman.OnAfterUse(from);
        }
    }
}
