using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class PetResurrectGump : StaticGump<PetResurrectGump>
{
    private readonly double _hitsScalar;
    private readonly BaseCreature _pet;

    public override bool Singleton => true;

    public PetResurrectGump(Mobile from, BaseCreature pet, double hitsScalar = 0.0) : base(50, 50)
    {
        _pet = pet;
        _hitsScalar = hitsScalar;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 265, 140, 0x242C);

        builder.AddItem(205, 40, 0x4);
        builder.AddItem(227, 40, 0x5);

        builder.AddItem(180, 78, 0xCAE);
        builder.AddItem(195, 90, 0xCAD);
        builder.AddItem(218, 95, 0xCB0);

        // <div align=center>Wilt thou sanctify the resurrection of:</div>
        builder.AddHtmlLocalized(30, 30, 150, 75, 1049665);
        builder.AddHtmlPlaceholder(30, 70, 150, 25, "petName", true);

        builder.AddButton(40, 105, 0x81A, 0x81B, 0x1);  // Okay
        builder.AddButton(110, 105, 0x819, 0x818, 0x2); // Cancel
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetHtmlText("petName", _pet.Name, align: TextAlignment.Center);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (_pet.Deleted || !_pet.IsBonded || !_pet.IsDeadPet)
        {
            return;
        }

        var from = state.Mobile;

        if (info.ButtonID != 1)
        {
            return;
        }

        if (_pet.Map?.CanFit(_pet.Location, 16, false, false) != true)
        {
            from.SendLocalizedMessage(503256); // You fail to resurrect the creature.
            return;
        }

        if (_pet.Region?.IsPartOf("Khaldun") == true) // TODO: Confirm for pets, as per Bandage's script.
        {
            // The veil of death in this area is too strong and resists thy efforts to restore life.
            from.SendLocalizedMessage(1010395);
            return;
        }

        _pet.PlaySound(0x214);
        _pet.FixedEffect(0x376A, 10, 16);
        _pet.ResurrectPet();

        var decreaseAmount = from == _pet.ControlMaster ? 0.1 : 0.2;

        for (var i = 0; i < _pet.Skills.Length; ++i) // Decrease all skills on pet.
        {
            _pet.Skills[i].Base -= decreaseAmount;
        }

        if (!_pet.IsDeadPet && _hitsScalar > 0)
        {
            _pet.Hits = (int)(_pet.HitsMax * _hitsScalar);
        }
    }
}
