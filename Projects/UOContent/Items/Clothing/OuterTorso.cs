using System;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseOuterTorso : BaseClothing
    {
        public BaseOuterTorso(int itemID, int hue = 0) : base(itemID, Layer.OuterTorso, hue)
        {
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x230E, 0x230D)]
    public partial class GildedDress : BaseOuterTorso
    {
        [Constructible]
        public GildedDress(int hue = 0) : base(0x230E, hue) => Weight = 3.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1F00, 0x1EFF)]
    public partial class FancyDress : BaseOuterTorso
    {
        [Constructible]
        public FancyDress(int hue = 0) : base(0x1F00, hue) => Weight = 3.0;
    }

    [SerializationGenerator(3, false)]
    public partial class DeathRobe : Robe
    {
        private static readonly TimeSpan m_DefaultDecayTime = TimeSpan.FromMinutes(1.0);

        [TimerDrift]
        [SerializableField(0)]
        private Timer _decayTimer;

        [DeserializeTimerField(0)]
        private void DeserializeDecayTimer(TimeSpan delay)
        {
            if (delay != TimeSpan.MinValue)
            {
                BeginDecay(delay);
            }
        }

        [Constructible]
        public DeathRobe()
        {
            LootType = LootType.Newbied;
            Hue = 2301;
            BeginDecay(m_DefaultDecayTime);
        }

        public override bool DisplayLootType => false;

        public new bool Scissor(Mobile from, Scissors scissors)
        {
            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }

        public void BeginDecay()
        {
            BeginDecay(m_DefaultDecayTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopTimer()
        {
            _decayTimer?.Stop();
            _decayTimer = null;
        }

        private void BeginDecay(TimeSpan delay)
        {
            StopTimer();
            _decayTimer = new InternalTimer(this, delay);
            _decayTimer.Start();
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            BeginDecay(m_DefaultDecayTime);

            return true;
        }

        public override bool OnDroppedInto(Mobile from, Container target, Point3D p)
        {
            if (base.OnDroppedInto(from, target, p))
            {
                StopTimer();
                return true;
            }

            return false;
        }

        public override bool OnDroppedToMobile(Mobile from, Mobile target)
        {
            StopTimer();
            return true;
        }

        public override void OnAfterDelete()
        {
            StopTimer();
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            if (reader.ReadBool())
            {
                var delay = reader.ReadDeltaTime();
                BeginDecay(delay - Core.Now);
            }
        }

        private class InternalTimer : Timer
        {
            private readonly DeathRobe _robe;

            public InternalTimer(DeathRobe c, TimeSpan delay) : base(delay) => _robe = c;

            protected override void OnTick()
            {
                if (_robe.Parent != null || _robe.IsLockedDown)
                {
                    Stop();
                    _robe._decayTimer = null;
                }
                else
                {
                    _robe.Delete();
                }
            }
        }
    }

    [Flippable]
    [SerializationGenerator(0, false)]
    public partial class RewardRobe : BaseOuterTorso, IRewardItem
    {
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _number;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _isRewardItem;

        [Constructible]
        public RewardRobe(int hue = 0, int labelNumber = 0) : base(0x1F03, hue)
        {
            Weight = 3.0;
            LootType = LootType.Blessed;

            _number = labelNumber;
        }

        public override int LabelNumber => _number > 0 ? _number : base.LabelNumber;

        public override int BasePhysicalResistance => 3;

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile mobile)
            {
                mobile.VirtualArmorMod += 2;
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile mobile)
            {
                mobile.VirtualArmorMod -= 2;
            }
        }

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Core.ML && IsRewardItem)
            {
                // X Year Veteran Reward
                list.Add(
                    RewardSystem.GetRewardYearLabel(
                        this,
                        new object[] { Hue, _number }
                    )
                );
            }
        }

        public override bool CanEquip(Mobile m) =>
            base.CanEquip(m) &&
            (!IsRewardItem || RewardSystem.CheckIsUsableBy(m, this, new object[] { Hue, _number }));

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Parent is Mobile mobile)
            {
                mobile.VirtualArmorMod += 2;
            }
        }
    }

    [Flippable]
    [SerializationGenerator(0, false)]
    public partial class RewardDress : BaseOuterTorso, IRewardItem
    {
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _number;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _isRewardItem;

        [Constructible]
        public RewardDress(int hue = 0, int labelNumber = 0) : base(0x1F01, hue)
        {
            Weight = 2.0;
            LootType = LootType.Blessed;

            _number = labelNumber;
        }

        public override int LabelNumber => _number > 0 ? _number : base.LabelNumber;

        public override int BasePhysicalResistance => 3;

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile mobile)
            {
                mobile.VirtualArmorMod += 2;
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile mobile)
            {
                mobile.VirtualArmorMod -= 2;
            }
        }

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (_isRewardItem)
            {
                // X Year Veteran Reward
                list.Add(
                    RewardSystem.GetRewardYearLabel(
                        this,
                        new object[] { Hue, _number }
                    )
                );
            }
        }

        public override bool CanEquip(Mobile m) =>
            base.CanEquip(m) &&
            (!_isRewardItem || RewardSystem.CheckIsUsableBy(m, this, new object[] { Hue, _number }));

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Parent is Mobile mobile)
            {
                mobile.VirtualArmorMod += 2;
            }
        }
    }

    [Flippable]
    [SerializationGenerator(2, false)]
    public partial class Robe : BaseOuterTorso, IArcaneEquip
    {
        [Constructible]
        public Robe(int hue = 0) : base(0x1F03, hue) => Weight = 3.0;

        [EncodedInt]
        [SerializableProperty(0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurArcaneCharges
        {
            get => _curArcaneCharges;
            set
            {
                _curArcaneCharges = value;
                InvalidateProperties();
                Update();
                this.MarkDirty();
            }
        }

        [EncodedInt]
        [SerializableProperty(1)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxArcaneCharges
        {
            get => _maxArcaneCharges;
            set
            {
                _maxArcaneCharges = value;
                InvalidateProperties();
                Update();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsArcane => _maxArcaneCharges > 0 && _curArcaneCharges >= 0;

        private void Deserialize(IGenericReader reader, int version)
        {
            if (reader.ReadBool())
            {
                _curArcaneCharges = reader.ReadInt();
                _maxArcaneCharges = reader.ReadInt();
            }
        }

        public void Update()
        {
            if (IsArcane)
            {
                ItemID = 0x26AE;
            }
            else if (ItemID == 0x26AE)
            {
                ItemID = 0x1F04;
            }

            if (IsArcane && CurArcaneCharges == 0)
            {
                Hue = 0;
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (IsArcane)
            {
                list.Add(1061837, $"{_curArcaneCharges}\t{_maxArcaneCharges}"); // arcane charges: ~1_val~ / ~2_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (IsArcane)
            {
                LabelTo(from, 1061837, $"{_curArcaneCharges}\t{_maxArcaneCharges}");
            }
        }

        public void Flip()
        {
            ItemID = ItemID switch
            {
                0x1F03 => 0x1F04,
                0x1F04 => 0x1F03,
                _      => ItemID
            };
        }
    }

    [SerializationGenerator(0, false)]
    public partial class MonkRobe : BaseOuterTorso
    {
        [Constructible]
        public MonkRobe(int hue = 0x21E) : base(0x2687, hue)
        {
            Weight = 1.0;
            StrRequirement = 0;
        }

        public override int LabelNumber => 1076584; // A monk's robe
        public override bool CanBeBlessed => false;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }
    }

    [Flippable(0x1f01, 0x1f02)]
    [SerializationGenerator(0, false)]
    public partial class PlainDress : BaseOuterTorso
    {
        [Constructible]
        public PlainDress(int hue = 0) : base(0x1F01, hue) => Weight = 2.0;
    }

    [Flippable(0x2799, 0x27E4)]
    [SerializationGenerator(0, false)]
    public partial class Kamishimo : BaseOuterTorso
    {
        [Constructible]
        public Kamishimo(int hue = 0) : base(0x2799, hue) => Weight = 3.0;
    }

    [Flippable(0x279C, 0x27E7)]
    [SerializationGenerator(0, false)]
    public partial class HakamaShita : BaseOuterTorso
    {
        [Constructible]
        public HakamaShita(int hue = 0) : base(0x279C, hue) => Weight = 3.0;
    }

    [Flippable(0x2782, 0x27CD)]
    [SerializationGenerator(0, false)]
    public partial class MaleKimono : BaseOuterTorso
    {
        [Constructible]
        public MaleKimono(int hue = 0) : base(0x2782, hue) => Weight = 3.0;
    }

    [Flippable(0x2783, 0x27CE)]
    [SerializationGenerator(0, false)]
    public partial class FemaleKimono : BaseOuterTorso
    {
        [Constructible]
        public FemaleKimono(int hue = 0) : base(0x2783, hue) => Weight = 3.0;
    }

    [Flippable(0x2FB9, 0x3173)]
    [SerializationGenerator(0)]
    public partial class MaleElvenRobe : BaseOuterTorso
    {
        [Constructible]
        public MaleElvenRobe(int hue = 0) : base(0x2FB9, hue) => Weight = 2.0;
    }

    [Flippable(0x2FBA, 0x3174)]
    [SerializationGenerator(0)]
    public partial class FemaleElvenRobe : BaseOuterTorso
    {
        [Constructible]
        public FemaleElvenRobe(int hue = 0) : base(0x2FBA, hue) => Weight = 2.0;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override bool AllowMaleWearer => false;
    }
}
