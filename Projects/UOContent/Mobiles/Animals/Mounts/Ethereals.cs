using System;
using Server.Engines.VeteranRewards;
using Server.Multis;
using Server.Spells;

namespace Server.Mobiles
{
    public class EtherealMount : Item, IMount, IMountItem, IRewardItem
    {
        private bool m_IsDonationItem;
        private int m_MountedID;
        private int m_RegularID;
        private Mobile m_Rider;

        [Constructible]
        public EtherealMount(int itemID, int mountID) : base(itemID)
        {
            m_MountedID = mountID;
            m_RegularID = itemID;
            m_Rider = null;

            Layer = Layer.Invalid;

            LootType = LootType.Blessed;
        }

        public EtherealMount(Serial serial)
            : base(serial)
        {
        }

        public override double DefaultWeight => 1.0;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool IsDonationItem
        {
            get => m_IsDonationItem;
            set
            {
                m_IsDonationItem = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MountedID
        {
            get => m_MountedID;
            set
            {
                if (m_MountedID != value)
                {
                    m_MountedID = value;

                    if (m_Rider != null)
                    {
                        ItemID = value;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RegularID
        {
            get => m_RegularID;
            set
            {
                if (m_RegularID != value)
                {
                    m_RegularID = value;

                    if (m_Rider == null)
                    {
                        ItemID = value;
                    }
                }
            }
        }

        public override bool DisplayLootType => false;

        public virtual int FollowerSlots => 1;

        public virtual int EtherealHue => 0x4001;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Rider
        {
            get => m_Rider;
            set
            {
                if (value != m_Rider)
                {
                    if (value == null)
                    {
                        Internalize();
                        UnmountMe();

                        RemoveFollowers();
                        m_Rider = null;
                    }
                    else
                    {
                        if (m_Rider != null)
                        {
                            Dismount(m_Rider);
                        }

                        Dismount(value);

                        RemoveFollowers();
                        m_Rider = value;
                        AddFollowers();

                        MountMe();
                    }
                }
            }
        }

        public void OnRiderDamaged(int amount, Mobile from, bool willKill)
        {
        }

        public IMount Mount => this;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsDonationItem)
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
            if (m_Rider != null)
            {
                m_Rider.Followers -= Math.Min(m_Rider.Followers, FollowerSlots);
            }
        }

        public void AddFollowers()
        {
            if (m_Rider != null)
            {
                m_Rider.Followers += FollowerSlots;
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

            LabelTo(from, m_IsDonationItem ? "Donation Ethereal" : "Veteran Reward");
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3); // version

            writer.Write(m_IsDonationItem);
            writer.Write(IsRewardItem);

            writer.Write(m_MountedID);
            writer.Write(m_RegularID);
            writer.Write(m_Rider);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_IsDonationItem = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        IsRewardItem = reader.ReadBool();
                        goto case 0;
                    }
                case 1:
                    reader.ReadInt();
                    goto case 0;
                case 0:
                    {
                        m_MountedID = reader.ReadInt();
                        m_RegularID = reader.ReadInt();
                        m_Rider = reader.ReadEntity<Mobile>();

                        if (m_MountedID == 0x3EA2)
                        {
                            m_MountedID = 0x3EAA;
                        }

                        break;
                    }
            }

            AddFollowers();

            if (version < 3 && Weight == 0)
            {
                Weight = -1;
            }
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
            var bp = m_Rider.Backpack;

            ItemID = m_RegularID;
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
                var loc = m_Rider.Location;
                var map = m_Rider.Map;

                if (map == null || map == Map.Internal)
                {
                    loc = m_Rider.LogoutLocation;
                    map = m_Rider.LogoutMap;
                }

                MoveToWorld(loc, map);
            }
        }

        public void MountMe()
        {
            ItemID = m_MountedID;
            Layer = Layer.Mount;
            Movable = false;

            if (Hue == 0)
            {
                Hue = EtherealHue;
            }

            ProcessDelta();
            m_Rider.ProcessDelta();
            m_Rider.EquipItem(this);
            m_Rider.ProcessDelta();
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

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable)
            {
                if (type is DisturbType.EquipRequest or DisturbType.UseRequest /* || type == DisturbType.Hurt*/)
                {
                    return false;
                }

                return true;
            }

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

    public class EtherealHorse : EtherealMount
    {
        [Constructible]
        public EtherealHorse()
            : base(0x20DD, 0x3EAA)
        {
        }

        public EtherealHorse(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1041298; // Ethereal Horse Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal horse")
            {
                Name = null;
            }

            if (ItemID == 0x2124)
            {
                ItemID = 0x20DD;
            }
        }
    }

    public class EtherealLlama : EtherealMount
    {
        [Constructible]
        public EtherealLlama()
            : base(0x20F6, 0x3EAB)
        {
        }

        public EtherealLlama(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1041300; // Ethereal Llama Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal llama")
            {
                Name = null;
            }
        }
    }

    public class EtherealOstard : EtherealMount
    {
        [Constructible]
        public EtherealOstard()
            : base(0x2135, 0x3EAC)
        {
        }

        public EtherealOstard(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1041299; // Ethereal Ostard Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal ostard")
            {
                Name = null;
            }
        }
    }

    public class EtherealRidgeback : EtherealMount
    {
        [Constructible]
        public EtherealRidgeback()
            : base(0x2615, 0x3E9A)
        {
        }

        public EtherealRidgeback(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1049747; // Ethereal Ridgeback Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal ridgeback")
            {
                Name = null;
            }
        }
    }

    public class EtherealUnicorn : EtherealMount
    {
        [Constructible]
        public EtherealUnicorn()
            : base(0x25CE, 0x3E9B)
        {
        }

        public EtherealUnicorn(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1049745; // Ethereal Unicorn Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal unicorn")
            {
                Name = null;
            }
        }
    }

    public class EtherealBeetle : EtherealMount
    {
        [Constructible]
        public EtherealBeetle()
            : base(0x260F, 0x3E97)
        {
        }

        public EtherealBeetle(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1049748; // Ethereal Beetle Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal beetle")
            {
                Name = null;
            }
        }
    }

    public class EtherealKirin : EtherealMount
    {
        [Constructible]
        public EtherealKirin()
            : base(0x25A0, 0x3E9C)
        {
        }

        public EtherealKirin(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1049746; // Ethereal Ki-Rin Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal kirin")
            {
                Name = null;
            }
        }
    }

    public class EtherealSwampDragon : EtherealMount
    {
        [Constructible]
        public EtherealSwampDragon()
            : base(0x2619, 0x3E98)
        {
        }

        public EtherealSwampDragon(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1049749; // Ethereal Swamp Dragon Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Name == "an ethereal swamp dragon")
            {
                Name = null;
            }
        }
    }

    public class RideablePolarBear : EtherealMount
    {
        [Constructible]
        public RideablePolarBear()
            : base(0x20E1, 0x3EC5)
        {
        }

        public RideablePolarBear(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1076159; // Rideable Polar Bear
        public override int EtherealHue => 0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class EtherealCuSidhe : EtherealMount
    {
        [Constructible]
        public EtherealCuSidhe()
            : base(0x2D96, 0x3E91)
        {
        }

        public EtherealCuSidhe(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1080386; // Ethereal Cu Sidhe Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class EtherealHiryu : EtherealMount
    {
        [Constructible]
        public EtherealHiryu()
            : base(0x276A, 0x3E94)
        {
        }

        public EtherealHiryu(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1113813; // Ethereal Hiryu Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class EtherealReptalon : EtherealMount
    {
        [Constructible]
        public EtherealReptalon()
            : base(0x2d95, 0x3e90)
        {
        }

        public EtherealReptalon(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1113812; // Ethereal Reptalon Statuette

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class ChargerOfTheFallen : EtherealMount
    {
        [Constructible]
        public ChargerOfTheFallen()
            : base(0x2D9C, 0x3E92)
        {
        }

        public ChargerOfTheFallen(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1074816; // Charger of the Fallen Statuette

        public override int EtherealHue => 0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version <= 1 && Hue != 0)
            {
                Hue = 0;
            }
        }
    }
}
