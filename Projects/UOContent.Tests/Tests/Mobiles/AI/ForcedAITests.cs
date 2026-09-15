using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class ForcedAITests
{
    private sealed class CountingAI : BaseAI
    {
        public CountingAI(BaseCreature m) : base(m)
        {
        }
    }

    private sealed class ForcedStub : BaseCreature
    {
        public int Constructions;

        public ForcedStub() : base(AIType.AI_Melee)
        {
        }

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }

        // Not sector-gated: a BaseAI ctor starts its timer immediately.
        public override bool PlayerRangeSensitive => false;

        protected override BaseAI ForcedAI
        {
            get
            {
                Constructions++;
                return new CountingAI(this);
            }
        }
    }

    [Fact]
    public void ChangeAIType_ConstructsForcedAIOnce()
    {
        var bc = new ForcedStub();

        try
        {
            Assert.Equal(1, bc.Constructions);
            Assert.IsType<CountingAI>(bc.AIObject);
        }
        finally
        {
            bc.Delete();
        }
    }
}
