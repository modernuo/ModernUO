using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Spells.Ninjitsu
{
    public class AnimalForm : NinjaSpell
    {
        public enum MorphResult
        {
            Success,
            Fail,
            NoSkill
        }

        private static readonly SpellInfo _info = new(
            "Animal Form",
            null,
            -1,
            9002
        );

        private static readonly Dictionary<Mobile, int> _lastAnimalForms = new();
        private static readonly Dictionary<Mobile, AnimalFormContext> _table = new();

        private bool m_WasMoving;

        public AnimalForm(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => Core.ML ? 10 : 0;
        public override int CastRecoveryBase => Core.ML ? 10 : base.CastRecoveryBase;

        public override bool BlockedByAnimalForm => false;

        public static AnimalFormEntry[] Entries { get; } =
        {
            new(typeof(Kirin), 1029632, 9632, 0, 1070811, 100.0, 0x84, 0, 0),
            new(typeof(Unicorn), 1018214, 9678, 0, 1070812, 100.0, 0x7A, 0, 0),
            new(typeof(BakeKitsune), 1030083, 10083, 0, 1070810, 82.5, 0xF6, 0, 0),
            new(typeof(GreyWolf), 1028482, 9681, 2309, 1070810, 82.5, 0x19, 0x8FD, 0x90E),
            new(typeof(Llama), 1028438, 8438, 0, 1070809, 70.0, 0xDC, 0, 0),
            new(typeof(ForestOstard), 1018273, 8503, 2212, 1070809, 70.0, 0xDB, 0x899, 0x8B0),
            new(typeof(BullFrog), 1028496, 8496, 2003, 1070807, 50.0, 0x51, 0x7D1, 0x7D6, false, false),
            new(typeof(GiantSerpent), 1018114, 9663, 2009, 1070808, 50.0, 0x15, 0x7D1, 0x7E2, false, false),
            new(typeof(Dog), 1018280, 8476, 2309, 1070806, 40.0, 0xD9, 0x8FD, 0x90E, false, false),
            new(typeof(Cat), 1018264, 8475, 2309, 1070806, 40.0, 0xC9, 0x8FD, 0x90E, false, false),
            new(typeof(Rat), 1018294, 8483, 2309, 1070805, 20.0, 0xEE, 0x8FD, 0x90E, true, false),
            new(typeof(Rabbit), 1028485, 8485, 2309, 1070805, 20.0, 0xCD, 0x8FD, 0x90E, true, false),
            new(typeof(Squirrel), 1031671, 11671, 0, 0, 20.0, 0x116, 0, 0, false, false),
            new(typeof(Ferret), 1031672, 11672, 0, 1075220, 40.0, 0x117, 0, 0, false, false, true),
            new(typeof(CuSidhe), 1031670, 11670, 0, 1075221, 60.0, 0x115, 0, 0, false, false),
            new(typeof(Reptalon), 1075202, 11669, 0, 1075222, 90.0, 0x114, 0, 0, false, false)
        };

        public static void Initialize()
        {
            EventSink.Login += OnLogin;
        }

        public static void OnLogin(Mobile m)
        {
            if (GetContext(m)?.SpeedBoost == true)
            {
                m.NetState.SendSpeedControl(SpeedControlSetting.Mount);
            }
        }

        public override bool CheckCast()
        {
            if (!Caster.CanBeginAction<PolymorphSpell>())
            {
                Caster.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
                return false;
            }

            if (TransformationSpellHelper.UnderTransformation(Caster))
            {
                Caster.SendLocalizedMessage(1063219); // You cannot mimic an animal while in that form.
                return false;
            }

            if (DisguiseTimers.IsDisguised(Caster))
            {
                Caster.SendLocalizedMessage(1061631); // You can't do that while disguised.
                return false;
            }

            return base.CheckCast();
        }

        public override bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable) => false;

        private bool CasterIsMoving() =>
            Core.TickCount - Caster.LastMoveTime <= Caster.ComputeMovementSpeed(Caster.Direction);

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            Caster.FixedEffect(0x37C4, 10, 14, 4, 3);
            m_WasMoving = CasterIsMoving();
        }

        public override bool CheckFizzle() => true;

        public override void OnCast()
        {
            if (!Caster.CanBeginAction<PolymorphSpell>())
            {
                Caster.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
            }
            else if (TransformationSpellHelper.UnderTransformation(Caster))
            {
                Caster.SendLocalizedMessage(1063219); // You cannot mimic an animal while in that form.
            }
            else if (!Caster.CanBeginAction<IncognitoSpell>() || Caster.IsBodyMod && GetContext(Caster) == null)
            {
                DoFizzle();
            }
            else if (CheckSequence())
            {
                var context = GetContext(Caster);

                var mana = ScaleMana(RequiredMana);
                if (mana > Caster.Mana)
                {
                    // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                    Caster.SendLocalizedMessage(1060174, mana.ToString());
                }
                else if (context != null)
                {
                    RemoveContext(Caster, context, true);
                    Caster.Mana -= mana;
                }
                else if (Caster is PlayerMobile)
                {
                    var skipGump = m_WasMoving || CasterIsMoving();

                    if (GetLastAnimalForm(Caster) == -1 || !skipGump)
                    {
                        Caster.CloseGump<AnimalFormGump>();
                        Caster.SendGump(new AnimalFormGump(Caster, Entries, this));
                    }
                    else
                    {
                        if (Morph(Caster, GetLastAnimalForm(Caster)) == MorphResult.Fail)
                        {
                            DoFizzle();
                        }
                        else
                        {
                            Caster.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);
                            Caster.Mana -= mana;
                        }
                    }
                }
                else if (Morph(Caster, GetLastAnimalForm(Caster)) == MorphResult.Fail)
                {
                    DoFizzle();
                }
                else
                {
                    Caster.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);
                    Caster.Mana -= mana;
                }
            }

            FinishSequence();
        }

        public int GetLastAnimalForm(Mobile m) => _lastAnimalForms.TryGetValue(m, out var value) ? value : -1;

        public static MorphResult Morph(Mobile m, int entryID)
        {
            if (entryID < 0 || entryID >= Entries.Length)
            {
                return MorphResult.Fail;
            }

            var entry = Entries[entryID];

            _lastAnimalForms[m] = entryID; // On OSI, it's the last /attempted/ one not the last succeeded one

            if (m.Skills.Ninjitsu.Value < entry.ReqSkill)
            {
                // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
                m.SendLocalizedMessage(1063013, $"{entry.ReqSkill:F1}\t{SkillName.Ninjitsu}\t ");
                return MorphResult.NoSkill;
            }

            /*
            if (!m.CheckSkill( SkillName.Ninjitsu, entry.ReqSkill, entry.ReqSkill + 37.5 ))
              return MorphResult.Fail;
             *
             * On OSI,it seems you can only gain starting at '0' using Animal form.
            */

            var ninjitsu = m.Skills.Ninjitsu.Value;

            if (ninjitsu < entry.ReqSkill + 37.5)
            {
                var chance = (ninjitsu - entry.ReqSkill) / 37.5;

                if (chance < Utility.RandomDouble())
                {
                    return MorphResult.Fail;
                }
            }

            m.CheckSkill(SkillName.Ninjitsu, 0.0, 37.5);

            if (!BaseFormTalisman.EntryEnabled(m, entry.Type))
            {
                return MorphResult.Success; // Still consumes mana, just no effect
            }

            BaseMount.Dismount(m);

            var bodyMod = entry.BodyMod;
            var hueMod = entry.HueMod;

            m.BodyMod = bodyMod;
            m.HueMod = hueMod;

            if (entry.SpeedBoost)
            {
                m.NetState.SendSpeedControl(SpeedControlSetting.Mount);
            }

            // TODO: Determine if transform spell skill mods to a generic location like stat mods
            SkillMod mod = null;

            if (entry.StealthBonus)
            {
                mod = new DefaultSkillMod(SkillName.Stealth, "StealthAnimalForm", true, 20.0) { ObeyCap = true };
                m.AddSkillMod(mod);
            }

            SkillMod stealingMod = null;

            if (entry.StealingBonus)
            {
                stealingMod = new DefaultSkillMod(SkillName.Stealing, "StealingAnimalForm", true, 10.0) { ObeyCap = true };
                m.AddSkillMod(stealingMod);
            }

            Timer timer = new AnimalFormTimer(m, bodyMod, hueMod);
            timer.Start();

            AddContext(m, new AnimalFormContext(timer, mod, entry.SpeedBoost, entry.Type, stealingMod));
            m.CheckStatTimers();
            return MorphResult.Success;
        }

        public static void AddContext(Mobile m, AnimalFormContext context)
        {
            _table[m] = context;

            if (context.Type == typeof(BakeKitsune) || context.Type == typeof(GreyWolf))
            {
                m.CheckStatTimers();
            }
        }

        public static void RemoveContext(Mobile m, bool resetGraphics)
        {
            var context = GetContext(m);

            if (context != null)
            {
                RemoveContext(m, context, resetGraphics);
            }
        }

        public static void RemoveContext(Mobile m, AnimalFormContext context, bool resetGraphics)
        {
            _table.Remove(m);

            if (context.SpeedBoost)
            {
                m.NetState.SendSpeedControl(SpeedControlSetting.Disable);
            }

            var mod = context.Mod;

            if (mod != null)
            {
                m.RemoveSkillMod(mod);
            }

            mod = context.StealingMod;

            if (mod != null)
            {
                m.RemoveSkillMod(mod);
            }

            if (resetGraphics)
            {
                m.HueMod = -1;
                m.BodyMod = 0;
            }

            m.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);

            context.Timer.Stop();
        }

        public static AnimalFormContext GetContext(Mobile m) => _table.TryGetValue(m, out var context) ? context : null;

        public static bool UnderTransformation(Mobile m) => _table.ContainsKey(m);

        public static bool UnderTransformation(Mobile m, Type type) => GetContext(m)?.Type == type;

        /*
            private delegate void AnimalFormCallback( Mobile from );
            private delegate bool AnimalFormRequirementCallback( Mobile from );
        */

        public class AnimalFormEntry
        {
            private readonly int m_HueModMax;

            private readonly int m_HueModMin;
            /*
            private AnimalFormCallback m_TransformCallback;
            private AnimalFormCallback m_UntransformCallback;
            private AnimalFormRequirementCallback m_RequirementCallback;
            */

            public AnimalFormEntry(
                Type type, TextDefinition name, int itemID, int hue, int tooltip, double reqSkill,
                int bodyMod, int hueModMin, int hueModMax, bool stealthBonus = false, bool speedBoost = true,
                bool stealingBonus = false
            )
            {
                Type = type;
                Name = name;
                ItemID = itemID;
                Hue = hue;
                Tooltip = tooltip;
                ReqSkill = reqSkill;
                BodyMod = bodyMod;
                m_HueModMin = hueModMin;
                m_HueModMax = hueModMax;
                StealthBonus = stealthBonus;
                SpeedBoost = speedBoost;
                StealingBonus = stealingBonus;
            }

            public Type Type { get; }

            public TextDefinition Name { get; }

            public int ItemID { get; }

            public int Hue { get; }

            public int Tooltip { get; }

            public double ReqSkill { get; }

            public int BodyMod { get; }

            public int HueMod => Utility.RandomMinMax(m_HueModMin, m_HueModMax);
            public bool StealthBonus { get; }

            public bool SpeedBoost { get; }

            public bool StealingBonus { get; }
        }

        public class AnimalFormGump : Gump
        {
            // TODO: Convert this for ML to the BaseImageTileButtonsGump
            private readonly Mobile m_Caster;
            private readonly AnimalForm m_Spell;

            public AnimalFormGump(Mobile caster, AnimalFormEntry[] entries, AnimalForm spell)
                : base(50, 50)
            {
                m_Caster = caster;
                m_Spell = spell;

                AddPage(0);

                AddBackground(0, 0, 520, 404, 0x13BE);
                AddImageTiled(10, 10, 500, 20, 0xA40);
                AddImageTiled(10, 40, 500, 324, 0xA40);
                AddImageTiled(10, 374, 500, 20, 0xA40);
                AddAlphaRegion(10, 10, 500, 384);

                AddHtmlLocalized(14, 12, 500, 20, 1063394, 0x7FFF); // <center>Polymorph Selection Menu</center>

                AddButton(10, 374, 0xFB1, 0xFB2, 0);
                AddHtmlLocalized(45, 376, 450, 20, 1011012, 0x7FFF); // CANCEL

                var ninjitsu = caster.Skills.Ninjitsu.Value;

                var current = 0;

                for (var i = 0; i < entries.Length; ++i)
                {
                    var enabled = ninjitsu >= entries[i].ReqSkill && BaseFormTalisman.EntryEnabled(caster, entries[i].Type);

                    var page = current / 10 + 1;
                    var pos = current % 10;

                    if (pos == 0)
                    {
                        if (page > 1)
                        {
                            AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page);
                            AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next
                        }

                        AddPage(page);

                        if (page > 1)
                        {
                            AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
                            AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
                        }
                    }

                    if (!enabled)
                    {
                        continue;
                    }

                    var x = pos % 2 == 0 ? 14 : 264;
                    var y = pos / 2 * 64 + 44;

                    var b = ItemBounds.Table[entries[i].ItemID];

                    AddImageTiledButton(
                        x,
                        y,
                        0x918,
                        0x919,
                        i + 1,
                        GumpButtonType.Reply,
                        0,
                        entries[i].ItemID,
                        entries[i].Hue,
                        40 - b.Width / 2 - b.X,
                        30 - b.Height / 2 - b.Y
                    );
                    AddTooltip(entries[i].Tooltip);
                    AddHtmlLocalized(x + 84, y, 250, 60, entries[i].Name, 0x7FFF);

                    current++;
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                var entryID = info.ButtonID - 1;

                if (entryID < 0 || entryID >= AnimalForm.Entries.Length)
                {
                    return;
                }

                var mana = m_Spell.ScaleMana(m_Spell.RequiredMana);
                var entry = AnimalForm.Entries[entryID];

                if (mana > m_Caster.Mana)
                {
                    // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                    m_Caster.SendLocalizedMessage(1060174, mana.ToString());
                }
                else if (m_Caster is PlayerMobile mobile && mobile.MountBlockReason != BlockMountType.None)
                {
                    mobile.SendLocalizedMessage(1063108); // You cannot use this ability right now.
                }
                else if (BaseFormTalisman.EntryEnabled(sender.Mobile, entry.Type))
                {
                    if ((m_Caster as PlayerMobile)?.DuelContext?.AllowSpellCast(m_Caster, m_Spell) == false)
                    {
                    }
                    else if (Morph(m_Caster, entryID) == MorphResult.Fail)
                    {
                        m_Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.
                        m_Caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                        m_Caster.PlaySound(0x5C);
                    }
                    else
                    {
                        m_Caster.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);
                        m_Caster.Mana -= mana;
                    }
                }
            }
        }
    }

    public class AnimalFormContext
    {
        public AnimalFormContext(Timer timer, SkillMod mod, bool speedBoost, Type type, SkillMod stealingMod)
        {
            Timer = timer;
            Mod = mod;
            SpeedBoost = speedBoost;
            Type = type;
            StealingMod = stealingMod;
        }

        public Timer Timer { get; }

        public SkillMod Mod { get; }

        public bool SpeedBoost { get; }

        public Type Type { get; }

        public SkillMod StealingMod { get; }
    }

    public class AnimalFormTimer : Timer
    {
        private int _body;
        private int _hue;
        private Mobile _mobile;
        private int _counter;
        private Mobile _lastTarget;

        public AnimalFormTimer(Mobile from, int body, int hue)
            : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
        {
            _mobile = from;
            _body = body;
            _hue = hue;
            _counter = 0;
        }

        protected override void OnTick()
        {
            if (_mobile.Deleted || !_mobile.Alive || _mobile.Body != _body || _mobile.Hue != _hue)
            {
                AnimalForm.RemoveContext(_mobile, true);
                Stop();
                return;
            }

            if (_body == 0x115) // Cu Sidhe
            {
                if (_counter++ >= 8)
                {
                    if (_mobile.Hits < _mobile.HitsMax && _mobile.Backpack != null)
                    {
                        var b = _mobile.Backpack.FindItemByType<Bandage>();

                        if (b != null)
                        {
                            _mobile.Hits += Utility.RandomMinMax(20, 50);
                            b.Consume();
                        }
                    }

                    _counter = 0;
                }
            }
            else if (_body == 0x114) // Reptalon
            {
                if (_mobile.Combatant != null && _mobile.Combatant != _lastTarget)
                {
                    _counter = 1;
                    _lastTarget = _mobile.Combatant;
                }

                if (_mobile.Warmode && _lastTarget is { Alive: true, Deleted: false } && _counter-- <= 0)
                {
                    if (_mobile.CanBeHarmful(_lastTarget) && _lastTarget.Map == _mobile.Map &&
                        _lastTarget.InRange(_mobile.Location, BaseCreature.DefaultRangePerception) &&
                        _mobile.InLOS(_lastTarget))
                    {
                        _mobile.Direction = _mobile.GetDirectionTo(_lastTarget);
                        _mobile.Freeze(TimeSpan.FromSeconds(1));
                        _mobile.PlaySound(0x16A);

                        StartTimer(TimeSpan.FromSeconds(1.3), () => BreathEffect_Callback(_lastTarget));
                    }

                    _counter = Math.Min((int)_mobile.GetDistanceToSqrt(_lastTarget), 10);
                }
            }
        }

        public void BreathEffect_Callback(Mobile target)
        {
            if (_mobile.CanBeHarmful(target))
            {
                _mobile.RevealingAction();
                _mobile.PlaySound(0x227);
                Effects.SendMovingEffect(_mobile, target, 0x36D4, 5, 0);

                StartTimer(TimeSpan.FromSeconds(1), () => BreathDamage_Callback(target));
            }
        }

        public void BreathDamage_Callback(Mobile target)
        {
            if (_mobile.CanBeHarmful(target))
            {
                _mobile.RevealingAction();
                _mobile.DoHarmful(target);
                AOS.Damage(target, _mobile, 20, !target.Player, 0, 100, 0, 0, 0);
            }
        }
    }
}
