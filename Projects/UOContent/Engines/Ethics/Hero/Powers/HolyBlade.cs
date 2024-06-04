namespace Server.Ethics.Hero;

public sealed class HolyBlade : Power
{
    public HolyBlade() =>
        Definition = new PowerDefinition(
            10,
            "Holy Blade",
            "Erstok Reyam",
            ""
        );

    public override void BeginInvoke(Player from)
    {
    }
}