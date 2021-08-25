using System;

namespace Server.Items
{
    public enum ECEffectType
    {
        None,
        Moving,
        Location,
        Target,
        Lightning
    }

    public enum EffectTriggerType
    {
        None,
        Sequenced,
        DoubleClick,
        InRange
    }

    public class EffectController : Item
    {
        private IEntity m_Source;
        private IEntity m_Target;

        [Constructible]
        public EffectController() : base(0x1B72)
        {
            Movable = false;
            Visible = false;
            TriggerType = EffectTriggerType.Sequenced;
            EffectLayer = (EffectLayer)255;
        }

        public EffectController(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ECEffectType EffectType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public EffectTriggerType TriggerType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public EffectLayer EffectLayer { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan EffectDelay { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan TriggerDelay { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SoundDelay { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item SourceItem
        {
            get => m_Source as Item;
            set => m_Source = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile SourceMobile
        {
            get => m_Source as Mobile;
            set => m_Source = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceNull
        {
            get => m_Source == null;
            set
            {
                if (value)
                {
                    m_Source = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item TargetItem
        {
            get => m_Target as Item;
            set => m_Target = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile TargetMobile
        {
            get => m_Target as Mobile;
            set => m_Target = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TargetNull
        {
            get => m_Target == null;
            set
            {
                if (value)
                {
                    m_Target = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EffectController Sequence { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        private bool FixedDirection { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        private bool Explodes { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        private bool PlaySoundAtTrigger { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EffectItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EffectHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RenderMode { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Speed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Duration { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ParticleEffect { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ExplodeParticleEffect { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ExplodeSound { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Unknown { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TriggerRange { get; set; }

        public override string DefaultName => "Effect Controller";

        public override bool HandlesOnMovement => TriggerType == EffectTriggerType.InRange;

        public override void OnDoubleClick(Mobile from)
        {
            if (TriggerType == EffectTriggerType.DoubleClick)
            {
                DoEffect(from);
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Location != oldLocation && TriggerType == EffectTriggerType.InRange &&
                Utility.InRange(GetWorldLocation(), m.Location, TriggerRange) &&
                !Utility.InRange(GetWorldLocation(), oldLocation, TriggerRange))
            {
                DoEffect(m);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(EffectDelay);
            writer.Write(TriggerDelay);
            writer.Write(SoundDelay);

            if (m_Source is Item srcItem)
            {
                writer.Write(srcItem);
            }
            else
            {
                writer.Write(m_Source as Mobile);
            }

            if (m_Target is Item targItem)
            {
                writer.Write(targItem);
            }
            else
            {
                writer.Write(m_Target as Mobile);
            }

            writer.Write(Sequence);

            writer.Write(FixedDirection);
            writer.Write(Explodes);
            writer.Write(PlaySoundAtTrigger);

            writer.WriteEncodedInt((int)EffectType);
            writer.WriteEncodedInt((int)EffectLayer);
            writer.WriteEncodedInt((int)TriggerType);

            writer.WriteEncodedInt(EffectItemID);
            writer.WriteEncodedInt(EffectHue);
            writer.WriteEncodedInt(RenderMode);
            writer.WriteEncodedInt(Speed);
            writer.WriteEncodedInt(Duration);
            writer.WriteEncodedInt(ParticleEffect);
            writer.WriteEncodedInt(ExplodeParticleEffect);
            writer.WriteEncodedInt(ExplodeSound);
            writer.WriteEncodedInt(Unknown);
            writer.WriteEncodedInt(SoundID);
            writer.WriteEncodedInt(TriggerRange);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        EffectDelay = reader.ReadTimeSpan();
                        TriggerDelay = reader.ReadTimeSpan();
                        SoundDelay = reader.ReadTimeSpan();

                        m_Source = reader.ReadEntity<IEntity>();
                        m_Target = reader.ReadEntity<IEntity>();
                        Sequence = reader.ReadEntity<EffectController>();

                        FixedDirection = reader.ReadBool();
                        Explodes = reader.ReadBool();
                        PlaySoundAtTrigger = reader.ReadBool();

                        EffectType = (ECEffectType)reader.ReadEncodedInt();
                        EffectLayer = (EffectLayer)reader.ReadEncodedInt();
                        TriggerType = (EffectTriggerType)reader.ReadEncodedInt();

                        EffectItemID = reader.ReadEncodedInt();
                        EffectHue = reader.ReadEncodedInt();
                        RenderMode = reader.ReadEncodedInt();
                        Speed = reader.ReadEncodedInt();
                        Duration = reader.ReadEncodedInt();
                        ParticleEffect = reader.ReadEncodedInt();
                        ExplodeParticleEffect = reader.ReadEncodedInt();
                        ExplodeSound = reader.ReadEncodedInt();
                        Unknown = reader.ReadEncodedInt();
                        SoundID = reader.ReadEncodedInt();
                        TriggerRange = reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public void PlaySound(IEntity trigger)
        {
            var ent = PlaySoundAtTrigger ? trigger : this;

            Effects.PlaySound((ent as Item)?.GetWorldLocation() ?? ent.Location, ent.Map, SoundID);
        }

        public void DoEffect(IEntity trigger)
        {
            if (Deleted || TriggerType == EffectTriggerType.None)
            {
                return;
            }

            if (trigger is Mobile { Hidden: true } mobile && mobile.AccessLevel > AccessLevel.Player)
            {
                return;
            }

            if (SoundID > 0)
            {
                Timer.StartTimer(SoundDelay, () => PlaySound(trigger));
            }

            if (Sequence != null)
            {
                Timer.StartTimer(TriggerDelay, () => Sequence.DoEffect(trigger));
            }

            if (EffectType != ECEffectType.None)
            {
                Timer.StartTimer(EffectDelay, () => InternalDoEffect(trigger));
            }
        }

        public void InternalDoEffect(IEntity trigger)
        {
            var from = m_Source ?? trigger;
            var to = m_Target ?? trigger;

            switch (EffectType)
            {
                case ECEffectType.Lightning:
                    {
                        Effects.SendBoltEffect(from, false, EffectHue);
                        break;
                    }
                case ECEffectType.Location:
                    {
                        Effects.SendLocationParticles(
                            EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
                            EffectItemID,
                            Speed,
                            Duration,
                            EffectHue,
                            RenderMode,
                            ParticleEffect,
                            Unknown
                        );
                        break;
                    }
                case ECEffectType.Moving:
                    {
                        if (from == this)
                        {
                            from = EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration);
                        }

                        if (to == this)
                        {
                            to = EffectItem.Create(to.Location, to.Map, EffectItem.DefaultDuration);
                        }

                        Effects.SendMovingParticles(
                            from,
                            to,
                            EffectItemID,
                            Speed,
                            Duration,
                            FixedDirection,
                            Explodes,
                            EffectHue,
                            RenderMode,
                            ParticleEffect,
                            ExplodeParticleEffect,
                            ExplodeSound,
                            EffectLayer,
                            Unknown
                        );
                        break;
                    }
                case ECEffectType.Target:
                    {
                        Effects.SendTargetParticles(
                            from,
                            EffectItemID,
                            Speed,
                            Duration,
                            EffectHue,
                            RenderMode,
                            ParticleEffect,
                            EffectLayer,
                            Unknown
                        );
                        break;
                    }
            }
        }
    }
}
