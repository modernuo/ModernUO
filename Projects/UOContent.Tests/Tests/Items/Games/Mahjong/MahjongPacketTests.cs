using Server;
using Server.Engines.Mahjong;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    [Collection("Sequential Tests")]
    public class MahjongPacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestMahjongJoinGame()
        {
            Serial game = (Serial)0x1024u;

            var expected = new MahjongJoinGame(game).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMahjongJoinGame(game);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestMahjongPlayersInfo(bool showScores)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var game = new MahjongGame { ShowScores = showScores };
            game.Players.Join(m);

            var expected = new MahjongPlayersInfo(game, m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMahjongPlayersInfo(game, m);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void TestMahjongGeneralInfo(bool showScores, bool spectatorVision)
        {
            var game = new MahjongGame { ShowScores = showScores, SpectatorVision = spectatorVision};

            var expected = new MahjongGeneralInfo(game).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMahjongGeneralInfo(game);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestMahjongTilesInfo(bool spectatorVision)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var game = new MahjongGame { SpectatorVision = spectatorVision };
            game.Players.Join(m);

            var expected = new MahjongTilesInfo(game, m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMahjongTilesInfo(game, m);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestMahjongTileInfo(bool spectatorVision)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var game = new MahjongGame { SpectatorVision = spectatorVision };
            game.Players.Join(m);

            var expected = new MahjongTileInfo(game.Tiles[0], m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMahjongTileInfo(game.Tiles[0], m);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestMahjongRelieve()
        {
            Serial game = (Serial)0x1024u;

            var expected = new MahjongRelieve(game).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMahjongRelieve(game);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }
    }
}
