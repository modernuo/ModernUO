using System;
using System.Linq;
using Server.Accounting;

namespace Server.Tests.Network
{
    public class MockAccount : IAccount, IComparable<MockAccount>
    {
        private readonly Mobile[] m_Mobiles;
        private string m_Password;

        public MockAccount(Mobile[] mobiles)
        {
            m_Mobiles = mobiles;
            foreach (var mobile in mobiles)
            {
                if (mobile != null)
                {
                    mobile.Account = this;
                }
            }

            Length = mobiles.Length;
            Count = mobiles.Count(t => t != null);
            Limit = mobiles.Length;
        }

        public int TotalGold { get; private set; }
        public int TotalPlat { get; private set; }

        public bool DepositGold(int amount)
        {
            TotalGold += amount;
            return true;
        }

        public bool DepositPlat(int amount)
        {
            TotalPlat += amount;
            return true;
        }

        public bool WithdrawGold(int amount)
        {
            if (TotalGold - amount < 0)
            {
                return false;
            }

            TotalGold -= amount;
            return true;
        }

        public bool WithdrawPlat(int amount)
        {
            if (TotalPlat - amount < 0)
            {
                return false;
            }

            TotalPlat -= amount;
            return true;
        }

        public long GetTotalGold() => TotalGold + TotalPlat * 100;

        public int CompareTo(IAccount other) => string.CompareOrdinal(Username, other?.Username);

        public string Username { get; set; }
        public string Email { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public int Length { get; }
        public int Limit { get; }
        public int Count { get; }

        public Mobile this[int index]
        {
            get => m_Mobiles[index];
            set => m_Mobiles[index] = value;
        }

        public void Delete()
        {
        }

        public void SetPassword(string password)
        {
            m_Password = password;
        }

        public bool CheckPassword(string password) => m_Password == password;

        public int CompareTo(MockAccount other) => string.CompareOrdinal(Username, other?.Username);
    }
}
