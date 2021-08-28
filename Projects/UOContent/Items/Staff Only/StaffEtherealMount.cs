/*
 * Original Author: snicker7 (GMEthereal.cs)
 * Released: 03/26/2006
 * Updated for ModernUO by Kamron
*/

using System.Collections.Generic;

namespace Server.Mobiles
{
    public enum EtherealType
    {
        Horse,
        Llama,
        Ostard,
        DesertOstard,
        FrenziedOstard,
        Ridgeback,
        Unicorn,
        Beetle,
        Kirin,
        SwampDragon,
        SkeletalHorse,
        Hiryu,
        ChargerOfTheFallen,
        SeaHorse,
        Chimera,
        CuSidhe,
        PolarBear,
        Boura
    }

    [Serializable(0)]
    public partial class StaffEtherealMount : EtherealMount
    {
        public override int FollowerSlots => 0;

        private static readonly Dictionary<EtherealType, EtherealInfo> EtherealTypes = new() {
            { EtherealType.Horse, new EtherealInfo(0x20DD, 0x3EAA) },
            { EtherealType.Llama, new EtherealInfo(0x20F6, 0x3EAB) },
            { EtherealType.Ostard, new EtherealInfo(0x2135, 0x3EAC) },
            { EtherealType.DesertOstard, new EtherealInfo(8501, 16035) },
            { EtherealType.FrenziedOstard, new EtherealInfo(8502, 16036) },
            { EtherealType.Ridgeback, new EtherealInfo(0x2615, 0x3E9A) },
            { EtherealType.Unicorn, new EtherealInfo(0x25CE, 0x3E9B) },
            { EtherealType.Beetle, new EtherealInfo(0x260F, 0x3E97) },
            { EtherealType.Kirin, new EtherealInfo(0x25A0, 0x3E9C) },
            { EtherealType.SwampDragon, new EtherealInfo(0x2619, 0x3E98) },
            { EtherealType.SkeletalHorse, new EtherealInfo(9751, 16059) },
            { EtherealType.Hiryu, new EtherealInfo(10090, 16020) },
            { EtherealType.ChargerOfTheFallen, new EtherealInfo(11676, 16018) },
            { EtherealType.SeaHorse, new EtherealInfo(9658, 16051) },
            { EtherealType.Chimera, new EtherealInfo(11669, 16016) },
            { EtherealType.CuSidhe, new EtherealInfo(11670, 16017) },
            { EtherealType.PolarBear, new EtherealInfo(8417, 16069) },
            { EtherealType.Boura, new EtherealInfo(0x46f8, 0x3EC6) },
        };

        [SerializableField(0)]
        private EtherealType _etherealType;

        [Constructible]
        public StaffEtherealMount() : this(EtherealType.Horse)
        {
        }

        [Constructible]
        public StaffEtherealMount(EtherealType type) : base(0, 0)
        {
            EthyType = type;
            LootType = LootType.Blessed;
            Name = "Staff Ethereal";
            Visible = false;
        }

        [CommandProperty(AccessLevel.Counselor)]
        public EtherealType EthyType
        {
            get => _etherealType;
            set
            {
                if (!EtherealTypes.TryGetValue(value, out var etherealInfo))
                {
                    return;
                }

                EtherealType = value;

                MountedID = etherealInfo._mountedId;
                RegularID = etherealInfo._regularId;
            }
        }

        public override bool IsAccessibleTo(Mobile from) => from.AccessLevel >= AccessLevel.GameMaster;

        public override void OnDoubleClickNotAccessible(Mobile from)
        {
            if (from.AccessLevel != AccessLevel.Player)
            {
                base.OnDoubleClickNotAccessible(from);
                return;
            }

            from.SendMessage("It vanishes without a trace.");
            Delete();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted || Rider != null || !Multis.DesignContext.Check(from))
            {
                return;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(1005583); // Please dismount first.
            }
            else if (from.Race == Race.Gargoyle)
            {
                from.SendLocalizedMessage(1112281); // Gargoyles can't mount
            }
            else if (from.HasTrade)
            {
                from.SendLocalizedMessage(1042317, "", 0x41); // You may not ride at this time
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1080063); // This must be in your backpack to use it.
            }
            else
            {
                Rider = from;

                if (_etherealType == EtherealType.SeaHorse)
                {
                    Rider.CanSwim = true;
                }
            }
        }

        private struct EtherealInfo
        {
            public readonly int _regularId;
            public readonly int _mountedId;
            public EtherealInfo(int id, int mid)
            {
                _regularId = id;
                _mountedId = mid;
            }
        }
    }
}
