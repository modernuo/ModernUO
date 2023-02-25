using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Engines.Stealables;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class Stealing
    {
        public static readonly bool ClassicMode = false;
        public static readonly bool SuspendOnMurder = false;

        public static void Initialize()
        {
            SkillInfo.Table[33].Callback = OnUse;
        }

        public static bool IsInGuild(Mobile m) => m is PlayerMobile mobile && mobile.NpcGuild == NpcGuild.ThievesGuild;

        public static bool IsInnocentTo(Mobile from, Mobile to) => Notoriety.Compute(from, to) == Notoriety.Innocent;

        public static bool IsEmptyHanded(Mobile from)
        {
            if (from.FindItemOnLayer(Layer.OneHanded) != null)
            {
                return false;
            }

            if (from.FindItemOnLayer(Layer.TwoHanded) != null)
            {
                return false;
            }

            return true;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (!IsEmptyHanded(m))
            {
                m.SendLocalizedMessage(1005584); // Both hands must be free to steal.
            }
            else if (m.Region.IsPartOf<SafeZone>())
            {
                m.SendMessage("You may not steal in this area.");
            }
            else
            {
                m.Target = new StealingTarget(m);
                m.RevealingAction();

                m.SendLocalizedMessage(502698); // Which item do you want to steal?
            }

            return TimeSpan.FromSeconds(10.0);
        }

        private class StealingTarget : Target
        {
            private readonly Mobile m_Thief;

            public StealingTarget(Mobile thief) : base(1, false, TargetFlags.None)
            {
                m_Thief = thief;
                AllowNonlocal = true;
            }

            private Item TryStealItem(Item toSteal, ref bool caught)
            {
                Item stolen = null;

                var root = toSteal.RootParent;
                var mobRoot = root as Mobile;

                StealableArtifacts.StealableInstance si = toSteal.Parent == null || !toSteal.Movable
                    ? StealableArtifacts.GetStealableInstance(toSteal)
                    : null;

                if (!IsEmptyHanded(m_Thief))
                {
                    m_Thief.SendLocalizedMessage(1005584); // Both hands must be free to steal.
                }
                else if (m_Thief.Region.IsPartOf<SafeZone>())
                {
                    m_Thief.SendMessage("You may not steal in this area.");
                }
                else if (mobRoot?.Player == true && !IsInGuild(m_Thief))
                {
                    m_Thief.SendLocalizedMessage(1005596); // You must be in the thieves guild to steal from other players.
                }
                else if (SuspendOnMurder && mobRoot?.Player == true && IsInGuild(m_Thief) &&
                         m_Thief.Kills > 0)
                {
                    m_Thief.SendLocalizedMessage(502706); // You are currently suspended from the thieves guild.
                }
                else if (root is BaseVendor vendor && vendor.IsInvulnerable)
                {
                    m_Thief.SendLocalizedMessage(1005598); // You can't steal from shopkeepers.
                }
                else if (root is PlayerVendor)
                {
                    m_Thief.SendLocalizedMessage(502709); // You can't steal from vendors.
                }
                else if (!m_Thief.CanSee(toSteal))
                {
                    m_Thief.SendLocalizedMessage(500237); // Target can not be seen.
                }
                else if (m_Thief.Backpack?.CheckHold(m_Thief, toSteal, false, true) != true)
                {
                    m_Thief.SendLocalizedMessage(1048147); // Your backpack can't hold anything else.
                }
                else if (toSteal is Sigil sig)
                {
                    var pl = PlayerState.Find(m_Thief);
                    var faction = pl?.Faction;

                    if (!m_Thief.InRange(sig.GetWorldLocation(), 1))
                    {
                        m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                    }
                    else if (root != null) // not on the ground
                    {
                        m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                    }
                    else if (faction != null)
                    {
                        if (!m_Thief.CanBeginAction<IncognitoSpell>())
                        {
                            m_Thief.SendLocalizedMessage(1010581); // You cannot steal the sigil when you are incognito
                        }
                        else if (DisguisePersistence.IsDisguised(m_Thief))
                        {
                            m_Thief.SendLocalizedMessage(1010583); // You cannot steal the sigil while disguised
                        }
                        else if (!m_Thief.CanBeginAction<PolymorphSpell>())
                        {
                            m_Thief.SendLocalizedMessage(1010582); // You cannot steal the sigil while polymorphed
                        }
                        else if (TransformationSpellHelper.UnderTransformation(m_Thief))
                        {
                            m_Thief.SendLocalizedMessage(1061622); // You cannot steal the sigil while in that form.
                        }
                        else if (AnimalForm.UnderTransformation(m_Thief))
                        {
                            m_Thief.SendLocalizedMessage(1063222); // You cannot steal the sigil while mimicking an animal.
                        }
                        else if (pl.IsLeaving)
                        {
                            // You are currently quitting a faction and cannot steal the town sigil
                            m_Thief.SendLocalizedMessage(1005589);
                        }
                        else if (sig.IsBeingCorrupted && sig.LastMonolith.Faction == faction)
                        {
                            m_Thief.SendLocalizedMessage(1005590); // You cannot steal your own sigil
                        }
                        else if (sig.IsPurifying)
                        {
                            m_Thief.SendLocalizedMessage(1005592); // You cannot steal this sigil until it has been purified
                        }
                        else if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, 80.0, 80.0))
                        {
                            if (Sigil.ExistsOn(m_Thief))
                            {
                                // The sigil has gone back to its home location because you already have a sigil.
                                m_Thief.SendLocalizedMessage(1010258);
                            }
                            else if (m_Thief.Backpack?.CheckHold(m_Thief, sig, false, true) != true)
                            {
                                // The sigil has gone home because your backpack is full
                                m_Thief.SendLocalizedMessage(1010259);
                            }
                            else
                            {
                                if (sig.IsBeingCorrupted)
                                {
                                    sig.GraceStart = Core.Now; // begin grace period
                                }

                                m_Thief.SendLocalizedMessage(1010586); // YOU STOLE THE SIGIL!!!   (woah, calm down now)

                                if (sig.LastMonolith?.Sigil != null)
                                {
                                    sig.LastMonolith.Sigil = null;
                                    sig.LastStolen = Core.Now;
                                }

                                return sig;
                            }
                        }
                        else
                        {
                            m_Thief.SendLocalizedMessage(1005594); // You do not have enough skill to steal the sigil
                        }
                    }
                    else
                    {
                        m_Thief.SendLocalizedMessage(1005588); // You must join a faction to do that
                    }
                }
                else if (si == null && (toSteal.Parent == null || !toSteal.Movable))
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (toSteal.LootType == LootType.Newbied || toSteal.CheckBlessed(mobRoot))
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (Core.AOS && si == null && toSteal is Container)
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (!m_Thief.InRange(toSteal.GetWorldLocation(), 1))
                {
                    m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                }
                else if (si != null && m_Thief.Skills.Stealing.Value < 100.0)
                {
                    // You're not skilled enough to attempt the theft of this item.
                    m_Thief.SendLocalizedMessage(1060025, "", 0x66D);
                }
                else if (toSteal.Parent is Mobile)
                {
                    m_Thief.SendLocalizedMessage(1005585); // You cannot steal items which are equipped.
                }
                else if (root == m_Thief)
                {
                    m_Thief.SendLocalizedMessage(502704); // You catch yourself red-handed.
                }
                else if (mobRoot?.AccessLevel > AccessLevel.Player)
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (mobRoot != null && !m_Thief.CanBeHarmful(mobRoot))
                {
                }
                else if (root is Corpse)
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else
                {
                    var w = toSteal.Weight + toSteal.TotalWeight;

                    if (w > 10)
                    {
                        m_Thief.SendMessage("That is too heavy to steal.");
                    }
                    else
                    {
                        if (toSteal.Stackable && toSteal.Amount > 1)
                        {
                            var maxAmount = Math.Clamp(
                                (int)(m_Thief.Skills.Stealing.Value / 10.0 / toSteal.Weight),
                                1,
                                toSteal.Amount
                            );

                            var amount = Utility.RandomMinMax(1, maxAmount);

                            if (amount >= toSteal.Amount)
                            {
                                var pileWeight = (int)Math.Ceiling(toSteal.Weight * toSteal.Amount);
                                pileWeight *= 10;

                                if (m_Thief.CheckTargetSkill(
                                    SkillName.Stealing,
                                    toSteal,
                                    pileWeight - 22.5,
                                    pileWeight + 27.5
                                ))
                                {
                                    stolen = toSteal;
                                }
                            }
                            else
                            {
                                var pileWeight = (int)Math.Ceiling(toSteal.Weight * amount);
                                pileWeight *= 10;

                                if (m_Thief.CheckTargetSkill(
                                    SkillName.Stealing,
                                    toSteal,
                                    pileWeight - 22.5,
                                    pileWeight + 27.5
                                ))
                                {
                                    stolen = Mobile.LiftItemDupe(toSteal, toSteal.Amount - amount) ?? toSteal;
                                }
                            }
                        }
                        else
                        {
                            var iw = (int)Math.Ceiling(w);
                            iw *= 10;

                            if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, iw - 22.5, iw + 27.5))
                            {
                                stolen = toSteal;
                            }
                        }

                        if (stolen != null)
                        {
                            m_Thief.SendLocalizedMessage(502724); // You successfully steal the item.

                            if (si != null)
                            {
                                toSteal.Movable = true;
                                si.Item = null;
                            }
                        }
                        else
                        {
                            m_Thief.SendLocalizedMessage(502723); // You fail to steal the item.
                        }

                        caught = m_Thief.Skills.Stealing.Value < Utility.Random(150);
                    }
                }

                return stolen;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                from.RevealingAction();

                Item stolen = null;
                IEntity root = null;
                var caught = false;

                if (target is Item item)
                {
                    root = item.RootParent;
                    stolen = TryStealItem(item, ref caught);
                }
                else if (target is Mobile mobile)
                {
                    var pack = mobile.Backpack;

                    if (pack?.Items.Count > 0)
                    {
                        root = mobile;
                        stolen = TryStealItem(pack.Items.RandomElement(), ref caught);
                    }
                }
                else
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }

                var mobRoot = root as Mobile;

                if (stolen != null)
                {
                    from.AddToBackpack(stolen);

                    if (!(stolen is Container || stolen.Stackable))
                    {
                        StolenItem.Add(stolen, m_Thief, mobRoot);
                    }
                }

                var corpse = root as Corpse;

                if (caught)
                {
                    if (root == null || corpse?.IsCriminalAction(m_Thief) == true)
                    {
                        m_Thief.CriminalAction(false);
                    }
                    else if (mobRoot != null)
                    {
                        if (!IsInGuild(mobRoot) && IsInnocentTo(m_Thief, mobRoot))
                        {
                            m_Thief.CriminalAction(false);
                        }

                        var message = $"You notice {m_Thief.Name} trying to steal from {mobRoot.Name}.";

                        foreach (var ns in m_Thief.GetClientsInRange(8))
                        {
                            if (ns.Mobile != m_Thief)
                            {
                                ns.Mobile.SendMessage(message);
                            }
                        }
                    }
                }
                else if (corpse?.IsCriminalAction(m_Thief) == true)
                {
                    m_Thief.CriminalAction(false);
                }

                if (mobRoot?.Player == true && m_Thief is PlayerMobile pm &&
                    IsInnocentTo(pm, mobRoot) && !IsInGuild(mobRoot))
                {
                    pm.PermaFlags.Add(mobRoot);
                    pm.Delta(MobileDelta.Noto);
                }
            }
        }
    }

    public class StolenItem
    {
        public static readonly TimeSpan StealTime = TimeSpan.FromMinutes(2.0);

        private static readonly Queue<StolenItem> m_Queue = new();

        public StolenItem(Item stolen, Mobile thief, Mobile victim)
        {
            Stolen = stolen;
            Thief = thief;
            Victim = victim;

            Expires = Core.Now + StealTime;
        }

        public Item Stolen { get; }

        public Mobile Thief { get; }

        public Mobile Victim { get; }

        public DateTime Expires { get; private set; }

        public bool IsExpired => Core.Now >= Expires;

        public static void Add(Item item, Mobile thief, Mobile victim)
        {
            Clean();

            m_Queue.Enqueue(new StolenItem(item, thief, victim));
        }

        public static bool IsStolen(Item item)
        {
            Mobile victim = null;

            return IsStolen(item, ref victim);
        }

        public static bool IsStolen(Item item, ref Mobile victim)
        {
            Clean();

            foreach (var si in m_Queue)
            {
                if (si.Stolen == item && !si.IsExpired)
                {
                    victim = si.Victim;
                    return true;
                }
            }

            return false;
        }

        public static void ReturnOnDeath(Mobile killed, Container corpse)
        {
            Clean();

            foreach (var si in m_Queue)
            {
                if (si.Stolen.RootParent == corpse && si.Victim != null && !si.IsExpired)
                {
                    if (si.Victim.AddToBackpack(si.Stolen))
                    {
                        si.Victim.SendLocalizedMessage(1010464); // the item that was stolen is returned to you.
                    }
                    else
                    {
                        si.Victim.SendLocalizedMessage(1010463); // the item that was stolen from you falls to the ground.
                    }

                    si.Expires = Core.Now; // such a hack
                }
            }
        }

        public static void Clean()
        {
            while (m_Queue.Count > 0)
            {
                var si = m_Queue.Peek();

                if (si.IsExpired)
                {
                    m_Queue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
