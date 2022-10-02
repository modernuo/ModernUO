namespace Server.Mobiles;

public class ColdBreath : FireBreath
{
    public override int BreathColdDamage => 100;
    public override int BreathFireDamage => 0;
    public override int BreathEffectHue => 0x480;
}
