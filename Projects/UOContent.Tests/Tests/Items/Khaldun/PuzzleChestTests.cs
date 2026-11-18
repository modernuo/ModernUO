using Server.Items;
using Server.Mobiles;
using Xunit;
using static Server.Items.PuzzleChestCylinder;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PuzzleChestTests
{
    [Fact]
    public void SecondWrongGuessReplacesTheFirst()
    {
        var chest = new MetalGoldenPuzzleChest();
        var player = new PlayerMobile { Player = true, Body = 400, RawStr = 100, Hits = 100 };

        try
        {
            // None never appears in a solution, so both guesses are wrong.
            var first = new PuzzleChestSolution(None, None, None, None, None);
            var second = new PuzzleChestSolution(Yellow, None, None, None, None);

            chest.SubmitSolution(player, first);
            chest.SubmitSolution(player, second);

            Assert.Equal(second.Cylinders, chest.GetLastGuess(player).Cylinders);
        }
        finally
        {
            chest.Delete();
            player.Delete();
        }
    }
}
