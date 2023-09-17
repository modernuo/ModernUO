using ModernUO.Serialization;
using System;
using System.Runtime.CompilerServices;
using Server.Engines.VeteranRewards;
using Server.Multis;
using Server.Spells;

namespace Server.Mobiles
{
    [SerializationGenerator(4, false)]
    public partial class EtherealMount : Item, IMount, IMountItem, IRewardItem
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool _isDonationItem;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SerializableFieldSaveFlag(0)]
        public bool ShouldSerializeIsDonationItem() => _isDonationItem;

        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        public bool _isRewardItem;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SerializableFieldSaveFlag(1)]
        public bool ShouldSerializeIsRewardItem() => _isRewardItem;

        [Constructible]
        public EtherealMount(int itemID, int mountID) : base(itemID)
        {
            _mountedID = mountID;
            _regularID = itemID;
            _rider = null;

            Layer = Layer.Invalid;

            LootType = LootType.Blessed;
        }

        public override double DefaultWeight => 1.0;

        [SerializableProperty(2)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int MountedID
        {
            get => _mountedID;
            set
            {
                if (_mountedID != value)
                {
                    _mountedID = value;

                    if (_rider != null)
                    {
                        ItemID = value;
                    }
                    this.MarkDirty();
                }
            }
        }

        [SerializableProperty(3)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int RegularID
        {
            get => _regularID;
            set
            {
                if (_regularID != value)
                {
                    _regularID = value;

                    if (_rider == null)
                    {
                        ItemID = value;
                    }
                    this.MarkDirty();
                }
            }
        }

        public override bool DisplayLootType => false;

        public virtual int FollowerSlots => 1;

        public virtual int EtherealHue => 0x4001;

        [SerializableProperty(4)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Rider
        {
            get => _rider;
            set
            {
                if (value != _rider)
                {
                    if (value == null)
                    {
                        Internalize();
                        UnmountMe();

                        RemoveFollowers();
                        _rider = null;
                    }
                    else
                    {
                        if (_rider != null)
                        {
                            Dismount(_rider);
                        }

                        Dismount(value);

                        RemoveFollowers();
                        _rider = value;
                        AddFollowers();

                        MountMe();
                    }

                    this.MarkDirty();
                }
            }
        }

        [SerializableFieldSaveFlag(4)]
        private bool ShouldSerializeRider() => _rider != null;

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(5)]
        public int Steps
        {
            get => _steps;
            set
            {
                _steps = Math.Clamp(value, 0, StepsMax);
                this.MarkDirty();
            }
        }

        [SerializableFieldSaveFlag(5)]
        private bool ShouldSerializeSteps() => _steps != StepsMax;

        public virtual int StepsMax => 3840; // Should be same as horse

        public virtual int StepsGainedPerIdleTime => 1;
        public virtual TimeSpan IdleTimePerStepsGain => TimeSpan.FromSeconds(10);

        public void OnRiderDamaged(int amount, Mobile from, bool willKill)
        {
        }

        public IMount Mount => this;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (_isDonationItem)
            {
                list.Add("Donation Ethereal");
                list.Add("7.5 sec slower cast time if not a 9mo. Veteran");
            }

            if (Core.ML && IsRewardItem)
            {
                list.Add(RewardSystem.GetRewardYearLabel(this, Array.Empty<object>())); // X Year Veteran Reward
            }
        }

        public void RemoveFollowers()
        {
            if (_rider != null)
            {
                _rider.Followers -= Math.Min(_rider.Followers, FollowerSlots);
            }
        }

        public void AddFollowers()
        {
            if (_rider != null)
            {
                _rider.Followers += FollowerSlots;
            }
        }

        public virtual bool Validate(Mobile from)
        {
            if (Parent == null)
            {
                from.SayTo(from, 1010095); // This must be on your person to use.
                return false;
            }

            if (IsRewardItem && !RewardSystem.CheckIsUsableBy(from, this) || !BaseMount.CheckMountAllowed(from))
            {
                return false;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(1005583); // Please dismount first.
                return false;
            }

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
                return false;
            }

            if (from.HasTrade)
            {
                from.SendLocalizedMessage(1042317, "", 0x41); // You may not ride at this time
                return false;
            }

            if (from.Followers + FollowerSlots > from.FollowersMax)
            {
                from.SendLocalizedMessage(1049679); // You have too many followers to summon your mount.
                return false;
            }

            return DesignContext.Check(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Validate(from))
            {
                new EtherealSpell(this, from).Cast();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, _isDonationItem ? "Donation Ethereal" : "Veteran Reward");
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _isDonationItem = reader.ReadBool();
            _isRewardItem = reader.ReadBool();
            _mountedID = reader.ReadInt();
            _regularID = reader.ReadInt();
            _rider = reader.ReadEntity<Mobile>();

            _steps = StepsMax;
        }

        [AfterDeserialization]
        private void AfterDeserialize()
        {
            AddFollowers();
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            Rider = null; // get off, move to pack

            return DeathMoveResult.RemainEquipped;
        }

        public static void Dismount(Mobile m)
        {
            var mount = m.Mount;

            if (mount != null)
            {
                mount.Rider = null;
            }
        }

        public void UnmountMe()
        {
            var bp = _rider.Backpack;

            ItemID = _regularID;
            Layer = Layer.Invalid;
            Movable = true;

            if (Hue == EtherealHue)
            {
                Hue = 0;
            }

            if (bp != null)
            {
                bp.DropItem(this);
            }
            else
            {
                var loc = _rider.Location;
                var map = _rider.Map;

                if (map == null || map == Map.Internal)
                {
                    loc = _rider.LogoutLocation;
                    map = _rider.LogoutMap;
                }

                MoveToWorld(loc, map);
            }
        }

        public void MountMe()
        {
            ItemID = _mountedID;
            Layer = Layer.Mount;
            Movable = false;

            if (Hue == 0)
            {
                Hue = EtherealHue;
            }

            ProcessDelta();
            _rider.ProcessDelta();
            _rider.EquipItem(this);
            _rider.ProcessDelta();
            ProcessDelta();
        }

        public static void StopMounting(Mobile mob)
        {
            (mob.Spell as EtherealSpell)?.Stop();
        }

        private class EtherealSpell : Spell
        {
            private static readonly SpellInfo m_Info = new("Ethereal Mount", "", 230);

            private readonly EtherealMount m_Mount;
            private readonly Mobile m_Rider;

            private bool m_Stop;

            public EtherealSpell(EtherealMount mount, Mobile rider)
                : base(rider, null, m_Info)
            {
                m_Rider = rider;
                m_Mount = mount;
            }

            public override bool ClearHandsOnCast => false;
            public override bool RevealOnCast => false;

            public override double CastDelayFastScalar => 0;

            public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(
                m_Mount.IsDonationItem && RewardSystem.GetRewardLevel(m_Rider) < 3 ? 7.5 + (Core.AOS ? 3.0 : 2.0) :
                Core.AOS ? 3.0 : 2.0
            );

            public override TimeSpan GetCastRecovery() => TimeSpan.Zero;

            public override int GetMana() => 0;

            public override bool ConsumeReagents() => true;

            public override bool CheckFizzle() => true;

            public void Stop()
            {
                m_Stop = true;
                Disturb(DisturbType.Hurt, false);
            }

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable) =>
                type != DisturbType.EquipRequest && type != DisturbType.UseRequest;

            public override void DoHurtFizzle()
            {
                if (!m_Stop)
                {
                    base.DoHurtFizzle();
                }
            }

            public override void DoFizzle()
            {
                if (!m_Stop)
                {
                    base.DoFizzle();
                }
            }

            public override void OnDisturb(DisturbType type, bool message)
            {
                if (message && !m_Stop)
                {
                    Caster.SendLocalizedMessage(
                        1049455
                    ); // You have been disrupted while attempting to summon your ethereal mount!
                }

                // m_Mount.UnmountMe();
            }

            public override void OnCast()
            {
                if (!m_Mount.Deleted && m_Mount.Rider == null && m_Mount.Validate(m_Rider))
                {
                    m_Mount.Rider = m_Rider;
                }

                FinishSequence();
            }
        }
    }


    [SerializationGenerator(0, false)]
    public partial class EtherealHorse : EtherealMount
    {
        [Constructible]
        public EtherealHorse()
            : base(0x20DD, 0x3EAA)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealLlama : EtherealMount
    {
        [Constructible]
        public EtherealLlama()
            : base(0x20F6, 0x3EAB)
        {
        }

        public override int LabelNumber => 1041300; // Ethereal Llama Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealOstard : EtherealMount
    {
        [Constructible]
        public EtherealOstard()
            : base(0x2135, 0x3EAC)
        {
        }

        public override int LabelNumber => 1041299; // Ethereal Ostard Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealRidgeback : EtherealMount
    {
        [Constructible]
        public EtherealRidgeback()
            : base(0x2615, 0x3E9A)
        {
        }

        public override int LabelNumber => 1049747; // Ethereal Ridgeback Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealUnicorn : EtherealMount
    {
        [Constructible]
        public EtherealUnicorn()
            : base(0x25CE, 0x3E9B)
        {
        }

        public override int LabelNumber => 1049745; // Ethereal Unicorn Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealBeetle : EtherealMount
    {
        [Constructible]
        public EtherealBeetle()
            : base(0x260F, 0x3E97)
        {
        }

        public override int LabelNumber => 1049748; // Ethereal Beetle Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealKirin : EtherealMount
    {
        [Constructible]
        public EtherealKirin()
            : base(0x25A0, 0x3E9C)
        {
        }

        public override int LabelNumber => 1049746; // Ethereal Ki-Rin Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class EtherealSwampDragon : EtherealMount
    {
        [Constructible]
        public EtherealSwampDragon()
            : base(0x2619, 0x3E98)
        {
        }

        public override int LabelNumber => 1049749; // Ethereal Swamp Dragon Statuette
    }

    [SerializationGenerator(0)]
    public partial class RideablePolarBear : EtherealMount
    {
        [Constructible]
        public RideablePolarBear()
            : base(0x20E1, 0x3EC5)
        {
        }

        public override int LabelNumber => 1076159; // Rideable Polar Bear
        public override int EtherealHue => 0;
    }

    [SerializationGenerator(0)]
    public partial class EtherealCuSidhe : EtherealMount
    {
        [Constructible]
        public EtherealCuSidhe()
            : base(0x2D96, 0x3E91)
        {
        }

        public override int LabelNumber => 1080386; // Ethereal Cu Sidhe Statuette
    }

    [SerializationGenerator(0)]
    public partial class EtherealHiryu : EtherealMount
    {
        [Constructible]
        public EtherealHiryu()
            : base(0x276A, 0x3E94)
        {
        }

        public override int LabelNumber => 1113813; // Ethereal Hiryu Statuette
    }

    [SerializationGenerator(0)]
    public partial class EtherealReptalon : EtherealMount
    {
        [Constructible]
        public EtherealReptalon()
            : base(0x2d95, 0x3e90)
        {
        }

        public override int LabelNumber => 1113812; // Ethereal Reptalon Statuette
    }

    [SerializationGenerator(0, false)]
    public partial class ChargerOfTheFallen : EtherealMount
    {
        [Constructible]
        public ChargerOfTheFallen()
            : base(0x2D9C, 0x3E92)
        {
        }

        public override int LabelNumber => 1074816; // Charger of the Fallen Statuette

        public override int EtherealHue => 0;
    }
}
