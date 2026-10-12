using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot says back to a mood it heard: a word for each of the seven, and the line that goes with an offer — a bandage,
/// a fire, a hunt, a place in a company — and the thanks that may come back. <c>{other}</c> is the one spoken to; an offer of a
/// hunt names its <c>{quarry}</c>, a fire pointed at its <c>{x}</c>, <c>{y}</c>.
///
/// <para>
/// Put into the voice's bank only where the bank has nothing for the occasion, the way the camp's words are, so
/// <c>bot-voice.json</c> may replace any of them.
/// </para>
/// </summary>
public static class BotSocialTalk
{
    public static Dictionary<string, string[]> Defaults() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["fellow:hurt"] = ["Hold still, {other}. Let me look at that.", "{other}, you are bleeding. Sit down a moment.", "That wants binding, {other}. Is there a healer about?"],
            ["offer:mend"] = ["Hold still, {other}; I have bandages.", "Let me see to that, {other}. Stand still.", "{other}, I can mend that. Give me a moment."],
            ["fellow:tired"] = ["Sit a while, {other}. The work will keep.", "You look done in, {other}. Rest.", "{other}, the road can wait. Sit."],
            ["offer:fire"] = ["Sit down, {other}; I'll get a fire going.", "A fire, {other}. Give me a minute to lay one.", "Rest your legs, {other}. Fire coming up."],
            ["offer:fire:near"] = ["There's a fire at ({x}, {y}), {other}. Go and sit by it.", "{other}, somebody has a fire going at ({x}, {y}). Warm your feet.", "A fire burns at ({x}, {y}), {other}. Nobody will mind you there."],
            ["fellow:grieving"] = ["I am sorry, {other}. They were one of the good ones.", "We all lose somebody, {other}. Walk with me a while.", "Grieve, {other}; then we go on. Together."],
            ["fellow:poor"] = ["Coin comes to those who hunt, {other}. Come on.", "Empty purse, {other}? Then we hunt. Nothing is free.", "There is always something worth killing, {other}. Let's go and find it."],
            ["offer:hunt"] = ["Come hunting with me, {other}. There's {quarry} worth the two of us.", "{other}, I've {quarry} in my sights. Share and share alike.", "With me, {other}: {quarry}, and a share of what it carries."],
            ["fellow:lonely"] = ["You are not alone now, {other}.", "Well, I am here, {other}. Talk.", "Company, {other}? You have it."],
            ["offer:company"] = ["Fall in with us, {other}. There's room.", "{other}, we could use another. Come along.", "Join the company, {other}; nobody walks alone in it."],
            ["offer:enlist"] = ["Room for one more in your company, {other}? I'm coming.", "Wait for me, {other}; I'll fall in with yours.", "{other}, I'll stand with your company."],
            ["fellow:bored"] = ["Bored, {other}? Walk with me; I'll find us trouble.", "Nothing to do, {other}? There is always something to kill.", "Something will turn up, {other}. It always does."],
            ["fellow:proud"] = ["Well done, {other}!", "Ha! Good work, {other}.", "That's the way, {other}. Keep it up.", "I saw that, {other}. Not bad at all."],
            ["answer:thanks"] = ["Thank you, {other}.", "Kind of you, {other}.", "Aye. Thanks, {other}.", "I'll remember that, {other}."]
        };

    /// <summary>Puts the defaults into the voice's bank, each only where the bank has nothing for that occasion. Returns how many were added.</summary>
    public static int Phrases()
    {
        var added = 0;

        foreach (var (key, lines) in Defaults())
        {
            if (!BotVoice.Has(key))
            {
                BotVoice.Phrases(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { [key] = lines });
                added++;
            }
        }

        return added;
    }
}
