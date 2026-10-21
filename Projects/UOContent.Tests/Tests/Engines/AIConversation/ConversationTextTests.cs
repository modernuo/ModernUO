using System.Linq;
using Server.Engines.AIConversation;
using Xunit;

namespace Server.Tests.Engines.AIConversation;

public class ConversationTextTests
{
    [Theory]
    [InlineData("hello", true)]
    [InlineData("Hello there, friend", true)]
    [InlineData("HAIL", true)]
    [InlineData("well met, traveler", true)]
    [InlineData("good day to you", true)]
    [InlineData("greetings", true)]
    [InlineData("hive of bees", false)] // "hi" must be a whole word
    [InlineData("heyday", false)]
    [InlineData("where is the bank", false)]
    [InlineData("", false)]
    public void IsGreeting_DetectsGreetingPhrases(string said, bool expected)
    {
        Assert.Equal(expected, ConversationText.IsGreeting(said));
    }

    [Theory]
    [InlineData("bye", true)]
    [InlineData("Goodbye!", true)]
    [InlineData("farewell, sage", true)]
    [InlineData("good bye", true)]
    [InlineData("byline", false)]
    [InlineData("later!", true)]
    [InlineData("goodness me", false)]
    public void IsFarewell_DetectsFarewellPhrases(string said, bool expected)
    {
        Assert.Equal(expected, ConversationText.IsFarewell(said));
    }

    [Theory]
    [InlineData("hail sage elric!", "Sage Elric", true)]
    [InlineData("ELRIC, a word please", "Sage Elric", true)]
    [InlineData("what do you think, elric?", "Sage Elric", true)]
    [InlineData("that belongs to elrics cousin", "Sage Elric", false)] // not a whole word
    [InlineData("melric is here", "Sage Elric", false)]
    [InlineData("hello there", "Sage Elric", false)]
    [InlineData("talk to al", "Al Zim", false)] // parts under 3 chars never match
    [InlineData("", "Sage Elric", false)]
    [InlineData("hail elric", null, false)]
    public void MentionsName_MatchesWholeWordsOnly(string said, string name, bool expected)
    {
        Assert.Equal(expected, ConversationText.MentionsName(said, name));
    }

    [Fact]
    public void Sanitize_StripsControlCharactersAndCollapsesWhitespace()
    {
        var raw = "Well met,\n\ntraveler!\tI have\r\n  many   tales.";

        Assert.Equal("Well met, traveler! I have many tales.", ConversationText.Sanitize(raw, 600));
    }

    [Fact]
    public void Sanitize_StripsWrappingQuotes()
    {
        Assert.Equal("Aye, that I can do.", ConversationText.Sanitize("\"Aye, that I can do.\"", 600));
    }

    [Fact]
    public void Sanitize_KeepsInteriorQuotes()
    {
        Assert.Equal("He said \"nay\" to me.", ConversationText.Sanitize("He said \"nay\" to me.", 600));
    }

    [Fact]
    public void Sanitize_TruncatesAtSentenceBoundary()
    {
        var text = "First sentence here. Second sentence is longer and will not fit at all.";
        var result = ConversationText.Sanitize(text, 30);

        Assert.Equal("First sentence here.", result);
    }

    [Fact]
    public void Sanitize_HardTruncatesWhenNoSentenceBoundaryExists()
    {
        var text = new string('a', 700);
        var result = ConversationText.Sanitize(text, 600);

        Assert.Equal(600, result.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\n\t  \r")]
    public void Sanitize_EmptyInputYieldsEmptyString(string text)
    {
        Assert.Equal("", ConversationText.Sanitize(text, 600));
    }

    [Fact]
    public void SplitIntoChunks_ShortTextIsSingleChunk()
    {
        var chunks = ConversationText.SplitIntoChunks("A short reply.", 120);

        Assert.Single(chunks);
        Assert.Equal("A short reply.", chunks[0]);
    }

    [Fact]
    public void SplitIntoChunks_PrefersSentenceBoundaries()
    {
        var chunks = ConversationText.SplitIntoChunks(
            "The vault is quite secure. None shall breach it while I live.",
            60
        );

        Assert.Equal(2, chunks.Count);
        Assert.Equal("The vault is quite secure.", chunks[0]);
        Assert.Equal("None shall breach it while I live.", chunks[1]);
        Assert.All(chunks, c => Assert.True(c.Length <= 60));
    }

    [Fact]
    public void SplitIntoChunks_FallsBackToWordBreaks()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 50));
        var chunks = ConversationText.SplitIntoChunks(text, 40);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Length <= 40));
        Assert.Equal(text, string.Join(' ', chunks));
    }

    [Fact]
    public void SplitIntoChunks_HardSplitsUnbrokenText()
    {
        var text = new string('x', 250);
        var chunks = ConversationText.SplitIntoChunks(text, 120);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, c => Assert.True(c.Length <= 120));
        Assert.Equal(250, chunks.Sum(c => c.Length));
    }
}
