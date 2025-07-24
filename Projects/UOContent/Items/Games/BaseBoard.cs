using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(2, false)]
public abstract partial class BaseBoard : Container, ISecurable
{
    [SerializedIgnoreDupe]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    public BaseBoard(int itemID) : base(itemID)
    {
        CreatePieces();
    }

    public override double DefaultWeight => 5.0;

    public override bool DisplaysContent => false; // Do not display (x items, y stones)

    public override bool IsDecoContainer => false;

    public override TimeSpan DecayTime => TimeSpan.FromDays(1.0);

    public abstract void CreatePieces();

    public void Reset()
    {
        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }

        CreatePieces();
    }

    public void CreatePiece(BasePiece piece, int x, int y)
    {
        AddItem(piece);
        piece.Location = new Point3D(x, y, 0);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        if (version == 1)
        {
            Level = (SecureLevel)reader.ReadInt();
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped) =>
        dropped is BasePiece piece && piece.Board == this && base.OnDragDrop(from, dropped);

    public override bool OnDragDropInto(Mobile from, Item dropped, Point3D point)
    {
        if (dropped is BasePiece piece && piece.Board == this && base.OnDragDropInto(from, dropped, point))
        {
            if (RootParent == from)
            {
                from.SendSound(0x127, GetWorldLocation());
            }
            else
            {
                Span<byte> buffer = stackalloc byte[OutgoingEffectPackets.SoundPacketLength].InitializePacket();

                foreach (var state in GetClientsInRange(2))
                {
                    OutgoingEffectPackets.CreateSoundEffect(buffer, 0x127, GetWorldLocation());
                    state.Send(buffer);
                }
            }

            return true;
        }

        return false;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (ValidateDefault(from, this))
        {
            list.Add(new DefaultEntry(from.AccessLevel >= AccessLevel.GameMaster ? -1 : 1));
        }

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public static bool ValidateDefault(Mobile from, BaseBoard board) =>
        !board.Deleted && (from.AccessLevel >= AccessLevel.GameMaster || from.Alive &&
            (board.IsChildOf(from.Backpack) || board.RootParent is not Mobile &&
                board.Map == from.Map && from.InRange(board.GetWorldLocation(), 1) &&
                BaseHouse.FindHouseAt(board)?.IsOwner(from) == true));

    public class DefaultEntry : ContextMenuEntry
    {
        public DefaultEntry(int range) : base(6162, range)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is BaseBoard board && ValidateDefault(from, board))
            {
                board.Reset();
            }
        }
    }
}
