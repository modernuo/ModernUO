using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Engines.Mahjong
{
    public class MahjongPlayers
    {
        private readonly bool[] m_InGame;
        private readonly Mobile[] m_Players;
        private readonly bool[] m_PublicHand;
        private readonly int[] m_Scores;
        private readonly List<Mobile> m_Spectators;

        public MahjongPlayers(MahjongGame game, int maxPlayers, int baseScore)
        {
            Game = game;
            m_Spectators = new List<Mobile>();

            m_Players = new Mobile[maxPlayers];
            m_InGame = new bool[maxPlayers];
            m_PublicHand = new bool[maxPlayers];
            m_Scores = new int[maxPlayers];

            for (var i = 0; i < m_Scores.Length; i++)
            {
                m_Scores[i] = baseScore;
            }
        }

        public MahjongPlayers(MahjongGame game, IGenericReader reader)
        {
            Game = game;
            m_Spectators = new List<Mobile>();

            var version = reader.ReadInt();

            var seats = reader.ReadInt();
            m_Players = new Mobile[seats];
            m_InGame = new bool[seats];
            m_PublicHand = new bool[seats];
            m_Scores = new int[seats];

            for (var i = 0; i < seats; i++)
            {
                m_Players[i] = reader.ReadEntity<Mobile>();
                m_PublicHand[i] = reader.ReadBool();
                m_Scores[i] = reader.ReadInt();
            }

            DealerPosition = reader.ReadInt();
        }

        public MahjongGame Game { get; }

        public int Seats => m_Players.Length;
        public Mobile Dealer => m_Players[DealerPosition];
        public int DealerPosition { get; private set; }

        public Mobile GetPlayer(int index)
        {
            if (index < 0 || index >= m_Players.Length)
            {
                return null;
            }

            return m_Players[index];
        }

        public int GetPlayerIndex(Mobile mobile)
        {
            for (var i = 0; i < m_Players.Length; i++)
            {
                if (m_Players[i] == mobile)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsInGameDealer(Mobile mobile)
        {
            if (Dealer != mobile)
            {
                return false;
            }

            return m_InGame[DealerPosition];
        }

        public bool IsInGamePlayer(int index)
        {
            if (index < 0 || index >= m_Players.Length || m_Players[index] == null)
            {
                return false;
            }

            return m_InGame[index];
        }

        public bool IsInGamePlayer(Mobile mobile)
        {
            var index = GetPlayerIndex(mobile);

            return IsInGamePlayer(index);
        }

        public bool IsSpectator(Mobile mobile) => m_Spectators.Contains(mobile);

        public int GetScore(int index)
        {
            if (index < 0 || index >= m_Scores.Length)
            {
                return 0;
            }

            return m_Scores[index];
        }

        public bool IsPublic(int index)
        {
            if (index < 0 || index >= m_PublicHand.Length)
            {
                return false;
            }

            return m_PublicHand[index];
        }

        public void SetPublic(int index, bool value)
        {
            if (index < 0 || index >= m_PublicHand.Length || m_PublicHand[index] == value)
            {
                return;
            }

            m_PublicHand[index] = value;

            SendTilesPacket(true, !Game.SpectatorVision);

            if (IsInGamePlayer(index))
            {
                m_Players[index].SendLocalizedMessage(value ? 1062775 : 1062776); // Your hand is [not] publicly viewable.
            }
        }

        public List<Mobile> GetInGameMobiles(bool players, bool spectators)
        {
            var list = new List<Mobile>();

            if (players)
            {
                for (var i = 0; i < m_Players.Length; i++)
                {
                    if (IsInGamePlayer(i))
                    {
                        list.Add(m_Players[i]);
                    }
                }
            }

            if (spectators)
            {
                list.AddRange(m_Spectators);
            }

            return list;
        }

        public void CheckPlayers()
        {
            var removed = false;

            Span<byte> relievePacket = stackalloc byte[MahjongPackets.MahjongRelievePacketLength].InitializePacket();

            for (var i = 0; i < m_Players.Length; i++)
            {
                var player = m_Players[i];

                if (player == null)
                {
                    continue;
                }

                if (player.Deleted)
                {
                    m_Players[i] = null;

                    SendPlayerExitMessage(player);
                    UpdateDealer(true);

                    removed = true;
                }
                else if (m_InGame[i])
                {
                    if (player.NetState == null)
                    {
                        m_InGame[i] = false;

                        SendPlayerExitMessage(player);
                        UpdateDealer(true);

                        removed = true;
                    }
                    else if (!Game.IsAccessibleTo(player) || player.Map != Game.Map ||
                             !player.InRange(Game.GetWorldLocation(), 5))
                    {
                        m_InGame[i] = false;

                        MahjongPackets.CreateMahjongRelieve(relievePacket, Game.Serial);
                        player.NetState?.Send(relievePacket);

                        SendPlayerExitMessage(player);
                        UpdateDealer(true);

                        removed = true;
                    }
                }
            }

            for (var i = 0; i < m_Spectators.Count;)
            {
                var mobile = m_Spectators[i];

                if (mobile.NetState == null || mobile.Deleted)
                {
                    m_Spectators.RemoveAt(i);
                }
                else if (!Game.IsAccessibleTo(mobile) || mobile.Map != Game.Map ||
                         !mobile.InRange(Game.GetWorldLocation(), 5))
                {
                    m_Spectators.RemoveAt(i);

                    MahjongPackets.CreateMahjongRelieve(relievePacket, Game.Serial);
                    mobile.NetState?.Send(relievePacket);
                }
                else
                {
                    i++;
                }
            }

            if (removed && !UpdateSpectators())
            {
                SendPlayersPacket(true, true);
            }
        }

        private void UpdateDealer(bool message)
        {
            if (IsInGamePlayer(DealerPosition))
            {
                return;
            }

            for (var i = DealerPosition + 1; i < m_Players.Length; i++)
            {
                if (IsInGamePlayer(i))
                {
                    DealerPosition = i;

                    if (message)
                    {
                        SendDealerChangedMessage();
                    }

                    return;
                }
            }

            for (var i = 0; i < DealerPosition; i++)
            {
                if (IsInGamePlayer(i))
                {
                    DealerPosition = i;

                    if (message)
                    {
                        SendDealerChangedMessage();
                    }

                    return;
                }
            }
        }

        private int GetNextSeat()
        {
            for (var i = DealerPosition; i < m_Players.Length; i++)
            {
                if (m_Players[i] == null)
                {
                    return i;
                }
            }

            for (var i = 0; i < DealerPosition; i++)
            {
                if (m_Players[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool UpdateSpectators()
        {
            if (m_Spectators.Count == 0)
            {
                return false;
            }

            var nextSeat = GetNextSeat();

            if (nextSeat >= 0)
            {
                var newPlayer = m_Spectators[0];

                m_Spectators.RemoveAt(0);

                AddPlayer(newPlayer, nextSeat, false);

                UpdateSpectators();

                return true;
            }

            return false;
        }

        private void AddPlayer(Mobile player, int index, bool sendJoinGame)
        {
            m_Players[index] = player;
            m_InGame[index] = true;

            UpdateDealer(false);

            if (sendJoinGame)
            {
                player.NetState.SendMahjongJoinGame(Game.Serial);
            }

            SendPlayersPacket(true, true);

            player.NetState.SendMahjongGeneralInfo(Game);
            player.NetState.SendMahjongTilesInfo(Game, player);

            if (DealerPosition == index)
            {
                SendLocalizedMessage(1062773, player.Name); // ~1_name~ has entered the game as the dealer.
            }
            else
            {
                SendLocalizedMessage(1062772, player.Name); // ~1_name~ has entered the game as a player.
            }
        }

        private void AddSpectator(Mobile mobile)
        {
            if (!IsSpectator(mobile))
            {
                m_Spectators.Add(mobile);
            }

            mobile.NetState.SendMahjongJoinGame(Game.Serial);
            mobile.NetState.SendMahjongPlayersInfo(Game, mobile);
            mobile.NetState.SendMahjongGeneralInfo(Game);
            mobile.NetState.SendMahjongTilesInfo(Game, mobile);
        }

        public void Join(Mobile mobile)
        {
            var index = GetPlayerIndex(mobile);

            if (index >= 0)
            {
                AddPlayer(mobile, index, true);
                return;
            }

            var nextSeat = GetNextSeat();

            if (nextSeat >= 0)
            {
                AddPlayer(mobile, nextSeat, true);
            }
            else
            {
                AddSpectator(mobile);
            }
        }

        public void LeaveGame(Mobile player)
        {
            var index = GetPlayerIndex(player);
            if (index >= 0)
            {
                m_InGame[index] = false;

                SendPlayerExitMessage(player);
                UpdateDealer(true);

                SendPlayersPacket(true, true);
            }
            else
            {
                m_Spectators.Remove(player);
            }
        }

        public void ResetScores(int value)
        {
            for (var i = 0; i < m_Scores.Length; i++)
            {
                m_Scores[i] = value;
            }

            SendPlayersPacket(true, Game.ShowScores);

            SendLocalizedMessage(1062697); // The dealer redistributes the score sticks evenly.
        }

        public void TransferScore(Mobile from, int toPosition, int amount)
        {
            var fromPosition = GetPlayerIndex(from);
            var to = GetPlayer(toPosition);

            if (fromPosition < 0 || to == null || m_Scores[fromPosition] < amount)
            {
                return;
            }

            m_Scores[fromPosition] -= amount;
            m_Scores[toPosition] += amount;

            if (Game.ShowScores)
            {
                SendPlayersPacket(true, true);
            }
            else
            {
                from.NetState.SendMahjongPlayersInfo(Game, from);
                to.NetState.SendMahjongPlayersInfo(Game, to);
            }

            // ~1_giver~ gives ~2_receiver~ ~3_number~ points.
            SendLocalizedMessage(1062774, $"{from.Name}\t{to.Name}\t{amount}");
        }

        public void OpenSeat(int index)
        {
            var player = GetPlayer(index);
            if (player == null)
            {
                return;
            }

            if (m_InGame[index])
            {
                player.NetState.SendMahjongRelieve(Game.Serial);
            }

            m_Players[index] = null;

            SendLocalizedMessage(1062699, player.Name); // ~1_name~ is relieved from the game by the dealer.

            UpdateDealer(true);

            if (!UpdateSpectators())
            {
                SendPlayersPacket(true, true);
            }
        }

        public void AssignDealer(int index)
        {
            var to = GetPlayer(index);

            if (to == null || !m_InGame[index])
            {
                return;
            }

            var oldDealer = DealerPosition;

            DealerPosition = index;

            if (IsInGamePlayer(oldDealer))
            {
                m_Players[oldDealer].NetState.SendMahjongPlayersInfo(Game, m_Players[oldDealer]);
            }

            to.NetState.SendMahjongPlayersInfo(Game, to);

            SendDealerChangedMessage();
        }

        private void SendDealerChangedMessage()
        {
            if (Dealer != null)
            {
                SendLocalizedMessage(1062698, Dealer.Name); // ~1_name~ is assigned the dealer.
            }
        }

        private void SendPlayerExitMessage(Mobile who)
        {
            SendLocalizedMessage(1062762, who.Name); // ~1_name~ has left the game.
        }

        public void SendPlayersPacket(bool players, bool spectators)
        {
            foreach (var mobile in GetInGameMobiles(players, spectators))
            {
                mobile.NetState.SendMahjongPlayersInfo(Game, mobile);
            }
        }

        public void SendGeneralPacket(bool players, bool spectators)
        {
            var mobiles = GetInGameMobiles(players, spectators);

            if (mobiles.Count == 0)
            {
                return;
            }

            Span<byte> generalInfo = stackalloc byte[MahjongPackets.MahjongGeneralInfoPacketLength].InitializePacket();

            foreach (var mobile in mobiles)
            {
                MahjongPackets.CreateMahjongGeneralInfo(generalInfo, Game);
                mobile.NetState?.Send(generalInfo);
            }
        }

        public void SendTilesPacket(bool players, bool spectators)
        {
            foreach (var mobile in GetInGameMobiles(players, spectators))
            {
                mobile.NetState.SendMahjongTilesInfo(Game, mobile);
            }
        }

        public void SendTilePacket(MahjongTile tile, bool players, bool spectators)
        {
            foreach (var mobile in GetInGameMobiles(players, spectators))
            {
                mobile.NetState.SendMahjongTileInfo(tile, mobile);
            }
        }

        public void SendRelievePacket(bool players, bool spectators)
        {
            var mobiles = GetInGameMobiles(players, spectators);

            if (mobiles.Count == 0)
            {
                return;
            }

            Span<byte> relievePacket = stackalloc byte[MahjongPackets.MahjongRelievePacketLength].InitializePacket();

            foreach (var mobile in mobiles)
            {
                MahjongPackets.CreateMahjongRelieve(relievePacket, Game.Serial);
                mobile.NetState?.Send(relievePacket);
            }
        }

        public void SendLocalizedMessage(int number)
        {
            foreach (var mobile in GetInGameMobiles(true, true))
            {
                mobile.SendLocalizedMessage(number);
            }
        }

        public void SendLocalizedMessage(int number, string args)
        {
            foreach (var mobile in GetInGameMobiles(true, true))
            {
                mobile.SendLocalizedMessage(number, args);
            }
        }

        public void Save(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(Seats);

            for (var i = 0; i < Seats; i++)
            {
                writer.Write(m_Players[i]);
                writer.Write(m_PublicHand[i]);
                writer.Write(m_Scores[i]);
            }

            writer.Write(DealerPosition);
        }
    }
}
