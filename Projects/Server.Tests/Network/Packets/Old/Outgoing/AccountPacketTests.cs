using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
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
      public int Limit { get; }
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
        Count = mobiles.Count(t => t != null);
        Limit = mobiles.Length;
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

      var account = new TestAccount(new[] {firstMobile, null, null});

      Span<byte> data = new ChangeCharacter(account).Compile();

      Span<byte> expectedData = stackalloc byte[5 + account.Length * 60];
      int pos = 0;

      ((byte)0x81).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length
      ((byte)1).CopyTo(ref pos, expectedData); // Count of non-null characters
      ((byte)0).CopyTo(ref pos, expectedData);

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
      var firstMobile = new Mobile(0x1);
      firstMobile.DefaultMobileInit();
      firstMobile.Name = "Test Mobile";

      var account = new TestAccount(new[] {firstMobile, null, null, null, null});

      NetState ns = new NetState(new TestConnectionContext
      {
        RemoteEndPoint = IPEndPoint.Parse("127.0.0.1")
      })
      {
        Account = account
      };

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
      ((byte)m.Direction).CopyTo(ref pos, expectedData);
      pos += 9;
      var map = m.Map;

      if (map == null || map == Map.Internal)
        map = m.LogoutMap;

      ((ushort)(map?.Width ?? Map.Felucca.Width)).CopyTo(ref pos, expectedData);
      ((ushort)(map?.Height ?? Map.Felucca.Height)).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestLoginComplete()
    {
      Span<byte> data = new LoginComplete().Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x55 // Packet ID
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestCharacterListUpdate()
    {
      var firstMobile = new Mobile(0x1);
      firstMobile.DefaultMobileInit();
      firstMobile.Name = "Test Mobile";

      var account = new TestAccount(new[] {firstMobile, null, null, null, null});

      Span<byte> data = new CharacterListUpdate(account).Compile();

      Span<byte> expectedData = stackalloc byte[4 + account.Length * 60];

      int pos = 0;
      ((byte)0x86).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      int highSlot = -1;
      for (int i = account.Length - 1; i >= 0; i--)
        if (account[i] != null)
        {
          highSlot = i;
          break;
        }

      int count = Math.Max(Math.Max(highSlot + 1, account.Limit), 5);
      ((byte)count).CopyTo(ref pos, expectedData);

      for (int i = 0; i < count; i++)
      {
        var m = account[i];
        if (m != null)
        {
          m.Name.CopyASCIIFixedTo(ref pos, 30, expectedData);
          expectedData.Clear(ref pos, 30);
        }
        else
        {
          expectedData.Clear(ref pos, 60);
        }
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestCharacterList()
    {
      var firstMobile = new Mobile(0x1);
      firstMobile.DefaultMobileInit();
      firstMobile.Name = "Test Mobile";

      var account = new TestAccount(new[] {firstMobile, null, null, null, null});
      var info = new[]
      {
        new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
      };

      Span<byte> data = new CharacterList(account, info).Compile();

      Span<byte> expectedData = stackalloc byte[11 + account.Length * 60 + info.Length * 89];

      int pos = 0;
      ((byte)0xA9).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      int highSlot = -1;
      for (int i = account.Length - 1; i >= 0; i--)
        if (account[i] != null)
        {
          highSlot = i;
          break;
        }

      int count = Math.Max(Math.Max(highSlot + 1, account.Limit), 5);
      ((byte)count).CopyTo(ref pos, expectedData);

      for (int i = 0; i < count; i++)
      {
        var m = account[i];
        if (m != null)
        {
          m.Name.CopyASCIIFixedTo(ref pos, 30, expectedData);
          expectedData.Clear(ref pos, 30);
        }
        else
        {
          expectedData.Clear(ref pos, 60);
        }
      }

      ((byte)info.Length).CopyTo(ref pos, expectedData);

      for (int i = 0; i < info.Length; i++)
      {
        var ci = info[i];
        ((byte)i).CopyTo(ref pos, expectedData);
        ci.City.CopyASCIIFixedTo(ref pos, 32, expectedData);
        ci.Building.CopyASCIIFixedTo(ref pos, 32, expectedData);
        ci.X.CopyTo(ref pos, expectedData);
        ci.Y.CopyTo(ref pos, expectedData);
        ci.Z.CopyTo(ref pos, expectedData);
        ci.Map.MapID.CopyTo(ref pos, expectedData);
        ci.Description.CopyTo(ref pos, expectedData);
        expectedData.Clear(ref pos, 4);
      }

      var flags = ExpansionInfo.GetInfo(Expansion.EJ).CharacterListFlags;
      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (account.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      ((int)flags).CopyTo(ref pos, expectedData);
      ((short)-1).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestCharacterListOld()
    {
      var firstMobile = new Mobile(0x1);
      firstMobile.DefaultMobileInit();
      firstMobile.Name = "Test Mobile";

      var account = new TestAccount(new[] {firstMobile, null, null, null, null});
      var info = new[]
      {
        new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
      };

      Span<byte> data = new CharacterListOld(account, info).Compile();

      Span<byte> expectedData = stackalloc byte[9 + account.Length * 60 + info.Length * 63];

      int pos = 0;
      ((byte)0xA9).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      int highSlot = -1;
      for (int i = account.Length - 1; i >= 0; i--)
        if (account[i] != null)
        {
          highSlot = i;
          break;
        }

      int count = Math.Max(Math.Max(highSlot + 1, account.Limit), 5);
      ((byte)count).CopyTo(ref pos, expectedData);

      for (int i = 0; i < count; i++)
      {
        var m = account[i];
        if (m != null)
        {
          m.Name.CopyASCIIFixedTo(ref pos, 30, expectedData);
          expectedData.Clear(ref pos, 30);
        }
        else
        {
          expectedData.Clear(ref pos, 60);
        }
      }

      ((byte)info.Length).CopyTo(ref pos, expectedData);

      for (int i = 0; i < info.Length; i++)
      {
        var ci = info[i];
        ((byte)i).CopyTo(ref pos, expectedData);
        ci.City.CopyASCIIFixedTo(ref pos, 31, expectedData);
        ci.Building.CopyASCIIFixedTo(ref pos, 31, expectedData);
      }

      var flags = ExpansionInfo.GetInfo(Expansion.EJ).CharacterListFlags;
      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (account.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      ((int)flags).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestAccountLoginRej()
    {
      var reason = ALRReason.BadComm;
      Span<byte> data = new AccountLoginRej(reason).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x82, // Packet ID
        (byte)reason
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestAccountLoginAck()
    {
      var info = new[]
      {
        new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"))
      };

      Span<byte> data = new AccountLoginAck(info).Compile();

      Span<byte> expectedData = stackalloc byte[6 + info.Length * 40];

      int pos = 0;
      ((byte)0xA8).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData);
      ((byte)0x5D).CopyTo(ref pos, expectedData); // Unknown
      ((ushort)info.Length).CopyTo(ref pos, expectedData);

      for (int i = 0; i < info.Length; i++)
      {
        var si = info[i];
        ((ushort)i).CopyTo(ref pos, expectedData);
        si.Name.CopyASCIIFixedTo(ref pos, 32, expectedData);
        ((byte)si.FullPercent).CopyTo(ref pos, expectedData);
        ((byte)si.TimeZone).CopyTo(ref pos, expectedData);
        Utility.GetAddressValue(si.Address.Address).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestPlayServerAck()
    {
      var si = new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"));

      Span<byte> data = new PlayServerAck(si).Compile();

      var addr = Utility.GetAddressValue(si.Address.Address);

      Span<byte> expectedData = stackalloc byte[]
      {
        0x8C, // Packet ID
        (byte)addr, // IP Address in LE
        (byte)(addr >> 8),
        (byte)(addr >> 16),
        (byte)(addr >> 24),
        0x00, 0x00, // Port
        0x00, 0x00, 0x00, 0x00 // Auth ID
      };

      ((ushort)si.Address.Port).CopyTo(expectedData.Slice(5, 2));
      (-1).CopyTo(expectedData.Slice(7, 4)); // Auth ID

      AssertThat.Equal(data, expectedData);
    }
  }
}
