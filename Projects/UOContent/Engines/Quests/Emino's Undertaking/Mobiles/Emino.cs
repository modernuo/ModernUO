using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0, false)]
public partial class Emino : BaseQuester
{
    [Constructible]
    public Emino() : base("the Notorious")
    {
    }

    public override string DefaultName => "Daimyo Emino";

    public override int TalkNumber => -1;

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83FE;

        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        HairItemID = 0x203B;
        HairHue = 0x901;

        AddItem(new MaleKimono());
        AddItem(new SamuraiTabi());
        AddItem(new Bandana());

        AddItem(new PlateHaidate());
        AddItem(new PlateDo());
        AddItem(new PlateHiroSode());

        var nunchaku = new Nunchaku();
        nunchaku.Movable = false;
        AddItem(nunchaku);
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 2;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is not EminosUndertakingQuest)
        {
            return;
        }

        if (EminosUndertakingQuest.HasLostNoteForZoel(player))
        {
            var note = new NoteForZoel();

            if (player.PlaceInBackpack(note))
            {
                qs.AddConversation(new LostNoteConversation());
            }
            else
            {
                note.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }

            return;
        }

        if (EminosUndertakingQuest.HasLostEminosKatana(player))
        {
            qs.AddConversation(new LostSwordConversation());
            return;
        }

        if (qs.FindObjective<FindEminoBeginObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.FindObjective<UseTeleporterObjective>() is { Completed: false } obj2)
        {
            var note = new NoteForZoel();

            if (player.PlaceInBackpack(note))
            {
                obj2.Complete();

                player.AddToBackpack(new LeatherNinjaPants());
                player.AddToBackpack(new LeatherNinjaMitts());
            }
            else
            {
                note.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }

            return;
        }

        if (qs.FindObjective<ReturnFromInnObjective>() is { Completed: false } obj3)
        {
            var cont = GetNewContainer();

            for (var i = 0; i < 10; i++)
            {
                cont.DropItem(new LesserHealPotion());
            }

            cont.DropItem(new LeatherNinjaHood());
            cont.DropItem(new LeatherNinjaJacket());

            if (player.PlaceInBackpack(cont))
            {
                obj3.Complete();
            }
            else
            {
                cont.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }

            return;
        }

        if (qs.IsObjectiveInProgress(typeof(SlayHenchmenObjective)))
        {
            qs.AddConversation(new ContinueSlayHenchmenConversation());
            return;
        }

        if (qs.FindObjective<GiveEminoSwordObjective>() is { Completed: false } obj4)
        {
            var katana = player.Backpack?.FindItemByType<EminosKatana>();

            if (katana == null)
            {
                return;
            }

            var stolenTreasure = false;

            var walk = qs.FindObjective<HallwayWalkObjective>();

            if (walk != null)
            {
                stolenTreasure = walk.StolenTreasure;
            }

            var kama = new Kama();
            BaseRunicTool.ApplyAttributesTo(kama, 1, 10, stolenTreasure ? 20 : 30);

            if (player.PlaceInBackpack(kama))
            {
                katana.Delete();
                obj4.Complete();

                if (stolenTreasure)
                {
                    qs.AddConversation(new EarnLessGiftsConversation());
                }
                else
                {
                    qs.AddConversation(new EarnGiftsConversation());
                }
            }
            else
            {
                kama.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                player.SendLocalizedMessage(1046260);
            }
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (m.Frozen || m.Alive || !InRange(m, 4) || InRange(oldLocation, 4) || !InLOS(m))
        {
            return;
        }

        if (m.Map?.CanFit(m.Location, 16, false, false) != true)
        {
            m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
        }
        else
        {
            Direction = GetDirectionTo(m);

            m.PlaySound(0x214);
            m.FixedEffect(0x376A, 10, 16);

            m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
        }
    }
}
