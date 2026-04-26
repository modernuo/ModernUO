using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class ScrollOfAbraxus : QuestItem
{
    [Constructible]
    public ScrollOfAbraxus() : base(0x227B)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1028827; // Scroll of Abraxus

    public override bool CanDrop(PlayerMobile player) => player.Quest is not DarkTidesQuest;

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (RootParent is PlayerMobile pm)
        {
            var qs = pm.Quest;

            if (qs is DarkTidesQuest)
            {
                var obj = qs.FindObjective<RetrieveAbraxusScrollObjective>();

                if (obj?.Completed == false)
                {
                    obj.Complete();
                }
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            ScrollOfAbraxusGump.DisplayTo(from);

            if (from is PlayerMobile pm)
            {
                var qs = pm.Quest;

                if (qs is DarkTidesQuest)
                {
                    var obj = qs.FindObjective<ReadAbraxusScrollObjective>();

                    if (obj?.Completed == false)
                    {
                        obj.Complete();
                    }
                }
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }
}

public class ScrollOfAbraxusGump : StaticGump<ScrollOfAbraxusGump>
{
    public override bool Singleton => true;

    private ScrollOfAbraxusGump() : base(150, 50)
    {
    }

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null)
        {
            return;
        }

        from.SendGump(new ScrollOfAbraxusGump());
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(0, 0, 1228);
        builder.AddImage(340, 255, 9005);

        /* Security at the Crystal Cave<BR><BR>
         *
         * We have taken great measuresto ensure the safety of the
         * Scroll of Calling, which we have so valiantly taken from
         * the Necromancer Maabus during the battle of the wood
         * nearly 200 years ago.<BR><BR>
         *
         * The scroll must never fall into the hands of the
         * Necromancers again, lest they use it to summon the ancient
         * daemon Kronus.  The scroll of calling is a necessity in the
         * series of dark rites the Necromancers must perform to once again
         * re-awaken Kronus.<BR><BR>
         *
         * Should Kronus ever rise again, the days of the Paladins, and
         * indeed humanity as we know it will be numbered.<BR><BR>
         *
         * For this reason, we have posted the honorable Horus, former
         * General of the Northern Legions to guard the entrance of the
         * Crystal Cave where we keep the Scroll of Calling.  Horus was
         * infused with magical life from the tree Urywen during his last
         * battle.  The power gave him eternal life, but it also,
         * unfortunately, took his eye sight.<BR><BR>
         *
         * Since Horus cannot see those he admits to the Crystal Cave,
         * he will only allow those that know the secret password to enter.
         * Speak the following word to Horus and he shall grant you passage
         * to the Crystal Cave:<BR><BR>
         *
         * <I>Urywen</I><BR><BR>
         *
         * Do not speak this password anywhere except when seeking passage
         * into the Crystal Cave, as our adversaries are lurking in the
         * shadows ' they are everywhere.<BR><BR>Go with the light, friend.<BR><BR>
         *
         * <I>- Frater Melkeer</I>
         */
        builder.AddHtmlLocalized(25, 36, 350, 210, 1060116, 1, false, true);
    }
}
