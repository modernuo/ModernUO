using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public delegate void InstrumentPickedCallback(Mobile from, BaseInstrument instrument);

    public enum InstrumentQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseInstrument : Item, ICraftable, ISlayer
    {
        private static readonly Dictionary<Mobile, BaseInstrument> m_Instruments = new();
        private Mobile m_Crafter;

        private DateTime m_LastReplenished;
        private InstrumentQuality m_Quality;

        private bool m_ReplenishesCharges;
        private SlayerName m_Slayer, m_Slayer2;
        private int m_UsesRemaining;

        public BaseInstrument(int itemID, int wellSound, int badlySound) : base(itemID)
        {
            SuccessSound = wellSound;
            FailureSound = badlySound;
            UsesRemaining = Utility.RandomMinMax(InitMinUses, InitMaxUses);
        }

        public BaseInstrument(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SuccessSound { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FailureSound { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public InstrumentQuality Quality
        {
            get => m_Quality;
            set
            {
                UnscaleUses();
                m_Quality = value;
                InvalidateProperties();
                ScaleUses();
            }
        }

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

        public virtual int InitMinUses => 350;
        public virtual int InitMaxUses => 450;

        public virtual TimeSpan ChargeReplenishRate => TimeSpan.FromMinutes(5.0);

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get
            {
                CheckReplenishUses();
                return m_UsesRemaining;
            }
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastReplenished
        {
            get => m_LastReplenished;
            set
            {
                m_LastReplenished = value;
                CheckReplenishUses();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ReplenishesCharges
        {
            get => m_ReplenishesCharges;
            set
            {
                if (value != m_ReplenishesCharges && value)
                {
                    m_LastReplenished = Core.Now;
                }

                m_ReplenishesCharges = value;
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
                Crafter = from;
            }

            return quality;
        }

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

        public void CheckReplenishUses(bool invalidate = true)
        {
            if (!m_ReplenishesCharges || m_UsesRemaining >= InitMaxUses)
            {
                return;
            }

            if (m_LastReplenished + ChargeReplenishRate < Core.Now)
            {
                var timeDifference = Core.Now - m_LastReplenished;

                m_UsesRemaining = Math.Min(
                    m_UsesRemaining + (int)(timeDifference.Ticks / ChargeReplenishRate.Ticks),
                    InitMaxUses
                ); // How rude of TimeSpan to not allow timespan division.
                m_LastReplenished = Core.Now;

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

        public int GetUsesScalar() => m_Quality == InstrumentQuality.Exceptional ? 200 : 100;

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
            if (m_Instruments.TryGetValue(from, out var instrument) && instrument.IsChildOf(from.Backpack))
            {
                return instrument;
            }

            m_Instruments.Remove(from);
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

            if (m_Quality == InstrumentQuality.Exceptional)
            {
                val -= 5.0; // 10%
            }

            if (m_Slayer != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer);

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

            if (m_Slayer2 != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer2);

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
            m_Instruments[from] = item;
        }

        public override void GetProperties(IPropertyList list)
        {
            var oldUses = m_UsesRemaining;
            CheckReplenishUses(false);

            base.GetProperties(list);

            if (m_Crafter != null)
            {
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~
            }

            if (m_Quality == InstrumentQuality.Exceptional)
            {
                list.Add(1060636); // exceptional
            }

            list.Add(1060584, m_UsesRemaining); // uses remaining: ~1_val~

            if (m_ReplenishesCharges)
            {
                list.Add(1070928); // Replenish Charges
            }

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

            if (m_UsesRemaining != oldUses)
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

            if (m_Quality == InstrumentQuality.Exceptional)
            {
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));
            }

            if (m_ReplenishesCharges)
            {
                attrs.Add(new EquipInfoAttribute(1070928)); // Replenish Charges
            }

            // TODO: Must this support item identification?
            if (m_Slayer != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer);
                if (entry != null)
                {
                    attrs.Add(new EquipInfoAttribute(entry.Title));
                }
            }

            if (m_Slayer2 != SlayerName.None)
            {
                var entry = SlayerGroup.GetEntryByName(m_Slayer2);
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

            from.NetState.SendDisplayEquipmentInfo(Serial, number, m_Crafter?.RawName, false, attrs);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3); // version

            writer.Write(m_ReplenishesCharges);
            if (m_ReplenishesCharges)
            {
                writer.Write(m_LastReplenished);
            }

            writer.Write(m_Crafter);

            writer.WriteEncodedInt((int)m_Quality);
            writer.WriteEncodedInt((int)m_Slayer);
            writer.WriteEncodedInt((int)m_Slayer2);

            writer.WriteEncodedInt(UsesRemaining);

            writer.WriteEncodedInt(SuccessSound);
            writer.WriteEncodedInt(FailureSound);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_ReplenishesCharges = reader.ReadBool();

                        if (m_ReplenishesCharges)
                        {
                            m_LastReplenished = reader.ReadDateTime();
                        }

                        goto case 2;
                    }
                case 2:
                    {
                        m_Crafter = reader.ReadEntity<Mobile>();

                        m_Quality = (InstrumentQuality)reader.ReadEncodedInt();
                        m_Slayer = (SlayerName)reader.ReadEncodedInt();
                        m_Slayer2 = (SlayerName)reader.ReadEncodedInt();

                        UsesRemaining = reader.ReadEncodedInt();

                        SuccessSound = reader.ReadEncodedInt();
                        FailureSound = reader.ReadEncodedInt();

                        break;
                    }
                case 1:
                    {
                        m_Crafter = reader.ReadEntity<Mobile>();

                        m_Quality = (InstrumentQuality)reader.ReadEncodedInt();
                        m_Slayer = (SlayerName)reader.ReadEncodedInt();

                        UsesRemaining = reader.ReadEncodedInt();

                        SuccessSound = reader.ReadEncodedInt();
                        FailureSound = reader.ReadEncodedInt();

                        break;
                    }
                case 0:
                    {
                        SuccessSound = reader.ReadInt();
                        FailureSound = reader.ReadInt();
                        UsesRemaining = Utility.RandomMinMax(InitMinUses, InitMaxUses);

                        break;
                    }
            }

            CheckReplenishUses();
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
}
