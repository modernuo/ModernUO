namespace Server.Engines.BulkOrders
{
    public interface IBOBEntry : ISerializable
    {
        bool RequireExceptional { get; }
        BODType DeedType { get; }
        BulkMaterialType Material { get; }
        int AmountMax { get; }
        int Price { get; set; }
        Item Reconstruct();
    }
}
