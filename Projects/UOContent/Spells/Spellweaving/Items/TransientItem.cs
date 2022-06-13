using System;

namespace Server.Items
{
    public class TransientItem : Item
    {
        private TimerExecutionToken _timerToken;

        [Constructible]
        public TransientItem(int itemID, TimeSpan lifeSpan)
            : base(itemID)
        {
            CreationTime = Core.Now;
            LifeSpan = lifeSpan;

            Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry, out _timerToken);
        }

        public TransientItem(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan LifeSpan { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime CreationTime { get; set; }

        public override bool Nontransferable => true;

        public virtual TextDefinition InvalidTransferMessage => null;

        public override void HandleInvalidTransfer(Mobile from)
        {
            if (InvalidTransferMessage != null)
            {
                TextDefinition.SendMessageTo(from, InvalidTransferMessage);
            }

            Delete();
        }

        public virtual void Expire(Mobile parent)
        {
            parent?.SendLocalizedMessage(1072515, Name ?? $"#{LabelNumber}"); // The ~1_name~ expired...

            Effects.PlaySound(GetWorldLocation(), Map, 0x201);

            Delete();
        }

        public virtual void SendTimeRemainingMessage(Mobile to)
        {
            to.SendLocalizedMessage(
                1072516, // ~1_name~ will expire in ~2_val~ seconds!
                $"{Name ?? $"#{LabelNumber}"}\t{(int)LifeSpan.TotalSeconds}"
            );
        }

        public override void OnDelete()
        {
            _timerToken.Cancel();
            base.OnDelete();
        }

        public virtual void CheckExpiry()
        {
            if (CreationTime + LifeSpan < Core.Now)
            {
                Expire(RootParent as Mobile);
            }
            else
            {
                InvalidateProperties();
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            var remaining = CreationTime + LifeSpan - Core.Now;

            list.Add(1072517, $"{(int)remaining.TotalSeconds}"); // Lifespan: ~1_val~ seconds
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(LifeSpan);
            writer.Write(CreationTime);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            LifeSpan = reader.ReadTimeSpan();
            CreationTime = reader.ReadDateTime();

            Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry, out _timerToken);
        }
    }
}
