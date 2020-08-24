using System.Collections.Generic;

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

            for (int i = 0; i < m_Scores.Length; i++)
                m_Scores[i] = baseScore;
        }

        public MahjongPlayers(MahjongGame game, IGenericReader reader)
        {
            Game = game;
            m_Spectators = new List<Mobile>();

            int version = reader.ReadInt();

            int seats = reader.ReadInt();
            m_Players = new Mobile[seats];
            m_InGame = new bool[seats];
            m_PublicHand = new bool[seats];
            m_Scores = new int[seats];

            for (int i = 0; i < seats; i++)
            {
                m_Players[i] = reader.ReadMobile();
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
                return null;
            return m_Players[index];
        }

        public int GetPlayerIndex(Mobile mobile)
        {
            for (int i = 0; i < m_Players.Length; i++)
                if (m_Players[i] == mobile)
                    return i;
            return -1;
        }

        public bool IsInGameDealer(Mobile mobile)
        {
            if (Dealer != mobile)
                return false;
            return m_InGame[DealerPosition];
        }

        public bool IsInGamePlayer(int index)
        {
            if (index < 0 || index >= m_Players.Length || m_Players[index] == null)
                return false;
            return m_InGame[index];
        }

        public bool IsInGamePlayer(Mobile mobile)
        {
            int index = GetPlayerIndex(mobile);

            return IsInGamePlayer(index);
        }

        public bool IsSpectator(Mobile mobile) => m_Spectators.Contains(mobile);

        public int GetScore(int index)
        {
            if (index < 0 || index >= m_Scores.Length)
                return 0;
            return m_Scores[index];
        }

        public bool IsPublic(int index)
        {
            if (index < 0 || index >= m_PublicHand.Length)
                return false;
            return m_PublicHand[index];
        }

        public void SetPublic(int index, bool value)
        {
            if (index < 0 || index >= m_PublicHand.Length || m_PublicHand[index] == value)
                return;

            m_PublicHand[index] = value;

            SendTilesPacket(true, !Game.SpectatorVision);

            if (IsInGamePlayer(index))
                m_Players[index].SendLocalizedMessage(value ? 1062775 : 1062776); // Your hand is [not] publicly viewable.
        }

        public List<Mobile> GetInGameMobiles(bool players, bool spectators)
        {
            List<Mobile> list = new List<Mobile>();

            if (players)
                for (int i = 0; i < m_Players.Length; i++)
                    if (IsInGamePlayer(i))
                        list.Add(m_Players[i]);

            if (spectators)
                list.AddRange(m_Spectators);

            return list;
        }

        public void CheckPlayers()
        {
            bool removed = false;

            for (int i = 0; i < m_Players.Length; i++)
            {
                Mobile player = m_Players[i];

                if (player == null)
                    continue;

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

                        player.Send(new MahjongRelieve(Game));

                        SendPlayerExitMessage(player);
                        UpdateDealer(true);

                        removed = true;
                    }
                }
            }

            for (int i = 0; i < m_Spectators.Count;)
            {
                Mobile mobile = m_Spectators[i];

                if (mobile.NetState == null || mobile.Deleted)
                {
                    m_Spectators.RemoveAt(i);
                }
                else if (!Game.IsAccessibleTo(mobile) || mobile.Map != Game.Map ||
                         !mobile.InRange(Game.GetWorldLocation(), 5))
                {
                    m_Spectators.RemoveAt(i);

                    mobile.Send(new MahjongRelieve(Game));
                }
                else
                {
                    i++;
                }
            }

            if (removed && !UpdateSpectators())
                SendPlayersPacket(true, true);
        }

        private void UpdateDealer(bool message)
        {
            if (IsInGamePlayer(DealerPosition))
                return;

            for (int i = DealerPosition + 1; i < m_Players.Length; i++)
                if (IsInGamePlayer(i))
                {
                    DealerPosition = i;

                    if (message)
                        SendDealerChangedMessage();

                    return;
                }

            for (int i = 0; i < DealerPosition; i++)
                if (IsInGamePlayer(i))
                {
                    DealerPosition = i;

                    if (message)
                        SendDealerChangedMessage();

                    return;
                }
        }

        private int GetNextSeat()
        {
            for (int i = DealerPosition; i < m_Players.Length; i++)
                if (m_Players[i] == null)
                    return i;

            for (int i = 0; i < DealerPosition; i++)
                if (m_Players[i] == null)
                    return i;

            return -1;
        }

        private bool UpdateSpectators()
        {
            if (m_Spectators.Count == 0)
                return false;

            int nextSeat = GetNextSeat();

            if (nextSeat >= 0)
            {
                Mobile newPlayer = m_Spectators[0];

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
                player.Send(new MahjongJoinGame(Game));

            SendPlayersPacket(true, true);

            player.Send(new MahjongGeneralInfo(Game));
            player.Send(new MahjongTilesInfo(Game, player));

            if (DealerPosition == index)
                SendLocalizedMessage(1062773, player.Name); // ~1_name~ has entered the game as the dealer.
            else
                SendLocalizedMessage(1062772, player.Name); // ~1_name~ has entered the game as a player.
        }

        private void AddSpectator(Mobile mobile)
        {
            if (!IsSpectator(mobile)) m_Spectators.Add(mobile);

            mobile.Send(new MahjongJoinGame(Game));
            mobile.Send(new MahjongPlayersInfo(Game, mobile));
            mobile.Send(new MahjongGeneralInfo(Game));
            mobile.Send(new MahjongTilesInfo(Game, mobile));
        }

        public void Join(Mobile mobile)
        {
            int index = GetPlayerIndex(mobile);

            if (index >= 0)
            {
                AddPlayer(mobile, index, true);
            }
            else
            {
                int nextSeat = GetNextSeat();

                if (nextSeat >= 0)
                    AddPlayer(mobile, nextSeat, true);
                else
                    AddSpectator(mobile);
            }
        }

        public void LeaveGame(Mobile player)
        {
            int index = GetPlayerIndex(player);
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
            for (int i = 0; i < m_Scores.Length; i++) m_Scores[i] = value;

            SendPlayersPacket(true, Game.ShowScores);

            SendLocalizedMessage(1062697); // The dealer redistributes the score sticks evenly.
        }

        public void TransferScore(Mobile from, int toPosition, int amount)
        {
            int fromPosition = GetPlayerIndex(from);
            Mobile to = GetPlayer(toPosition);

            if (fromPosition < 0 || to == null || m_Scores[fromPosition] < amount)
                return;

            m_Scores[fromPosition] -= amount;
            m_Scores[toPosition] += amount;

            if (Game.ShowScores)
            {
                SendPlayersPacket(true, true);
            }
            else
            {
                from.Send(new MahjongPlayersInfo(Game, from));
                to.Send(new MahjongPlayersInfo(Game, to));
            }

            SendLocalizedMessage(1062774,
                $"{from.Name}\t{to.Name}\t{amount}"); // ~1_giver~ gives ~2_receiver~ ~3_number~ points.
        }

        public void OpenSeat(int index)
        {
            Mobile player = GetPlayer(index);
            if (player == null)
                return;

            if (m_InGame[index])
                player.Send(new MahjongRelieve(Game));

            m_Players[index] = null;

            SendLocalizedMessage(1062699, player.Name); // ~1_name~ is relieved from the game by the dealer.

            UpdateDealer(true);

            if (!UpdateSpectators())
                SendPlayersPacket(true, true);
        }

        public void AssignDealer(int index)
        {
            Mobile to = GetPlayer(index);

            if (to == null || !m_InGame[index])
                return;

            int oldDealer = DealerPosition;

            DealerPosition = index;

            if (IsInGamePlayer(oldDealer))
                m_Players[oldDealer].Send(new MahjongPlayersInfo(Game, m_Players[oldDealer]));

            to.Send(new MahjongPlayersInfo(Game, to));

            SendDealerChangedMessage();
        }

        private void SendDealerChangedMessage()
        {
            if (Dealer != null)
                SendLocalizedMessage(1062698, Dealer.Name); // ~1_name~ is assigned the dealer.
        }

        private void SendPlayerExitMessage(Mobile who)
        {
            SendLocalizedMessage(1062762, who.Name); // ~1_name~ has left the game.
        }

        public void SendPlayersPacket(bool players, bool spectators)
        {
            foreach (Mobile mobile in GetInGameMobiles(players, spectators))
                mobile.Send(new MahjongPlayersInfo(Game, mobile));
        }

        public void SendGeneralPacket(bool players, bool spectators)
        {
            List<Mobile> mobiles = GetInGameMobiles(players, spectators);

            if (mobiles.Count == 0)
                return;

            MahjongGeneralInfo generalInfo = new MahjongGeneralInfo(Game);

            generalInfo.Acquire();

            foreach (Mobile mobile in mobiles)
                mobile.Send(generalInfo);

            generalInfo.Release();
        }

        public void SendTilesPacket(bool players, bool spectators)
        {
            foreach (Mobile mobile in GetInGameMobiles(players, spectators))
                mobile.Send(new MahjongTilesInfo(Game, mobile));
        }

        public void SendTilePacket(MahjongTile tile, bool players, bool spectators)
        {
            foreach (Mobile mobile in GetInGameMobiles(players, spectators))
                mobile.Send(new MahjongTileInfo(tile, mobile));
        }

        public void SendRelievePacket(bool players, bool spectators)
        {
            List<Mobile> mobiles = GetInGameMobiles(players, spectators);

            if (mobiles.Count == 0)
                return;

            MahjongRelieve relieve = new MahjongRelieve(Game);

            relieve.Acquire();

            foreach (Mobile mobile in mobiles)
                mobile.Send(relieve);

            relieve.Release();
        }

        public void SendLocalizedMessage(int number)
        {
            foreach (Mobile mobile in GetInGameMobiles(true, true))
                mobile.SendLocalizedMessage(number);
        }

        public void SendLocalizedMessage(int number, string args)
        {
            foreach (Mobile mobile in GetInGameMobiles(true, true))
                mobile.SendLocalizedMessage(number, args);
        }

        public void Save(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(Seats);

            for (int i = 0; i < Seats; i++)
            {
                writer.Write(m_Players[i]);
                writer.Write(m_PublicHand[i]);
                writer.Write(m_Scores[i]);
            }

            writer.Write(DealerPosition);
        }
    }
}
