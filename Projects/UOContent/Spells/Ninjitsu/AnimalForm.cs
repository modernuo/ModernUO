using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Spells.Ninjitsu;

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

    // TODO: Cleanup periodically if players have logged out for a while
    private static readonly Dictionary<Mobile, int> _lastAnimalForms = new();
    private static readonly Dictionary<Mobile, AnimalFormContext> _table = new();

    private bool _wasMoving;

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

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (GetContext(pm)?.SpeedBoost == true)
        {
            pm.NetState.SendSpeedControl(SpeedControlSetting.Mount);
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

        if (DisguisePersistence.IsDisguised(Caster))
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
        _wasMoving = CasterIsMoving();
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
                RemoveContext(Caster, context);
                Caster.Mana -= mana;
            }
            else
            {
                var lastAnimalForm = GetLastAnimalForm(Caster);
                if (Caster is PlayerMobile && lastAnimalForm == -1 && !_wasMoving && !CasterIsMoving())
                {
                    Caster.SendGump(new AnimalFormGump(Caster, Entries, this));
                }
                else if (Morph(Caster, lastAnimalForm) == MorphResult.Fail)
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

        FinishSequence();
    }

    public static int GetLastAnimalForm(Mobile m) => _lastAnimalForms.GetValueOrDefault(m, -1);

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

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void RemoveContext(Mobile m)
    {
        var context = GetContext(m);

        if (context != null)
        {
            RemoveContext(m, context);
        }
    }

    public static void RemoveContext(Mobile m, AnimalFormContext context)
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

        m.HueMod = -1;
        m.BodyMod = 0;

        m.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);

        context.Timer.Stop();
    }

    public static AnimalFormContext GetContext(Mobile m) => _table.GetValueOrDefault(m);

    public static bool UnderTransformation(Mobile m) => _table.ContainsKey(m);

    public static bool UnderTransformation(Mobile m, Type type) => GetContext(m)?.Type == type;

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void RemoveLastAnimalForm(Mobile m) => _table.Remove(m);

    public class AnimalFormEntry
    {
        private readonly int _hueModMax;
        private readonly int _hueModMin;

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
            _hueModMin = hueModMin;
            _hueModMax = hueModMax;
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

        public int HueMod => Utility.RandomMinMax(_hueModMin, _hueModMax);
        public bool StealthBonus { get; }

        public bool SpeedBoost { get; }

        public bool StealingBonus { get; }
    }

    public class AnimalFormGump : DynamicGump
    {
        // TODO: Convert this for ML to the BaseImageTileButtonsGump
        private readonly Mobile _caster;
        private readonly AnimalForm _spell;
        private readonly AnimalFormEntry[] _entries;

        public override bool Singleton => true;

        public AnimalFormGump(Mobile caster, AnimalFormEntry[] entries, AnimalForm spell) : base(50, 50)
        {
            _caster = caster;
            _spell = spell;
            _entries = entries;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 520, 404, 0x13BE);
            builder.AddImageTiled(10, 10, 500, 20, 0xA40);
            builder.AddImageTiled(10, 40, 500, 324, 0xA40);
            builder.AddImageTiled(10, 374, 500, 20, 0xA40);
            builder.AddAlphaRegion(10, 10, 500, 384);

            builder.AddHtmlLocalized(14, 12, 500, 20, 1063394, 0x7FFF); // <center>Polymorph Selection Menu</center>

            builder.AddButton(10, 374, 0xFB1, 0xFB2, 0);
            builder.AddHtmlLocalized(45, 376, 450, 20, 1011012, 0x7FFF); // CANCEL

            int ninjitsu = _caster.Skills[SkillName.Ninjitsu].Fixed;
            int current = 0;

            for (int i = 0; i < _entries.Length; ++i)
            {
                bool enabled = ninjitsu >= _entries[i].ReqSkill && BaseFormTalisman.EntryEnabled(_caster, _entries[i].Type);

                int page = current / 10 + 1;
                int pos = current % 10;

                if (pos == 0)
                {
                    if (page > 1)
                    {
                        builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page);
                        builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next
                    }

                    builder.AddPage(page);

                    if (page > 1)
                    {
                        builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
                        builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
                    }
                }

                if (!enabled)
                {
                    continue;
                }

                AnimalFormEntry entry = _entries[i];

                int y = Math.DivRem(pos, 2, out var rem) * 64 + 44;
                int x = rem == 0 ? 14 : 264;
                Rectangle2D b = ItemBounds.Bounds[entry.ItemID];

                builder.AddImageTiledButton(x, y, 0x918, 0x919, i + 1, GumpButtonType.Reply, 0, entry.ItemID,
                    entry.Hue, 40 - b.Width / 2 - b.X, 30 - b.Height / 2 - b.Y, entry.Tooltip);

                builder.AddHtmlLocalized(x + 84, y, 250, 60, entry.Name, 0x7FFF);

                current++;
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var entryID = info.ButtonID - 1;

            if (entryID < 0 || entryID >= Entries.Length)
            {
                return;
            }

            var mana = _spell.ScaleMana(_spell.RequiredMana);
            var entry = Entries[entryID];

            if (!BaseFormTalisman.EntryEnabled(sender.Mobile, entry.Type))
            {
                return;
            }

            if (mana > _caster.Mana)
            {
                // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                _caster.SendLocalizedMessage(1060174, mana.ToString());
            }
            else if (_caster is PlayerMobile mobile
                     && (mobile.MountBlockReason != BlockMountType.None
                         || mobile.DuelContext?.AllowSpellCast(_caster, _spell) == false))
            {
                _caster.SendLocalizedMessage(1063108); // You cannot use this ability right now.
            }
            else if (Morph(_caster, entryID) == MorphResult.Fail)
            {
                _caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.
                _caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                _caster.PlaySound(0x5C);
            }
            else
            {
                _caster.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);
                _caster.Mana -= mana;
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
    private readonly int _body;
    private readonly int _hue;
    private readonly Mobile _mobile;
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
            AnimalForm.RemoveContext(_mobile);
            Stop();
            return;
        }

        if (_body == 0x115) // Cu Sidhe
        {
            // Once every 8 seconds
            if (_counter++ < 8)
            {
                return;
            }

            if (_mobile.Hits < _mobile.HitsMax)
            {
                var b = _mobile.Backpack?.FindItemByType<Bandage>();

                if (b != null)
                {
                    _mobile.Hits += Utility.RandomMinMax(20, 50);
                    b.Consume();
                }
            }
            else if (_mobile.Map == null || _mobile.Map == Map.Internal) // Logged out
            {
                Stop();
            }

            _counter = 0;
        }
        else if (_body == 0x114) // Reptalon
        {
            // Logged out
            if (_mobile.Map == null || _mobile.Map == Map.Internal)
            {
                Stop();
                _counter = 0;
                return;
            }

            if (_mobile.Combatant != null && _mobile.Combatant != _lastTarget)
            {
                _counter = 1;
                _lastTarget = _mobile.Combatant;
            }
            else if (_mobile.Warmode && _lastTarget is { Alive: true, Deleted: false } && _lastTarget.Map == _mobile.Map &&
                     _counter-- <= 0)
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
