using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public abstract partial class BaseShieldGuard : BaseCreature
{
    public BaseShieldGuard() : base(AIType.AI_Melee, FightMode.Aggressor, 14)
    {
        InitStats(1000, 1000, 1000);
        Title = "the guard";

        SetSpeed(0.5, 2.0);
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");

            AddItem(new FemalePlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());

            switch (Utility.Random(2))
            {
                case 0:
                    {
                        AddItem(new Doublet(Utility.RandomNondyedHue()));
                        break;
                    }
                case 1:
                    {
                        AddItem(new BodySash(Utility.RandomNondyedHue()));
                        break;
                    }
            }

            switch (Utility.Random(2))
            {
                case 0:
                    {
                        AddItem(new Skirt(Utility.RandomNondyedHue()));
                        break;
                    }
                case 1:
                    {
                        AddItem(new Kilt(Utility.RandomNondyedHue()));
                        break;
                    }
            }
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());

            switch (Utility.Random(3))
            {
                case 0:
                    {
                        AddItem(new Doublet(Utility.RandomNondyedHue()));
                        break;
                    }
                case 1:
                    {
                        AddItem(new Tunic(Utility.RandomNondyedHue()));
                        break;
                    }
                case 2:
                    {
                        AddItem(new BodySash(Utility.RandomNondyedHue()));
                        break;
                    }
            }
        }

        Utility.AssignRandomHair(this);
        if (Utility.RandomBool())
        {
            Utility.AssignRandomFacialHair(this, HairHue);
        }

        var weapon = new VikingSword();
        weapon.Movable = false;
        AddItem(weapon);

        var shield = Shield;
        shield.Movable = false;
        AddItem(shield);

        PackGold(250, 500);

        Skills.Anatomy.Base = 120.0;
        Skills.Tactics.Base = 120.0;
        Skills.Swords.Base = 120.0;
        Skills.MagicResist.Base = 120.0;
        Skills.DetectHidden.Base = 100.0;
    }

    public abstract int Keyword { get; }
    public abstract BaseShield Shield { get; }
    public abstract int SignupNumber { get; }
    public abstract GuildType Type { get; }

    public override bool HandlesOnSpeech(Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            return true;
        }

        return base.HandlesOnSpeech(from);
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (!e.Handled && e.HasKeyword(Keyword) && e.Mobile.InRange(Location, 2))
        {
            e.Handled = true;

            var from = e.Mobile;

            if (from.Guild is not Guild g || g.Type != Type)
            {
                Say(SignupNumber);
            }
            else
            {
                var pack = from.Backpack;
                var shield = Shield;
                var twoHanded = from.FindItemOnLayer(Layer.TwoHanded);

                if (pack?.FindItemByType(shield.GetType()) != null ||
                    twoHanded != null && shield.GetType().IsInstanceOfType(twoHanded))
                {
                    Say(1007110); // Why dost thou ask about virtue guards when thou art one?
                    shield.Delete();
                }
                else if (from.PlaceInBackpack(shield))
                {
                    Say(Utility.Random(1007101, 5));
                    Say(1007139); // I see you are in need of our shield, Here you go.
                    from.AddToBackpack(shield);
                }
                else
                {
                    from.SendLocalizedMessage(502868); // Your backpack is too full.
                    shield.Delete();
                }
            }
        }

        base.OnSpeech(e);
    }
}
