using System;
using System.Collections.Generic;

using Server;
using Server.Gumps;

namespace Server.Poker
{
    public class PokerPlayer
    {
        private int m_Gold;
        private int m_Bet;
        private int m_RoundGold;
        private int m_RoundBet;
        private bool m_RequestLeave;
        private bool m_IsAllIn;
        private bool m_Forced;
        private bool m_LonePlayer;
        private Mobile m_Mobile;
        private PokerGame m_Game;
        private Point3D m_Seat;
        private DateTime m_BetStart;
        private List<Card> m_HoleCards;
        private PlayerAction m_Action;

        public int Gold { get { return m_Gold; } set { m_Gold = value; } }
        public int Bet { get { return m_Bet; } set { m_Bet = value; } }
        public int RoundGold { get { return m_RoundGold; } set { m_RoundGold = value; } }
        public int RoundBet { get { return m_RoundBet; } set { m_RoundBet = value; } }
        public bool RequestLeave { get { return m_RequestLeave; } set { m_RequestLeave = value; } }
        public bool IsAllIn { get { return m_IsAllIn; } set { m_IsAllIn = value; } }
        public bool Forced { get { return m_Forced; } set { m_Forced = value; } }
        public bool LonePlayer { get { return m_LonePlayer; } set { m_LonePlayer = value; } }
        public Mobile Mobile { get { return m_Mobile; } set { m_Mobile = value; } }
        public PokerGame Game { get { return m_Game; } set { m_Game = value; } }
        public Point3D Seat { get { return m_Seat; } set { m_Seat = value; } }
        public DateTime BetStart { get { return m_BetStart; } set { m_BetStart = value; } }
        public List<Card> HoleCards { get { return m_HoleCards; } }
        public PlayerAction Action
        {
            get { return m_Action; }
            set
            {
                m_Action = value;

                switch (m_Action)
                {
                    case PlayerAction.None: break;
                    default:
                        if (m_Game != null)
                            m_Game.PokerGame_PlayerMadeDecision(this);

                        break;
                }
            }
        }

        public bool HasDealerButton { get { return (m_Game.DealerButton == this); } }
        public bool HasSmallBlind { get { return (m_Game.SmallBlind == this); } }
        public bool HasBigBlind { get { return (m_Game.BigBlind == this); } }
        public bool HasBlindBet { get { return (m_Game.SmallBlind == this || m_Game.BigBlind == this); } }

        public PokerPlayer(Mobile from)
        {
            m_Mobile = from;
            m_HoleCards = new List<Card>();
        }

        public void ClearGame()
        {
            m_Bet = 0;
            m_RoundGold = 0;
            m_RoundBet = 0;
            m_HoleCards.Clear();
            m_Game = null;
            CloseAllGumps();
            m_Action = PlayerAction.None;
            m_IsAllIn = false;
            m_Forced = false;
            m_LonePlayer = false;
        }

        public void AddCard(Card card)
        {
            m_HoleCards.Add(card);
        }

        public void SetBBAction()
        {
            m_Action = PlayerAction.Bet;
        }

        public HandRank GetBestHand(List<Card> communityCards, out List<Card> bestCards)
        {
            return HandRanker.GetBestHand(GetAllCards(communityCards), out bestCards);
        }

        public List<Card> GetAllCards(List<Card> communityCards)
        {
            List<Card> hand = new List<Card>(communityCards);
            hand.AddRange(m_HoleCards);
            hand.Sort();
            return hand;
        }

        public void CloseAllGumps()
        {
            if (m_Mobile != null)
            {
                m_Mobile.CloseGump<PokerTableGump>();
                m_Mobile.CloseGump<PokerLeaveGump>();
                m_Mobile.CloseGump<PokerJoinGump>();
                m_Mobile.CloseGump<PokerBetGump>();
            }

        }
        public void SendGump(Gump toSend)
        {
            if (m_Mobile != null)
                m_Mobile.SendGump(toSend);
        }

        public void SendMessage(string message)
        {
            if (m_Mobile != null)
                m_Mobile.SendMessage(message);
        }

        public void SendMessage(int hue, string message)
        {
            if (m_Mobile != null)
                m_Mobile.SendMessage(hue, message);
        }

        public void TeleportToSeat()
        {
            if (m_Mobile != null && m_Seat != Point3D.Zero)
                m_Mobile.Location = m_Seat;
        }

        public bool IsOnline()
        {
            if (m_Mobile != null)
                if (m_Mobile.NetState != null)
                    return true;
                    //if (m_Mobile.NetState.Socket != null)
                    //    if (m_Mobile.NetState.Socket.Connected)
                    //        return true;

            return false;
        }
    }
}
