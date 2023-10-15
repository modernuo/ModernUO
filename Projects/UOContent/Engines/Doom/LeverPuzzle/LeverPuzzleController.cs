using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Engines.Doom;

[SerializationGenerator(0, false)]
public partial class LeverPuzzleController : Item
{
    private static bool installed;

    public static string[] Msgs =
    {
        "You are pinned down by the weight of the boulder!!!", // 0
        "A speeding rock hits you in the head!",               // 1
        "OUCH!"                                                // 2
    };
    /* font&hue for above msgs. index matches */

    public static int[][] MsgParams =
    {
        new[] { 0x66d, 3 },
        new[] { 0x66d, 3 },
        new[] { 0x34, 3 }
    };
    /* World data for items */

    public static int[][] TA =
    {
        new[] { 316, 64, 5 }, /* 3D Coords for levers */
        new[] { 323, 58, 5 },
        new[] { 332, 63, 5 },
        new[] { 323, 71, 5 },

        new[] { 324, 64 }, /* 2D Coords for standing regions */
        new[] { 316, 65 },
        new[] { 324, 58 },
        new[] { 332, 64 },
        new[] { 323, 72 },

        new[] { 468, 92, -1 }, new[] { 0x181D, 0x482 }, /* 3D coord, itemid+hue for L.R. teles */
        new[] { 469, 92, -1 }, new[] { 0x1821, 0x3fd },
        new[] { 470, 92, -1 }, new[] { 0x1825, 0x66d },

        new[] { 319, 70, 18 }, new[] { 0x12d8 }, /* 3D coord, itemid for statues */
        new[] { 329, 60, 18 }, new[] { 0x12d9 },

        new[] { 469, 96, 6 } /* 3D Coords for Fake Box */
    };

    /* CLILOC data for statue "correct souls" messages */

    public static int[] Statue_Msg = { 1050009, 1050007, 1050008, 1050008 };

    /* Exit & Enter locations for the lamp room */

    public static Point3D lr_Exit = new(353, 172, -1);
    public static Point3D lr_Enter = new(467, 96, -1);

    /* "Center" location in puzzle */

    public static Point3D lp_Center = new(324, 64, -1);

    /* Lamp Room Area */

    public static Rectangle2D lr_Rect = new(465, 92, 10, 10);

    /* Lamp Room area Poison message data */

    public static int[][] PA =
    {
        new[] { 0, 0, 0xA6 },
        new[] { 1050001, 0x485, 0xAA },
        new[] { 1050003, 0x485, 0xAC },
        new[] { 1050056, 0x485, 0xA8 },
        new[] { 1050057, 0x485, 0xA4 },
        new[] { 1062091, 0x23F3, 0xAC }
    };

    public static Poison[] PA2 =
    {
        Poison.Lesser,
        Poison.Regular,
        Poison.Greater,
        Poison.Deadly,
        Poison.Lethal,
        Poison.Lethal
    };

    /* SOUNDS */

    private static readonly int[] fs = { 0x144, 0x154 };
    private static readonly int[] ms = { 0x144, 0x14B };
    private static readonly int[] fs2 = { 0x13F, 0x154 };
    private static readonly int[] ms2 = { 0x13F, 0x14B };
    private static readonly int[] cs1 = { 0x244 };
    private static readonly int[] exp = { 0x307 };
    private TimerExecutionToken _resetTimerToken;
    private Region _lampRoom;

    [SerializableField(0, getter: "private", setter: "private")]
    private List<Item> _levers;

    [SerializableField(1, getter: "private", setter: "private")]
    private List<Item> _statues;

    [SerializableField(2, getter: "private", setter: "private")]
    private List<Item> _teles;

    [SerializableField(3, getter: "private", setter: "private")]
    private LampRoomBox _box;

    private List<LeverPuzzleRegion> _tiles;

    private Timer m_Timer;

    public LeverPuzzleController() : base(0x1822)
    {
        Movable = false;
        Hue = 0x4c;
        installed = true;
        var i = 0;

        _levers = new List<Item>(); /* codes are 0x1 shifted left x # of bits, easily handled here */
        for (; i < 4; i++)
        {
            _levers.Add(AddLeverPuzzlePart(TA[i], new LeverPuzzleLever((ushort)(1 << i), this)));
        }

        _tiles = new List<LeverPuzzleRegion>();
        for (; i < 9; i++)
        {
            _tiles.Add(new LeverPuzzleRegion(TA[i]));
        }

        _teles = new List<Item>();
        for (; i < 15; i++)
        {
            _teles.Add(AddLeverPuzzlePart(TA[i], new LampRoomTeleporter(TA[++i])));
        }

        _statues = new List<Item>();
        for (; i < 19; i++)
        {
            _statues.Add(AddLeverPuzzlePart(TA[i], new LeverPuzzleStatue(TA[++i], this)));
        }

        if (!installed)
        {
            Delete();
        }
        else
        {
            Enabled = true;
        }

        _box = (LampRoomBox)AddLeverPuzzlePart(TA[i], new LampRoomBox(this));
        _lampRoom = new LampRoomRegion(this);
        GenKey();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public ushort MyKey { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public ushort TheirKey { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Enabled { get; set; }

    public Mobile Successful { get; private set; }

    public bool CircleComplete
    {
        get /* OSI: all 5 must be occupied */
        {
            for (var i = 0; i < 5; i++)
            {
                if (GetOccupant(i) == null)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static void Initialize()
    {
        CommandSystem.Register("GenLeverPuzzle", AccessLevel.Administrator, GenLampPuzzle_OnCommand);
    }

    [Usage("GenLeverPuzzle"), Description("Generates lamp room and lever puzzle in doom.")]
    public static void GenLampPuzzle_OnCommand(CommandEventArgs e)
    {
        var eable = Map.Malas.GetItemsInRange(lp_Center, 0);

        foreach (var item in eable)
        {
            if (item is LeverPuzzleController)
            {
                e.Mobile.SendMessage("Lamp room puzzle already exists: please delete the existing controller first ...");
                return;
            }
        }

        e.Mobile.SendMessage("Generating Lamp Room puzzle...");

        NetState.FlushAll();

        new LeverPuzzleController().MoveToWorld(lp_Center, Map.Malas);

        e.Mobile.SendMessage(
            !installed ? "There was a problem generating the puzzle." : "Lamp room puzzle successfully generated."
        );
    }

    public static Item AddLeverPuzzlePart(int[] loc, Item newitem)
    {
        if (newitem?.Deleted != false)
        {
            installed = false;
        }
        else
        {
            newitem.MoveToWorld(new Point3D(loc[0], loc[1], loc[2]), Map.Malas);
        }

        return newitem;
    }

    public override void OnDelete()
    {
        KillTimers();
        base.OnDelete();
    }

    public override void OnAfterDelete()
    {
        NukeItemList(_teles);
        NukeItemList(_statues);
        NukeItemList(_levers);

        _lampRoom?.Unregister();
        if (_tiles != null)
        {
            foreach (var region in _tiles)
            {
                region.Unregister();
            }
        }

        if (_box?.Deleted == false)
        {
            _box.Delete();
        }
    }

    public static void NukeItemList(List<Item> list)
    {
        if (list?.Count > 0)
        {
            foreach (var item in list)
            {
                if (item?.Deleted == false)
                {
                    item.Delete();
                }
            }
        }
    }

    public virtual PlayerMobile GetOccupant(int index)
    {
        var region = _tiles[index];

        if (region?.Occupant?.Alive == true)
        {
            return (PlayerMobile)region.Occupant;
        }

        return null;
    }

    public virtual LeverPuzzleStatue GetStatue(int index)
    {
        var statue = (LeverPuzzleStatue)_statues[index];
        return statue?.Deleted == false ? statue : null;
    }

    public virtual LeverPuzzleLever GetLever(int index)
    {
        var lever = (LeverPuzzleLever)_levers[index];

        return lever?.Deleted == false ? lever : null;
    }

    public virtual void PuzzleStatus(int message, string fstring = null)
    {
        for (var i = 0; i < 2; i++)
        {
            Item s;
            if ((s = GetStatue(i)) != null)
            {
                s.PublicOverheadMessage(MessageType.Regular, 0x3B2, message, fstring);
            }
        }
    }

    public virtual void ResetPuzzle()
    {
        PuzzleStatus(1062053);
        ResetLevers();
    }

    public virtual void ResetLevers()
    {
        for (var i = 0; i < 4; i++)
        {
            Item l;
            if ((l = GetLever(i)) != null)
            {
                l.ItemID = 0x108E;
                Effects.PlaySound(l.Location, Map, 0x3E8);
            }
        }

        TheirKey ^= TheirKey;
    }

    public virtual void KillTimers()
    {
        _resetTimerToken.Cancel();
        m_Timer?.Stop();
    }

    public virtual void RemoveSuccessful()
    {
        Successful = null;
    }

    public virtual void LeverPulled(ushort code)
    {
        var correct = 0;

        KillTimers();

        /* if one bit in each of the four nibbles is set, this is false */

        if ((TheirKey = (ushort)(code | (TheirKey <<= 4))) < 0x0FFF)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(30.0), ResetPuzzle, out _resetTimerToken);
            return;
        }

        if (!CircleComplete)
        {
            PuzzleStatus(1050004); // The circle is the key...
        }
        else
        {
            Mobile player;
            if (TheirKey == MyKey)
            {
                GenKey();
                if ((Successful = player = GetOccupant(0)) != null)
                {
                    SendLocationEffect(lp_Center, 0x1153, 0, 60, 1);
                    PlaySounds(lp_Center, cs1);

                    Effects.SendBoltEffect(player);
                    player.MoveToWorld(lr_Enter, Map.Malas);

                    m_Timer = new LampRoomTimer(this);
                    m_Timer.Start();
                    Enabled = false;
                }
            }
            else
            {
                for (var i = 0; i < 16; i++) /* Count matching SET bits, ie correct codes */
                {
                    if (((MyKey >> i) & 1) == 1 && ((TheirKey >> i) & 1) == 1)
                    {
                        correct++;
                    }
                }

                PuzzleStatus(Statue_Msg[correct], correct > 0 ? correct.ToString() : null);

                for (var i = 0; i < 5; i++)
                {
                    if ((player = GetOccupant(i)) != null)
                    {
                        new RockTimer(player).Start();
                    }
                }
            }
        }

        ResetLevers();
    }

    public virtual void GenKey()
    {
        Span<ushort> ca = stackalloc ushort[] { 1, 2, 4, 8 };
        ca.Shuffle();

        for (var i = 0; i < 4; i++)
        {
            MyKey = (ushort)(ca[i] | (MyKey <<= 4));
        }
    }

    private static bool IsValidDamagable(Mobile m) =>
        m?.Deleted == false &&
        (m.Player && m.Alive ||
         m is BaseCreature bc && (bc.Controlled || bc.Summoned) && !bc.IsDeadBondedPet);

    public static void MoveMobileOut(Mobile m)
    {
        if (m != null)
        {
            if (m is PlayerMobile && !m.Alive && m.Corpse?.Deleted == false)
            {
                m.Corpse.MoveToWorld(lr_Exit, Map.Malas);
            }

            BaseCreature.TeleportPets(m, lr_Exit, Map.Malas);
            m.Location = lr_Exit;
            m.ProcessDelta();
        }
    }

    public static bool AniSafe(Mobile m) =>
        m?.BodyMod == 0 && m.Alive && !TransformationSpellHelper.UnderTransformation(m);

    public static IEntity ZAdjustedIEFromMobile(Mobile m, int zDelta) => new Entity(
        Serial.Zero,
        new Point3D(m.X, m.Y, m.Z + zDelta),
        m.Map
    );

    public static void DoDamage(Mobile m, int min, int max, bool poison)
    {
        if (m?.Deleted == false && m.Alive)
        {
            var damage = Utility.Random(min, max);
            AOS.Damage(m, damage, poison ? 0 : 100, 0, 0, poison ? 100 : 0, 0);
        }
    }

    public static Point3D RandomPointIn(Point3D point, int range) => RandomPointIn(
        point.X - range,
        point.Y - range,
        range * 2,
        range * 2,
        point.Z
    );

    public static Point3D RandomPointIn(Rectangle2D rect, int z) =>
        RandomPointIn(rect.X, rect.Y, rect.Height, rect.Width, z);

    public static Point3D RandomPointIn(int x, int y, int x2, int y2, int z) =>
        new(Utility.Random(x, x2), Utility.Random(y, y2), z);

    public static void PlaySounds(Point3D location, int[] sounds)
    {
        foreach (var soundid in sounds)
        {
            Effects.PlaySound(location, Map.Malas, soundid);
        }
    }

    private static void PlayEffect(IEntity from, IEntity to, int itemid, int speed, bool explodes)
    {
        Effects.SendMovingParticles(from, to, itemid, speed, 0, true, explodes, 2, 0, 0);
    }

    private static void SendLocationEffect(Point3D p, int itemID, int speed, int duration, int hue)
    {
        Effects.SendLocationEffect(p, Map.Malas, itemID, speed, duration, hue);
    }

    public static void PlayerSendASCII(Mobile player, int index)
    {
        player.NetState.SendMessage(
            Serial.MinusOne,
            0xFFFF,
            MessageType.Label,
            MsgParams[index][0],
            MsgParams[index][1],
            true,
            null,
            null,
            Msgs[index]
        );
    }

    /* I cant find any better way to send "speech" using fonts other than default */
    public static void POHMessage(Mobile from, int index)
    {
        Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(Msgs[index])].InitializePacket();

        var eable = from.Map.GetClientsInRange(from.Location);

        foreach (var state in eable)
        {
            var length = OutgoingMessagePackets.CreateMessage(
                buffer,
                from.Serial,
                from.Body,
                MessageType.Regular,
                MsgParams[index][0],
                MsgParams[index][1],
                true,
                null,
                from.Name,
                Msgs[index]
            );

            if (length != buffer.Length)
            {
                buffer = buffer[..length]; // Adjust to the actual size
            }

            state.Send(buffer);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _levers = reader.ReadEntityList<Item>();
        _statues = reader.ReadEntityList<Item>();
        _teles = reader.ReadEntityList<Item>();
        _box = reader.ReadEntity<LampRoomBox>();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _tiles = new List<LeverPuzzleRegion>();
        for (var i = 4; i < 9; i++)
        {
            _tiles.Add(new LeverPuzzleRegion(TA[i]));
        }

        _lampRoom = new LampRoomRegion(this);
        Enabled = true;
        TheirKey = 0;
        MyKey = 0;
        GenKey();
    }

    public class RockTimer : Timer
    {
        private readonly Mobile m_Player;
        private int Count;

        public RockTimer(Mobile player) : base(TimeSpan.Zero, TimeSpan.FromSeconds(.25))
        {
            Count = 0;
            m_Player = player;
        }

        private int Rock() => 0x1363 + Utility.Random(0, 11);

        protected override void OnTick()
        {
            if (m_Player == null || m_Player.Map != Map.Malas)
            {
                Stop();
            }
            else
            {
                Count++;
                if (Count == 1) /* TODO consolidate */
                {
                    m_Player.Paralyze(TimeSpan.FromSeconds(2));
                    Effects.SendTargetEffect(m_Player, 0x11B7, 20, 10);
                    PlayerSendASCII(m_Player, 0); // You are pinned down ...

                    PlaySounds(m_Player.Location, !m_Player.Female ? fs : ms);
                    PlayEffect(ZAdjustedIEFromMobile(m_Player, 50), m_Player, 0x11B7, 20, false);
                }
                else if (Count == 2)
                {
                    DoDamage(m_Player, 80, 90, false);
                    Effects.SendTargetEffect(m_Player, 0x36BD, 20, 10);
                    PlaySounds(m_Player.Location, exp);
                    PlayerSendASCII(m_Player, 1); // A speeding rock  ...

                    if (AniSafe(m_Player))
                    {
                        m_Player.Animate(21, 10, 1, true, true, 0);
                    }
                }
                else if (Count == 3)
                {
                    Stop();

                    Effects.SendTargetEffect(m_Player, 0x36B0, 20, 10);
                    PlayerSendASCII(m_Player, 1); // A speeding rock  ...
                    PlaySounds(m_Player.Location, !m_Player.Female ? fs2 : ms2);

                    var j = Utility.Random(6, 10);
                    for (var i = 0; i < j; i++)
                    {
                        IEntity m_IEntity = new Entity(Serial.Zero, RandomPointIn(m_Player.Location, 10), m_Player.Map);

                        var eable = m_IEntity.Map.GetMobilesInRange(m_IEntity.Location, 2);
                        var mobiles = new List<Mobile>();
                        mobiles.AddRange(eable);

                        for (var k = 0; k < mobiles.Count; k++)
                        {
                            if (IsValidDamagable(mobiles[k]) && mobiles[k] != m_Player)
                            {
                                PlayEffect(m_Player, mobiles[k], Rock(), 8, true);
                                DoDamage(mobiles[k], 25, 30, false);

                                if (mobiles[k].Player)
                                {
                                    POHMessage(mobiles[k], 2); // OUCH!
                                }
                            }
                        }

                        PlayEffect(m_Player, m_IEntity, Rock(), 8, false);
                    }
                }
            }
        }
    }

    public class LampRoomKickTimer : Timer
    {
        private readonly Mobile m;

        public LampRoomKickTimer(Mobile player) : base(TimeSpan.FromSeconds(.25)) => m = player;

        protected override void OnTick()
        {
            MoveMobileOut(m);
        }
    }

    public class LampRoomTimer : Timer
    {
        public int level;
        public LeverPuzzleController m_Controller;
        public int ticks;

        public LampRoomTimer(LeverPuzzleController controller)
            : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
        {
            level = 0;
            ticks = 0;
            m_Controller = controller;
        }

        protected override void OnTick()
        {
            ticks++;
            var mobiles = m_Controller._lampRoom.GetMobiles();

            if (ticks >= 71 || m_Controller._lampRoom.GetPlayerCount() == 0)
            {
                foreach (var mobile in mobiles)
                {
                    if (mobile?.Deleted == false && !mobile.IsDeadBondedPet)
                    {
                        mobile.Kill();
                    }
                }

                m_Controller.Enabled = true;
                Stop();
            }
            else
            {
                if (ticks % 12 == 0)
                {
                    level++;
                }

                foreach (var mobile in mobiles)
                {
                    if (IsValidDamagable(mobile))
                    {
                        if (ticks % 2 == 0 && level == 5)
                        {
                            if (mobile.Player)
                            {
                                mobile.Say(1062092);
                                if (AniSafe(mobile))
                                {
                                    mobile.Animate(32, 5, 1, true, false, 0);
                                }
                            }

                            DoDamage(mobile, 15, 20, true);
                        }

                        if (Utility.Random((int)(level & ~0xfffffffc), 3) == 3)
                        {
                            mobile.ApplyPoison(mobile, PA2[level]);
                        }

                        if (ticks % 12 == 0 && level > 0 && mobile.Player)
                        {
                            mobile.SendLocalizedMessage(PA[level][0], null, PA[level][1]);
                        }
                    }
                }

                for (var i = 0; i <= level; i++)
                {
                    SendLocationEffect(RandomPointIn(lr_Rect, -1), 0x36B0, Utility.Random(150, 200), 0, PA[level][2]);
                }
            }
        }
    }
}
