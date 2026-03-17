using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Server.Collections;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Text;

namespace Server.Engines.PlayerMurderSystem;

public static class BountyMessage
{
    /// <summary>
    /// Sends a synthetic container content packet (0x3C) using offset player serials.
    /// The client displays these as bulletin board message entries. When clicked,
    /// HandleBBRequest reverses the offset to recover the real player serial.
    /// </summary>
    public static void SendBountyContainerContent(NetState ns, BaseBulletinBoard board, uint syntheticSerialBase)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var bounties = PlayerMurderSystem.GetActiveBounties();
        var entrySize = ns.ContainerGridLines ? 20 : 19;
        var totalSize = 5 + bounties.Count * entrySize;

        var writer = totalSize > 1024 ? new SpanWriter(totalSize) : new SpanWriter(stackalloc byte[totalSize]);
        writer.Write((byte)0x3C); // Packet ID
        writer.Seek(4, SeekOrigin.Current); // Length & count placeholder

        var written = 0;

        foreach (var (player, _) in bounties)
        {
            writer.Write((Serial)(syntheticSerialBase + (uint)player.Serial));
            writer.Write((ushort)0xEB0); // BulletinMessage ItemID
            writer.Write((byte)0);       // signed, itemID offset
            writer.Write((ushort)1);     // Amount
            writer.Write((short)0);      // X
            writer.Write((short)0);      // Y
            if (ns.ContainerGridLines)
            {
                writer.Write((byte)0); // Grid location
            }
            writer.Write(board.Serial);  // Container = this board
            writer.Write((ushort)0);     // Hue
            written++;
        }

        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)writer.BytesWritten);
        writer.Write((ushort)written);
        writer.Seek(0, SeekOrigin.End);

        ns.Send(writer.Span);

        writer.Dispose();
    }

    /// <summary>
    /// Sends a synthetic BB message packet (header or content) for a bounty entry,
    /// built entirely from live MurderSystem data — no real BulletinMessage item required.
    /// </summary>
    public static void SendBountyBBMessage(
        NetState ns, BaseBulletinBoard board, Serial messageSerial, PlayerMobile player, int bounty, DateTime lastMurderTime, bool content)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var posterName = player.Name ?? "";
        var subject = $"{bounty} gold";
        var time = lastMurderTime.ToString("MMM dd, yyyy");

        var lines = content ? CreateLines(player, bounty) : null;

        BulletinEquip[] equip = null;
        if (content)
        {
            using var list = PooledRefQueue<BulletinEquip>.Create(player.Items.Count);
            for (var i = 0; i < player.Items.Count; ++i)
            {
                var item = player.Items[i];
                if (item.Layer >= Layer.OneHanded && item.Layer <= Layer.Mount)
                {
                    list.Enqueue(new BulletinEquip(item.ItemID, item.Hue));
                }
            }

            equip = list.ToArray();
        }

        // Calculate packet size
        var longestTextLine = 0;
        var maxLength = 22;
        posterName.UpdateLengthCounters(ref maxLength, ref longestTextLine);
        subject.UpdateLengthCounters(ref maxLength, ref longestTextLine);
        time.UpdateLengthCounters(ref maxLength, ref longestTextLine);

        if (content)
        {
            var equipLength = Math.Min(255, equip!.Length);
            var linesLength = Math.Min(255, lines!.Length);
            maxLength += 2 + equipLength * 4;
            for (var i = 0; i < linesLength; i++)
            {
                lines[i].UpdateLengthCounters(ref maxLength, ref longestTextLine, true);
            }
        }

        Span<byte> textBuffer = stackalloc byte[TextEncoding.UTF8.GetMaxByteCount(longestTextLine)];

        var writer = maxLength > 1024 ? new SpanWriter(maxLength) : new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x71); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write((byte)(content ? 0x02 : 0x01)); // Command
        writer.Write(board.Serial);
        writer.Write(messageSerial); // Synthetic serial — must match the 0x3C entry
        if (!content)
        {
            writer.Write(Serial.Zero); // Thread serial (all root-level)
        }

        writer.WriteString(posterName, textBuffer);
        writer.WriteString(subject, textBuffer);
        writer.WriteString(time, textBuffer);

        if (content)
        {
            writer.Write((short)player.Body);
            writer.Write((short)player.Hue);

            var equipLength = Math.Min(255, equip!.Length);
            writer.Write((byte)equipLength);
            for (var i = 0; i < equipLength; i++)
            {
                writer.Write((short)equip[i]._itemID);
                writer.Write((short)equip[i]._hue);
            }

            var linesLength = Math.Min(255, lines!.Length);
            writer.Write((byte)linesLength);
            for (var i = 0; i < linesLength; i++)
            {
                writer.WriteString(lines[i], textBuffer, true);
            }
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);

        writer.Dispose();
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

        var lines = new List<string> { title, "" };

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
    // Human hair hues: 0x44E–0x47D. Index 0 (hue 0) = indeterminate, index 1+ maps to 0x44E+
    private static string GetHairColor(int hue) => hue switch
    {
        0                                        => "indeterminate color",
        0x44E or 0x44F or 0x450                  => "white",
        0x451 or 0x452 or 0x453                  => "graying",
        0x454 or 0x455                           => "black",
        0x456 or 0x457 or 0x458                  => "copper",
        0x459 or 0x45A or 0x45B or 0x45C         => "brown",
        0x45D                                    => "reddish brown",
        0x45E or 0x45F or 0x460                  => "blonde",
        0x461 or 0x462 or 0x463                  => "light brown",
        0x464 or 0x465                           => "golden brown",
        0x466 or 0x467 or 0x468                  => "golden",
        0x469 or 0x46A or 0x46B                  => "bronze",
        0x46C or 0x46D                           => "dark brown",
        0x46E or 0x46F                           => "sandy",
        0x470 or 0x471 or 0x472                  => "honey-colored",
        0x473 or 0x474 or 0x475                  => "red",
        0x476 or 0x477 or 0x478                  => "nut brown",
        0x479 or 0x47A or 0x47B                  => "rich brown",
        0x47C or 0x47D                           => "very dark brown",
        _                                        => "outlandishly colored"
    };

    // Maps body hues to skin tone descriptions (from uo98 bountyboard.m lookup table)
    // Human skin hues: 0x3EA–0x422 (may have 0x8000 flag). Index 1 = 0x3EA.
    private static string GetSkinTone(int hue) => (hue & 0x7FFF) switch
    {
        0x3F1 or 0x3F2 or 0x3F8 or 0x3FF or 0x400 or 0x406 or 0x407 or 0x408
            or 0x40D or 0x40E or 0x415 or 0x416                              => "pale",
        0x3EA or 0x3EB or 0x3F9 or 0x3FA or 0x3FB or 0x417                   => "fair",
        0x3F3 or 0x3F4 or 0x3FC or 0x401 or 0x402 or 0x409 or 0x40F
            or 0x410 or 0x411 or 0x418                                        => "tanned",
        0x3EC or 0x3ED or 0x3EE or 0x3F5 or 0x403 or 0x412 or 0x413
            or 0x419 or 0x421                                                 => "copper",
        0x3EF or 0x3F0 or 0x3F6 or 0x3F7 or 0x3FD or 0x3FE or 0x404
            or 0x405 or 0x40A or 0x40B or 0x40C or 0x414 or 0x41A
            or 0x41B or 0x420                                                 => "dark",
        0x41C or 0x41D or 0x41E                                               => "yellow",
        _                                                                     => "deathly"
    };
}
