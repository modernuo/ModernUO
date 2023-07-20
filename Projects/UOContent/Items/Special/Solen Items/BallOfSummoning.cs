using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.ConPVP;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;
using Server.Spells.Ninjitsu;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(2)]
public partial class BallOfSummoning : Item, TranslocationItem
{
    [SerializableField(3, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _petName;

    [Constructible]
    public BallOfSummoning() : base(0xE2E)
    {
        Weight = 10.0;
        Light = LightType.Circle150;

        _charges = Utility.RandomMinMax(3, 9);
        _petName = null;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Recharges
    {
        get => _recharges;
        set
        {
            _recharges = Math.Clamp(value, 0, MaxRecharges);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = Math.Clamp(value, 0, MaxCharges);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public BaseCreature Pet
    {
        get
        {
            if (_pet?.Deleted == true)
            {
                _pet = null;
                InternalUpdatePetName();
            }

            return _pet;
        }
        set
        {
            _pet = value;
            InternalUpdatePetName();
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxCharges => 20;

    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxRecharges => 255;

    private static TextDefinition _translocationItemName = 1054129; // crystal ball of pet summoning
    public TextDefinition TranslocationItemName => _translocationItemName;

    public override void AddNameProperty(IPropertyList list)
    {
        // a crystal ball of pet summoning: [charges: ~1_charges~] : [linked pet: ~2_petName~]
        list.Add(1054131, $"{_charges}\t{_petName.DefaultIfNullOrEmpty(" ")}");
    }

    public override void OnSingleClick(Mobile from)
    {
        // a crystal ball of pet summoning: [charges: ~1_charges~] : [linked pet: ~2_petName~]
        LabelTo(from, 1054131, $"{_charges}\t{_petName.DefaultIfNullOrEmpty(" ")}");
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (!from.Alive || RootParent != from)
        {
            return;
        }

        if (Pet == null)
        {
            list.Add(new BallEntry(LinkPet, 6180));
        }
        else
        {
            list.Add(new BallEntry(CastSummonPet, 6181));
            list.Add(new BallEntry(UpdatePetName, 6183));
            list.Add(new BallEntry(UnlinkPet, 6182));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        // TODO: Previous implementation allowed use on ground, without house protection checks. What is the correct behavior?
        if (RootParent != from)
        {
            // That must be in your pack for you to use it.
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1042001);
            return;
        }

        var animalContext = AnimalForm.GetContext(from);

        if (Core.ML && animalContext != null)
        {
            // You cannot use a Crystal Ball of Pet Summoning while in animal form.
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1080073);
            return;
        }

        if (Pet == null)
        {
            LinkPet(from);
        }
        else
        {
            CastSummonPet(from);
        }
    }

    public void LinkPet(Mobile from)
    {
        var pet = Pet;

        if (Deleted || pet != null || RootParent != from)
        {
            return;
        }

        // Target your pet that you wish to link to this Crystal Ball of Pet Summoning.
        from.SendLocalizedMessage(1054114);
        from.Target = new PetLinkTarget(this);
    }

    public void CastSummonPet(Mobile from)
    {
        var pet = Pet;

        if (Deleted || pet == null || RootParent != from || from is not PlayerMobile pm)
        {
            return;
        }

        if (Charges == 0)
        {
            // The Crystal Ball darkens. It must be charged before it can be used again.
            SendLocalizedMessageTo(from, 1054122);
        }
        else if (pet is BaseMount mount && mount.Rider == from)
        {
            // The Crystal Ball fills with a yellow mist. Why would you summon your pet while riding it?
            this.SendLocalizedMessageTo(from, 1054124, 0x36);
        }
        else if (pet.Map == Map.Internal && (!pet.IsStabled || from.Followers + pet.ControlSlots > from.FollowersMax))
        {
            // The Crystal Ball fills with a blue mist. Your pet is not responding to the summons.
            this.SendLocalizedMessageTo(from, 1054125, 0x5);
        }
        else if ((!pet.Controlled || pet.ControlMaster != from) && pm.Stabled?.Contains(pet) != true)
        {
            // The Crystal Ball fills with a grey mist. You are not the owner of the pet you are attempting to summon.
            this.SendLocalizedMessageTo(from, 1054126, 0x8FD);
        }
        else if (!pet.IsBonded)
        {
            // The Crystal Ball fills with a red mist. You appear to have let your bond to your pet deteriorate.
            this.SendLocalizedMessageTo(from, 1054127, 0x22);
        }
        else if (from.Map == Map.Ilshenar || from.Region.IsPartOf<DungeonRegion>() ||
                 from.Region.IsPartOf<JailRegion>() || from.Region.IsPartOf<SafeZone>())
        {
            // You cannot summon your pet to this location.
            this.SendLocalizedMessageTo(from, 1080049, 0x22);
        }
        else if (Core.ML && from is PlayerMobile mobile && Core.Now < mobile.LastPetBallTime.AddSeconds(15.0))
        {
            // You must wait a few seconds before you can summon your pet.
            this.SendLocalizedMessageTo(mobile, 1080072, 0x22);
        }
        else if (Core.ML)
        {
            new PetSummoningSpell(this, from).Cast();
        }
        else
        {
            SummonPet(from);
        }
    }

    public void SummonPet(Mobile from)
    {
        var pet = Pet;

        if (pet == null || from is not PlayerMobile pm)
        {
            return;
        }

        Charges--;

        if (pet.IsStabled)
        {
            pet.SetControlMaster(from);

            if (pet.Summoned)
            {
                pet.SummonMaster = from;
            }

            pet.ControlTarget = from;
            pet.ControlOrder = OrderType.Follow;

            pet.IsStabled = false;
            pet.StabledBy = null;
            pm.RemoveStabled(pet);
            pm.AutoStabled?.Remove(pet);
        }

        pet.MoveToWorld(from.Location, from.Map);

        // The Crystal Ball fills with a green mist. Your pet has been summoned.
        this.SendLocalizedMessageTo(from, 1054128, 0x43);

        if (from is PlayerMobile playerMobile)
        {
            playerMobile.LastPetBallTime = Core.Now;
        }
    }

    public void UnlinkPet(Mobile from)
    {
        if (!Deleted && Pet != null && RootParent == from)
        {
            Pet = null;

            SendLocalizedMessageTo(from, 1054120); // This crystal ball is no longer linked to a pet.
        }
    }

    public void UpdatePetName(Mobile from)
    {
        InternalUpdatePetName();
    }

    private void InternalUpdatePetName()
    {
        PetName = Pet?.Name ?? "";
        InvalidateProperties();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _recharges = reader.ReadEncodedInt();
        _charges = reader.ReadEncodedInt();
        _pet = (BaseCreature)reader.ReadEntity<Mobile>();
        _petName = reader.ReadString();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        InternalUpdatePetName();
    }

    private delegate void BallCallback(Mobile from);

    private class BallEntry : ContextMenuEntry
    {
        private readonly BallCallback _callback;

        public BallEntry(BallCallback callback, int number) : base(number, 2) => _callback = callback;

        public override void OnClick()
        {
            var from = Owner.From;

            if (from.CheckAlive())
            {
                _callback(from);
            }
        }
    }

    private class PetLinkTarget : Target
    {
        private readonly BallOfSummoning _ball;

        public PetLinkTarget(BallOfSummoning ball) : base(-1, false, TargetFlags.None) => _ball = ball;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_ball.Deleted || _ball.Pet != null)
            {
                return;
            }

            if (_ball.RootParent != from)
            {
                // That must be in your pack for you to use it.
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1042001);
                return;
            }

            if (targeted is BaseCreature creature)
            {
                if (!creature.Controlled || creature.ControlMaster != from)
                {
                    // You may only link your own pets to a Crystal Ball of Pet Summoning.
                    _ball.SendLocalizedMessageTo(from, 1054117, 0x59);
                }
                else if (!creature.IsBonded)
                {
                    // You must bond with your pet before it can be linked to a Crystal Ball of Pet Summoning.
                    _ball.SendLocalizedMessageTo(from, 1054118, 0x59);
                }
                else
                {
                    // Your pet is now linked to this Crystal Ball of Pet Summoning.
                    _ball.SendLocalizedMessageTo(from, 1054119, 0x59);
                    _ball.Pet = creature;
                }
            }
            else if (targeted == _ball)
            {
                // The Crystal Ball of Pet Summoning cannot summon itself.
                _ball.SendLocalizedMessageTo(from, 1054115, 0x59);
            }
            else
            {
                // Only pets can be linked to this Crystal Ball of Pet Summoning.
                _ball.SendLocalizedMessageTo(from, 1054116, 0x59);
            }
        }
    }

    private class PetSummoningSpell : Spell
    {
        private static readonly SpellInfo _info = new("Ball Of Summoning", "", 230);

        private readonly BallOfSummoning _ball;
        private readonly Mobile _caster;

        private bool _stop;

        public PetSummoningSpell(BallOfSummoning ball, Mobile caster) : base(caster, null, _info)
        {
            _caster = caster;
            _ball = ball;
        }

        public override bool ClearHandsOnCast => false;
        public override bool RevealOnCast => true;

        public override double CastDelayFastScalar => 0;

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override TimeSpan GetCastRecovery() => TimeSpan.Zero;

        public override int GetMana() => 0;

        public override bool ConsumeReagents() => true;

        public override bool CheckFizzle() => true;

        public void Stop()
        {
            _stop = true;
            Disturb(DisturbType.Hurt, false);
        }

        public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable) =>
            type != DisturbType.EquipRequest && type != DisturbType.UseRequest;

        public override void DoHurtFizzle()
        {
            if (!_stop)
            {
                base.DoHurtFizzle();
            }
        }

        public override void DoFizzle()
        {
            if (!_stop)
            {
                base.DoFizzle();
            }
        }

        public override void OnDisturb(DisturbType type, bool message)
        {
            if (message && !_stop)
            {
                Caster.SendLocalizedMessage(1080074); // You have been disrupted while attempting to summon your pet!
            }
        }

        public override void OnCast()
        {
            _ball.SummonPet(_caster);

            FinishSequence();
        }
    }
}
