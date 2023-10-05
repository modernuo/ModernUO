namespace Server.Engines.BulkOrders;

public class BOBEntries : GenericEntityPersistence<IBOBEntry>
{
    private static BOBEntries _bobEntriesPersistence;

    public static void Configure()
    {
        _bobEntriesPersistence = new BOBEntries();
    }

    public BOBEntries() : base("BOBEntries", 3, 0x1, 0x7FFFFFFF)
    {
    }

    public static Serial NewBOBEntry => _bobEntriesPersistence.NewEntity;

    public static void Add(IBOBEntry entity) => _bobEntriesPersistence.AddEntity(entity);

    public static void Remove(IBOBEntry entity) => _bobEntriesPersistence.AddEntity(entity);
}
