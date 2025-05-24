using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class Chyloth : BaseQuester
{
    private static readonly int[] _offsets =
    {
        -1, -1,
        -1, 0,
        -1, 1,
        0, -1,
        0, 1,
        1, -1,
        1, 0,
        1, 1
    };

    [Constructible]
    public Chyloth() : base("the Ferryman")
    {
    }

    public override string DefaultName => "Chyloth";

    public BellOfTheDead Bell { get; set; }

    public Mobile AngryAt { get; set; }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x8455;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        EquipItem(new ChylothShroud());
        EquipItem(new ChylothStaff());
    }

    public virtual void BeginGiveWarning()
    {
        if (Deleted || AngryAt == null)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(4.0), EndGiveWarning);
    }

    public virtual void EndGiveWarning()
    {
        if (Deleted || AngryAt == null)
        {
            return;
        }

        // You have summoned me in vain ~1_NAME~!  Only the dead may cross!
        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1050013, AngryAt.Name);
        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1050014); // Why have you disturbed me, mortal?!?

        BeginSummonDragon();
    }

    public virtual void BeginSummonDragon()
    {
        if (Deleted || AngryAt == null)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(30.0), EndSummonDragon);
    }

    public virtual void BeginRemove(TimeSpan delay)
    {
        Timer.StartTimer(delay, EndRemove);
    }

    public virtual void EndRemove()
    {
        if (Deleted)
        {
            return;
        }

        var loc = Location;
        var map = Map;

        Effects.SendLocationParticles(
            EffectItem.Create(loc, map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            0,
            0,
            2023,
            0
        );
        Effects.PlaySound(loc, map, 0x1FE);

        Delete();
    }

    public virtual void EndSummonDragon()
    {
        if (Deleted || AngryAt == null)
        {
            return;
        }

        var map = AngryAt.Map;

        if (map == null)
        {
            return;
        }

        if (!AngryAt.Region.IsPartOf("Doom"))
        {
            return;
        }

        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1050015);                        // Feel the wrath of my legions!!!
        PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "MUHAHAHAHA HAHAH HAHA"); // A wee bit crazy, aren't we?

        var dragon = new SkeletalDragon();

        var offset = Utility.Random(8) * 2;

        var foundLoc = false;

        for (var i = 0; i < _offsets.Length; i += 2)
        {
            var x = AngryAt.X + _offsets[(offset + i) % _offsets.Length];
            var y = AngryAt.Y + _offsets[(offset + i + 1) % _offsets.Length];

            if (map.CanSpawnMobile(x, y, AngryAt.Z))
            {
                dragon.MoveToWorld(new Point3D(x, y, AngryAt.Z), map);
                foundLoc = true;
                break;
            }

            var z = map.GetAverageZ(x, y);

            if (map.CanSpawnMobile(x, y, z))
            {
                dragon.MoveToWorld(new Point3D(x, y, z), map);
                foundLoc = true;
                break;
            }
        }

        if (!foundLoc)
        {
            dragon.MoveToWorld(AngryAt.Location, map);
        }

        dragon.Combatant = AngryAt;

        if (Bell != null)
        {
            Bell.Dragon = dragon;
        }
    }

    public static void TeleportToFerry(Mobile from)
    {
        var loc = new Point3D(408, 251, 2);
        var map = Map.Malas;

        Effects.SendLocationParticles(
            EffectItem.Create(loc, map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            0,
            0,
            2023,
            0
        );
        Effects.PlaySound(loc, map, 0x1FE);

        TeleportPets(from, loc, map);

        from.MoveToWorld(loc, map);
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is GoldenSkull)
        {
            dropped.Delete();

            // Very well, ~1_NAME~, I accept your token. You may cross.
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 1050046, from.Name);
            BeginRemove(TimeSpan.FromSeconds(4.0));

            var p = PartySystem.Party.Get(from);

            for (var i = 0; i < p?.Members.Count; ++i)
            {
                var pmi = p.Members[i];
                var member = pmi.Mobile;

                if (member != from && member.Map == Map.Malas && member.Region.IsPartOf("Doom"))
                {
                    if (AngryAt == member)
                    {
                        AngryAt = null;
                    }

                    member.SendGump(new ChylothPartyGump(from));
                }
            }

            if (AngryAt == from)
            {
                AngryAt = null;
            }

            TeleportToFerry(from);

            return false;
        }

        return base.OnDragDrop(from, dropped);
    }

    public override bool CanTalkTo(PlayerMobile to) => false;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }
}

public class ChylothPartyGump : StaticGump<ChylothPartyGump>
{
    private readonly Mobile _leader;

    public override bool Singleton => true;

    public ChylothPartyGump(Mobile leader) : base(150, 50) => _leader = leader;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

        builder.AddImage(0, 0, 3600);

        builder.AddImageTiled(0, 14, 15, 200, 3603);
        builder.AddImageTiled(380, 14, 14, 200, 3605);
        builder.AddImage(0, 201, 3606);
        builder.AddImageTiled(15, 201, 370, 16, 3607);
        builder.AddImageTiled(15, 0, 370, 16, 3601);
        builder.AddImage(380, 0, 3602);
        builder.AddImage(380, 201, 3608);
        builder.AddImageTiled(15, 15, 365, 190, 2624);

        builder.AddRadio(30, 140, 9727, 9730, true, 1);
        builder.AddHtmlLocalized(65, 145, 300, 25, 1050050, 0x7FFF); // Yes, let's go!

        builder.AddRadio(30, 175, 9727, 9730, false, 0);
        builder.AddHtmlLocalized(65, 178, 300, 25, 1050049, 0x7FFF); // No thanks, I'd rather stay here.

        // Another player has paid Chyloth for your passage across lake Mortis:
        builder.AddHtmlLocalized(30, 20, 360, 35, 1050047, 0x7FFF);

        builder.AddHtmlLocalized(30, 105, 345, 40, 1050048, 0x5B2D); // Do you wish to accept their invitation at this time?

        builder.AddImage(65, 72, 5605);

        builder.AddImageTiled(80, 90, 200, 1, 9107);
        builder.AddImageTiled(95, 92, 200, 1, 9157);

        builder.AddLabelPlaceholder(90, 70, 1645, "leader");

        builder.AddButton(290, 175, 247, 248, 2);

        builder.AddImageTiled(15, 14, 365, 1, 9107);
        builder.AddImageTiled(380, 14, 1, 190, 9105);
        builder.AddImageTiled(15, 205, 365, 1, 9107);
        builder.AddImageTiled(15, 14, 1, 190, 9105);
        builder.AddImageTiled(0, 0, 395, 1, 9157);
        builder.AddImageTiled(394, 0, 1, 217, 9155);
        builder.AddImageTiled(0, 216, 395, 1, 9157);
        builder.AddImageTiled(0, 0, 1, 217, 9155);
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetStringSlot("leader", _leader.Name);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var member = sender.Mobile;

        if (info.ButtonID == 2 && info.IsSwitched(1))
        {
            if (member.Region.IsPartOf("Doom"))
            {
                // ~1_NAME~ has accepted your invitation to cross lake Mortis.
                _leader.SendLocalizedMessage(1050054, member.Name);

                Chyloth.TeleportToFerry(member);
            }
            else
            {
                member.SendLocalizedMessage(1050051); // The invitation has been revoked.
            }
        }
        else
        {
            member.SendLocalizedMessage(1050052); // You have declined their invitation.
            // ~1_NAME~ has declined your invitation to cross lake Mortis.
            _leader.SendLocalizedMessage(1050053, member.Name);
        }
    }
}
