using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

public class AdvancedSearchPagingTests
{
    [Theory]
    [InlineData(20, 0, 18, 18)]  // full first page
    [InlineData(20, 18, 18, 2)]  // partial last page -> 2 visible (bug rendered 0 in descending)
    [InlineData(5, 0, 18, 5)]
    [InlineData(0, 0, 18, 0)]
    public void VisibleCount_IsCorrect(int total, int from, int max, int expected)
    {
        Assert.Equal(expected, AdvancedSearchGump.VisibleCount(total, from, max));
    }
}
