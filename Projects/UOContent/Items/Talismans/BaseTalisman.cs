using System;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Spells.Second;
using Server.Targeting;
using Server.Utilities;

namespace Server.Items
{
    public enum TalismanRemoval
    {
        None = 0,
        Ward = 390,
        Damage = 404,
        Curse = 407,
        Wildfire = 2843
    }

    public class BaseTalisman : Item
    {
        private static readonly int[] m_ItemIDs =
        {
            0x2F58, 0x2F59, 0x2F5A, 0x2F5B
        };

        private static readonly Type[] m_Summons =
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

        private static readonly int[] m_SummonLabels =
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

        private static readonly Type[] m_Killers =
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

        private static readonly int[] m_KillerLabels =
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

        private static readonly SkillName[] m_Skills =
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

        private bool m_Blessed;
        private int m_Charges;
        private int m_ChargeTime;
        private Mobile m_Creature;
        private int m_ExceptionalBonus;
        private TalismanAttribute m_Killer;

        private int m_MaxCharges;
        private int m_MaxChargeTime;

        private TalismanAttribute m_Protection;
        private TalismanRemoval m_Removal;

        private SkillName m_Skill;

        private TalismanSlayerName m_Slayer;
        private int m_SuccessBonus;

        private TalismanAttribute m_Summoner;

        private TimerExecutionToken _timerToken;

        public BaseTalisman()
            : this(GetRandomItemID())
        {
        }

        public BaseTalisman(int itemID)
            : base(itemID)
        {
            Layer = Layer.Talisman;
            Weight = 1.0;

            m_Protection = new TalismanAttribute();
            m_Killer = new TalismanAttribute();
            m_Summoner = new TalismanAttribute();
            Attributes = new AosAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);
        }

        public BaseTalisman(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1071023; // Talisman
        public virtual bool ForceShowName => false; // used to override default summoner/removal name

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxCharges
        {
            get => m_MaxCharges;
            set
            {
                m_MaxCharges = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = value;

                if (m_ChargeTime > 0)
                {
                    StartTimer();
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxChargeTime
        {
            get => m_MaxChargeTime;
            set
            {
                m_MaxChargeTime = value;
                InvalidateProperties();
            }
        }

        public int ChargeTime
        {
            get => m_ChargeTime;
            set
            {
                m_ChargeTime = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Blessed
        {
            get => m_Blessed;
            set
            {
                m_Blessed = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TalismanSlayerName Slayer
        {
            get => m_Slayer;
            set
            {
                m_Slayer = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TalismanAttribute Summoner
        {
            get => m_Summoner;
            set
            {
                m_Summoner = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TalismanRemoval Removal
        {
            get => m_Removal;
            set
            {
                m_Removal = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TalismanAttribute Protection
        {
            get => m_Protection;
            set
            {
                m_Protection = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TalismanAttribute Killer
        {
            get => m_Killer;
            set
            {
                m_Killer = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get => m_Skill;
            set
            {
                m_Skill = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SuccessBonus
        {
            get => m_SuccessBonus;
            set
            {
                m_SuccessBonus = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ExceptionalBonus
        {
            get => m_ExceptionalBonus;
            set
            {
                m_ExceptionalBonus = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosAttributes Attributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosSkillBonuses SkillBonuses { get; private set; }

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

            talisman.m_Summoner = new TalismanAttribute(m_Summoner);
            talisman.m_Protection = new TalismanAttribute(m_Protection);
            talisman.m_Killer = new TalismanAttribute(m_Killer);
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

                if (m_Blessed && BlessedFor == null)
                {
                    BlessedFor = from;
                    LootType = LootType.Blessed;
                }

                if (m_ChargeTime > 0)
                {
                    m_ChargeTime = m_MaxChargeTime;
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

                if (m_Creature?.Deleted == false)
                {
                    Effects.SendLocationParticles(
                        EffectItem.Create(m_Creature.Location, m_Creature.Map, EffectItem.DefaultDuration),
                        0x3728,
                        8,
                        20,
                        5042
                    );
                    Effects.PlaySound(m_Creature, 0x201);

                    m_Creature.Delete();
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

            if (m_ChargeTime > 0)
            {
                from.SendLocalizedMessage(
                    1074882,
                    m_ChargeTime.ToString()
                ); // You must wait ~1_val~ seconds for this to recharge.
                return;
            }

            if (m_Charges == 0 && m_MaxCharges > 0)
            {
                from.SendLocalizedMessage(1042544); // This item is out of charges.
                return;
            }

            var type = GetSummoner();

            if (m_Summoner?.IsEmpty == false)
            {
                type = m_Summoner.Type;
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

                    if (m_Summoner?.Amount > 1)
                    {
                        if (item.Stackable)
                        {
                            item.Amount = m_Summoner.Amount;
                        }
                        else
                        {
                            count = m_Summoner.Amount;
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
                    else if (m_Summoner?.Name != null)
                    {
                        from.SendLocalizedMessage(1074853, m_Summoner.Name.ToString()); // You have been given ~1_name~
                    }
                }
                else if (entity is BaseCreature mob)
                {
                    if (m_Creature?.Deleted == false || from.Followers + mob.ControlSlots > from.FollowersMax)
                    {
                        from.SendLocalizedMessage(1074270); // You have too many followers to summon another one.
                        mob.Delete();
                        return;
                    }

                    BaseCreature.Summon(mob, from, from.Location, mob.BaseSoundID, TimeSpan.FromMinutes(10));
                    Effects.SendLocationParticles(
                        EffectItem.Create(mob.Location, mob.Map, EffectItem.DefaultDuration),
                        0x3728,
                        1,
                        10,
                        0x26B6
                    );

                    mob.Summoned = false;
                    mob.ControlOrder = OrderType.Friend;

                    m_Creature = mob;
                }
                else
                {
                    entity?.Delete();
                }

                OnAfterUse(from);
            }

            if (m_Removal != TalismanRemoval.None)
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
            else if (m_Summoner?.IsEmpty == false)
            {
                var name = m_Summoner?.Name;
                if (name?.Number > 0)
                {
                    list.Add(1072400, name.Number); // Talisman of ~1_name~ Summoning
                }
                else
                {
                    list.Add(1072400, name?.String ?? "Unknown"); // Talisman of ~1_name~ Summoning
                }
            }
            else if (m_Removal != TalismanRemoval.None)
            {
                list.AddLocalized(1072389, 1072000 + (int)m_Removal); // Talisman of ~1_name~
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
                    list.Add(
                        1072304,
                        !string.IsNullOrEmpty(BlessedFor.Name) ? BlessedFor.Name : "Unnamed Warrior"
                    ); // Owned by ~1_name~
                }
                else
                {
                    list.Add(1072304, "Nobody"); // Owned by ~1_name~
                }
            }

            if (Parent is Mobile && m_MaxChargeTime > 0)
            {
                if (m_ChargeTime > 0)
                {
                    list.Add(1074884, m_ChargeTime); // Charge time left: ~1_val~
                }
                else
                {
                    list.Add(1074883); // Fully Charged
                }
            }

            list.Add(1075085); // Requirement: Mondain's Legacy

            if (m_Killer?.IsEmpty == false && m_Killer.Amount > 0)
            {
                list.Add(
                    1072388, // ~1_NAME~ Killer: +~2_val~%
                    $"{m_Killer.Name ?? "Unknown"}\t{m_Killer.Amount}"
                );
            }

            if (m_Protection?.IsEmpty == false && m_Protection.Amount > 0)
            {
                list.Add(
                    1072387, // ~1_NAME~ Protection: +~2_val~%
                    $"{m_Protection.Name ?? "Unknown"}\t{m_Protection.Amount}"
                );
            }

            if (m_ExceptionalBonus != 0)
            {
                list.Add(
                    1072395, // ~1_NAME~ Exceptional Bonus: ~2_val~%
                    $"{AosSkillBonuses.GetLabel(m_Skill):#}\t{m_ExceptionalBonus}"
                );
            }

            if (m_SuccessBonus != 0)
            {
                list.Add(
                    1072394, // ~1_NAME~ Bonus: ~2_val~%
                    $"{AosSkillBonuses.GetLabel(m_Skill):#}\t{m_SuccessBonus}"
                );
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

            if (m_MaxCharges > 0)
            {
                list.Add(1060741, m_Charges); // charges: ~1_val~
            }

            if (m_Slayer != TalismanSlayerName.None)
            {
                list.Add(1072503 + (int)m_Slayer);
            }
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
            {
                flags |= toSet;
            }
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            var flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.Attributes, !Attributes.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.SkillBonuses, !SkillBonuses.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.Protection, m_Protection?.IsEmpty == false);
            SetSaveFlag(ref flags, SaveFlag.Killer, m_Killer?.IsEmpty == false);
            SetSaveFlag(ref flags, SaveFlag.Summoner, m_Summoner?.IsEmpty == false);
            SetSaveFlag(ref flags, SaveFlag.Removal, m_Removal != TalismanRemoval.None);
            SetSaveFlag(ref flags, SaveFlag.Skill, (int)m_Skill != 0);
            SetSaveFlag(ref flags, SaveFlag.SuccessBonus, m_SuccessBonus != 0);
            SetSaveFlag(ref flags, SaveFlag.ExceptionalBonus, m_ExceptionalBonus != 0);
            SetSaveFlag(ref flags, SaveFlag.MaxCharges, m_MaxCharges != 0);
            SetSaveFlag(ref flags, SaveFlag.Charges, m_Charges != 0);
            SetSaveFlag(ref flags, SaveFlag.MaxChargeTime, m_MaxChargeTime != 0);
            SetSaveFlag(ref flags, SaveFlag.ChargeTime, m_ChargeTime != 0);
            SetSaveFlag(ref flags, SaveFlag.Blessed, m_Blessed);
            SetSaveFlag(ref flags, SaveFlag.Slayer, m_Slayer != TalismanSlayerName.None);

            writer.WriteEncodedInt((int)flags);

            if (GetSaveFlag(flags, SaveFlag.Attributes))
            {
                Attributes.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
            {
                SkillBonuses.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.Protection))
            {
                Protection.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.Killer))
            {
                Killer.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.Summoner))
            {
                Summoner.Serialize(writer);
            }

            if (GetSaveFlag(flags, SaveFlag.Removal))
            {
                writer.WriteEncodedInt((int)m_Removal);
            }

            if (GetSaveFlag(flags, SaveFlag.Skill))
            {
                writer.WriteEncodedInt((int)m_Skill);
            }

            if (GetSaveFlag(flags, SaveFlag.SuccessBonus))
            {
                writer.WriteEncodedInt(m_SuccessBonus);
            }

            if (GetSaveFlag(flags, SaveFlag.ExceptionalBonus))
            {
                writer.WriteEncodedInt(m_ExceptionalBonus);
            }

            if (GetSaveFlag(flags, SaveFlag.MaxCharges))
            {
                writer.WriteEncodedInt(m_MaxCharges);
            }

            if (GetSaveFlag(flags, SaveFlag.Charges))
            {
                writer.WriteEncodedInt(m_Charges);
            }

            if (GetSaveFlag(flags, SaveFlag.MaxChargeTime))
            {
                writer.WriteEncodedInt(m_MaxChargeTime);
            }

            if (GetSaveFlag(flags, SaveFlag.ChargeTime))
            {
                writer.WriteEncodedInt(m_ChargeTime);
            }

            if (GetSaveFlag(flags, SaveFlag.Slayer))
            {
                writer.WriteEncodedInt((int)m_Slayer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            var flags = (SaveFlag)reader.ReadEncodedInt();

            Attributes = new AosAttributes(this);
            if (GetSaveFlag(flags, SaveFlag.Attributes))
            {
                Attributes.Deserialize(reader);
            }

            SkillBonuses = new AosSkillBonuses(this);
            if (GetSaveFlag(flags, SaveFlag.SkillBonuses))
            {
                SkillBonuses.Deserialize(reader);
            }

            // Backward compatibility
            if (GetSaveFlag(flags, SaveFlag.Owner))
            {
                BlessedFor = reader.ReadEntity<Mobile>();
            }

            m_Protection = GetSaveFlag(flags, SaveFlag.Protection)
                ? new TalismanAttribute(reader)
                : new TalismanAttribute();
            m_Killer = GetSaveFlag(flags, SaveFlag.Killer)
                ? new TalismanAttribute(reader)
                : new TalismanAttribute();
            m_Summoner = GetSaveFlag(flags, SaveFlag.Summoner)
                ? new TalismanAttribute(reader)
                : new TalismanAttribute();

            if (GetSaveFlag(flags, SaveFlag.Removal))
            {
                m_Removal = (TalismanRemoval)reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.OldKarmaLoss))
            {
                Attributes.IncreasedKarmaLoss = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.Skill))
            {
                m_Skill = (SkillName)reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.SuccessBonus))
            {
                m_SuccessBonus = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.ExceptionalBonus))
            {
                m_ExceptionalBonus = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.MaxCharges))
            {
                m_MaxCharges = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.Charges))
            {
                m_Charges = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.MaxChargeTime))
            {
                m_MaxChargeTime = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.ChargeTime))
            {
                m_ChargeTime = reader.ReadEncodedInt();
            }

            if (GetSaveFlag(flags, SaveFlag.Slayer))
            {
                m_Slayer = (TalismanSlayerName)reader.ReadEncodedInt();
            }

            m_Blessed = GetSaveFlag(flags, SaveFlag.Blessed);

            if (Parent is Mobile m)
            {
                Attributes.AddStatBonuses(m);
                SkillBonuses.AddTo(m);

                if (m_ChargeTime > 0)
                {
                    StartTimer();
                }
            }
        }

        public virtual void OnAfterUse(Mobile m)
        {
            m_ChargeTime = m_MaxChargeTime;

            if (m_Charges > 0 && m_MaxCharges > 0)
            {
                m_Charges -= 1;
            }

            if (m_ChargeTime > 0)
            {
                StartTimer();
            }

            InvalidateProperties();
        }

        public virtual Type GetSummoner() => null;

        public virtual void SetSummoner(Type type, TextDefinition name)
        {
            m_Summoner = new TalismanAttribute(type, name);
        }

        public virtual void SetProtection(Type type, TextDefinition name, int amount)
        {
            m_Protection = new TalismanAttribute(type, name, amount);
        }

        public virtual void SetKiller(Type type, TextDefinition name, int amount)
        {
            m_Killer = new TalismanAttribute(type, name, amount);
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

            if (m_ChargeTime - 10 > 0)
            {
                m_ChargeTime -= 10;
            }
            else
            {
                m_ChargeTime = 0;

                StopTimer();
            }

            InvalidateProperties();
        }

        public static int GetRandomItemID() => m_ItemIDs.RandomElement();

        public static Type GetRandomSummonType() => m_Summons.RandomElement();

        public static TalismanAttribute GetRandomSummoner()
        {
            if (Utility.RandomDouble() < 0.975)
            {
                return new TalismanAttribute();
            }

            var num = Utility.Random(m_Summons.Length);

            return num > 14
                ? new TalismanAttribute(m_Summons[num], m_SummonLabels[num], 10)
                : new TalismanAttribute(m_Summons[num], m_SummonLabels[num]);
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

            var num = Utility.Random(m_Killers.Length);

            return new TalismanAttribute(m_Killers[num], m_KillerLabels[num], Utility.RandomMinMax(10, 100));
        }

        public static TalismanAttribute GetRandomProtection() => GetRandomProtection(true);

        public static TalismanAttribute GetRandomProtection(bool includingNone)
        {
            if (includingNone && Utility.RandomBool())
            {
                return new TalismanAttribute();
            }

            var num = Utility.Random(m_Killers.Length);

            return new TalismanAttribute(m_Killers[num], m_KillerLabels[num], Utility.RandomMinMax(5, 60));
        }

        public static SkillName GetRandomSkill() => m_Skills.RandomElement();

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

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            Attributes = 0x00000001,
            SkillBonuses = 0x00000002,
            Owner = 0x00000004,
            Protection = 0x00000008,
            Killer = 0x00000010,
            Summoner = 0x00000020,
            Removal = 0x00000040,
            OldKarmaLoss = 0x00000080,
            Skill = 0x00000100,
            SuccessBonus = 0x00000200,
            ExceptionalBonus = 0x00000400,
            MaxCharges = 0x00000800,
            Charges = 0x00001000,
            MaxChargeTime = 0x00002000,
            ChargeTime = 0x00004000,
            Blessed = 0x00008000,
            Slayer = 0x00010000
        }

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
}
