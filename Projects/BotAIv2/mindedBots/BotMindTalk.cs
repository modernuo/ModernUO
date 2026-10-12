using System.Collections.Generic;

namespace Server.BotAI.Mind;

/// <summary>
/// The one place the thinking bots can hear each other.
///
/// <para>
/// <b>A board, not a conversation, and the difference is what keeps it from eating the shard.</b> Three minds
/// that replied to each other would take turns at a graphics card that answers one question at a time, and a
/// remark would cost the same second and a half a decision costs — so a round of pleasantries is a round of
/// decisions nobody made. Here a line is a by-product: it rides along on a decision that was being taken
/// anyway, on the same call, in the same answer. Nothing is ever asked purely in order to speak.
/// </para>
///
/// <para>
/// <b>And it is heard rather than addressed.</b> There is no recipient and no reply: a mind writes what it is
/// doing or what it has found, and whoever is asked next reads the last few lines as part of the world it is
/// deciding about. That is the same shape as everything else the population shares — a fact left lying where
/// others will pass it — and it means a mind that dies, or is switched off, breaks nothing.
/// </para>
/// </summary>
public static class BotMindTalk
{
    /// <summary>How many lines are kept. Short: this is what somebody said lately, not a history.</summary>
    public static int Keep { get; set; } = 6;

    /// <summary>How long a line is worth repeating to anybody. Older than this and it is about a dead moment.</summary>
    public static int HoldsMs { get; set; } = 240000;

    /// <summary>Longest line a mind may say. Anything past this is cut: the prompt has to stay short.</summary>
    public static int MostLetters { get; set; } = 120;

    /// <summary>How often one mind may speak out loud in the world. Reading is not rationed; saying is.</summary>
    public static int SpeakEveryMs { get; set; } = 25000;

    private static readonly List<(string Who, string What, long Tick)> _said = [];

    /// <summary>Lines posted this session.</summary>
    public static long Lines { get; private set; }

    /// <summary>Puts a line on the board. Trimmed, capped, and never allowed to repeat the speaker's last.</summary>
    public static bool Post(string who, string what)
    {
        if (string.IsNullOrWhiteSpace(who) || string.IsNullOrWhiteSpace(what))
        {
            return false;
        }

        var line = what.Replace('\n', ' ').Replace('\r', ' ').Trim();

        if (line.Length > MostLetters)
        {
            line = line[..MostLetters];
        }

        // Saying the same thing twice running is not communication, and a model given a field to fill will
        // fill it every time whether or not anything has changed.
        for (var i = _said.Count - 1; i >= 0; i--)
        {
            if (_said[i].Who == who)
            {
                if (string.Equals(_said[i].What, line, System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                break;
            }
        }

        _said.Add((who, line, Core.TickCount));
        Lines++;

        while (_said.Count > Keep)
        {
            _said.RemoveAt(0);
        }

        return true;
    }

    /// <summary>What everybody except this one has said lately, oldest first.</summary>
    public static IEnumerable<(string Who, string What, int SecondsAgo)> Heard(string listener)
    {
        for (var i = 0; i < _said.Count; i++)
        {
            var (who, what, tick) = _said[i];
            var since = Core.TickCount - tick;

            if (who == listener || since >= HoldsMs)
            {
                continue;
            }

            yield return (who, what, (int)(since / 1000));
        }
    }

    public static void Forget()
    {
        _said.Clear();
        Lines = 0;
    }
}
