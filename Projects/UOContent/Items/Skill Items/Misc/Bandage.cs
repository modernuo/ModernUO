using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.ConPVP;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bandage : Item, IDyable
{
    public static int Range = Core.AOS ? 2 : 1;

    [Constructible]
    public Bandage(int amount = 1) : base(0xE21)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    public static void Initialize()
    {
        EventSink.BandageTargetRequest += EventSink_BandageTargetRequest;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), Range))
        {
            from.RevealingAction();

            from.SendLocalizedMessage(500948); // Who will you use the bandages on?

            from.Target = new InternalTarget(this);
        }
        else
        {
            from.SendLocalizedMessage(500295); // You are too far away to do that.
        }
    }

    private static void EventSink_BandageTargetRequest(Mobile from, Item item, Mobile target)
    {
        if (item is not Bandage b || b.Deleted)
        {
            return;
        }

        if (!from.InRange(b.GetWorldLocation(), Range))
        {
            from.SendLocalizedMessage(500295); // You are too far away to do that.
            return;
        }

        if (from.Target != null)
        {
            Target.Cancel(from);
            from.Target = null;
        }

        from.RevealingAction();
        from.SendLocalizedMessage(500948); // Who will you use the bandages on?

        new InternalTarget(b).Invoke(from, target);
    }

    private class InternalTarget : Target
    {
        private readonly Bandage _bandage;

        public InternalTarget(Bandage bandage) : base(Bandage.Range, false, TargetFlags.Beneficial) =>
            _bandage = bandage;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_bandage.Deleted)
            {
                return;
            }

            if (targeted is Mobile mobile)
            {
                if (from.InRange(_bandage.GetWorldLocation(), Bandage.Range))
                {
                    if (!(BandageContext.BeginHeal(from, mobile) == null || DuelContext.IsFreeConsume(from)))
                    {
                        _bandage.Consume();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }

                return;
            }

            if (targeted is PlagueBeastInnard innard)
            {
                if (innard.OnBandage(from))
                {
                    _bandage.Consume();
                }

                return;
            }

            from.SendLocalizedMessage(500970); // Bandages can not be used on that.
        }

        protected override void OnNonlocalTarget(Mobile from, object targeted)
        {
            if (targeted is PlagueBeastInnard innard)
            {
                if (innard.OnBandage(from))
                {
                    _bandage.Consume();
                }
            }
            else
            {
                base.OnNonlocalTarget(from, targeted);
            }
        }
    }
}

public class BandageContext : Timer
{
    private static readonly Dictionary<Mobile, BandageContext> _table = new();

    public BandageContext(Mobile healer, Mobile patient, TimeSpan delay) : base(delay)
    {
        Healer = healer;
        Patient = patient;
    }

    public Mobile Healer { get; }

    public Mobile Patient { get; }

    public int Slips { get; set; }

    public void Slip()
    {
        Healer.SendLocalizedMessage(500961); // Your fingers slip!
        ++Slips;
    }

    public void StopHeal()
    {
        _table.Remove(Healer);
        Stop();
    }

    public static BandageContext GetContext(Mobile healer)
    {
        _table.TryGetValue(healer, out var bc);
        return bc;
    }

    public static SkillName GetPrimarySkill(Mobile m)
    {
        if (!m.Player && (m.Body.IsMonster || m.Body.IsAnimal))
        {
            return SkillName.Veterinary;
        }

        return SkillName.Healing;
    }

    public static SkillName GetSecondarySkill(Mobile m)
    {
        if (!m.Player && (m.Body.IsMonster || m.Body.IsAnimal))
        {
            return SkillName.AnimalLore;
        }

        return SkillName.Anatomy;
    }

    protected override void OnTick()
    {
        StopHeal();

        int healerNumber;
        int patientNumber;
        var playSound = true;
        bool checkSkills;

        var primarySkill = GetPrimarySkill(Patient);
        var secondarySkill = GetSecondarySkill(Patient);

        var petPatient = Patient as BaseCreature;

        if (!Healer.Alive)
        {
            Healer.SendLocalizedMessage(500962); // You were unable to finish your work before you died.
            return;
        }

        if (!Healer.InRange(Patient, Bandage.Range))
        {
            Healer.SendLocalizedMessage(500963); // You did not stay close enough to heal your target.
            return;
        }

        if (!Patient.Alive || petPatient?.IsDeadPet == true)
        {
            if (Patient.Map?.CanFit(Patient.Location, 16, false, false) != true)
            {
                Healer.SendLocalizedMessage(501042);  // Target can not be resurrected at that location.
                Patient.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                return;
            }

            if (Patient.Region?.IsPartOf("Khaldun") == true)
            {
                // The veil of death in this area is too strong and resists thy efforts to restore life.
                Healer.SendLocalizedMessage(1010395);
                return;
            }

            var healing = Healer.Skills[primarySkill].Value;
            var anatomy = Healer.Skills[secondarySkill].Value;
            var chance = (healing - 68.0) / 50.0 - Slips * 0.02;
            checkSkills = healing >= 80.0 && anatomy >= 80.0;

            // TODO: Dbl check doesn't check for faction of the horse here?
            if (!(checkSkills && chance > Utility.RandomDouble())
                && (!Core.SE || petPatient is not FactionWarHorse || petPatient.ControlMaster != Healer))
            {
                if (petPatient?.IsDeadPet == true)
                {
                    Healer.SendLocalizedMessage(503256); // You fail to resurrect the creature.
                }
                else
                {
                    Healer.SendLocalizedMessage(500966); // You are unable to resurrect your patient.
                }

                return;
            }

            healerNumber = 500965; // You are able to resurrect your patient.
            patientNumber = -1;

            Patient.PlaySound(0x214);
            Patient.FixedEffect(0x376A, 10, 16);

            if (petPatient?.IsDeadPet == true)
            {
                var master = petPatient.ControlMaster;

                if (master != null && Healer == master)
                {
                    petPatient.ResurrectPet();

                    for (var i = 0; i < petPatient.Skills.Length; ++i)
                    {
                        petPatient.Skills[i].Base -= 0.1;
                    }
                }
                else if (master?.InRange(petPatient, 3) == true)
                {
                    healerNumber = 503255; // You are able to resurrect the creature.

                    master.CloseGump<PetResurrectGump>();
                    master.SendGump(new PetResurrectGump(Healer, petPatient));
                }
                else
                {
                    var found = false;

                    var friends = petPatient.Friends;

                    for (var i = 0; i < friends?.Count; ++i)
                    {
                        var friend = friends[i];

                        if (friend.InRange(petPatient, 3))
                        {
                            healerNumber = 503255; // You are able to resurrect the creature.

                            friend.CloseGump<PetResurrectGump>();
                            friend.SendGump(new PetResurrectGump(Healer, petPatient));

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        healerNumber = 1049670; // The pet's owner must be nearby to attempt resurrection.
                    }
                }
            }
            else
            {
                Patient.CloseGump<ResurrectGump>();
                Patient.SendGump(new ResurrectGump(Patient, Healer));
            }
        }
        else if (Patient.Poisoned)
        {
            Healer.SendLocalizedMessage(500969); // You finish applying the bandages.

            var healing = Healer.Skills[primarySkill].Value;
            var anatomy = Healer.Skills[secondarySkill].Value;
            var chance = (healing - 30.0) / 50.0 - Patient.Poison.Level * 0.1 - Slips * 0.02;
            checkSkills = healing >= 60.0 && anatomy >= 60.0;

            if (!(checkSkills && chance > Utility.RandomDouble()))
            {
                Healer.SendLocalizedMessage(1010060); // You have failed to cure your target!
                return;
            }

            if (Patient.CurePoison(Healer))
            {
                healerNumber = Healer == Patient ? -1 : 1010058; // You have cured the target of all poisons.
                patientNumber = 1010059;                         // You have been cured of all poisons.
            }
            else
            {
                healerNumber = -1;
                patientNumber = -1;
            }
        }
        else
        {
            if (BleedAttack.IsBleeding(Patient))
            {
                Healer.SendLocalizedMessage(1060088);  // You bind the wound and stop the bleeding
                Patient.SendLocalizedMessage(1060167); // The bleeding wounds have healed, you are no longer bleeding!

                BleedAttack.EndBleed(Patient, false);
                return;
            }

            if (MortalStrike.IsWounded(Patient))
            {
                if (Healer == Patient)
                {
                    Healer.SendLocalizedMessage(1005000); // You cannot heal yourself in your current state.
                }
                else
                {
                    Healer.SendLocalizedMessage(1010398); // You cannot heal that target in their current state.
                }

                return;
            }

            if (Patient.Hits == Patient.HitsMax)
            {
                Healer.SendLocalizedMessage(1010395); // You heal what little damage your patient had.
                return;
            }

            checkSkills = true;
            patientNumber = -1;

            var healing = Healer.Skills[primarySkill].Value;
            var anatomy = Healer.Skills[secondarySkill].Value;
            var chance = (healing + 10.0) / 100.0 - Slips * 0.02;

            if (chance > Utility.RandomDouble())
            {
                healerNumber = 500969; // You finish applying the bandages.

                double min, max;

                if (Core.AOS)
                {
                    min = anatomy / 8.0 + healing / 5.0 + 4.0;
                    max = anatomy / 6.0 + healing / 2.5 + 4.0;
                }
                else
                {
                    min = anatomy / 5.0 + healing / 5.0 + 3.0;
                    max = anatomy / 5.0 + healing / 2.0 + 10.0;
                }

                var toHeal = min + Utility.RandomDouble() * (max - min);

                if (Patient.Body.IsMonster || Patient.Body.IsAnimal)
                {
                    toHeal += Patient.HitsMax / 100.0;
                }

                if (Core.AOS)
                {
                    toHeal -= toHeal * Slips * 0.35; // TODO: Verify algorithm
                }
                else
                {
                    toHeal -= Slips * 4;
                }

                if (toHeal < 1)
                {
                    toHeal = 1;
                    healerNumber = 500968; // You apply the bandages, but they barely help.
                }

                Patient.Heal((int)toHeal, Healer, false);
            }
            else
            {
                healerNumber = 500968; // You apply the bandages, but they barely help.
                playSound = false;
            }
        }

        if (healerNumber != -1)
        {
            Healer.SendLocalizedMessage(healerNumber);
        }

        if (patientNumber != -1)
        {
            Patient.SendLocalizedMessage(patientNumber);
        }

        if (playSound)
        {
            Patient.PlaySound(0x57);
        }

        if (checkSkills)
        {
            Healer.CheckSkill(secondarySkill, 0.0, 120.0);
            Healer.CheckSkill(primarySkill, 0.0, 120.0);
        }
    }

    public static BandageContext BeginHeal(Mobile healer, Mobile patient)
    {
        var creature = patient as BaseCreature;

        if (patient is Golem)
        {
            healer.SendLocalizedMessage(500970); // Bandages cannot be used on that.
        }
        else if (creature?.IsAnimatedDead == true)
        {
            healer.SendLocalizedMessage(500951); // You cannot heal that.
        }
        else if (!patient.Poisoned && patient.Hits == patient.HitsMax && !BleedAttack.IsBleeding(patient) &&
                 creature?.IsDeadPet != true)
        {
            healer.SendLocalizedMessage(500955); // That being is not damaged!
        }
        else if (!patient.Alive && patient.Map?.CanFit(patient.Location, 16, false, false) != true)
        {
            healer.SendLocalizedMessage(501042); // Target cannot be resurrected at that location.
        }
        else if (healer.CanBeBeneficial(patient, true, true))
        {
            healer.DoBeneficial(patient);

            var onSelf = healer == patient;
            var dex = healer.Dex;

            double seconds;
            var resDelay = patient.Alive ? 0.0 : 5.0;

            if (onSelf)
            {
                if (Core.AOS)
                {
                    seconds = 5.0 + 0.5 * ((double)(120 - dex) / 10); // TODO: Verify algorithm
                }
                else
                {
                    seconds = 9.4 + 0.6 * ((double)(120 - dex) / 10);
                }
            }
            else if (Core.AOS && GetPrimarySkill(patient) == SkillName.Veterinary)
            {
                seconds = 2.0;
            }
            else if (Core.AOS)
            {
                if (dex < 204)
                {
                    seconds = 3.2 - Math.Sin((double)dex / 130) * 2.5 + resDelay;
                }
                else
                {
                    seconds = 0.7 + resDelay;
                }
            }
            else if (dex >= 100)
            {
                seconds = 3.0 + resDelay;
            }
            else if (dex >= 40)
            {
                seconds = 4.0 + resDelay;
            }
            else
            {
                seconds = 5.0 + resDelay;
            }

            var context = GetContext(healer);

            context?.StopHeal();
            seconds *= 1000;

            context = new BandageContext(healer, patient, TimeSpan.FromMilliseconds(seconds));
            _table[healer] = context;
            context.Start();

            if (!onSelf)
            {
                patient.SendLocalizedMessage(1008078, false, healer.Name); // : Attempting to heal you.
            }

            healer.SendLocalizedMessage(500956); // You begin applying the bandages.
            return context;
        }

        return null;
    }
}
