using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Collections;
using Server.Engines.PlayerMurderSystem;
using Server.Mobiles;
using Server.Network;
using Server.Text;

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

        var state = from.NetState;
        state.SendBBDisplayBoard(this);
        SendBountyContainerContent(state);
    }

    public override bool HandleBBRequest(int packetID, Mobile from, SpanReader reader)
    {
        switch (packetID)
        {
            case 3: // Request content
            case 4: // Request header
            {
                var serial = (Serial)reader.ReadUInt32();
                if (World.FindMobile(serial) is PlayerMobile player)
                {
                    var bounty = PlayerMurderSystem.GetBounty(player);
                    if (bounty > 0)
                    {
                        SendBountyMessage(from.NetState, this, player, bounty, packetID == 3);
                    }
                }

                return true;
            }
            case 5: // Post - blocked
                from.SendLocalizedMessage(1062398); // You are not allowed to post to this bulletin board.
                return true;
            case 6: // Remove - blocked
                return true;
        }

        return false;
    }

    /// <summary>
    /// Sends a synthetic container content packet (0x3C) using player serials as message serials.
    /// The client displays these as bulletin board message entries. When the client clicks one,
    /// it sends back the player serial which HandleBBRequest intercepts — World.FindItem returns
    /// null for mobile serials, so the default BB handlers harmlessly no-op.
    /// </summary>
    private void SendBountyContainerContent(NetState ns)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var bounties = PlayerMurderSystem.GetActiveBounties();
        var entrySize = ns.ContainerGridLines ? 20 : 19;

        var writer = new SpanWriter(stackalloc byte[5 + bounties.Count * entrySize]);
        writer.Write((byte)0x3C); // Packet ID
        writer.Seek(4, SeekOrigin.Current); // Length & count placeholder

        var written = 0;

        foreach (var (player, _) in bounties)
        {
            writer.Write(player.Serial);
            writer.Write((ushort)0xEB0); // BulletinMessage ItemID
            writer.Write((byte)0);       // signed, itemID offset
            writer.Write((ushort)1);     // Amount
            writer.Write((short)0);      // X
            writer.Write((short)0);      // Y
            if (ns.ContainerGridLines)
            {
                writer.Write((byte)0); // Grid location
            }
            writer.Write(Serial);        // Container = this board
            writer.Write((ushort)0);     // Hue
            written++;
        }

        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)writer.BytesWritten);
        writer.Write((ushort)written);
        writer.Seek(0, SeekOrigin.End);

        ns.Send(writer.Span);
    }

    /// <summary>
    /// Sends a synthetic BB message packet (header or content) for a bounty entry,
    /// built entirely from live MurderSystem data — no real BulletinMessage item required.
    /// </summary>
    private static void SendBountyMessage(
        NetState ns, BaseBulletinBoard board, PlayerMobile player, int bounty, bool content)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var posterName = player.Name ?? "";
        var subject = $"{bounty} gold";
        var time = Core.Now.ToString("MMM dd, yyyy");

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
        CalcStringSize(posterName, ref maxLength, ref longestTextLine);
        CalcStringSize(subject, ref maxLength, ref longestTextLine);
        CalcStringSize(time, ref maxLength, ref longestTextLine);

        if (content)
        {
            var equipLength = Math.Min(255, equip!.Length);
            var linesLength = Math.Min(255, lines!.Length);
            maxLength += 2 + equipLength * 4;
            for (var i = 0; i < linesLength; i++)
            {
                CalcStringSize(lines[i], ref maxLength, ref longestTextLine, true);
            }
        }

        Span<byte> textBuffer = stackalloc byte[TextEncoding.UTF8.GetMaxByteCount(longestTextLine)];

        var writer = maxLength > 1024 ? new SpanWriter(maxLength) : new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x71); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write((byte)(content ? 0x02 : 0x01)); // Command
        writer.Write(board.Serial);
        writer.Write(player.Serial); // Message serial = player serial
        if (!content)
        {
            writer.Write(Serial.Zero); // Thread serial (all root-level)
        }

        WriteBBString(ref writer, posterName, textBuffer);
        WriteBBString(ref writer, subject, textBuffer);
        WriteBBString(ref writer, time, textBuffer);

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
                WriteBBString(ref writer, lines[i], textBuffer, true);
            }
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);

        writer.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CalcStringSize(string text, ref int maxLength, ref int longestTextLine, bool pad = false)
    {
        var line = Math.Min(255, text.Length);
        var byteCount = TextEncoding.UTF8.GetMaxByteCount(line) + (pad ? 3 : 2);
        maxLength += byteCount;
        longestTextLine = Math.Max(byteCount, longestTextLine);
    }

    private static void WriteBBString(ref SpanWriter writer, string text, Span<byte> buffer, bool pad = false)
    {
        var tail = pad ? 2 : 1;
        var length = Math.Min(pad ? 253 : 254, text.GetBytesUtf8(buffer));
        writer.Write((byte)(length + tail));
        writer.Write(buffer[..length]);

        if (pad)
        {
            writer.Write((ushort)0); // Compensating for an old client bug
        }
        else
        {
            writer.Write((byte)0); // Terminator
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
