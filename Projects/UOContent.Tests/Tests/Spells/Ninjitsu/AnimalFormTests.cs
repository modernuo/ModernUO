using System;
using Server.Mobiles;
using Server.Spells.Ninjitsu;
using Xunit;

namespace Server.Tests.Spells.Ninjitsu;

[Collection("Sequential UOContent Tests")]
public class AnimalFormTests
{
    private static int IndexOf(Type type) => Array.FindIndex(AnimalForm.Entries, e => e.Type == type);

    private static Mobile NewMobileWithNinjitsu(double skill)
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        m.Skills.Ninjitsu.Base = skill;
        return m;
    }

    // Issue #2452: with only 30 Ninjitsu the gump showed every form (including Dog, which needs 40)
    // because the selectable check compared Skill.Fixed (value x10) against the raw ReqSkill.
    [Fact]
    public void CanSelectEntry_FormAboveSkill_IsNotSelectable()
    {
        var m = NewMobileWithNinjitsu(30.0);

        Assert.False(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Dog))]));  // ReqSkill 40
        Assert.False(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Cat))]));  // ReqSkill 40

        m.Delete();
    }

    [Fact]
    public void CanSelectEntry_FormAtOrBelowSkill_IsSelectable()
    {
        var m = NewMobileWithNinjitsu(40.0);

        Assert.True(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Dog))]));    // ReqSkill 40 (== boundary)
        Assert.True(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Rat))]));    // ReqSkill 20
        Assert.True(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Rabbit))])); // ReqSkill 20

        m.Delete();
    }

    // Talisman-gated forms are never selectable without the matching talisman, regardless of skill.
    [Fact]
    public void CanSelectEntry_TalismanGatedForm_RequiresTalisman()
    {
        var m = NewMobileWithNinjitsu(120.0);

        Assert.False(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Squirrel))]));
        Assert.False(AnimalForm.CanSelectEntry(m, AnimalForm.Entries[IndexOf(typeof(Ferret))]));

        m.Delete();
    }

    // Issue #2452: attempting a form above your skill must report NoSkill (so the caller can
    // refrain from charging mana) and must not actually transform the caster.
    [Fact]
    public void Morph_InsufficientSkill_ReturnsNoSkillAndDoesNotTransform()
    {
        var m = NewMobileWithNinjitsu(30.0);

        var result = AnimalForm.Morph(m, IndexOf(typeof(Dog))); // needs 40

        Assert.Equal(AnimalForm.MorphResult.NoSkill, result);
        Assert.Null(AnimalForm.GetContext(m));
        Assert.Equal(0, (int)m.BodyMod);

        m.Delete();
    }

    [Fact]
    public void Morph_SufficientSkill_TransformsCaster()
    {
        // >= ReqSkill + 37.5 guarantees the success-chance roll is skipped.
        var m = NewMobileWithNinjitsu(60.0);

        var result = AnimalForm.Morph(m, IndexOf(typeof(Rat))); // needs 20

        Assert.Equal(AnimalForm.MorphResult.Success, result);
        Assert.NotNull(AnimalForm.GetContext(m));

        AnimalForm.RemoveContext(m); // stop the form timer / clear state
        m.Delete();
    }
}
