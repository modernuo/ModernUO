namespace Server.Engines.MLQuests
{
    public class MLQuestPersistence : Item
    {
        private static MLQuestPersistence m_Instance;

        private MLQuestPersistence() : base(1) => Movable = false;

        public MLQuestPersistence(Serial serial) : base(serial) => m_Instance = this;

        public override string DefaultName => "ML quests persistence - Internal";

        public static void EnsureExistence()
        {
            m_Instance ??= new MLQuestPersistence();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version
            writer.Write(MLQuestSystem.Contexts.Count);

            foreach (var context in MLQuestSystem.Contexts.Values)
            {
                context.Serialize(writer);
            }

            writer.Write(MLQuestSystem.Quests.Count);

            foreach (var quest in MLQuestSystem.Quests.Values)
            {
                MLQuest.Serialize(writer, quest);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            var contexts = reader.ReadInt();

            for (var i = 0; i < contexts; ++i)
            {
                var context = new MLQuestContext(reader, version);

                if (context.Owner != null)
                {
                    MLQuestSystem.Contexts[context.Owner] = context;
                }
            }

            var quests = reader.ReadInt();

            for (var i = 0; i < quests; ++i)
            {
                MLQuest.Deserialize(reader, version);
            }
        }
    }
}
