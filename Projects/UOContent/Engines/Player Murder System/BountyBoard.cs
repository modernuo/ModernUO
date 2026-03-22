using System.Buffers;
using ModernUO.Serialization;
using Server.Engines.PlayerMurderSystem;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BountyBoard : BaseBulletinBoard
{
    // Offset added to player serials (max 0x3FFFFFFF) to produce synthetic serials
    // in the unused range (0x80000001–0xBFFFFFFF) for BB packets. Reversible by subtraction.
    private const uint SyntheticSerialBase = 0x80000000;

    [Constructible]
    public BountyBoard() : base(0x1E5E) => BoardName = "bounty board";

    public override void OnSingleClick(Mobile from)
    {
        var count = PlayerMurderSystem.GetActiveBountyCount();
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
        BountyMessage.SendBountyContainerContent(state, this, SyntheticSerialBase);
    }

    public override bool HandleBBRequest(int packetID, Mobile from, SpanReader reader)
    {
        switch (packetID)
        {
            case 3: // Request content
            case 4: // Request header
                {
                    var syntheticSerial = reader.ReadUInt32();
                    var playerSerial = (Serial)(syntheticSerial - SyntheticSerialBase);

                    if (World.FindMobile(playerSerial) is PlayerMobile player &&
                        PlayerMurderSystem.GetMurderContext(player, out var context) &&
                        context.Bounty > 0)
                    {
                        BountyMessage.SendBountyBBMessage(
                            from.NetState,
                            this,
                            (Serial)syntheticSerial,
                            player,
                            context.Bounty,
                            context.LastMurderTime,
                            packetID == 3
                        );
                    }

                    return true;
                }
            case 5: // Post - blocked
                {
                    from.SendLocalizedMessage(1062398); // You are not allowed to post to this bulletin board.
                    return true;
                }
            case 6: // Remove - blocked
                {
                    return true;
                }
        }

        return false;
    }
}
