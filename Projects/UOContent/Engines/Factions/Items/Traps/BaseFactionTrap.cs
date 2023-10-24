using System;
using Server.Items;
using Server.Network;

namespace Server.Factions
{
    public enum AllowedPlacing
    {
        Everywhere,

        AnyFactionTown,
        ControlledFactionTown,
        FactionStronghold
    }

    public abstract class BaseFactionTrap : BaseTrap
    {
        private TimerExecutionToken _concealingTimerToken;

        public BaseFactionTrap(Faction f, Mobile m, int itemID) : base(itemID)
        {
            Visible = false;

            Faction = f;
            TimeOfPlacement = Core.Now;
            Placer = m;
        }

        public BaseFactionTrap(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Faction Faction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Placer { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeOfPlacement { get; set; }

        public virtual int EffectSound => 0;

        public virtual int SilverFromDisarm => 100;

        public virtual int MessageHue => 0;

        public virtual int AttackMessage => 0;
        public virtual int DisarmMessage => 0;

        public virtual AllowedPlacing AllowedPlacing => AllowedPlacing.Everywhere;

        public virtual TimeSpan ConcealPeriod => TimeSpan.FromMinutes(1.0);

        public virtual TimeSpan DecayPeriod => Core.AOS ? TimeSpan.FromDays(1.0) : TimeSpan.MaxValue;

        public override void OnTrigger(Mobile from)
        {
            if (!IsEnemy(from))
            {
                return;
            }

            Conceal();

            DoVisibleEffect();
            Effects.PlaySound(Location, Map, EffectSound);
            DoAttackEffect(from);

            var silverToAward = from.Alive ? 20 : 40;

            if (Placer != null && Faction != null)
            {
                var victimState = PlayerState.Find(from);

                if (victimState?.CanGiveSilverTo(Placer) == true && victimState.KillPoints > 0)
                {
                    var silverGiven = Faction.AwardSilver(Placer, silverToAward);

                    if (silverGiven > 0)
                    {
                        // TODO: Get real message
                        if (from.Alive)
                        {
                            Placer.SendMessage(
                                $"You have earned {silverGiven} silver pieces because {from.Name} fell for your trap."
                            );
                        }
                        else
                        {
                            // You have earned ~1_SILVER_AMOUNT~ pieces for vanquishing ~2_PLAYER_NAME~!
                            Placer.SendLocalizedMessage(1042736, $"{silverGiven} silver\t{from.Name}");
                        }
                    }

                    victimState.OnGivenSilverTo(Placer);
                }
            }

            from.LocalOverheadMessage(MessageType.Regular, MessageHue, AttackMessage);
        }

        public abstract void DoVisibleEffect();
        public abstract void DoAttackEffect(Mobile m);

        public virtual int IsValidLocation() => IsValidLocation(GetWorldLocation(), Map);

        public virtual int IsValidLocation(Point3D p, Map m)
        {
            if (m == null)
            {
                return 502956; // You cannot place a trap on that.
            }

            if (Core.ML)
            {
                foreach (var item in m.GetItemsAt(p))
                {
                    if (item is BaseFactionTrap trap && trap.Faction == Faction)
                    {
                        return 1075263; // There is already a trap belonging to your faction at this location.;
                    }
                }
            }

            switch (AllowedPlacing)
            {
                case AllowedPlacing.FactionStronghold:
                    {
                        var region = Region.Find(p, m).GetRegion<StrongholdRegion>();

                        if (region != null && region.Faction == Faction)
                        {
                            return 0;
                        }

                        return 1010355; // This trap can only be placed in your stronghold
                    }
                case AllowedPlacing.AnyFactionTown:
                    {
                        var town = Town.FromRegion(Region.Find(p, m));

                        if (town != null)
                        {
                            return 0;
                        }

                        return 1010356; // This trap can only be placed in a faction town
                    }
                case AllowedPlacing.ControlledFactionTown:
                    {
                        var town = Town.FromRegion(Region.Find(p, m));

                        if (town != null && town.Owner == Faction)
                        {
                            return 0;
                        }

                        return 1010357; // This trap can only be placed in a town your faction controls
                    }
            }

            return 0;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (!CheckDecay() && CheckRange(m.Location, oldLocation, 6))
            {
                if (Faction.Find(m) != null &&
                    (m.Skills.DetectHidden.Value - 80.0) / 20.0 > Utility.RandomDouble())
                {
                    PrivateOverheadLocalizedMessage(m, 1010154, MessageHue, "", ""); // [Faction Trap]
                }
            }
        }

        public void PrivateOverheadLocalizedMessage(Mobile to, int number, int hue, string name, string args)
        {
            to?.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, hue, 3, number, name, args);
        }

        public virtual bool CheckDecay()
        {
            var decayPeriod = DecayPeriod;

            if (decayPeriod == TimeSpan.MaxValue)
            {
                return false;
            }

            if (TimeOfPlacement + decayPeriod < Core.Now)
            {
                Timer.StartTimer(Delete);
                return true;
            }

            return false;
        }

        public virtual void BeginConceal()
        {
            _concealingTimerToken.Cancel();
            Timer.StartTimer(ConcealPeriod, Conceal, out _concealingTimerToken);
        }

        public virtual void Conceal()
        {
            _concealingTimerToken.Cancel();

            if (!Deleted)
            {
                Visible = false;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            Faction.WriteReference(writer, Faction);
            writer.Write(Placer);
            writer.Write(TimeOfPlacement);

            if (Visible)
            {
                BeginConceal();
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Faction = Faction.ReadReference(reader);
            Placer = reader.ReadEntity<Mobile>();
            TimeOfPlacement = reader.ReadDateTime();

            if (Visible)
            {
                BeginConceal();
            }

            CheckDecay();
        }

        public override void OnDelete()
        {
            if (Faction?.Traps.Contains(this) == true)
            {
                Faction.Traps.Remove(this);
            }

            base.OnDelete();
        }

        public virtual bool IsEnemy(Mobile mob)
        {
            if (mob.Hidden && mob.AccessLevel > AccessLevel.Player)
            {
                return false;
            }

            if (!mob.Alive || mob.IsDeadBondedPet)
            {
                return false;
            }

            var faction = Faction.Find(mob, true);

            if (faction == null && mob is BaseFactionGuard guard)
            {
                faction = guard.Faction;
            }

            if (faction == null)
            {
                return false;
            }

            return faction != Faction;
        }
    }
}
