using System.Threading;

namespace Server
{
    public sealed class DualSaveStrategy : StandardSaveStrategy
    {
        public override string Name => "Dual";

        public override void Save(bool permitBackgroundWrite)
        {
            PermitBackgroundWrite = permitBackgroundWrite;

            var saveThread = new Thread(SaveItems);

            saveThread.Name = "Item Save Subset";
            saveThread.Start();

            SaveMobiles();
            SaveGuilds();

            saveThread.Join();

            if (permitBackgroundWrite && UseSequentialWriters
            ) // If we're permitted to write in the background, but we don't anyways, then notify.
            {
                World.NotifyDiskWriteComplete();
            }
        }
    }
}
