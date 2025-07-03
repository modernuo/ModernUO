using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
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

namespace Server.SkillHandlers;

public static class Stealing
{
    public static bool ClassicMode { get; private set; }

    public static bool SuspendOnMurder { get; private set; }

    public static int MaxWeightToSteal { get; private set; }

    public static bool CanStealContainers { get; private set; }

    public static void Configure()
    {
        ClassicMode = ServerConfiguration.GetSetting("stealing.classicMode", !Core.AOS);
        SuspendOnMurder = ServerConfiguration.GetSetting("stealing.suspendOnMurder", !Core.AOS);
        CanStealContainers = ServerConfiguration.GetSetting("stealing.canStealContainers", !Core.AOS);
        MaxWeightToSteal = ServerConfiguration.GetSetting("stealing.maxWeightToSteal", 10);
    }

    public static void Initialize()
    {
        SkillInfo.Table[33].Callback = OnUse;
    }

    public static bool IsInGuild(Mobile m) => m is PlayerMobile mobile && mobile.NpcGuild == NpcGuild.ThievesGuild;

    public static bool IsInnocentTo(Mobile from, Mobile to) => Notoriety.Compute(from, to) == Notoriety.Innocent;

    public static bool IsEmptyHanded(Mobile from) =>
        from.FindItemOnLayer(Layer.OneHanded) == null && from.FindItemOnLayer(Layer.TwoHanded) == null;

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

        return TimeSpan.FromSeconds(30.0);
    }

    private class StealingTarget : Target
    {
        private readonly Mobile _thief;

        public StealingTarget(Mobile thief) : base(1, false, TargetFlags.None)
        {
            _thief = thief;
            AllowNonlocal = true;
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            from.NextSkillTime = Core.TickCount;
        }

        private Item TryStealItem(Item toSteal, ref bool caught)
        {
            Item stolen = null;

            var root = toSteal.RootParent;
            var mobRoot = root as Mobile;
            var rootIsPlayer = mobRoot?.Player == true;

            var si = toSteal.Parent == null || !toSteal.Movable
                ? StealableArtifacts.GetStealableInstance(toSteal)
                : null;

            if (!IsEmptyHanded(_thief))
            {
                _thief.SendLocalizedMessage(1005584); // Both hands must be free to steal.
            }
            else if (_thief.Region.IsPartOf<SafeZone>())
            {
                _thief.SendMessage("You may not steal in this area.");
            }
            else if ((_thief as PlayerMobile)?.Young == true && (rootIsPlayer || mobRoot is BaseCreature))
            {
                _thief.SendLocalizedMessage(502700); // You cannot steal from people or monsters right now.  Practice on chests and barrels.
            }
            else if (rootIsPlayer && !IsInGuild(_thief))
            {
                _thief.SendLocalizedMessage(1005596); // You must be in the thieves guild to steal from other players.
            }
            else if (SuspendOnMurder && rootIsPlayer && IsInGuild(_thief) && _thief.Kills > 0)
            {
                _thief.SendLocalizedMessage(502706); // You are currently suspended from the thieves guild.
            }
            else if ((mobRoot as PlayerMobile)?.Young == true)
            {
                _thief.SendLocalizedMessage(502699); // You cannot steal from the Young.
            }
            else if ((root as BaseVendor)?.IsInvulnerable == true)
            {
                _thief.SendLocalizedMessage(1005598); // You can't steal from shopkeepers.
            }
            else if (root is PlayerVendor)
            {
                _thief.SendLocalizedMessage(502709); // You can't steal from vendors.
            }
            else if (!_thief.CanSee(toSteal))
            {
                _thief.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (toSteal is Sigil sig)
            {
                var pl = PlayerState.Find(_thief);
                var faction = pl?.Faction;

                if (!_thief.InRange(sig.GetWorldLocation(), 1))
                {
                    _thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                }
                else if (root != null) // not on the ground
                {
                    _thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (faction == null)
                {
                    _thief.SendLocalizedMessage(1005588); // You must join a faction to do that
                }
                else if (!_thief.CanBeginAction<IncognitoSpell>())
                {
                    _thief.SendLocalizedMessage(1010581); // You cannot steal the sigil when you are incognito
                }
                else if (DisguisePersistence.IsDisguised(_thief))
                {
                    _thief.SendLocalizedMessage(1010583); // You cannot steal the sigil while disguised
                }
                else if (!_thief.CanBeginAction<PolymorphSpell>())
                {
                    _thief.SendLocalizedMessage(1010582); // You cannot steal the sigil while polymorphed
                }
                else if (TransformationSpellHelper.UnderTransformation(_thief))
                {
                    _thief.SendLocalizedMessage(1061622); // You cannot steal the sigil while in that form.
                }
                else if (AnimalForm.UnderTransformation(_thief))
                {
                    _thief.SendLocalizedMessage(1063222); // You cannot steal the sigil while mimicking an animal.
                }
                else if (pl.IsLeaving)
                {
                    // You are currently quitting a faction and cannot steal the town sigil
                    _thief.SendLocalizedMessage(1005589);
                }
                else if (sig.IsBeingCorrupted && sig.LastMonolith.Faction == faction)
                {
                    _thief.SendLocalizedMessage(1005590); // You cannot steal your own sigil
                }
                else if (sig.IsPurifying)
                {
                    _thief.SendLocalizedMessage(1005592); // You cannot steal this sigil until it has been purified
                }
                else if (!_thief.CheckTargetSkill(SkillName.Stealing, toSteal, 80.0, 80.0))
                {
                    _thief.SendLocalizedMessage(1005594); // You do not have enough skill to steal the sigil
                }
                else if (Sigil.ExistsOn(_thief))
                {
                    // The sigil has gone back to its home location because you already have a sigil.
                    _thief.SendLocalizedMessage(1010258);
                }
                else if (_thief.Backpack?.CheckHold(_thief, sig, false, true) != true)
                {
                    // The sigil has gone home because your backpack is full
                    _thief.SendLocalizedMessage(1010259);
                }
                else
                {
                    if (sig.IsBeingCorrupted)
                    {
                        sig.GraceStart = Core.Now; // begin grace period
                    }

                    _thief.SendLocalizedMessage(1010586); // YOU STOLE THE SIGIL!!!   (woah, calm down now)

                    if (sig.LastMonolith?.Sigil != null)
                    {
                        sig.LastMonolith.Sigil = null;
                        sig.LastStolen = Core.Now;
                    }

                    return sig;
                }
            }
            else if (_thief.Backpack?.CheckHold(_thief, toSteal, false, true) != true)
            {
                _thief.SendLocalizedMessage(1048147); // Your backpack can't hold anything else.
            }
            else if (si == null && (toSteal.Parent == null || !toSteal.Movable) || toSteal.LootType == LootType.Newbied ||
                     toSteal.CheckBlessed(mobRoot) || !CanStealContainers && si == null && toSteal is Container)
            {
                _thief.SendLocalizedMessage(502710); // You can't steal that!
            }
            else if (!_thief.InRange(toSteal.GetWorldLocation(), 1))
            {
                _thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
            }
            else if (si != null && _thief.Skills.Stealing.Value < 100.0)
            {
                // You're not skilled enough to attempt the theft of this item.
                _thief.SendLocalizedMessage(1060025, "", 0x66D);
            }
            else if (toSteal.Parent is Mobile)
            {
                _thief.SendLocalizedMessage(1005585); // You cannot steal items which are equipped.
            }
            else if (root == _thief)
            {
                _thief.SendLocalizedMessage(502704); // You catch yourself red-handed.
            }
            else if (mobRoot?.AccessLevel > AccessLevel.Player)
            {
                _thief.SendLocalizedMessage(502710); // You can't steal that!
            }
            else if (mobRoot != null && !_thief.CanBeHarmful(mobRoot))
            {
            }
            else if (root is Corpse)
            {
                _thief.SendLocalizedMessage(502710); // You can't steal that!
            }
            else
            {
                var w = toSteal.Weight + toSteal.TotalWeight;

                if (w > MaxWeightToSteal)
                {
                    // This item is too heavy to steal from someone's backpack.
                    _thief.SendLocalizedMessage(502722);
                }
                else
                {
                    if (toSteal.Stackable && toSteal.Amount > 1)
                    {
                        var maxAmount = Math.Clamp(
                            (int)(_thief.Skills.Stealing.Value / 10.0 / toSteal.Weight),
                            1,
                            toSteal.Amount
                        );

                        var amount = Utility.RandomMinMax(1, maxAmount);

                        if (amount >= toSteal.Amount)
                        {
                            var pileWeight = (int)Math.Ceiling(toSteal.Weight * toSteal.Amount);
                            pileWeight *= 10;

                            if (_thief.CheckTargetSkill(
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

                            if (_thief.CheckTargetSkill(
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

                        if (_thief.CheckTargetSkill(SkillName.Stealing, toSteal, iw - 22.5, iw + 27.5))
                        {
                            stolen = toSteal;
                        }
                    }

                    if (stolen != null)
                    {
                        _thief.SendLocalizedMessage(502724); // You successfully steal the item.

                        if (si != null)
                        {
                            toSteal.Movable = true;
                            si.Item = null;
                        }
                    }
                    else
                    {
                        _thief.SendLocalizedMessage(502723); // You fail to steal the item.
                    }

                    caught = _thief.Skills.Stealing.Value < Utility.Random(150);
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
                _thief.SendLocalizedMessage(502710); // You can't steal that!
            }

            var mobRoot = root as Mobile;

            if (stolen != null)
            {
                from.AddToBackpack(stolen);

                if (!(stolen is Container || stolen.Stackable))
                {
                    StolenItem.Add(stolen, _thief, mobRoot);
                }
            }

            var corpse = root as Corpse;

            if (caught)
            {
                if (root == null || corpse?.IsCriminalAction(_thief) == true)
                {
                    _thief.CriminalAction(false);
                }
                else if (mobRoot != null)
                {
                    if (!IsInGuild(mobRoot) && IsInnocentTo(_thief, mobRoot))
                    {
                        _thief.CriminalAction(false);
                    }

                    var message = $"You notice {_thief.Name} trying to steal from {mobRoot.Name}.";

                    foreach (var ns in _thief.GetClientsInRange(8))
                    {
                        if (ns.Mobile != _thief)
                        {
                            ns.Mobile.SendMessage(message);
                        }
                    }
                }
            }
            else if (corpse?.IsCriminalAction(_thief) == true)
            {
                _thief.CriminalAction(false);
            }

            if (mobRoot?.Player == true && _thief is PlayerMobile pm &&
                IsInnocentTo(pm, mobRoot) && !IsInGuild(mobRoot))
            {
                pm.PermaFlags.Add(mobRoot);
                pm.Delta(MobileDelta.Noto);
            }

            from.NextSkillTime = Core.TickCount + 10000; // 10 seconds cooldown
        }
    }
}

public class StolenItem
{
    public static readonly TimeSpan StealTime = TimeSpan.FromMinutes(2.0);

    private static readonly Queue<StolenItem> _queue = [];

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

        _queue.Enqueue(new StolenItem(item, thief, victim));
    }

    public static bool IsStolen(Item item)
    {
        Mobile victim = null;

        return IsStolen(item, ref victim);
    }

    public static bool IsStolen(Item item, ref Mobile victim)
    {
        Clean();

        foreach (var si in _queue)
        {
            if (si.Stolen == item && !si.IsExpired)
            {
                victim = si.Victim;
                return true;
            }
        }

        return false;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void ReturnOnDeath(Mobile killed)
    {
        Clean();

        var corpse = killed.Corpse;

        foreach (var si in _queue)
        {
            if (si.Stolen.RootParent != corpse || si.Victim == null || si.IsExpired)
            {
                continue;
            }

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

    public static void Clean()
    {
        while (_queue.Count > 0)
        {
            var si = _queue.Peek();

            if (!si.IsExpired)
            {
                break;
            }

            _queue.Dequeue();
        }
    }
}
