using Server;
using Server.Engines.Mahjong;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class MahjongScoreTests
{
    [Fact]
    public void TransferScore_RejectsNegativeAmounts()
    {
        var a = new Mobile((Serial)0x1);
        a.DefaultMobileInit();
        var b = new Mobile((Serial)0x2);
        b.DefaultMobileInit();

        var game = new MahjongGame();
        game.Players.Join(a);
        game.Players.Join(b);
        var from = game.Players.GetPlayerIndex(a);
        var to = game.Players.GetPlayerIndex(b);
        var fromScore = game.Players.GetScore(from);
        var toScore = game.Players.GetScore(to);

        game.Players.TransferScore(a, to, -1000);

        Assert.Equal(fromScore, game.Players.GetScore(from));
        Assert.Equal(toScore, game.Players.GetScore(to));
    }
}
