using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Utilities;

namespace Server.Spells.Necromancy
{
    public class SummonFamiliarSpell : NecromancerSpell
    {
        private static readonly SpellInfo _info = new(
            "Summon Familiar",
            "Kal Xen Bal",
            203,
            9031,
            Reagent.BatWing,
            Reagent.GraveDust,
            Reagent.DaemonBlood
        );

        public SummonFamiliarSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 30.0;
        public override int RequiredMana => 17;

        public static Dictionary<Mobile, BaseCreature> Table { get; } = new();

        public static SummonFamiliarEntry[] Entries { get; } =
        {
            new(typeof(HordeMinionFamiliar), 1060146, 30.0, 30.0), // Horde Minion
            new(typeof(ShadowWispFamiliar), 1060142, 50.0, 50.0),  // Shadow Wisp
            new(typeof(DarkWolfFamiliar), 1060143, 60.0, 60.0),    // Dark Wolf
            new(typeof(DeathAdder), 1060145, 80.0, 80.0),          // Death Adder
            new(typeof(VampireBatFamiliar), 1060144, 100.0, 100.0) // Vampire Bat
        };

        public override bool CheckCast()
        {
            if (!(Table.TryGetValue(Caster, out var check) && check?.Deleted == false))
            {
                return base.CheckCast();
            }

            Caster.SendLocalizedMessage(1061605); // You already have a familiar.
            return false;
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.CloseGump<SummonFamiliarGump>();
                Caster.SendGump(new SummonFamiliarGump(Caster, Entries, this));
            }

            FinishSequence();
        }
    }

    public class SummonFamiliarEntry
    {
        public SummonFamiliarEntry(Type type, object name, double reqNecromancy, double reqSpiritSpeak)
        {
            Type = type;
            Name = name;
            ReqNecromancy = reqNecromancy;
            ReqSpiritSpeak = reqSpiritSpeak;
        }

        public Type Type { get; }

        public object Name { get; }

        public double ReqNecromancy { get; }

        public double ReqSpiritSpeak { get; }
    }

    public class SummonFamiliarGump : Gump
    {
        private const int EnabledColor16 = 0x0F20;
        private const int DisabledColor16 = 0x262A;

        private const int EnabledColor32 = 0x18CD00;
        private const int DisabledColor32 = 0x4A8B52;

        private readonly SummonFamiliarEntry[] m_Entries;
        private readonly Mobile m_From;

        private readonly SummonFamiliarSpell m_Spell;

        public SummonFamiliarGump(Mobile from, SummonFamiliarEntry[] entries, SummonFamiliarSpell spell) : base(200, 100)
        {
            m_From = from;
            m_Entries = entries;
            m_Spell = spell;

            AddPage(0);

            AddBackground(10, 10, 250, 178, 9270);
            AddAlphaRegion(20, 20, 230, 158);

            AddImage(220, 20, 10464);
            AddImage(220, 72, 10464);
            AddImage(220, 124, 10464);

            AddItem(188, 16, 6883);
            AddItem(198, 168, 6881);
            AddItem(8, 15, 6882);
            AddItem(2, 168, 6880);

            AddHtmlLocalized(30, 26, 200, 20, 1060147, EnabledColor16); // Chose thy familiar...

            var necro = from.Skills.Necromancy.Value;
            var spirit = from.Skills.SpiritSpeak.Value;

            for (var i = 0; i < entries.Length; ++i)
            {
                var name = entries[i].Name;

                var enabled = necro >= entries[i].ReqNecromancy && spirit >= entries[i].ReqSpiritSpeak;

                AddButton(27, 53 + i * 21, 9702, 9703, i + 1);

                if (name is int intName)
                {
                    AddHtmlLocalized(50, 51 + i * 21, 150, 20, intName, enabled ? EnabledColor16 : DisabledColor16);
                }
                else if (name is string strName)
                {
                    AddHtml(
                        50,
                        51 + i * 21,
                        150,
                        20,
                        $"<BASEFONT COLOR=#{(enabled ? EnabledColor32 : DisabledColor32):X6}>{strName}</BASEFONT>"
                    );
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var index = info.ButtonID - 1;

            if (index >= 0 && index < m_Entries.Length)
            {
                var entry = m_Entries[index];

                var necro = m_From.Skills.Necromancy.Value;
                var spirit = m_From.Skills.SpiritSpeak.Value;

                if ((m_From as PlayerMobile)?.DuelContext?.AllowSpellCast(m_From, m_Spell) == false)
                {
                }
                else if (SummonFamiliarSpell.Table.TryGetValue(m_From, out var check) && check?.Deleted == false)
                {
                    m_From.SendLocalizedMessage(1061605); // You already have a familiar.
                }
                else if (necro < entry.ReqNecromancy || spirit < entry.ReqSpiritSpeak)
                {
                    // That familiar requires ~1_NECROMANCY~ Necromancy and ~2_SPIRIT~ Spirit Speak.
                    m_From.SendLocalizedMessage(1061606, $"{entry.ReqNecromancy:F1}\t{entry.ReqSpiritSpeak:F1}");

                    m_From.CloseGump<SummonFamiliarGump>();
                    m_From.SendGump(new SummonFamiliarGump(m_From, SummonFamiliarSpell.Entries, m_Spell));
                }
                else if (entry.Type == null)
                {
                    m_From.SendMessage("That familiar has not yet been defined.");

                    m_From.CloseGump<SummonFamiliarGump>();
                    m_From.SendGump(new SummonFamiliarGump(m_From, SummonFamiliarSpell.Entries, m_Spell));
                }
                else
                {
                    try
                    {
                        var bc = entry.Type.CreateInstance<BaseCreature>();

                        // TODO: Is this right?
                        bc.Skills.MagicResist.Base = m_From.Skills.MagicResist.Base;

                        if (BaseCreature.Summon(bc, m_From, m_From.Location, -1, TimeSpan.FromDays(1.0)))
                        {
                            m_From.FixedParticles(0x3728, 1, 10, 9910, EffectLayer.Head);
                            bc.PlaySound(bc.GetIdleSound());
                            SummonFamiliarSpell.Table[m_From] = bc;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            else
            {
                m_From.SendLocalizedMessage(1061825); // You decide not to summon a familiar.
            }
        }
    }
}
