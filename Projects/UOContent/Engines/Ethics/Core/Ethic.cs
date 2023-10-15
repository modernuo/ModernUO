using Server.Ethics.Evil;
using Server.Ethics.Hero;
using Server.Items;
using Server.Mobiles;

namespace Server.Ethics
{
    public abstract class Ethic
    {
        public static readonly Ethic Hero = new HeroEthic();
        public static readonly Ethic Evil = new EvilEthic();

        public static readonly Ethic[] Ethics =
        {
            Hero,
            Evil
        };

        protected EthicDefinition m_Definition;

        protected PlayerCollection m_Players;

        public Ethic() => m_Players = new PlayerCollection();
        public static bool Enabled { get; private set; }

        public EthicDefinition Definition => m_Definition;

        public PlayerCollection Players => m_Players;

        public static Ethic Find(Item item)
        {
            if ((item.SavedFlags & 0x100) != 0)
            {
                if (item.Hue == Hero.Definition.PrimaryHue)
                {
                    return Hero;
                }

                item.SavedFlags &= ~0x100;
            }

            if ((item.SavedFlags & 0x200) != 0)
            {
                if (item.Hue == Evil.Definition.PrimaryHue)
                {
                    return Evil;
                }

                item.SavedFlags &= ~0x200;
            }

            return null;
        }

        public static bool CheckTrade(Mobile from, Mobile to, Mobile newOwner, Item item)
        {
            var itemEthic = Find(item);

            if (itemEthic == null || Find(newOwner) == itemEthic)
            {
                return true;
            }

            if (itemEthic == Hero)
            {
                (from == newOwner ? to : from).SendMessage("Only heros may receive this item.");
            }
            else if (itemEthic == Evil)
            {
                (from == newOwner ? to : from).SendMessage("Only the evil may receive this item.");
            }

            return false;
        }

        public static bool CheckEquip(Mobile from, Item item)
        {
            var itemEthic = Find(item);

            if (itemEthic == null || Find(from) == itemEthic)
            {
                return true;
            }

            if (itemEthic == Hero)
            {
                from.SendMessage("Only heros may wear this item.");
            }
            else if (itemEthic == Evil)
            {
                from.SendMessage("Only the evil may wear this item.");
            }

            return false;
        }

        public static bool IsImbued(Item item) => IsImbued(item, false);

        public static bool IsImbued(Item item, bool recurse)
        {
            if (Find(item) != null)
            {
                return true;
            }

            if (recurse)
            {
                foreach (var child in item.Items)
                {
                    if (IsImbued(child, true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("ethics.enable", false);
        }

        public static void Initialize()
        {
            if (Enabled)
            {
                EventSink.Speech += EventSink_Speech;
            }
        }

        public static void EventSink_Speech(SpeechEventArgs e)
        {
            if (e.Blocked || e.Handled)
            {
                return;
            }

            var pl = Player.Find(e.Mobile);

            if (pl == null)
            {
                for (var i = 0; i < Ethics.Length; ++i)
                {
                    var ethic = Ethics[i];

                    if (!ethic.IsEligible(e.Mobile))
                    {
                        continue;
                    }

                    if (!ethic.Definition.JoinPhrase.String.InsensitiveEquals(e.Speech))
                    {
                        continue;
                    }

                    var found = false;

                    foreach (var item in e.Mobile.GetItemsInRange(2))
                    {
                        if (item is AnkhNorth or AnkhWest)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        continue;
                    }

                    pl = new Player(ethic, e.Mobile);

                    pl.Attach();

                    e.Mobile.FixedEffect(0x373A, 10, 30);
                    e.Mobile.PlaySound(0x209);

                    e.Handled = true;
                    break;
                }
            }
            else
            {
                if (e.Mobile is PlayerMobile mobile && mobile.DuelContext != null)
                {
                    return;
                }

                var ethic = pl.Ethic;

                for (var i = 0; i < ethic.Definition.Powers.Length; ++i)
                {
                    var power = ethic.Definition.Powers[i];

                    if (!power.Definition.Phrase.String.InsensitiveEquals(e.Speech))
                    {
                        continue;
                    }

                    if (!power.CheckInvoke(pl))
                    {
                        continue;
                    }

                    power.BeginInvoke(pl);
                    e.Handled = true;

                    break;
                }
            }
        }

        public static Ethic Find(Mobile mob) => Find(mob, false, false);

        public static Ethic Find(Mobile mob, bool inherit) => Find(mob, inherit, false);

        public static Ethic Find(Mobile mob, bool inherit, bool allegiance)
        {
            var pl = Player.Find(mob);

            if (pl != null)
            {
                return pl.Ethic;
            }

            if (inherit && mob is BaseCreature bc)
            {
                if (bc.Controlled)
                {
                    return Find(bc.ControlMaster, false);
                }

                if (bc.Summoned)
                {
                    return Find(bc.SummonMaster, false);
                }

                if (allegiance)
                {
                    return bc.EthicAllegiance;
                }
            }

            return null;
        }

        public abstract bool IsEligible(Mobile mob);

        public virtual void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        var playerCount = reader.ReadEncodedInt();

                        for (var i = 0; i < playerCount; ++i)
                        {
                            var pl = new Player(this, reader);

                            if (pl.Mobile != null)
                            {
                                Timer.StartTimer(pl.CheckAttach);
                            }
                        }

                        break;
                    }
            }
        }

        public virtual void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_Players.Count);

            for (var i = 0; i < m_Players.Count; ++i)
            {
                m_Players[i].Serialize(writer);
            }
        }
    }
}
