namespace Server.Items
{
    public class DisguisePersistance : Item
    {
        public DisguisePersistance() : base(1)
        {
            Movable = false;

            if (Instance?.Deleted != false)
            {
                Instance = this;
            }
            else
            {
                base.Delete();
            }
        }

        public DisguisePersistance(Serial serial) : base(serial) => Instance = this;

        public static DisguisePersistance Instance { get; private set; }

        public override string DefaultName => "Disguise Persistance - Internal";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            var timerCount = DisguiseTimers.Timers.Count;

            writer.Write(timerCount);

            foreach (var entry in DisguiseTimers.Timers)
            {
                var m = entry.Key;

                writer.Write(m);
                writer.Write(entry.Value.Next - Core.Now);
                writer.Write(m.NameMod);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        var count = reader.ReadInt();

                        for (var i = 0; i < count; ++i)
                        {
                            var m = reader.ReadEntity<Mobile>();
                            DisguiseTimers.CreateTimer(m, reader.ReadTimeSpan());
                            m.NameMod = reader.ReadString();
                        }

                        break;
                    }
            }
        }

        public override void Delete()
        {
        }
    }
}
