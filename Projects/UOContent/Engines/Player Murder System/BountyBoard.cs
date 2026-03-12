using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.PlayerMurderSystem;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BountyBoard : BaseBulletinBoard
{
    [Constructible]
    public BountyBoard() : base(0x1E5E)
    {
        BoardName = "bounty board";
    }

    public override void OnSingleClick(Mobile from)
    {
        var count = PlayerMurderSystem.GetActiveBounties().Count;
        if (count > 0)
        {
            LabelTo(from, $"a bounty board with {count} posted {(count == 1 ? "bounty" : "bounties")}");
        }
        else
        {
            LabelTo(from, 1042679); // a bounty board with no bounties posted.
        }
    }

    public override void PostMessage(Mobile from, BulletinMessage thread, string subject, string[] lines)
    {
        from.SendLocalizedMessage(1062398); // You are not allowed to post to this bulletin board.
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!CheckRange(from))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        SyncBounties();

        var state = from.NetState;
        state.SendBBDisplayBoard(this);
        state.SendContainerContent(from, this);
    }

    private void SyncBounties()
    {
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            Items[i].Delete();
        }

        foreach (var (player, bounty) in PlayerMurderSystem.GetActiveBounties())
        {
            var subject = $"{player.Name}:  {bounty} gold.";
            AddItem(new BulletinMessage(player, null, subject, CreateLines(player, bounty)));
        }
    }

    private static string[] CreateLines(PlayerMobile player, int bounty)
    {
        var isFemale  = player.Body.IsFemale;
        var pronoun   = isFemale ? "she"  : "he";
        var possessive = isFemale ? "her"  : "his";
        var objective  = isFemale ? "her"  : "him";

        // Random title prefix/suffix (6 variants, matching uo98 bountyboard.m)
        var title = Utility.Random(6) switch
        {
            0 => $"Bounty for {player.RawName}!",
            1 => $"{player.RawName} must die!",
            2 => $"A price on {player.RawName}!",
            3 => $"{player.RawName} outlawed!",
            4 => $"Execute {player.RawName}!",
            _ => $"WANTED: {player.RawName}!"
        };

        // Random verb phrase (18 variants, matching uo98 bountyboard.m)
        var verb = Utility.Random(18) switch
        {
            0  => "hath murdered one too many!",
            1  => "shall not slay again!",
            2  => "hath slain too many!",
            3  => "cannot continue to kill!",
            4  => "must be stopped.",
            5  => "is a bloodthirsty monster.",
            6  => "is a killer of the worst sort.",
            7  => "hath no conscience!",
            8  => "hath cowardly slain many.",
            9  => "must die for all our sakes.",
            10 => "sheds innocent blood!",
            11 => "must fall to preserve us.",
            12 => "must be taken care of.",
            13 => "is a thug and must die.",
            14 => "cannot be redeemed.",
            15 => "is a shameless butcher.",
            16 => "is a callous monster.",
            _  => "is a cruel, casual killer."
        };

        // Random bounty intro phrase (7 variants, matching uo98 bountyboard.m)
        var intro = Utility.Random(7) switch
        {
            0 => "  A bounty is hereby offered",
            1 => "  Lord British sets a price",
            2 => "  Claim the reward! 'Tis",
            3 => "  Lord Blackthorn set a price",
            4 => "  The Paladins set a price",
            5 => "  The Merchants set a price",
            _ => "  Lord British's bounty "
        };

        var paragraph =
            $"The foul scum known as {player.RawName} {verb}  For {pronoun} is responsible for " +
            $"{player.Kills} murders.  {intro} of {bounty} gold pieces for {possessive} head!";

        var lines = new List<string> { title };

        // Word-wrap the main paragraph at 28 chars (matching uo98 bountyboard.m CONST:28)
        WordWrap(lines, paragraph, 28);

        // Physical description
        var hairStyle = GetHairStyle(player.HairItemID);
        var hairColor = GetHairColor(player.HairHue);
        var skinTone  = GetSkinTone(player.Hue);

        lines.Add("");
        lines.Add("  A description:");
        lines.Add($"    - {hairStyle}");
        lines.Add($"    - {hairColor} hair");
        lines.Add($"    - {skinTone} skin");

        lines.Add("");
        lines.Add($"If you kill {objective}, remove the");
        lines.Add("head, and give it to a guard");
        lines.Add("to claim your reward.");

        return lines.ToArray();
    }

    private static void WordWrap(List<string> lines, string text, int maxWidth)
    {
        var current = 0;

        while (current < text.Length)
        {
            var length = text.Length - current;

            if (length > maxWidth)
            {
                length = maxWidth;

                while (length > 0 && text[current + length] != ' ')
                {
                    length--;
                }

                if (length == 0)
                {
                    length = maxWidth; // hard break — no space found
                }
                else
                {
                    length++; // include the space
                }

                lines.Add(text.Substring(current, length));
            }
            else
            {
                lines.Add(text.Substring(current, length) + " ");
            }

            current += length;
        }
    }

    // Maps hair item IDs to style descriptions (from uo98 bountyboard.m lookup table)
    private static string GetHairStyle(int itemID) => itemID switch
    {
        0x203B => "hair worn short",
        0x203C => "hair worn long",
        0x203D => "hair tied back",
        0x2044 => "a mohawk hairstyle",
        0x2045 => "pageboy hair",
        0x2046 => "hair tied in buns",
        0x2047 => "curly hair",
        0x2048 => "receding hairline",
        0x2049 => "hair in two pigtails",
        0x204A => "shaved head and topknot",
        _      => "bald"
    };

    // Maps hair hues to color descriptions (from uo98 bountyboard.m lookup table)
    private static string GetHairColor(int hue) => hue switch
    {
        0                    => "indeterminate color",
        1 or 2 or 3          => "white",
        4 or 5 or 6          => "graying",
        7 or 8               => "black",
        9 or 10 or 11        => "copper",
        12 or 13 or 14 or 15 => "brown",
        16                   => "reddish brown",
        17 or 18 or 19       => "blonde",
        20 or 21 or 22       => "light brown",
        23 or 24             => "golden brown",
        25 or 26 or 27       => "golden",
        28 or 29 or 30       => "bronze",
        31 or 32             => "dark brown",
        33 or 34             => "sandy",
        35 or 36 or 37       => "honey-colored",
        38 or 39 or 40       => "red",
        41 or 42 or 43       => "nut brown",
        44 or 45 or 46       => "rich brown",
        47 or 48             => "very dark brown",
        _                    => "outlandishly colored"
    };

    // Maps body hues to skin tone descriptions (from uo98 bountyboard.m lookup table)
    private static string GetSkinTone(int hue) => hue switch
    {
        8 or 9 or 15 or 22 or 23 or 29 or 30 or 31 or 36 or 37 or 44 or 45 => "pale",
        1 or 2 or 16 or 17 or 18 or 46                                       => "fair",
        10 or 11 or 19 or 24 or 25 or 32 or 38 or 39 or 40 or 47            => "tanned",
        3 or 4 or 5 or 12 or 26 or 41 or 42 or 48 or 56                     => "copper",
        6 or 7 or 13 or 14 or 20 or 21 or 27 or 28 or 33 or 34 or 35
            or 43 or 49 or 50 or 57                                          => "dark",
        51 or 52 or 53                                                        => "yellow",
        _                                                                     => "deathly"
    };
}
