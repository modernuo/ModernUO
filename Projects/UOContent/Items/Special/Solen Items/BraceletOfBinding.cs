using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Factions;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Regions;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public class BraceletOfBinding : BaseBracelet, TranslocationItem
    {
        private BraceletOfBinding m_Bound;
        private int m_Charges;
        private string m_Inscription;
        private int m_Recharges;
        private TransportTimer m_Timer;

        [Constructible]
        public BraceletOfBinding() : base(0x1086)
        {
            Hue = 0x489;
            Weight = 1.0;

            m_Inscription = "";
        }

        public BraceletOfBinding(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Inscription
        {
            get => m_Inscription;
            set
            {
                m_Inscription = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BraceletOfBinding Bound
        {
            get
            {
                if (m_Bound?.Deleted == true)
                {
                    m_Bound = null;
                }

                return m_Bound;
            }
            set => m_Bound = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = Math.Clamp(value, 0, MaxCharges);
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Recharges
        {
            get => m_Recharges;
            set
            {
                m_Recharges = Math.Clamp(value, 0, MaxRecharges);
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxCharges => 20;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxRecharges => 255;

        public string TranslocationItemName => "bracelet of binding";

        public override void AddNameProperty(IPropertyList list)
        {
            // a bracelet of binding : ~1_val~ ~2_val~
            list.Add(1054000, $"{m_Charges}\t{m_Inscription.DefaultIfNullOrEmpty(" ")}");
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(
                from,
                1054000, // a bracelet of binding : ~1_val~ ~2_val~
                $"{m_Charges}\t{m_Inscription.DefaultIfNullOrEmpty(" ")}"
            );
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && IsChildOf(from))
            {
                var bound = Bound;

                list.Add(new BraceletEntry(Activate, 6170, bound != null));
                list.Add(new BraceletEntry(Search, 6171, bound != null));
                list.Add(new BraceletEntry(Bind, bound == null ? 6173 : 6174, true));
                list.Add(new BraceletEntry(Inscribe, 6175, true));
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            var bound = Bound;

            if (Bound == null)
            {
                Bind(from);
            }
            else
            {
                Activate(from);
            }
        }

        public void Activate(Mobile from)
        {
            var bound = Bound;

            if (Deleted || bound == null)
            {
                return;
            }

            if (!IsChildOf(from))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else if (m_Timer != null)
            {
                from.SendLocalizedMessage(
                    1054013
                ); // The bracelet is already attempting contact. You decide to wait a moment.
            }
            else
            {
                from.PlaySound(0xF9);
                from.LocalOverheadMessage(
                    MessageType.Regular,
                    0x5D,
                    true,
                    "* You concentrate on the bracelet to summon its power *"
                );

                from.Frozen = true;

                m_Timer = new TransportTimer(this, from);
                m_Timer.Start();
            }
        }

        public void Search(Mobile from)
        {
            var bound = Bound;

            if (Deleted || bound == null)
            {
                return;
            }

            if (!IsChildOf(from))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else
            {
                CheckUse(from, true);
            }
        }

        private bool CheckUse(Mobile from, bool successMessage)
        {
            var bound = Bound;

            if (bound == null)
            {
                return false;
            }

            var boundRoot = bound.RootParent as Mobile;

            if (Charges == 0)
            {
                from.SendLocalizedMessage(
                    1054005
                ); // The bracelet glows black. It must be charged before it can be used again.
                return false;
            }

            if (from.FindItemOnLayer(Layer.Bracelet) != this)
            {
                from.SendLocalizedMessage(1054004); // You must equip the bracelet in order to use its power.
                return false;
            }

            if (boundRoot?.NetState == null || boundRoot.FindItemOnLayer(Layer.Bracelet) != bound)
            {
                from.SendLocalizedMessage(
                    1054006
                ); // The bracelet emits a red glow. The bracelet's twin is not available for transport.
                return false;
            }

            if (!Core.AOS && from.Map != boundRoot.Map)
            {
                from.SendLocalizedMessage(1054014); // The bracelet glows black. The bracelet's target is on another facet.
                return false;
            }

            if (Sigil.ExistsOn(from))
            {
                from.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                return false;
            }

            if (!SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom))
            {
                return false;
            }

            if (!SpellHelper.CheckTravel(from, boundRoot.Map, boundRoot.Location, TravelCheckType.RecallTo))
            {
                return false;
            }

            if (boundRoot.Map == Map.Felucca && from is PlayerMobile mobile && mobile.Young)
            {
                mobile.SendLocalizedMessage(1049543); // You decide against traveling to Felucca while you are still young.
                return false;
            }

            if (from.Kills >= 5 && boundRoot.Map != Map.Felucca)
            {
                from.SendLocalizedMessage(1019004); // You are not allowed to travel there.
                return false;
            }

            if (from.Criminal)
            {
                from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }

            if (SpellHelper.CheckCombat(from))
            {
                from.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }

            if (WeightOverloading.IsOverloaded(from))
            {
                from.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
                return false;
            }

            if (from.Region.IsPartOf<JailRegion>())
            {
                from.SendLocalizedMessage(1114345, "", 0x35); // You'll need a better jailbreak plan than that!
                return false;
            }

            if (boundRoot.Region.IsPartOf<JailRegion>())
            {
                from.SendLocalizedMessage(1019004); // You are not allowed to travel there.
                return false;
            }

            if (successMessage)
            {
                from.SendLocalizedMessage(1054015); // The bracelet's twin is available for transport.
            }

            return true;
        }

        public void Bind(Mobile from)
        {
            if (Deleted)
            {
                return;
            }

            if (!IsChildOf(from))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else
            {
                from.SendLocalizedMessage(1054001); // Target the bracelet of binding you wish to bind this bracelet to.
                from.Target = new BindTarget(this);
            }
        }

        public void Inscribe(Mobile from)
        {
            if (Deleted)
            {
                return;
            }

            if (!IsChildOf(from))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else
            {
                from.SendLocalizedMessage(1054009); // Enter the text to inscribe upon the bracelet :
                from.Prompt = new InscribePrompt(this);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version

            writer.WriteEncodedInt(m_Recharges);

            writer.WriteEncodedInt(m_Charges);
            writer.Write(m_Inscription);
            writer.Write(Bound);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        m_Recharges = reader.ReadEncodedInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Charges = Math.Min(reader.ReadEncodedInt(), MaxCharges);
                        m_Inscription = reader.ReadString();
                        Bound = (BraceletOfBinding)reader.ReadEntity<Item>();
                        break;
                    }
            }
        }

        private delegate void BraceletCallback(Mobile from);

        private class BraceletEntry : ContextMenuEntry
        {
            private readonly BraceletCallback m_Callback;

            public BraceletEntry(BraceletCallback callback, int number, bool enabled) : base(number)
            {
                m_Callback = callback;

                if (!enabled)
                {
                    Flags |= CMEFlags.Disabled;
                }
            }

            public override void OnClick()
            {
                var from = Owner.From;

                if (from.CheckAlive())
                {
                    m_Callback(from);
                }
            }
        }

        private class TransportTimer : Timer
        {
            private readonly BraceletOfBinding m_Bracelet;
            private readonly Mobile m_From;

            public TransportTimer(BraceletOfBinding bracelet, Mobile from) : base(TimeSpan.FromSeconds(2.0))
            {
                m_Bracelet = bracelet;
                m_From = from;
            }

            protected override void OnTick()
            {
                m_Bracelet.m_Timer = null;
                m_From.Frozen = false;

                if (m_Bracelet.Deleted || m_From.Deleted ||
                    !m_Bracelet.CheckUse(m_From, false) ||
                    m_Bracelet.Bound.RootParent is not Mobile boundRoot)
                {
                    return;
                }

                m_Bracelet.Charges--;

                BaseCreature.TeleportPets(m_From, boundRoot.Location, boundRoot.Map, true);

                m_From.PlaySound(0x1FC);
                m_From.MoveToWorld(boundRoot.Location, boundRoot.Map);
                m_From.PlaySound(0x1FC);
            }
        }

        private class BindTarget : Target
        {
            private readonly BraceletOfBinding m_Bracelet;

            public BindTarget(BraceletOfBinding bracelet) : base(-1, false, TargetFlags.None) => m_Bracelet = bracelet;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Bracelet.Deleted)
                {
                    return;
                }

                if (!m_Bracelet.IsChildOf(from))
                {
                    from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                }
                else if (targeted is BraceletOfBinding bindBracelet)
                {
                    if (bindBracelet == m_Bracelet)
                    {
                        from.SendLocalizedMessage(1054012); // You cannot bind a bracelet of binding to itself!
                    }
                    else if (!bindBracelet.IsChildOf(from))
                    {
                        from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                    }
                    else
                    {
                        from.SendLocalizedMessage(
                            1054003
                        ); // You bind the bracelet to its counterpart. The bracelets glow with power.
                        from.PlaySound(0x1FA);

                        m_Bracelet.Bound = bindBracelet;
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1054002); // You can only bind this bracelet to another bracelet of binding!
                }
            }
        }

        private class InscribePrompt : Prompt
        {
            private readonly BraceletOfBinding m_Bracelet;

            public InscribePrompt(BraceletOfBinding bracelet) => m_Bracelet = bracelet;

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Bracelet.Deleted)
                {
                    return;
                }

                if (!m_Bracelet.IsChildOf(from))
                {
                    from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                }
                else
                {
                    from.SendLocalizedMessage(1054011); // You mark the bracelet with your inscription.
                    m_Bracelet.Inscription = text;
                }
            }

            public override void OnCancel(Mobile from)
            {
                from.SendLocalizedMessage(1054010); // You decide not to inscribe the bracelet at this time.
            }
        }
    }
}
