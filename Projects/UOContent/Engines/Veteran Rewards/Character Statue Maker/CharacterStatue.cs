using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Mobiles
{
    public enum StatueType
    {
        Marble,
        Jade,
        Bronze
    }

    public enum StatuePose
    {
        Ready,
        Casting,
        Salute,
        AllPraiseMe,
        Fighting,
        HandsOnHips
    }

    public enum StatueMaterial
    {
        Antique,
        Dark,
        Medium,
        Light
    }

    public class CharacterStatue : Mobile, IRewardItem
    {
        private int m_Animation;
        private int m_Frames;
        private StatueMaterial m_Material;
        private StatuePose m_Pose;

        private Mobile m_SculptedBy;
        private StatueType m_Type;

        public CharacterStatue(Mobile from, StatueType type)
        {
            m_Type = type;
            m_Pose = StatuePose.Ready;
            m_Material = StatueMaterial.Antique;

            Direction = Direction.South;
            AccessLevel = AccessLevel.Counselor;
            Hits = HitsMax;
            Blessed = true;
            Frozen = true;

            CloneBody(from);
            CloneClothes(from);
            InvalidateHues();
        }

        public CharacterStatue(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StatueType StatueType
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateHues();
                InvalidatePose();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StatuePose Pose
        {
            get => m_Pose;
            set
            {
                m_Pose = value;
                InvalidatePose();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StatueMaterial Material
        {
            get => m_Material;
            set
            {
                m_Material = value;
                InvalidateHues();
                InvalidatePose();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile SculptedBy
        {
            get => m_SculptedBy;
            set
            {
                m_SculptedBy = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SculptedOn { get; set; }

        public CharacterStatuePlinth Plinth { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem { get; set; }

        public override void OnDoubleClick(Mobile from)
        {
            DisplayPaperdollTo(from);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_SculptedBy != null)
            {
                if (m_SculptedBy.ShowFameTitle && (m_SculptedBy.Player || m_SculptedBy.Body.IsHuman) &&
                    m_SculptedBy.Fame >= 10000)
                {
                    list.Add(
                        1076202,
                        $"{(m_SculptedBy.Female ? "Lady" : "Lord")} {m_SculptedBy.Name}"
                    ); // Sculpted by ~1_Name~
                }
                else
                {
                    list.Add(1076202, m_SculptedBy.Name); // Sculpted by ~1_Name~
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && m_SculptedBy != null)
            {
                var house = BaseHouse.FindHouseAt(this);

                if (house?.IsCoOwner(from) == true || from.AccessLevel > AccessLevel.Counselor)
                {
                    list.Add(new DemolishEntry(this));
                }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (Plinth?.Deleted == false)
            {
                Plinth.Delete();
            }
        }

        protected override void OnMapChange(Map oldMap)
        {
            InvalidatePose();

            if (Plinth != null)
            {
                Plinth.Map = Map;
            }
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            InvalidatePose();

            if (Plinth != null)
            {
                Plinth.Location = new Point3D(X, Y, Z - 5);
            }
        }

        public override bool CanBeRenamedBy(Mobile from) => false;

        public override bool CanBeDamaged() => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRequestedAnimation(Mobile from)
        {
            from.NetState.SendStatueAnimation(Serial, 1, m_Animation, m_Frames);
        }

        public override void OnAosSingleClick(Mobile from)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write((int)m_Type);
            writer.Write((int)m_Pose);
            writer.Write((int)m_Material);

            writer.Write(m_SculptedBy);
            writer.Write(SculptedOn);

            writer.Write(Plinth);
            writer.Write(IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Type = (StatueType)reader.ReadInt();
            m_Pose = (StatuePose)reader.ReadInt();
            m_Material = (StatueMaterial)reader.ReadInt();

            m_SculptedBy = reader.ReadEntity<Mobile>();
            SculptedOn = reader.ReadDateTime();

            Plinth = reader.ReadEntity<CharacterStatuePlinth>();
            IsRewardItem = reader.ReadBool();

            InvalidatePose();

            Frozen = true;

            if (m_SculptedBy == null || Map == Map.Internal) // Remove preview statues
            {
                Timer.StartTimer(Delete);
            }
        }

        public void Sculpt(Mobile by)
        {
            m_SculptedBy = by;
            SculptedOn = Core.Now;

            InvalidateProperties();
        }

        public bool Demolish(Mobile by)
        {
            var deed = new CharacterStatueDeed(null);

            if (by.PlaceInBackpack(deed))
            {
                Delete();

                deed.Statue = this;
                deed.StatueType = m_Type;
                deed.IsRewardItem = IsRewardItem;

                Plinth?.Delete();

                return true;
            }

            by.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
            deed.Delete();

            return false;
        }

        public void Restore(CharacterStatue from)
        {
            m_Material = from.Material;
            m_Pose = from.Pose;

            Direction = from.Direction;

            CloneBody(from);
            CloneClothes(from);

            InvalidateHues();
            InvalidatePose();
        }

        public void CloneBody(Mobile from)
        {
            Name = from.Name;
            Body = from.Body;
            Female = from.Female;
            HairItemID = from.HairItemID;
            FacialHairItemID = from.FacialHairItemID;
        }

        public void CloneClothes(Mobile from)
        {
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                Items[i].Delete();
            }

            for (var i = from.Items.Count - 1; i >= 0; i--)
            {
                var item = from.Items[i];

                if (item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank)
                {
                    AddItem(CloneItem(item));
                }
            }
        }

        public Item CloneItem(Item item)
        {
            var cloned = new Item(item.ItemID)
            {
                Layer = item.Layer,
                Name = item.Name,
                Hue = item.Hue,
                Weight = item.Weight,
                Movable = false
            };

            return cloned;
        }

        public void InvalidateHues()
        {
            Hue = 0xB8F + (int)m_Type * 4 + (int)m_Material;

            HairHue = Hue;

            if (FacialHairItemID > 0)
            {
                FacialHairHue = Hue;
            }

            for (var i = Items.Count - 1; i >= 0; i--)
            {
                Items[i].Hue = Hue;
            }

            Plinth?.InvalidateHue();
        }

        public void InvalidatePose()
        {
            switch (m_Pose)
            {
                case StatuePose.Ready:
                    m_Animation = 4;
                    m_Frames = 0;
                    break;
                case StatuePose.Casting:
                    m_Animation = 16;
                    m_Frames = 2;
                    break;
                case StatuePose.Salute:
                    m_Animation = 33;
                    m_Frames = 1;
                    break;
                case StatuePose.AllPraiseMe:
                    m_Animation = 17;
                    m_Frames = 4;
                    break;
                case StatuePose.Fighting:
                    m_Animation = 31;
                    m_Frames = 5;
                    break;
                case StatuePose.HandsOnHips:
                    m_Animation = 6;
                    m_Frames = 1;
                    break;
            }

            if (Map == null)
            {
                return;
            }

            ProcessDelta();

            var eable = Map.GetClientsInRange(Location);
            Span<byte> animPacket = stackalloc byte[CharacterStatuePackets.StatueAnimationPacketLength].InitializePacket();

            foreach (var state in eable)
            {
                state.Mobile.ProcessDelta();
                CharacterStatuePackets.CreateStatueAnimation(animPacket, Serial, 1, m_Animation, m_Frames);
                state.Send(animPacket);
            }

            eable.Free();
        }

        private class DemolishEntry : ContextMenuEntry
        {
            private readonly CharacterStatue m_Statue;

            public DemolishEntry(CharacterStatue statue) : base(6275, 2) => m_Statue = statue;

            public override void OnClick()
            {
                if (!m_Statue.Deleted)
                {
                    m_Statue.Demolish(Owner.From);
                }
            }
        }
    }

    public class CharacterStatueDeed : Item, IRewardItem
    {
        private bool m_IsRewardItem;

        private StatueType m_Type;

        public CharacterStatueDeed(CharacterStatue statue) : base(0x14F0)
        {
            Statue = statue;

            if (statue != null)
            {
                m_Type = statue.StatueType;
                m_IsRewardItem = statue.IsRewardItem;
            }

            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public CharacterStatueDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber
        {
            get
            {
                var t = m_Type;

                if (Statue != null)
                {
                    t = Statue.StatueType;
                }

                return t switch
                {
                    StatueType.Marble => 1076189,
                    StatueType.Jade   => 1076188,
                    StatueType.Bronze => 1076190,
                    _                 => 1076173
                };
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CharacterStatue Statue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public StatueType StatueType
        {
            get
            {
                if (Statue != null)
                {
                    return Statue.StatueType;
                }

                return m_Type;
            }
            set => m_Type = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get => m_IsRewardItem;
            set
            {
                m_IsRewardItem = value;
                InvalidateProperties();
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsRewardItem)
            {
                list.Add(1076222); // 6th Year Veteran Reward
            }

            if (Statue != null)
            {
                list.Add(1076231, Statue.Name); // Statue of ~1_Name~
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Account is Account acct && from.AccessLevel == AccessLevel.Player)
            {
                var time = TimeSpan.FromDays(RewardSystem.RewardInterval.TotalDays * 6) - acct.AccountAge;

                if (time > TimeSpan.Zero)
                {
                    from.SendLocalizedMessage(
                        1008126,
                        true,
                        Math.Ceiling(time.TotalDays / RewardSystem.RewardInterval.TotalDays)
                            .ToString()
                    ); // Your account is not old enough to use this item. Months until you can use this item :
                    return;
                }
            }

            if (IsChildOf(from.Backpack))
            {
                if (!from.IsBodyMod)
                {
                    from.SendLocalizedMessage(1076194); // Select a place where you would like to put your statue.
                    from.Target = new CharacterStatueTarget(this, StatueType);
                }
                else
                {
                    from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            Statue?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version

            writer.Write((int)m_Type);

            writer.Write(Statue);
            writer.Write(m_IsRewardItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version >= 1)
            {
                m_Type = (StatueType)reader.ReadInt();
            }

            Statue = reader.ReadEntity<CharacterStatue>();
            m_IsRewardItem = reader.ReadBool();
        }
    }

    public class CharacterStatueTarget : Target
    {
        private readonly Item m_Maker;
        private readonly StatueType m_Type;

        public CharacterStatueTarget(Item maker, StatueType type) : base(-1, true, TargetFlags.None)
        {
            m_Maker = maker;
            m_Type = type;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            var p = targeted as IPoint3D;
            var map = from.Map;

            if (p == null || map == null || m_Maker?.Deleted != false)
            {
                return;
            }

            if (!m_Maker.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            SpellHelper.GetSurfaceTop(ref p);
            BaseHouse house = null;
            var loc = new Point3D(p);

            if (targeted is Item item && !item.IsLockedDown && !item.IsSecure && item is not AddonComponent)
            {
                from.SendLocalizedMessage(1076191); // Statues can only be placed in houses.
                return;
            }

            if (from.IsBodyMod)
            {
                from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
                return;
            }

            var result = CouldFit(loc, map, from, ref house);

            if (result == AddonFitResult.Valid)
            {
                var statue = new CharacterStatue(from, m_Type);
                var plinth = new CharacterStatuePlinth(statue);

                house.Addons.Add(plinth);

                if (m_Maker is IRewardItem rewardItem)
                {
                    statue.IsRewardItem = rewardItem.IsRewardItem;
                }

                statue.Plinth = plinth;
                plinth.MoveToWorld(loc, map);
                statue.InvalidatePose();

                /*
                     * TODO: Previously the maker wasn't deleted until after statue
                     * customization, leading to redeeding issues. Exact OSI behavior
                     * needs looking into.
                     */
                m_Maker.Delete();
                statue.Sculpt(from);

                from.CloseGump<CharacterStatueGump>();
                from.SendGump(new CharacterStatueGump(m_Maker, statue, from));
                return;
            }

            if (result == AddonFitResult.Blocked)
            {
                from.SendLocalizedMessage(500269); // You cannot build that there.
                return;
            }

            if (result == AddonFitResult.NotInHouse)
            {
                // Statues can only be placed in houses where you are the owner or co-owner.
                from.SendLocalizedMessage(1076192);
                return;
            }

            if (result == AddonFitResult.DoorTooClose)
            {
                from.SendLocalizedMessage(500271); // You cannot build near the door.
            }
        }

        public static AddonFitResult CouldFit(Point3D p, Map map, Mobile from, ref BaseHouse house)
        {
            if (!map.CanFit(p.X, p.Y, p.Z, 20, true))
            {
                return AddonFitResult.Blocked;
            }

            if (!BaseAddon.CheckHouse(from, p, map, 20, out house))
            {
                return AddonFitResult.NotInHouse;
            }

            return CheckDoors(p, 20, house);
        }

        public static AddonFitResult CheckDoors(Point3D p, int height, BaseHouse house)
        {
            var doors = house.Doors;

            for (var i = 0; i < doors.Count; i++)
            {
                var door = doors[i];

                var doorLoc = door.GetWorldLocation();
                var doorHeight = door.ItemData.CalcHeight;

                if (Utility.InRange(doorLoc, p, 1) &&
                    (p.Z == doorLoc.Z || p.Z + height > doorLoc.Z && doorLoc.Z + doorHeight > p.Z))
                {
                    return AddonFitResult.DoorTooClose;
                }
            }

            return AddonFitResult.Valid;
        }
    }
}
