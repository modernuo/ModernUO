using System;
using System.Collections.Generic;
using Server.Accounting;

namespace Server.Tests.Network;

/// <summary>
/// Minimal IAccount so a test NetState looks authenticated.
/// </summary>
public class MockAccount : IAccount
{
    public int TotalGold { get; }
    public int TotalPlat { get; }
    public bool DepositGold(int amount) => throw new NotImplementedException();
    public bool DepositPlat(int amount) => throw new NotImplementedException();
    public bool WithdrawGold(int amount) => throw new NotImplementedException();
    public bool WithdrawPlat(int amount) => throw new NotImplementedException();
    public long GetTotalGold() => throw new NotImplementedException();
    public int CompareTo(IAccount other) => throw new NotImplementedException();
    public string Username { get; }
    public string Email { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public int Length { get; private set; }
    public int Limit { get; set; } = 6; // Default to 6 character slots
    public int Count { get; private set; }

    private readonly Dictionary<int, Mobile> _mobiles = new();
    public Mobile this[int index]
    {
        get => _mobiles.GetValueOrDefault(index);
        set
        {
            var old = _mobiles.GetValueOrDefault(index);
            _mobiles[index] = value;

            Length = Math.Max(Length, index + 1);
            Count += (value != null ? 1 : 0) - (old != null ? 1 : 0);

            if (value != null)
            {
                value.Account = this;
            }
        }
    }

    public DateTime Created { get; set; }
    public Serial Serial { get; }
    public void Deserialize(IGenericReader reader) => throw new NotImplementedException();
    public void Serialize(IGenericWriter writer) => throw new NotImplementedException();
    public bool Deleted { get; }
    public void Delete() => throw new NotImplementedException();
    public bool TrySetUsername(string username) => throw new NotImplementedException();
    public void SetPassword(string password) => throw new NotImplementedException();
    public bool CheckPassword(string password) => throw new NotImplementedException();
}
