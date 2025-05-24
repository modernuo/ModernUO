using Server.Misc;
using Xunit;

namespace Server.Tests;

public class ProfanityProtectionTests
{
    [Theory]
    [InlineData("Hello world")]
    [InlineData("This is a normal conversation")]
    [InlineData("I would like to trade with you")]
    public void Speech_WithoutProfanity_PassesValidation(string speech)
    {
        Assert.False(ProfanityProtection.ContainsProfanity(speech));
    }

    [Fact]
    public void Speech_Empty_PassesValidation()
    {
        Assert.False(ProfanityProtection.ContainsProfanity(""));
    }

    [Theory]
    [InlineData("I'm going to class tomorrow")]  // contains "ass" but in "class"
    [InlineData("This assignment is hard")]      // contains "ass" but in "assignment"
    [InlineData("That's a nice cocktail")]       // contains "cock" but in "cocktail"
    public void Speech_WithWordsThatLookLikeProfanity_PassesValidation(string speech)
    {
        Assert.False(ProfanityProtection.ContainsProfanity(speech));
    }

    [Theory]
    [InlineData("This is ass")]
    [InlineData("What the fuck")]
    [InlineData("You're a bitch")]
    public void Speech_WithProfanity_FailsValidation(string speech)
    {
        Assert.True(ProfanityProtection.ContainsProfanity(speech));
    }

    [Theory]
    [InlineData("ass")]           // standalone profanity
    [InlineData("an ass joke")]   // profanity with word boundaries
    [InlineData("ass.")]          // profanity followed by punctuation
    public void ContainsDisallowedWord_DetectsProfanityWithBoundaries(string speech)
    {
        Assert.True(NameVerification.ContainsDisallowedWord(
            speech,
            ProfanityProtection.Disallowed,
            ProfanityProtection.DisallowedSearchValues
        ));
    }
}
