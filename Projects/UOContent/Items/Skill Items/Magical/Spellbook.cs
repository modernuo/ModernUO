using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Craft;
using Server.Ethics;
using Server.Multis;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public enum SpellbookType
    {
        Invalid = -1,
        Regular,
        Necromancer,
        Paladin,
        Ninja,
        Samurai,
        Arcanist,
        Mystic
    }

    public enum BookQuality
    {
        Regular,
        Exceptional
    }

    public class Spellbook : Item, ICraftable, ISlayer
    {
        private static readonly Dictionary<Mobile, List<Spellbook>> m_Table = new();

        private static readonly int[] m_LegendPropertyCounts =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0 properties : 21/52 : 40%
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,                   // 1 property   : 15/52 : 29%
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,                                  // 2 properties : 10/52 : 19%
            3, 3, 3, 3, 3, 3                                               // 3 properties :  6/52 : 12%
        };

        private static readonly int[] m_ElderPropertyCounts =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0 properties : 15/34 : 44%
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1,                // 1 property   : 10/34 : 29%
            2, 2, 2, 2, 2, 2,                            // 2 properties :  6/34 : 18%
            3, 3, 3                                      // 3 properties :  3/34 :  9%
        };

        private static readonly int[] m_GrandPropertyCounts =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0 properties : 10/20 : 50%
            1, 1, 1, 1, 1, 1,             // 1 property   :  6/20 : 30%
            2, 2, 2,                      // 2 properties :  3/20 : 15%
            3                             // 3 properties :  1/20 :  5%
        };

        private static readonly int[] m_MasterPropertyCounts =
        {
            0, 0, 0, 0, 0, 0, // 0 properties : 6/10 : 60%
            1, 1, 1,          // 1 property   : 3/10 : 30%
            2                 // 2 properties : 1/10 : 10%
        };

        private static readonly int[] m_AdeptPropertyCounts =
        {
            0, 0, 0, // 0 properties : 3/4 : 75%
            1        // 1 property   : 1/4 : 25%
        };

        private ulong m_Content;

        private Mobile m_Crafter;
        private string m_EngravedText;
        private BookQuality m_Quality;

        private SlayerName m_Slayer;
        private SlayerName m_Slayer2;

        [Constructible]
        public Spellbook(ulong content = 0, int itemID = 0xEFA) : base(itemID)
        {
            Attributes = new AosAttributes(this);
            SkillBonuses = new AosSkillBonuses(this);

            Weight = 3.0;
            Layer = Layer.OneHanded;
            LootType = LootType.Blessed;

            Content = content;
        }

        public Spellbook(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string EngravedText
        {
            get => m_EngravedText;
            set
            {
                m_EngravedText = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BookQuality Quality
        {
            get => m_Quality;
            set
            {
                m_Quality = value;
                InvalidateProperties();
            }
        }

        public override bool DisplayWeight => false;

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosAttributes Attributes { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public AosSkillBonuses SkillBonuses { get; private set; }

        public virtual SpellbookType SpellbookType => SpellbookType.Regular;
        public virtual int BookOffset => 0;
        public virtual int BookCount => 64;

        [CommandProperty(AccessLevel.GameMaster)]
        public ulong Content
        {
            get => m_Content;
            set
            {
                if (m_Content != value)
                {
                    m_Content = value;

                    SpellCount = 0;

                    while (value > 0)
                    {
                        SpellCount += (int)(value & 0x1);
                        value >>= 1;
                    }

                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpellCount { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get => m_Crafter;
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        public override bool DisplayLootType => Core.AOS;

        public virtual int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            BaseTool tool, CraftItem craftItem, int resHue
        )
        {
            var magery = from.Skills.Magery.BaseFixedPoint;

            if (magery >= 800)
            {
                int[] propertyCounts;
                int minIntensity;
                int maxIntensity;

                if (magery >= 1000)
                {
                    if (magery >= 1200)
                    {
                        propertyCounts = m_LegendPropertyCounts;
                    }
                    else if (magery >= 1100)
                    {
                        propertyCounts = m_ElderPropertyCounts;
                    }
                    else
                    {
                        propertyCounts = m_GrandPropertyCounts;
                    }

                    minIntensity = 55;
                    maxIntensity = 75;
                }
                else if (magery >= 900)
                {
                    propertyCounts = m_MasterPropertyCounts;
                    minIntensity = 25;
                    maxIntensity = 45;
                }
                else
                {
                    propertyCounts = m_AdeptPropertyCounts;
                    minIntensity = 0;
                    maxIntensity = 15;
                }

                var propertyCount = propertyCounts.RandomElement();

                BaseRunicTool.ApplyAttributesTo(this, true, 0, propertyCount, minIntensity, maxIntensity);
            }

            if (makersMark)
            {
                Crafter = from;
            }

            m_Quality = (BookQuality)(quality - 1);

            return quality;
        }
        // Currently though there are no dual slayer spellbooks, OSI has a habit of putting dual slayer stuff in later

        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName Slayer
        {
            get => m_Slayer;
            set
            {
                m_Slayer = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName Slayer2
        {
            get => m_Slayer2;
            set
            {
                m_Slayer2 = value;
                InvalidateProperties();
            }
        }

        public static void Initialize()
        {
            EventSink.OpenSpellbookRequest += EventSink_OpenSpellbookRequest;
            EventSink.CastSpellRequest += EventSink_CastSpellRequest;
            EventSink.TargetedSpell += EventSink_TargetedSpell;

            CommandSystem.Register("AllSpells", AccessLevel.GameMaster, AllSpells_OnCommand);
        }

        [Usage("AllSpells"), Description("Completely fills a targeted spellbook with scrolls.")]
        private static void AllSpells_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, AllSpells_OnTarget);
            e.Mobile.SendMessage("Target the spellbook to fill.");
        }

        private static void AllSpells_OnTarget(Mobile from, object obj)
        {
            if (obj is Spellbook book)
            {
                if (book.BookCount == 64)
                {
                    book.Content = ulong.MaxValue;
                }
                else
                {
                    book.Content = (1ul << book.BookCount) - 1;
                }

                from.SendMessage("The spellbook has been filled.");

                CommandLogging.WriteLine(
                    from,
                    "{0} {1} filling spellbook {2}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    CommandLogging.Format(book)
                );
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, AllSpells_OnTarget);
                from.SendMessage("That is not a spellbook. Try again.");
            }
        }

        private static void EventSink_OpenSpellbookRequest(Mobile from, int typeID)
        {
            if (!DesignContext.Check(from))
            {
                return; // They are customizing
            }

            var type = typeID switch
            {
                1 => SpellbookType.Regular,
                2 => SpellbookType.Necromancer,
                3 => SpellbookType.Paladin,
                4 => SpellbookType.Ninja,
                5 => SpellbookType.Samurai,
                6 => SpellbookType.Arcanist,
                7 => SpellbookType.Mystic,
                _ => SpellbookType.Regular
            };

            var book = Find(from, -1, type);

            book?.DisplayTo(from);
        }

        private static void EventSink_TargetedSpell(Mobile from, IEntity target, int spellId)
        {
            if (!DesignContext.Check(from))
            {
                return; // They are customizing
            }

            var book = Find(from, spellId);

            if (book?.HasSpell(spellId) != true)
            {
                from.SendLocalizedMessage(500015); // You do not have that spell!
                return;
            }

            var move = SpellRegistry.GetSpecialMove(spellId);

            if (move != null)
            {
                SpecialMove.SetCurrentMove(from, move);
            }
            else
            {
                SpellRegistry.NewSpell(spellId, from, null)?.Cast();
            }
        }

        private static void EventSink_CastSpellRequest(Mobile from, int spellID, Item item)
        {
            if (!DesignContext.Check(from))
            {
                return; // They are customizing
            }

            var book = item as Spellbook;

            if (book?.HasSpell(spellID) != true)
            {
                book = Find(from, spellID);
            }

            if (book?.HasSpell(spellID) == true)
            {
                var move = SpellRegistry.GetSpecialMove(spellID);

                if (move != null)
                {
                    SpecialMove.SetCurrentMove(from, move);
                }
                else
                {
                    var spell = SpellRegistry.NewSpell(spellID, from, null);

                    if (spell != null)
                    {
                        spell.Cast();
                    }
                    else
                    {
                        from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(500015); // You do not have that spell!
            }
        }

        public static SpellbookType GetTypeForSpell(int spellID)
        {
            if (spellID >= 0 && spellID < 64)
            {
                return SpellbookType.Regular;
            }

            if (spellID >= 100 && spellID < 117)
            {
                return SpellbookType.Necromancer;
            }

            if (spellID >= 200 && spellID < 210)
            {
                return SpellbookType.Paladin;
            }

            if (spellID >= 400 && spellID < 406)
            {
                return SpellbookType.Samurai;
            }

            if (spellID >= 500 && spellID < 508)
            {
                return SpellbookType.Ninja;
            }

            if (spellID >= 600 && spellID < 617)
            {
                return SpellbookType.Arcanist;
            }

            if (spellID >= 677 && spellID < 693)
            {
                return SpellbookType.Mystic;
            }

            return SpellbookType.Invalid;
        }

        public static Spellbook FindRegular(Mobile from) => Find(from, -1, SpellbookType.Regular);

        public static Spellbook FindNecromancer(Mobile from) => Find(from, -1, SpellbookType.Necromancer);

        public static Spellbook FindPaladin(Mobile from) => Find(from, -1, SpellbookType.Paladin);

        public static Spellbook FindSamurai(Mobile from) => Find(from, -1, SpellbookType.Samurai);

        public static Spellbook FindNinja(Mobile from) => Find(from, -1, SpellbookType.Ninja);

        public static Spellbook FindArcanist(Mobile from) => Find(from, -1, SpellbookType.Arcanist);

        public static Spellbook FindMystic(Mobile from) => Find(from, -1, SpellbookType.Mystic);

        public static Spellbook Find(Mobile from, int spellID) => Find(from, spellID, GetTypeForSpell(spellID));

        public static Spellbook Find(Mobile from, int spellID, SpellbookType type)
        {
            if (from == null)
            {
                return null;
            }

            if (from.Deleted)
            {
                m_Table.Remove(from);
                return null;
            }

            var searchAgain = false;

            if (!m_Table.TryGetValue(from, out var list))
            {
                m_Table[from] = list = FindAllSpellbooks(from);
            }
            else
            {
                searchAgain = true;
            }

            var book = FindSpellbookInList(list, from, spellID, type);

            if (book == null && searchAgain)
            {
                m_Table[from] = list = FindAllSpellbooks(from);

                book = FindSpellbookInList(list, from, spellID, type);
            }

            return book;
        }

        public static Spellbook FindSpellbookInList(List<Spellbook> list, Mobile from, int spellID, SpellbookType type)
        {
            var pack = from.Backpack;

            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (i >= list.Count)
                {
                    continue;
                }

                var book = list[i];

                if (!book.Deleted && (book.Parent == from || pack != null && book.Parent == pack) &&
                    ValidateSpellbook(book, spellID, type))
                {
                    return book;
                }

                list.RemoveAt(i);
            }

            return null;
        }

        public static List<Spellbook> FindAllSpellbooks(Mobile from)
        {
            var list = new List<Spellbook>();

            var spellbook = FindEquippedSpellbook(from);

            if (spellbook != null)
            {
                list.Add(spellbook);
            }

            var pack = from.Backpack;

            for (var i = 0; i < pack?.Items.Count; ++i)
            {
                if (pack.Items[i] is Spellbook sp)
                {
                    list.Add(sp);
                }
            }

            return list;
        }

        public static Spellbook FindEquippedSpellbook(Mobile from) => from.FindItemOnLayer<Spellbook>(Layer.OneHanded);

        public static bool ValidateSpellbook(Spellbook book, int spellID, SpellbookType type) =>
            book.SpellbookType == type && (spellID == -1 || book.HasSpell(spellID));

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) =>
            Ethic.CheckTrade(from, to, newOwner, this) && base.AllowSecureTrade(from, to, newOwner, accepted);

        public override bool CanEquip(Mobile from)
        {
            if (!Ethic.CheckEquip(from, this))
            {
                return false;
            }

            if (!from.CanBeginAction<BaseWeapon>())
            {
                return false;
            }

            return base.CanEquip(from);
        }

        public override bool AllowEquippedCast(Mobile from) => true;

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is not SpellScroll { Amount: 1 } scroll)
            {
                return false;
            }

            var type = GetTypeForSpell(scroll.SpellID);

            if (type != SpellbookType)
            {
                return false;
            }

            if (HasSpell(scroll.SpellID))
            {
                from.SendLocalizedMessage(500179); // That spell is already present in that spellbook.
                return false;
            }

            var val = scroll.SpellID - BookOffset;

            if (val >= 0 && val < BookCount)
            {
                m_Content |= (ulong)1 << val;
                ++SpellCount;

                InvalidateProperties();

                scroll.Delete();

                from.SendSound(0x249, GetWorldLocation());
                return true;
            }

            return false;
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not Spellbook book)
            {
                return;
            }

            book.Attributes = new AosAttributes(newItem, Attributes);
            book.SkillBonuses = new AosSkillBonuses(newItem, SkillBonuses);
        }

        public override void OnAdded(IEntity parent)
        {
            if (Core.AOS && parent is Mobile from)
            {
                SkillBonuses.AddTo(from);

                var strBonus = Attributes.BonusStr;
                var dexBonus = Attributes.BonusDex;
                var intBonus = Attributes.BonusInt;

                if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
                {
                    var serial = Serial;

                    if (strBonus != 0)
                    {
                        from.AddStatMod(new StatMod(StatType.Str, $"{serial}Str", strBonus, TimeSpan.Zero));
                    }

                    if (dexBonus != 0)
                    {
                        from.AddStatMod(new StatMod(StatType.Dex, $"{serial}Dex", dexBonus, TimeSpan.Zero));
                    }

                    if (intBonus != 0)
                    {
                        from.AddStatMod(new StatMod(StatType.Int, $"{serial}Int", intBonus, TimeSpan.Zero));
                    }
                }

                from.CheckStatTimers();
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            if (Core.AOS && parent is Mobile from)
            {
                SkillBonuses.Remove();

                var serial = Serial;

                from.RemoveStatMod($"{serial}Str");
                from.RemoveStatMod($"{serial}Dex");
                from.RemoveStatMod($"{serial}Int");

                from.CheckStatTimers();
            }
        }

        public bool HasSpell(int spellID)
        {
            spellID -= BookOffset;

            return spellID >= 0 && spellID < BookCount && (m_Content & ((ulong)1 << spellID)) != 0;
        }

        public void DisplayTo(Mobile to)
        {
            // The client must know about the spellbook or it will crash!
            var ns = to.NetState;

            if (ns.CannotSendPackets())
            {
                return;
            }

            if (Parent == null)
            {
                SendWorldPacketTo(to.NetState);
            }
            else if (Parent is Item)
            {
                to.NetState.SendContainerContentUpdate(this);
            }
            else if (Parent is Mobile)
            {
                to.NetState.SendEquipUpdate(this);
            }

            to.NetState.SendDisplaySpellbook(Serial);
            to.NetState.SendSpellbookContent(Serial, ItemID, BookOffset + 1, m_Content);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Quality == BookQuality.Exceptional)
            {
                list.Add(1063341); // exceptional
            }

            if (m_EngravedText != null)
            {
                list.Add(1072305, m_EngravedText); // Engraved: ~1_INSCRIPTION~
            }

            if (m_Crafter != null)
            {
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~
            }

            SkillBonuses.GetProperties(list);

            if (m_Slayer != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer);
                if (entry != null)
                {
                    list.Add(entry.Title);
                }
            }

            if (m_Slayer2 != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer2);
                if (entry != null)
                {
                    list.Add(entry.Title);
                }
            }

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

            list.Add(1042886, SpellCount); // ~1_NUMBERS_OF_SPELLS~ Spells
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Crafter != null)
            {
                LabelTo(from, 1050043, m_Crafter.Name); // crafted by ~1_NAME~
            }

            LabelTo(from, 1042886, SpellCount.ToString());
        }

        public override void OnDoubleClick(Mobile from)
        {
            var pack = from.Backpack;

            if (Parent == from || pack != null && Parent == pack)
            {
                DisplayTo(from);
            }
            else
            {
                from.SendLocalizedMessage(
                    500207
                ); // The spellbook must be in your backpack (and not in a container within) to open.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(5); // version

            writer.Write((byte)m_Quality);

            writer.Write(m_EngravedText);

            writer.Write(m_Crafter);

            writer.Write((int)m_Slayer);
            writer.Write((int)m_Slayer2);

            Attributes.Serialize(writer);
            SkillBonuses.Serialize(writer);

            writer.Write(m_Content);
            writer.Write(SpellCount);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        m_Quality = (BookQuality)reader.ReadByte();

                        goto case 4;
                    }
                case 4:
                    {
                        m_EngravedText = reader.ReadString();

                        goto case 3;
                    }
                case 3:
                    {
                        m_Crafter = reader.ReadEntity<Mobile>();
                        goto case 2;
                    }
                case 2:
                    {
                        m_Slayer = (SlayerName)reader.ReadInt();
                        m_Slayer2 = (SlayerName)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        Attributes = new AosAttributes(this);
                        Attributes.Deserialize(reader);
                        SkillBonuses = new AosSkillBonuses(this);
                        SkillBonuses.Deserialize(reader);

                        goto case 0;
                    }
                case 0:
                    {
                        m_Content = reader.ReadULong();
                        SpellCount = reader.ReadInt();

                        break;
                    }
            }

            Attributes ??= new AosAttributes(this);
            SkillBonuses ??= new AosSkillBonuses(this);

            if (Core.AOS && Parent is Mobile mobile)
            {
                SkillBonuses.AddTo(mobile);
            }

            var strBonus = Attributes.BonusStr;
            var dexBonus = Attributes.BonusDex;
            var intBonus = Attributes.BonusInt;

            if (Parent is Mobile m)
            {
                if (strBonus != 0 || dexBonus != 0 || intBonus != 0)
                {
                    var serial = Serial;

                    if (strBonus != 0)
                    {
                        m.AddStatMod(new StatMod(StatType.Str, $"{serial}Str", strBonus, TimeSpan.Zero));
                    }

                    if (dexBonus != 0)
                    {
                        m.AddStatMod(new StatMod(StatType.Dex, $"{serial}Dex", dexBonus, TimeSpan.Zero));
                    }

                    if (intBonus != 0)
                    {
                        m.AddStatMod(new StatMod(StatType.Int, $"{serial}Int", intBonus, TimeSpan.Zero));
                    }
                }

                m.CheckStatTimers();
            }
        }
    }
}
