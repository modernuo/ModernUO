using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Spells;

namespace Server.Engines.Doom;

[SerializationGenerator(0, false)]
public partial class LampRoomBox : Item
{
    [SerializableField(0)]
    private LeverPuzzleController _controller;
    private Mobile _wanderer;

    public LampRoomBox(LeverPuzzleController controller) : base(0xe80)
    {
        _controller = controller;
        ItemID = 0xe80;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 3))
        {
            return;
        }

        if (_controller.Enabled)
        {
            return;
        }

        if (_wanderer?.Alive != true)
        {
            _wanderer = new WandererOfTheVoid();
            _wanderer.MoveToWorld(LeverPuzzleController.lr_Enter, Map.Malas);
            _wanderer.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1060002); // I am the guardian of...
            Timer.StartTimer(TimeSpan.FromSeconds(5.0), CallBackMessage);
        }
    }

    public void CallBackMessage()
    {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1060003); // You try to pry the box open...
    }

    public override void OnAfterDelete()
    {
        if (_controller?.Deleted == false)
        {
            _controller.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class LeverPuzzleStatue : Item
{
    private LeverPuzzleController _controller;

    public LeverPuzzleStatue(int[] dat, LeverPuzzleController controller) : base(dat[0])
    {
        _controller = controller;
        Hue = 0x44E;
        Movable = false;
    }

    public override void OnAfterDelete()
    {
        if (_controller?.Deleted == false)
        {
            _controller.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class LeverPuzzleLever : Item
{
    [SerializableField(0, setter: "private")]
    private ushort _code;

    [SerializableField(1)]
    private LeverPuzzleController _controller;

    public LeverPuzzleLever(ushort code, LeverPuzzleController controller) : base(0x108E)
    {
        _controller = controller;
        _code = code;
        Hue = 0x66D;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m != null && _controller.Enabled)
        {
            ItemID ^= 2;
            Effects.PlaySound(Location, Map, 0x3E8);
            _controller.LeverPulled(Code);
        }
        else
        {
            m?.SendLocalizedMessage(1060001); // You throw the switch, but the mechanism cannot be engaged again so soon.
        }
    }

    public override void OnAfterDelete()
    {
        if (_controller?.Deleted == false)
        {
            _controller.Delete();
        }
    }
}

[TypeAlias("Server.Engines.Doom.LampRoomTelePorter")]
[SerializationGenerator(0, false)]
public partial class LampRoomTeleporter : Item
{
    public LampRoomTeleporter(int[] dat)
    {
        Hue = dat[1];
        ItemID = dat[0];
        Movable = false;
    }

    public override bool HandlesOnMovement => true;

    public override bool OnMoveOver(Mobile m)
    {
        if (m is PlayerMobile)
        {
            if (SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else
            {
                BaseCreature.TeleportPets(m, LeverPuzzleController.lr_Exit, Map.Malas);
                m.MoveToWorld(LeverPuzzleController.lr_Exit, Map.Malas);
                return false;
            }
        }

        return true;
    }
}
