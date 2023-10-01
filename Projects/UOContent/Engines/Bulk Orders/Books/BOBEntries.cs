namespace Server.Engines.BulkOrders;

public class BOBEntries : GenericEntitySerialization<IBOBEntry>
{
    public static void Configure()
    {
        Configure("BOBEntries");
    }
}
