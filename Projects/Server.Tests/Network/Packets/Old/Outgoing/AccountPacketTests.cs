using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Server.Accounting;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class AccountPacketTests : IClassFixture<ServerFixture>
  {
    internal class TestAccount : IAccount, IComparable<TestAccount>
    {
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
        if (TotalGold - amount < 0) return false;

        TotalGold -= amount;
        return true;
      }

      public bool WithdrawPlat(int amount)
      {
        if (TotalPlat - amount < 0) return false;

        TotalPlat -= amount;
        return true;
      }

      public long GetTotalGold() => TotalGold + TotalPlat * 100;

      public int CompareTo(TestAccount other) => other == null ? 1 : Username.CompareTo(other.Username);

      public int CompareTo(IAccount other) => other == null ? 1 : Username.CompareTo(other.Username);

      public string Username { get; set; }
      public string Email { get; set; }
      public AccessLevel AccessLevel { get; set; }
      public int Length { get; }
      public int Limit { get; } = 7;
      public int Count { get; }

      private Mobile[] m_Mobiles;
      private string m_Password;

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

      public TestAccount(Mobile[] mobiles)
      {
        m_Mobiles = mobiles;
        foreach (var mobile in mobiles)
          if (mobile != null)
            mobile.Account = this;

        Length = mobiles.Length;
        Count = mobiles.Length; // Assume no nulls, this is not an exhaustive check
      }
    }

    internal class TestConnectionContext : ConnectionContext
    {
      public override string ConnectionId { get; set; }
      public override IFeatureCollection Features { get; }
      public override IDictionary<object, object> Items { get; set; }
      public override IDuplexPipe Transport { get; set; }
    }

    [Fact]
    public void TestChangeCharacter()
    {
      var firstMobile = new Mobile(0x1);
      firstMobile.DefaultMobileInit();
      firstMobile.Name = "Test Mobile";

      var account = new TestAccount(new[]{ firstMobile, null, null });

      Span<byte> data = new ChangeCharacter(account).Compile();

      Span<byte> expectedData = stackalloc byte[5 + account.Length * 60];
      int pos = 0;

      expectedData[pos++] = 0x81; // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length
      expectedData[pos++] = 1; // Count of non-null characters
      expectedData[pos++] = 0;

      for (var i = 0; i < account.Length; ++i)
      {
        Mobile m = account[i];
        if (m == null)
        {
          expectedData.Clear(ref pos, 60);
        }
        else
        {
          m.Name.CopyASCIIFixedTo(ref pos, 30, expectedData);
          expectedData.Clear(ref pos, 30); // Password (empty)
        }
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestClientVersionReq()
    {
      Span<byte> data = new ClientVersionReq().Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBD, // Packet ID
        0x00, 0x03 // Length
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDeleteResult()
    {
      Span<byte> data = new DeleteResult(DeleteResultType.BadRequest).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x85, // Packet ID
        (byte)DeleteResultType.BadRequest
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestPopupMessage()
    {
      Span<byte> data = new PopupMessage(PMMessage.IdleWarning).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x53, // Packet ID
        (byte)PMMessage.IdleWarning
      };

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(ProtocolChanges.Version70610)]
    [InlineData(ProtocolChanges.Version6000)]
    public void TestSupportedFeatures(ProtocolChanges protocolChanges)
    {
      NetState ns = new NetState(new TestConnectionContext());
      ns.ProtocolChanges = protocolChanges;
      ns.Account = new TestAccount(new Mobile[0]);

      Span<byte> data = new SupportedFeatures(ns).Compile();

      Span<byte> expectedData = stackalloc byte[ns.ExtendedSupportedFeatures ? 5 : 3];
      expectedData[0] = 0xB9; // Packet ID

      var flags = ExpansionInfo.GetFeatures(Expansion.EJ);

      if (ns.Account.Limit >= 6)
      {
        flags |= FeatureFlags.LiveAccount;
        flags &= ~FeatureFlags.UOTD;

        if (ns.Account.Limit > 6)
          flags |= FeatureFlags.SeventhCharacterSlot;
        else
          flags |= FeatureFlags.SixthCharacterSlot;
      }

      if (ns.ExtendedSupportedFeatures)
        ((uint)flags).CopyTo(expectedData.Slice(1, 4));
      else
        ((ushort)flags).CopyTo(expectedData.Slice(1, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestLoginConfirm()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();
      m.Body = 0x100;
      m.X = 100;
      m.Y = 10;
      m.Z = -10;
      m.Direction = Direction.Down;
      m.LogoutMap = Map.Felucca;

      Span<byte> data = new LoginConfirm(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x1B, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, // Body
        0x00, 0x00, // X
        0x00, 0x00, // Y
        0x00, 0x00, // Z
        0x00, // Direction
        0x00,
        0xFF, 0xFF, 0xFF, 0xFF,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, // Map Width
        0x00, 0x00, // Map Height
        0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
      };

      int pos = 1;
      m.Serial.CopyTo(ref pos, expectedData);
      pos += 4;
      ((ushort)m.Body).CopyTo(ref pos, expectedData);
      ((ushort)m.X).CopyTo(ref pos, expectedData);
      ((ushort)m.Y).CopyTo(ref pos, expectedData);
      ((ushort)m.Z).CopyTo(ref pos, expectedData);
      expectedData[pos++] = (byte)m.Direction;
      pos += 9;
      var map = m.Map;

      if (map == null || map == Map.Internal)
        map = m.LogoutMap;

      ((ushort)(map?.Width ?? Map.Felucca.Width)).CopyTo(ref pos, expectedData);
      ((ushort)(map?.Height ?? Map.Felucca.Height)).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }
  }
}
