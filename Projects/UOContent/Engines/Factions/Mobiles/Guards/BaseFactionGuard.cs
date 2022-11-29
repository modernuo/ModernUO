using System;
using Server.Factions.AI;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
    public abstract class BaseFactionGuard : BaseCreature
    {
        private const int ListenRange = 12;

        private static readonly Type[] m_StrongPotions =
        {
            typeof(GreaterHealPotion), typeof(GreaterHealPotion), typeof(GreaterHealPotion),
            typeof(GreaterCurePotion), typeof(GreaterCurePotion), typeof(GreaterCurePotion),
            typeof(GreaterStrengthPotion), typeof(GreaterStrengthPotion),
            typeof(GreaterAgilityPotion), typeof(GreaterAgilityPotion),
            typeof(TotalRefreshPotion), typeof(TotalRefreshPotion),
            typeof(GreaterExplosionPotion)
        };

        private static readonly Type[] m_WeakPotions =
        {
            typeof(HealPotion), typeof(HealPotion), typeof(HealPotion),
            typeof(CurePotion), typeof(CurePotion), typeof(CurePotion),
            typeof(StrengthPotion), typeof(StrengthPotion),
            typeof(AgilityPotion), typeof(AgilityPotion),
            typeof(RefreshPotion), typeof(RefreshPotion),
            typeof(ExplosionPotion)
        };

        private Faction m_Faction;

        private DateTime m_OrdersEnd;
        private Town m_Town;

        public BaseFactionGuard(string title) : base(AIType.AI_Melee)
        {
            Orders = new Orders(this);
            Title = title;

            RangeHome = 6;
        }

        public BaseFactionGuard(Serial serial) : base(serial)
        {
        }

        public override bool BardImmune => true;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Faction Faction
        {
            get => m_Faction;
            set
            {
                Unregister();
                m_Faction = value;
                Register();
            }
        }

        public Orders Orders { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Town Town
        {
            get => m_Town;
            set
            {
                Unregister();
                m_Town = value;
                Register();
            }
        }

        public abstract GuardAI GuardAI { get; }

        protected override BaseAI ForcedAI => new FactionGuardAI(this);

        public override TimeSpan ReacquireDelay => TimeSpan.FromSeconds(2.0);

        public override bool ClickTitle => false;

        public void Register()
        {
            if (m_Town != null && m_Faction != null)
            {
                m_Town.RegisterGuard(this);
            }
        }

        public void Unregister()
        {
            m_Town?.UnregisterGuard(this);
        }

        public override bool IsEnemy(Mobile m)
        {
            var ourFaction = m_Faction;
            var theirFaction = Faction.Find(m);

            if (theirFaction == null && m is BaseFactionGuard guard)
            {
                theirFaction = guard.Faction;
            }

            if (ourFaction != null && theirFaction != null && ourFaction != theirFaction)
            {
                var reactionType = Orders.GetReaction(theirFaction).Type;

                if (reactionType == ReactionType.Attack)
                {
                    return true;
                }

                var list = m.Aggressed;

                for (var i = 0; i < list.Count; ++i)
                {
                    var ai = list[i];

                    if (ai.Defender is BaseFactionGuard bf && bf.Faction == ourFaction)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Player && m.Alive && InRange(m, 10) && !InRange(oldLocation, 10) && InLOS(m) &&
                Orders.GetReaction(Faction.Find(m)).Type == ReactionType.Warn)
            {
                Direction = GetDirectionTo(m);

                var warning = Utility.Random(6) switch
                {
                    0 => $"I warn you, {m.Name}, you would do well to leave this area before someone shows you the world of gray.",
                    1 => $"It would be wise to leave this area, {m.Name}, lest your head become my commanders' trophy.",
                    2 => $"You are bold, {m.Name}, for one of the meager {Faction.Find(m)?.Definition.FriendlyName ?? "civilians"}. Leave now, lest you be taught the taste of dirt.",
                    3 => $"Your presence here is an insult, {m.Name}. Be gone now, knave.",
                    4 => $"Dost thou wish to be hung by your toes, {m.Name}? Nay? Then come no closer.",
                    _ => $"Hey, {m.Name}. Yeah, you. Get out of here before I beat you with a stick." // 5
                };

                Say(warning);
            }
        }

        public override bool HandlesOnSpeech(Mobile from) => InRange(from, ListenRange) || base.HandlesOnSpeech(from);

        private void ChangeReaction(Faction faction, ReactionType type)
        {
            if (faction == null)
            {
                switch (type)
                {
                    case ReactionType.Ignore:
                        {
                            Say(1005179);
                            break; // Civilians will now be ignored.
                        }
                    case ReactionType.Warn:
                        {
                            Say(1005180);
                            break; // Civilians will now be warned of their impending deaths.
                        }
                    case ReactionType.Attack:
                        {
                            return;
                        }
                }
            }
            else
            {
                var def = type switch
                {
                    ReactionType.Ignore => faction.Definition.GuardIgnore,
                    ReactionType.Warn   => faction.Definition.GuardWarn,
                    _                   => faction.Definition.GuardAttack // ReactionType.Attack
                };

                if (def?.Number > 0)
                {
                    Say(def.Number);
                }
                else if (def?.String != null)
                {
                    Say(def.String);
                }
            }

            Orders.SetReaction(faction, type);
        }

        private bool WasNamed(string speech)
        {
            var name = Name;

            return name != null && speech.InsensitiveStartsWith(name);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            var from = e.Mobile;

            if (!e.Handled && InRange(from, ListenRange) && from.Alive)
            {
                if (e.HasKeyword(0xE6) && (e.Speech.InsensitiveEquals("orders") || WasNamed(e.Speech))) // *orders*
                {
                    if (m_Town?.IsSheriff(from) != true)
                    {
                        Say(1042189); // I don't work for you!
                    }
                    else if (Town.FromRegion(Region) == m_Town)
                    {
                        Say(1042180); // Your orders, sire?
                        m_OrdersEnd = Core.Now + TimeSpan.FromSeconds(10.0);
                    }
                }
                else if (Core.Now < m_OrdersEnd)
                {
                    if (m_Town?.IsSheriff(from) != true || Town.FromRegion(Region) != m_Town)
                    {
                        return;
                    }

                    m_OrdersEnd = Core.Now + TimeSpan.FromSeconds(10.0);

                    var understood = true;
                    ReactionType newType = 0;

                    if (e.Speech.InsensitiveContains("attack"))
                    {
                        newType = ReactionType.Attack;
                    }
                    else if (e.Speech.InsensitiveContains("warn"))
                    {
                        newType = ReactionType.Warn;
                    }
                    else if (e.Speech.InsensitiveContains("ignore"))
                    {
                        newType = ReactionType.Ignore;
                    }
                    else
                    {
                        understood = false;
                    }

                    if (understood)
                    {
                        understood = false;

                        if (e.Speech.InsensitiveContains("civil"))
                        {
                            ChangeReaction(null, newType);
                            understood = true;
                        }

                        var factions = Faction.Factions;

                        for (var i = 0; i < factions.Count; ++i)
                        {
                            var faction = factions[i];

                            if (faction != m_Faction && e.Speech.InsensitiveContains(faction.Definition.Keyword))
                            {
                                ChangeReaction(faction, newType);
                                understood = true;
                            }
                        }
                    }
                    else if (e.Speech.InsensitiveContains("patrol"))
                    {
                        Home = Location;
                        RangeHome = 6;
                        Combatant = null;
                        Orders.Movement = MovementType.Patrol;
                        Say(1005146); // This spot looks like it needs protection!  I shall guard it with my life.
                        understood = true;
                    }
                    else if (e.Speech.InsensitiveContains("follow"))
                    {
                        Home = Location;
                        RangeHome = 6;
                        Combatant = null;
                        Orders.Follow = from;
                        Orders.Movement = MovementType.Follow;
                        Say(1005144); // Yes, Sire.
                        understood = true;
                    }

                    if (!understood)
                    {
                        Say(1042183); // I'm sorry, I don't understand your orders...
                    }
                }
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Faction != null && Map == Faction.Facet)
            {
                list.Add(1060846, $"{m_Faction.Definition.PropName}"); // Guard: ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Faction != null && Map == Faction.Facet)
            {
                var text = $"(Guard, {m_Faction.Definition.FriendlyName})";

                var hue = Faction.Find(from) == m_Faction ? 98 : 38;

                PrivateOverheadMessage(MessageType.Label, hue, true, text, from.NetState);
            }

            base.OnSingleClick(from);
        }

        public virtual void GenerateRandomHair()
        {
            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);
        }

        public void PackStrongPotions(int min, int max)
        {
            PackStrongPotions(Utility.RandomMinMax(min, max));
        }

        public void PackStrongPotions(int count)
        {
            for (var i = 0; i < count; ++i)
            {
                PackStrongPotion();
            }
        }

        public void PackStrongPotion()
        {
            PackItem(Loot.Construct(m_StrongPotions));
        }

        public void PackWeakPotions(int min, int max)
        {
            PackWeakPotions(Utility.RandomMinMax(min, max));
        }

        public void PackWeakPotions(int count)
        {
            for (var i = 0; i < count; ++i)
            {
                PackWeakPotion();
            }
        }

        public void PackWeakPotion()
        {
            PackItem(Loot.Construct(m_WeakPotions));
        }

        public Item Immovable(Item item)
        {
            item.Movable = false;
            return item;
        }

        public Item Newbied(Item item)
        {
            item.LootType = LootType.Newbied;
            return item;
        }

        public Item Rehued(Item item, int hue)
        {
            item.Hue = hue;
            return item;
        }

        public Item Layered(Item item, Layer layer)
        {
            item.Layer = layer;
            return item;
        }

        public Item Resourced(BaseWeapon weapon, CraftResource resource)
        {
            weapon.Resource = resource;
            return weapon;
        }

        public Item Resourced(BaseArmor armor, CraftResource resource)
        {
            armor.Resource = resource;
            return armor;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            Unregister();
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            c.Delete();
        }

        public virtual void GenerateBody(bool isFemale, bool randomHair)
        {
            Hue = Race.Human.RandomSkinHue();

            if (isFemale)
            {
                Female = true;
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Female = false;
                Body = 400;
                Name = NameList.RandomName("male");
            }

            if (randomHair)
            {
                GenerateRandomHair();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            Faction.WriteReference(writer, m_Faction);
            Town.WriteReference(writer, m_Town);

            Orders.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Faction = Faction.ReadReference(reader);
            m_Town = Town.ReadReference(reader);
            Orders = new Orders(this, reader);

            Timer.StartTimer(Register);
        }
    }

    public class VirtualMount : IMount
    {
        private readonly VirtualMountItem m_Item;

        public VirtualMount(VirtualMountItem item) => m_Item = item;

        Mobile IMount.Rider
        {
            get => m_Item.Rider;
            set { }
        }

        public virtual void OnRiderDamaged(int amount, Mobile from, bool willKill)
        {
        }
    }

    public class VirtualMountItem : Item, IMountItem
    {
        private readonly VirtualMount m_Mount;

        public VirtualMountItem(Mobile mob) : base(0x3EA0)
        {
            Layer = Layer.Mount;

            Rider = mob;
            m_Mount = new VirtualMount(this);
        }

        public VirtualMountItem(Serial serial) : base(serial) => m_Mount = new VirtualMount(this);

        public Mobile Rider { get; private set; }

        public IMount Mount => m_Mount;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Rider);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Rider = reader.ReadEntity<Mobile>();

            if (Rider == null)
            {
                Delete();
            }
        }
    }
}
