namespace Server.Mobiles;

public class ColdBreath : FireBreath
{
    public override int ColdDamage => 100;
    public override int FireDamage => 0;
    public override int BreathEffectHue => 0x480;
}
