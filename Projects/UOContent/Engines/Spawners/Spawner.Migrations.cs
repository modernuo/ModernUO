namespace Server.Engines.Spawners;

public partial class Spawner
{
    private void MigrateFrom(V0Content content)
    {
        // V0 had no fields in Spawner, new v1 field defaults to false
        _useSpiralScan = false;
    }
}
