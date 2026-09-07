using Server;
using Server.Mobiles;

namespace UOContent.Tests.Mobiles.AI;

// Minimal tameable creature for AI-state tests. Uses the AIType constructor so the
// creature gets a real AIObject (the Serial ctor does not initialize AI).
public class PetTestStub : BaseCreature
{
    // When true, CheckIdle() reports "resting" so the idle-wander path must not move.
    public bool ForceIdle { get; set; }

    public PetTestStub() : base(AIType.AI_Animal, FightMode.Closest, 10, 1)
    {
        Body = 0xC8; // dog
    }

    // NPCSpeeds isn't configured in the test fixture; provide fixed speeds so the
    // AIType constructor doesn't hit the unconfigured speed table.
    public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
    {
        activeSpeed = 0.2;
        passiveSpeed = 0.4;
    }

    // Counts the Combatant writes that actually land, so tests can prove an order writes it once.
    // The base setter ignores a re-assignment of the same mobile, so a re-issued order that does
    // not null Combatant first leaves this at one - which is the point: a second effective write
    // would replay DoHarmful and the target's anger sound.
    public int CombatantSets { get; private set; }

    public override Mobile Combatant
    {
        get => base.Combatant;
        set
        {
            if (value != null && base.Combatant != value)
            {
                CombatantSets++;
            }

            base.Combatant = value;
        }
    }

    public override bool CheckIdle() => ForceIdle || base.CheckIdle();

    public PetTestStub(Serial serial) : base(serial)
    {
    }
}

public static class PetTestSetup
{
    // Places a player master and a controlled pet on Felucca and returns both.
    public static (PlayerMobile master, PetTestStub pet) SpawnControlledPet(
        Point3D masterLoc, Point3D petLoc)
    {
        var map = Map.Felucca;

        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(masterLoc, map);

        var pet = new PetTestStub();
        pet.MoveToWorld(petLoc, map);
        pet.SetControlMaster(master); // sets Controlled, Home=Zero, ControlOrder=Come

        return (master, pet);
    }
}
