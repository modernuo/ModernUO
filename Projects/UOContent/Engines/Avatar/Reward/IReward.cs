namespace Server.Engines.Avatar;

public interface IReward
{
    bool CanSelect { get; set; }
    bool CanSelectAnywhere { get; set; }
    int Cost { get; }
    string Description { get; }
    int Graphic { get; }
    string Name { get; }
    bool Static { get; set; }
}
