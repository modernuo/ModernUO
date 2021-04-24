using System;
using Server.Engines.MLQuests.Gumps;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Mobiles
{
    public class SirHelper : Mage
    {
        private static readonly Gump m_Gump = new InfoNPCGump(1078029, 1078028);
        private static readonly TimeSpan m_ShoutDelay = TimeSpan.FromSeconds(20);

        private static readonly TimeSpan
            m_ShoutCooldown = TimeSpan.FromDays(1); // TODO: Verify, could be a lot longer... or until a restart even

        private DateTime m_NextShout;

        [Constructible]
        public SirHelper()
        {
            Title = "the Profession Guide"; // TODO: Don't display in paperdoll

            Hue = 0x83EA;

            Direction = Direction.South;
            Frozen = true;
        }

        public SirHelper(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Sir Helper";
        public override bool IsActiveVendor => false;

        public override void InitSBInfo()
        {
        }

        public override bool GetGender() => false;

        public override void CheckMorph()
        {
        }

        public override void InitOutfit()
        {
            HairItemID = 0x203C;
            FacialHairItemID = 0x204D;
            HairHue = FacialHairHue = 0x8A7;

            AddItem(new Sandals());

            Item item;

            item = new Cloak();
            item.ItemID = 0x26AD;
            item.Hue = 0x455;
            AddItem(item);

            item = new Robe();
            item.ItemID = 0x26AE;
            item.Hue = 0x4AB;
            AddItem(item);

            item = new Backpack();
            item.Movable = false;
            AddItem(item);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.CanBeginAction(this))
            {
                from.BeginAction(this);
                Timer.DelayCall(m_ShoutCooldown, EndLock, from);
            }

            MLQuestSystem.TurnToFace(this, from);
            from.SendGump(m_Gump);

            // Paperdoll doesn't open
            // base.OnDoubleClick( from );
        }

        public override void OnThink()
        {
            base.OnThink();

            if (m_NextShout > Core.Now)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength("")].InitializePacket();

            foreach (var state in GetClientsInRange(12))
            {
                var m = state.Mobile;

                if (m.CanSee(this) && m.InLOS(this) && m.CanBeginAction(this))
                {
                    // Double Click On Me For Help!
                    var length = OutgoingMessagePackets.CreateMessageLocalized(
                        buffer, Serial, Body, MessageType.Regular, 946, 3, 1078099, Name
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length];
                    }

                    state.Send(buffer);
                }
            }

            m_NextShout = Core.Now + m_ShoutDelay;
        }

        private void EndLock(Mobile m)
        {
            m.EndAction(this);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Frozen = true;
        }
    }
}
