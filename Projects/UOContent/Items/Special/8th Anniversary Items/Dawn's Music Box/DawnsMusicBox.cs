using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    [Flippable(0x2AF9, 0x2AFD)]
    public class DawnsMusicBox : Item, ISecurable
    {
        private static readonly Dictionary<MusicName, DawnsMusicInfo> m_Info = new()
        {
            { MusicName.Samlethe, new DawnsMusicInfo(1075152, DawnsMusicRarity.Common) },
            { MusicName.Sailing, new DawnsMusicInfo(1075163, DawnsMusicRarity.Common) },
            { MusicName.Britain2, new DawnsMusicInfo(1075145, DawnsMusicRarity.Common) },
            { MusicName.Britain1, new DawnsMusicInfo(1075144, DawnsMusicRarity.Common) },
            { MusicName.Bucsden, new DawnsMusicInfo(1075146, DawnsMusicRarity.Common) },
            { MusicName.Forest_a, new DawnsMusicInfo(1075161, DawnsMusicRarity.Common) },
            { MusicName.Cove, new DawnsMusicInfo(1075176, DawnsMusicRarity.Common) },
            { MusicName.Death, new DawnsMusicInfo(1075171, DawnsMusicRarity.Common) },
            { MusicName.Dungeon9, new DawnsMusicInfo(1075160, DawnsMusicRarity.Common) },
            { MusicName.Dungeon2, new DawnsMusicInfo(1075175, DawnsMusicRarity.Common) },
            { MusicName.Cave01, new DawnsMusicInfo(1075159, DawnsMusicRarity.Common) },
            { MusicName.Combat3, new DawnsMusicInfo(1075170, DawnsMusicRarity.Common) },
            { MusicName.Combat1, new DawnsMusicInfo(1075168, DawnsMusicRarity.Common) },
            { MusicName.Combat2, new DawnsMusicInfo(1075169, DawnsMusicRarity.Common) },
            { MusicName.Jhelom, new DawnsMusicInfo(1075147, DawnsMusicRarity.Common) },
            { MusicName.Linelle, new DawnsMusicInfo(1075185, DawnsMusicRarity.Common) },
            { MusicName.LBCastle, new DawnsMusicInfo(1075148, DawnsMusicRarity.Common) },
            { MusicName.Minoc, new DawnsMusicInfo(1075150, DawnsMusicRarity.Common) },
            { MusicName.Moonglow, new DawnsMusicInfo(1075177, DawnsMusicRarity.Common) },
            { MusicName.Magincia, new DawnsMusicInfo(1075149, DawnsMusicRarity.Common) },
            { MusicName.Nujelm, new DawnsMusicInfo(1075174, DawnsMusicRarity.Common) },
            { MusicName.BTCastle, new DawnsMusicInfo(1075173, DawnsMusicRarity.Common) },
            { MusicName.Tavern04, new DawnsMusicInfo(1075167, DawnsMusicRarity.Common) },
            { MusicName.Skarabra, new DawnsMusicInfo(1075154, DawnsMusicRarity.Common) },
            { MusicName.Stones2, new DawnsMusicInfo(1075143, DawnsMusicRarity.Common) },
            { MusicName.Serpents, new DawnsMusicInfo(1075153, DawnsMusicRarity.Common) },
            { MusicName.Taiko, new DawnsMusicInfo(1075180, DawnsMusicRarity.Common) },
            { MusicName.Tavern01, new DawnsMusicInfo(1075164, DawnsMusicRarity.Common) },
            { MusicName.Tavern02, new DawnsMusicInfo(1075165, DawnsMusicRarity.Common) },
            { MusicName.Tavern03, new DawnsMusicInfo(1075166, DawnsMusicRarity.Common) },
            { MusicName.TokunoDungeon, new DawnsMusicInfo(1075179, DawnsMusicRarity.Common) },
            { MusicName.Trinsic, new DawnsMusicInfo(1075155, DawnsMusicRarity.Common) },
            { MusicName.OldUlt01, new DawnsMusicInfo(1075142, DawnsMusicRarity.Common) },
            { MusicName.Ocllo, new DawnsMusicInfo(1075151, DawnsMusicRarity.Common) },
            { MusicName.Vesper, new DawnsMusicInfo(1075156, DawnsMusicRarity.Common) },
            { MusicName.Victory, new DawnsMusicInfo(1075172, DawnsMusicRarity.Common) },
            { MusicName.Mountn_a, new DawnsMusicInfo(1075162, DawnsMusicRarity.Common) },
            { MusicName.Wind, new DawnsMusicInfo(1075157, DawnsMusicRarity.Common) },
            { MusicName.Yew, new DawnsMusicInfo(1075158, DawnsMusicRarity.Common) },
            { MusicName.Zento, new DawnsMusicInfo(1075178, DawnsMusicRarity.Common) },
            { MusicName.GwennoConversation, new DawnsMusicInfo(1075131, DawnsMusicRarity.Uncommon) },
            { MusicName.DreadHornArea, new DawnsMusicInfo(1075181, DawnsMusicRarity.Uncommon) },
            { MusicName.ElfCity, new DawnsMusicInfo(1075182, DawnsMusicRarity.Uncommon) },
            { MusicName.GoodEndGame, new DawnsMusicInfo(1075132, DawnsMusicRarity.Uncommon) },
            { MusicName.GoodVsEvil, new DawnsMusicInfo(1075133, DawnsMusicRarity.Uncommon) },
            { MusicName.GreatEarthSerpents, new DawnsMusicInfo(1075134, DawnsMusicRarity.Uncommon) },
            { MusicName.GrizzleDungeon, new DawnsMusicInfo(1075186, DawnsMusicRarity.Uncommon) },
            { MusicName.Humanoids_U9, new DawnsMusicInfo(1075135, DawnsMusicRarity.Uncommon) },
            { MusicName.MelisandesLair, new DawnsMusicInfo(1075183, DawnsMusicRarity.Uncommon) },
            { MusicName.MinocNegative, new DawnsMusicInfo(1075136, DawnsMusicRarity.Uncommon) },
            { MusicName.ParoxysmusLair, new DawnsMusicInfo(1075184, DawnsMusicRarity.Uncommon) },
            { MusicName.Paws, new DawnsMusicInfo(1075137, DawnsMusicRarity.Uncommon) },
            { MusicName.SelimsBar, new DawnsMusicInfo(1075138, DawnsMusicRarity.Rare) },
            { MusicName.SerpentIsleCombat_U7, new DawnsMusicInfo(1075139, DawnsMusicRarity.Rare) },
            { MusicName.ValoriaShips, new DawnsMusicInfo(1075140, DawnsMusicRarity.Rare) }
        };

        public static readonly MusicName[] m_CommonTracks =
        {
            MusicName.Samlethe, MusicName.Sailing, MusicName.Britain2, MusicName.Britain1,
            MusicName.Bucsden, MusicName.Forest_a, MusicName.Cove, MusicName.Death,
            MusicName.Dungeon9, MusicName.Dungeon2, MusicName.Cave01, MusicName.Combat3,
            MusicName.Combat1, MusicName.Combat2, MusicName.Jhelom, MusicName.Linelle,
            MusicName.LBCastle, MusicName.Minoc, MusicName.Moonglow, MusicName.Magincia,
            MusicName.Nujelm, MusicName.BTCastle, MusicName.Tavern04, MusicName.Skarabra,
            MusicName.Stones2, MusicName.Serpents, MusicName.Taiko, MusicName.Tavern01,
            MusicName.Tavern02, MusicName.Tavern03, MusicName.TokunoDungeon, MusicName.Trinsic,
            MusicName.OldUlt01, MusicName.Ocllo, MusicName.Vesper, MusicName.Victory,
            MusicName.Mountn_a, MusicName.Wind, MusicName.Yew, MusicName.Zento
        };

        public static readonly MusicName[] m_UncommonTracks =
        {
            MusicName.GwennoConversation, MusicName.DreadHornArea, MusicName.ElfCity,
            MusicName.GoodEndGame, MusicName.GoodVsEvil, MusicName.GreatEarthSerpents,
            MusicName.GrizzleDungeon, MusicName.Humanoids_U9, MusicName.MelisandesLair,
            MusicName.MinocNegative, MusicName.ParoxysmusLair, MusicName.Paws
        };

        public static readonly MusicName[] m_RareTracks =
        {
            MusicName.SelimsBar, MusicName.SerpentIsleCombat_U7, MusicName.ValoriaShips
        };

        private int m_Count;
        private int m_ItemID;

        private TimerExecutionToken _timerToken;

        [Constructible]
        public DawnsMusicBox() : base(0x2AF9)
        {
            Weight = 1.0;

            Tracks = new List<MusicName>();

            while (Tracks.Count < 4)
            {
                var name = RandomTrack(DawnsMusicRarity.Common);

                if (!Tracks.Contains(name))
                {
                    Tracks.Add(name);
                }
            }
        }

        public DawnsMusicBox(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075198; // Dawn's Music Box

        public List<MusicName> Tracks { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is not DawnsMusicBox box)
            {
                return;
            }

            box.Tracks = new List<MusicName>();
            box.Tracks.AddRange(Tracks);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            var commonSongs = 0;
            var uncommonSongs = 0;
            var rareSongs = 0;

            for (var i = 0; i < Tracks.Count; i++)
            {
                var info = GetInfo(Tracks[i]);

                switch (info.Rarity)
                {
                    case DawnsMusicRarity.Common:
                        commonSongs++;
                        break;
                    case DawnsMusicRarity.Uncommon:
                        uncommonSongs++;
                        break;
                    case DawnsMusicRarity.Rare:
                        rareSongs++;
                        break;
                }
            }

            if (commonSongs > 0)
            {
                list.Add(1075234, commonSongs); // ~1_NUMBER~ Common Tracks
            }

            if (uncommonSongs > 0)
            {
                list.Add(1075235, uncommonSongs); // ~1_NUMBER~ Uncommon Tracks
            }

            if (rareSongs > 0)
            {
                list.Add(1075236, rareSongs); // ~1_NUMBER~ Rare Tracks
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, this, list); // Set secure level
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack) && !IsLockedDown)
            {
                from.SendLocalizedMessage(
                    1061856
                ); // You must have the item in your backpack or locked down in order to use it.
            }
            else if (IsLockedDown && !HasAccess(from))
            {
                from.SendLocalizedMessage(502436); // That is not accessible.
            }
            else
            {
                from.CloseGump<DawnsMusicBoxGump>();
                from.SendGump(new DawnsMusicBoxGump(this));
            }
        }

        public bool HasAccess(Mobile m) =>
            m.AccessLevel >= AccessLevel.GameMaster || BaseHouse.FindHouseAt(this)?.HasAccess(m) == true;

        public void PlayMusic(Mobile m, MusicName music)
        {
            if (_timerToken.Running)
            {
                EndMusic(m);
            }
            else
            {
                m_ItemID = ItemID;
            }

            m.NetState.SendPlayMusic(music);
            Timer.StartTimer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 4, Animate, out _timerToken);
        }

        public void EndMusic(Mobile m)
        {
            _timerToken.Cancel();
            m.NetState.SendStopMusic();

            if (m_Count > 0)
            {
                ItemID = m_ItemID;
            }

            m_Count = 0;
        }

        private void Animate()
        {
            m_Count++;

            if (m_Count >= 4)
            {
                m_Count = 0;
                ItemID = m_ItemID;
            }
            else
            {
                ItemID++;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Tracks.Count);

            for (var i = 0; i < Tracks.Count; i++)
            {
                writer.Write((int)Tracks[i]);
            }

            writer.Write((int)Level);
            writer.Write(m_ItemID);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            var count = reader.ReadInt();
            Tracks = new List<MusicName>();

            for (var i = 0; i < count; i++)
            {
                Tracks.Add((MusicName)reader.ReadInt());
            }

            Level = (SecureLevel)reader.ReadInt();
            m_ItemID = reader.ReadInt();
        }

        public static DawnsMusicInfo GetInfo(MusicName name)
        {
            if (m_Info == null) // sanity
            {
                return null;
            }

            m_Info.TryGetValue(name, out var info);
            return info;
        }

        public static MusicName RandomTrack(DawnsMusicRarity rarity)
        {
            var list = rarity switch
            {
                DawnsMusicRarity.Common   => m_CommonTracks,
                DawnsMusicRarity.Uncommon => m_UncommonTracks,
                DawnsMusicRarity.Rare     => m_RareTracks,
                _                         => m_CommonTracks
            };

            return list.RandomElement();
        }
    }
}
