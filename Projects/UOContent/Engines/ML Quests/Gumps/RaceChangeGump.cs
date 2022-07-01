using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;

namespace Server.Engines.MLQuests.Gumps
{
    public interface IRaceChanger
    {
        bool CheckComplete(PlayerMobile from);
        void ConsumeNeeded(PlayerMobile from);
        void OnCancel(PlayerMobile from);
    }

    public class RaceChangeConfirmGump : Gump
    {
        private static Dictionary<NetState, RaceChangeState> m_Pending;
        private readonly PlayerMobile m_From;

        private readonly IRaceChanger m_Owner;
        private readonly Race m_Race;

        public RaceChangeConfirmGump(IRaceChanger owner, PlayerMobile from, Race targetRace)
            : base(50, 50)
        {
            from.CloseGump<RaceChangeConfirmGump>();

            m_Owner = owner;
            m_From = from;
            m_Race = targetRace;

            AddPage(0);
            AddBackground(0, 0, 240, 135, 0x2422);

            if (targetRace == Race.Human)
            {
                AddHtmlLocalized(15, 15, 210, 75, 1073643, 0); // Are you sure you wish to embrace your humanity?
            }
            else if (targetRace == Race.Elf)
            {
                AddHtmlLocalized(15, 15, 210, 75, 1073642, 0); // Are you sure you want to follow the elven ways?
            }
            else
            {
                AddHtml(15, 15, 210, 75, $"Are you sure you want to change your race to {targetRace.Name}?");
            }

            AddButton(160, 95, 0xF7, 0xF8, 1);
            AddButton(90, 95, 0xF2, 0xF1, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 0: // Cancel
                    {
                        m_Owner?.OnCancel(m_From);

                        break;
                    }
                case 1: // Okay
                    {
                        if (m_Owner?.CheckComplete(m_From) != false)
                        {
                            Offer(m_Owner, m_From, m_Race);
                        }

                        break;
                    }
            }
        }

        public static unsafe void Initialize()
        {
            m_Pending = new Dictionary<NetState, RaceChangeState>();

            IncomingExtendedCommandPackets.RegisterExtended(0x2A, true, &RaceChangeReply);
        }

        public static bool IsPending(NetState state) => state != null && m_Pending.ContainsKey(state);

        private static void Offer(IRaceChanger owner, PlayerMobile from, Race targetRace)
        {
            var ns = from.NetState;

            if (ns == null || !CanChange(from, targetRace))
            {
                return;
            }

            CloseCurrent(ns);

            m_Pending[ns] = new RaceChangeState(owner, ns, targetRace);
            ns.SendRaceChanger(from.Female, targetRace);
        }

        private static void CloseCurrent(NetState ns)
        {
            if (m_Pending.TryGetValue(ns, out var state))
            {
                state._timeoutToken.Cancel();
                m_Pending.Remove(ns);
            }

            ns.SendCloseRaceChanger();
        }

        private static void Timeout(NetState ns)
        {
            if (IsPending(ns))
            {
                m_Pending.Remove(ns);
                ns.SendCloseRaceChanger();
            }
        }

        public static bool IsWearingEquipment(Mobile from)
        {
            foreach (var item in from.Items)
            {
                switch (item.Layer)
                {
                    case Layer.Hair:
                    case Layer.FacialHair:
                    case Layer.Backpack:
                    case Layer.Mount:
                    case Layer.Bank:
                        {
                            continue; // ignore
                        }
                    default:
                        {
                            return true;
                        }
                }
            }

            return false;
        }

        private static bool CanChange(PlayerMobile from, Race targetRace)
        {
            if (from.Deleted)
            {
                return false;
            }

            if (from.Race == targetRace)
            {
                from.SendLocalizedMessage(1111918); // You are already that race.
            }
            else if (!MondainsLegacy.CheckML(from, false))
            {
                from.SendLocalizedMessage(1073651); // You must have Mondain's Legacy before proceeding...
            }
            else if (!from.Alive)
            {
                from.SendLocalizedMessage(1073646); // Only the living may proceed...
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1073647); // You may not continue while mounted...
            }
            else if (!from.CanBeginAction<PolymorphSpell>() || DisguiseTimers.IsDisguised(from) ||
                     AnimalForm.UnderTransformation(from) || !from.CanBeginAction<IncognitoSpell>() ||
                     from.IsBodyMod)                // TODO: Does this cover everything?
            {
                from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            }
            else if (from.Spell?.IsCasting == true)
            {
                from.SendLocalizedMessage(1073649); // One may not proceed while embracing magic...
            }
            else if (from.Poisoned)
            {
                from.SendLocalizedMessage(1073652); // You must be healthy to proceed...
            }
            else if (IsWearingEquipment(from))
            {
                from.SendLocalizedMessage(1073650); // To proceed you must be unburdened by equipment...
            }
            else
            {
                return true;
            }

            return false;
        }

        private static void RaceChangeReply(NetState state, CircularBufferReader reader, int packetLength)
        {
            if (!m_Pending.TryGetValue(state, out var raceChangeState))
            {
                return;
            }

            CloseCurrent(state);

            if (state.Mobile is not PlayerMobile pm)
            {
                return;
            }

            var owner = raceChangeState.m_Owner;
            var targetRace = raceChangeState.m_TargetRace;

            if (reader.Length == 5)
            {
                owner?.OnCancel(pm);

                return;
            }

            if (!CanChange(pm, targetRace) || owner?.CheckComplete(pm) == false)
            {
                return;
            }

            int hue = reader.ReadUInt16();
            int hairItemId = reader.ReadUInt16();
            int hairHue = reader.ReadUInt16();
            int facialHairItemId = reader.ReadUInt16();
            int facialHairHue = reader.ReadUInt16();

            pm.Race = targetRace;
            pm.Hue = targetRace.ClipSkinHue(hue) | 0x8000;

            if (targetRace.ValidateHair(pm, hairItemId))
            {
                pm.HairItemID = hairItemId;
                pm.HairHue = targetRace.ClipHairHue(hairHue);
            }
            else
            {
                pm.HairItemID = 0;
            }

            if (targetRace.ValidateFacialHair(pm, facialHairItemId))
            {
                pm.FacialHairItemID = facialHairItemId;
                pm.FacialHairHue = targetRace.ClipHairHue(facialHairHue);
            }
            else
            {
                pm.FacialHairItemID = 0;
            }

            if (targetRace == Race.Human)
            {
                pm.SendLocalizedMessage(1073654); // You are now fully human.
            }
            else if (targetRace == Race.Elf)
            {
                pm.SendLocalizedMessage(1073653); // You are now fully initiated into the Elven culture.
            }
            else
            {
                pm.SendMessage("You have fully changed your race to {0}.", targetRace.Name);
            }

            owner?.ConsumeNeeded(pm);
        }

        private class RaceChangeState
        {
            private static readonly TimeSpan m_TimeoutDelay = TimeSpan.FromMinutes(1);

            public readonly IRaceChanger m_Owner;
            public readonly Race m_TargetRace;
            public TimerExecutionToken _timeoutToken;

            public RaceChangeState(IRaceChanger owner, NetState ns, Race targetRace)
            {
                m_Owner = owner;
                m_TargetRace = targetRace;
                Timer.StartTimer(m_TimeoutDelay, () => Timeout(ns), out _timeoutToken);
            }
        }
    }

    public class RaceChangeDeed : Item, IRaceChanger
    {
        [Constructible]
        public RaceChangeDeed()
            : base(0x14F0) =>
            LootType = LootType.Blessed;

        public RaceChangeDeed(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "a race change deed";

        public bool CheckComplete(PlayerMobile pm)
        {
            if (Deleted)
            {
                return false;
            }

            if (!IsChildOf(pm.Backpack))
            {
                pm.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return false;
            }

            return true;
        }

        public void ConsumeNeeded(PlayerMobile pm)
        {
            Consume();
        }

        public void OnCancel(PlayerMobile pm)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is not PlayerMobile pm)
            {
                return;
            }

            if (CheckComplete(pm))
            {
                pm.SendGump(new RaceChangeConfirmGump(this, pm, pm.Race == Race.Human ? Race.Elf : Race.Human));
            }
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
        }
    }
}
