namespace Server.Items;

public interface IDirectionAddonDeed : IEntity
{
    public bool East { get; set; }

    public void SendTarget(Mobile m);
}
