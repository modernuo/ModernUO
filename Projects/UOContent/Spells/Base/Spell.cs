using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Logging;
using Server.Misc;
using Server.Mobiles;
using Server.Spells.Bushido;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Second;
using Server.Spells.Spellweaving;
using Server.Targeting;

namespace Server.Spells
{
    public abstract class Spell : ISpell
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Spell));
        private static readonly TimeSpan NextSpellDelay = TimeSpan.FromSeconds(0.75);

        private static readonly TimeSpan AnimateDelay = TimeSpan.FromSeconds(1.5);
        // In reality, it's ANY delayed Damage spell Post-AoS that can't stack, but, only
        // Expo & Magic Arrow have enough delay and a short enough cast time to bring up
        // the possibility of stacking 'em.  Note that a MA & an Explosion will stack, but
        // of course, two MA's won't.

        private static readonly Dictionary<Type, DelayedDamageContextWrapper> _contextTable = new();

        private AnimTimer _animTimer;
        private CastTimer _castTimer;

        public Spell(Mobile caster, Item scroll, SpellInfo info)
        {
            Caster = caster;
            Scroll = scroll;
            Info = info;
        }

        public SpellState State { get; set; }

        public Mobile Caster { get; }

        public SpellInfo Info { get; }

        public string Name => Info.Name;
        public string Mantra => Info.Mantra;
        public Type[] Reagents => Info.Reagents;
        public Item Scroll { get; }

        public long StartCastTime { get; private set; }

        public virtual SkillName CastSkill => SkillName.Magery;
        public virtual SkillName DamageSkill => SkillName.EvalInt;

        public virtual bool RevealOnCast => true;
        public virtual bool ClearHandsOnCast => true;
        public virtual bool ShowHandMovement => true;

        public virtual bool DelayedDamage => false;

        public static readonly Type[] AOSNoDelayedDamageStackingSelf = Core.AOS ? Array.Empty<Type>() : null;

        protected static bool IsTargetingSpell(Spell spell)
        {
            Type[] s = spell.GetType().GetInterfaces();
            foreach (Type t in s)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ITargetingSpell<>))
                    return true;
            }

            return false;
        }

        // Null means stacking is allowed while empty indicates no stacking with self
        // More than zero means no stacking with self and other spells
        public virtual Type[] DelayedDamageSpellFamilyStacking => null;

        public virtual bool BlockedByHorrificBeast => true;
        public virtual bool BlockedByAnimalForm => true;
        public virtual bool BlocksMovement => false;

        public virtual bool CheckNextSpellTime => Scroll is not BaseWand;

        public virtual int CastRecoveryBase => 6;
        public virtual int CastRecoveryFastScalar => 1;
        public virtual int CastRecoveryPerSecond => 4;
        public virtual int CastRecoveryMinimum => 0;

        public abstract TimeSpan CastDelayBase { get; }

        public virtual double CastDelayFastScalar => 1;
        public virtual double CastDelaySecondsPerTick => 0.25;
        public virtual TimeSpan CastDelayMinimum => TimeSpan.FromSeconds(0.25);

        public virtual bool IsCasting => State == SpellState.Casting;

        public virtual void OnCasterHurt()
        {
            // Confirm: Monsters and pets cannot be disturbed.
            if (Caster.Player && IsCasting)
            {
                var hasProtection = ProtectionSpell.Registry.TryGetValue(Caster, out var d);
                if (!hasProtection || d < 1000 && d < Utility.Random(1000))
                {
                    Disturb(DisturbType.Hurt, false, true);
                }
            }
        }

        public virtual void OnCasterKilled()
        {
            Disturb(DisturbType.Kill);
        }

        public virtual void OnConnectionChanged()
        {
            FinishSequence();
        }

        public virtual bool OnCasterMoving(Direction d)
        {
            if (BlocksMovement)
            {
                Caster.SendLocalizedMessage(500111); // You are frozen and can not move.
                return false;
            }

            return true;
        }

        public virtual bool OnCasterEquipping(Item item)
        {
            if (IsCasting)
            {
                Disturb(DisturbType.EquipRequest);
            }

            return true;
        }

        public virtual bool OnCasterUsingObject(IEntity entity)
        {
            if (State == SpellState.ApplyingCast)
            {
                Disturb(DisturbType.UseRequest);
            }

            return true;
        }

        public virtual bool OnCastInTown(Region r) => Info.AllowTown;
/// <summary>
/// sets spell.state - none,
/// if this spell is still attached to caster - nulls it
/// </summary>
        public virtual void FinishSequence()
        {
            State = SpellState.None;

            if (Caster.Spell == this)
            {
                Caster.Spell = null;
            }

            Caster.Delta(MobileDelta.Flags); // Remove paralyze
        }

        public void StartDelayedDamageContext(Mobile m, Timer t)
        {
            var damageStacking = DelayedDamageSpellFamilyStacking;
            if (damageStacking == null)
            {
                return; // Sanity
            }

            var type = GetType();

            if (!_contextTable.TryGetValue(type, out var context))
            {
                _contextTable[type] = context = new DelayedDamageContextWrapper();

                for (int i = 0; i < damageStacking.Length; i++)
                {
                    _contextTable.Add(damageStacking[i], context);
                }
            }

            context.Add(m, t);
        }

        public bool HasDelayedDamageContext(Mobile m) =>
            DelayedDamageSpellFamilyStacking != null &&
            _contextTable.TryGetValue(GetType(), out var context) && context.Contains(m);

        public void RemoveDelayedDamageContext(Mobile m)
        {
            if (m == null || DelayedDamageSpellFamilyStacking == null)
            {
                return; // Sanity
            }

            if (_contextTable.TryGetValue(GetType(), out var contexts))
            {
                contexts.Remove(m);
            }
        }

        public void HarmfulSpell(Mobile m)
        {
            (m as BaseCreature)?.OnHarmfulSpell(Caster);
        }

        public int GetNewAosDamage(int bonus, int dice, int sides, Mobile singleTarget) =>
            GetNewAosDamage(bonus, dice, sides, true, singleTarget);

        public virtual int GetNewAosDamage(int bonus, int dice, int sides, bool sdi = true, Mobile singleTarget = null)
        {
            if (singleTarget != null)
            {
                return GetNewAosDamage(
                    bonus,
                    dice,
                    sides,
                    Caster.Player && singleTarget.Player,
                    sdi,
                    GetDamageScalar(singleTarget)
                );
            }

            return GetNewAosDamage(bonus, dice, sides, sdi, false);
        }

        public virtual int GetNewAosDamage(
            int bonus, int dice, int sides, bool playerVsPlayer, bool sdi, double scalar = 1.0
        )
        {
            var damage = Utility.Dice(dice, sides, bonus) * 100;

            var inscribeSkill = GetInscribeFixed(Caster);
            var inscribeBonus = (inscribeSkill + 1000 * (inscribeSkill / 1000)) / 200;
            var damageBonus = inscribeBonus;

            var intBonus = Caster.Int / 10;
            damageBonus += intBonus;

            if (sdi)
            {
                var sdiBonus = AosAttributes.GetValue(Caster, AosAttribute.SpellDamage);
                // PvP spell damage increase cap of 15% from an item's magic property
                if (playerVsPlayer && sdiBonus > 15)
                {
                    sdiBonus = 15;
                }

                damageBonus += sdiBonus;
            }

            var context = TransformationSpellHelper.GetContext(Caster);

            if (context?.Spell is ReaperFormSpell spell)
            {
                damageBonus += spell.SpellDamageBonus;
            }

            damage = AOS.Scale(damage, 100 + damageBonus);

            var evalSkill = GetDamageFixed(Caster);
            var evalScale = 30 + 9 * evalSkill / 100;

            damage = AOS.Scale(damage, evalScale);

            damage = AOS.Scale(damage, (int)(scalar * 100));

            return damage / 100;
        }

        //true if: has scroll OR not a player OR had reagents and consumed them -1 means success
        public virtual bool ConsumeReagents()
        {
            // if (Caster?.AccessLevel > AccessLevel.Player)
            // {
            //     return true;
            // }

            //-1 is success code - consumed. or returns int of which reagent is not sufficient/ failure.
            //there is scroll. caster is GM. Succefully consumed reagents
           return Scroll != null || !Caster.Player || /*
            AosAttributes.GetValue(Caster, AosAttribute.LowerRegCost) > Utility.Random(100) ||
            DuelContext.IsFreeConsume(Caster) ||*/ Caster.Backpack?.ConsumeTotal(Info.Reagents, Info.Amounts) == -1;
        }

        public virtual double GetInscribeSkill(Mobile m) => m.Skills.Inscribe.Value;

        public virtual int GetInscribeFixed(Mobile m) => m.Skills.Inscribe.Fixed;

        public virtual int GetDamageFixed(Mobile m) => m.Skills[DamageSkill].Fixed;

        public virtual double GetDamageSkill(Mobile m) => m.Skills[DamageSkill].Value;

        public virtual double GetResistSkill(Mobile m) => m.Skills.MagicResist.Value;

        public virtual double GetDamageScalar(Mobile target)
        {
            var scalar = 1.0;

            if (!Core.AOS) // EvalInt stuff for AoS is handled elsewhere
            {
                var casterEI = Caster.Skills[DamageSkill].Value;
                var targetRS = target.Skills.MagicResist.Value;

                /*
                if (Core.AOS)
                  targetRS = 0;
                */

                // m_Caster.CheckSkill( DamageSkill, 0.0, 120.0 );

                if (casterEI > targetRS)
                {
                    scalar = 1.0 + (casterEI - targetRS) / 500.0;
                }
                else
                {
                    scalar = 1.0 + (casterEI - targetRS) / 200.0;
                }

                // magery damage bonus, -25% at 0 skill, +0% at 100 skill, +5% at 120 skill
                scalar += (Caster.Skills[CastSkill].Value - 100.0) / 400.0;

                if (!target.Player && !target.Body.IsHuman /*&& !Core.AOS*/)
                {
                    scalar *= 2.0; // Double magery damage to monsters/animals if not AOS
                }
            }

            (target as BaseCreature)?.AlterDamageScalarFrom(Caster, ref scalar);

            (Caster as BaseCreature)?.AlterDamageScalarTo(target, ref scalar);

            if (Core.SE)
            {
                scalar *= GetSlayerDamageScalar(target);
            }

            target.Region.SpellDamageScalar(Caster, target, ref scalar);

            if (Evasion.CheckSpellEvasion(target)) // Only single target spells an be evaded
            {
                scalar = 0;
            }

            return scalar;
        }

        public virtual double GetSlayerDamageScalar(Mobile defender)
        {
            var atkBook = Spellbook.FindEquippedSpellbook(Caster);

            var scalar = 1.0;
            if (atkBook != null)
            {
                var atkSlayer = SlayerGroup.GetEntryByName(atkBook.Slayer);
                var atkSlayer2 = SlayerGroup.GetEntryByName(atkBook.Slayer2);

                if (atkSlayer?.Slays(defender) == true || atkSlayer2?.Slays(defender) == true)
                {
                    defender.FixedEffect(0x37B9, 10, 5); // TODO: Confirm this displays on OSIs
                    scalar = 2.0;
                }

                var context = TransformationSpellHelper.GetContext(defender);

                if ((atkBook.Slayer == SlayerName.Silver || atkBook.Slayer2 == SlayerName.Silver) && context != null &&
                    context.Type != typeof(HorrificBeastSpell))
                {
                    scalar += .25; // Every necromancer transformation other than horrific beast take an additional 25% damage
                }

                if (scalar != 1.0)
                {
                    return scalar;
                }
            }

            var defISlayer = Spellbook.FindEquippedSpellbook(defender) ?? defender.Weapon as ISlayer;

            if (defISlayer != null)
            {
                var defSlayer = SlayerGroup.GetEntryByName(defISlayer.Slayer);
                var defSlayer2 = SlayerGroup.GetEntryByName(defISlayer.Slayer2);

                if (defSlayer?.Group.OppositionSuperSlays(Caster) == true ||
                    defSlayer2?.Group.OppositionSuperSlays(Caster) == true)
                {
                    scalar = 2.0;
                }
            }

            return scalar;
        }

        public virtual void DoFizzle()
        {
            Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.

            if (Caster.Player)
            {
                if (Core.AOS)
                {
                    Caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                }
                else
                {
                    Caster.FixedEffect(0x3735, 6, 30);
                }

                Caster.PlaySound(0x5C);
            }
        }

        public virtual bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable) =>
            !(resistable && Scroll is BaseWand);

        public void Disturb(DisturbType type, bool firstCircle = true, bool resistable = false)
        {
            if (!CheckDisturb(type, firstCircle, resistable))
            {
                return;
            }

            if (State == SpellState.None || !firstCircle && !Core.AOS && (this as MagerySpell)?.Circle == SpellCircle.First)
            {
                return;
            }

            var wasCasting = IsCasting; // Copy SpellState before resetting it to none
            State = SpellState.None;
            Caster.Spell = null;

            OnDisturb(type, wasCasting);

            if (wasCasting)
            {
                _castTimer?.Stop();
                _animTimer?.Stop();
                Caster.NextSpellTime = Core.TickCount + (int)GetDisturbRecovery().TotalMilliseconds;
            }
            // else
            // {
            //     Target.Cancel(Caster);
            // }

            if (Core.AOS && Caster.Player && type == DisturbType.Hurt)
            {
                DoHurtFizzle();
            }

            Caster.Delta(MobileDelta.Flags); // Remove paralyze
        }

        public virtual void DoHurtFizzle()
        {
            Caster.FixedEffect(0x3735, 6, 30);
            Caster.PlaySound(0x5C);
        }

        public virtual void OnDisturb(DisturbType type, bool message)
        {
            if (message)
            {
                Caster.SendLocalizedMessage(500641); // Your concentration is disturbed, thus ruining thy spell.
            }
        }
        //if this spell required to check cast
        public virtual bool CheckCast() => true;

        public virtual void SayMantra()
        {
            if (Scroll is BaseWand)
            {
                return;
            }

            if (!string.IsNullOrEmpty(Info.Mantra) && Caster.Player)
            {
                Caster.PublicOverheadMessage(MessageType.Spell, Caster.SpeechHue, true, Info.Mantra, false);
            }
        }



        public void StartAnimation()
        {
            var castDelay = GetCastDelay();

            if (ShowHandMovement && (Caster.Body.IsHuman || Caster.Player && Caster.Body.IsMonster))
            {
                var count = (int)Math.Ceiling(castDelay.TotalSeconds / AnimateDelay.TotalSeconds);

                if (count != 0)
                {
                    _animTimer = new AnimTimer(this, count);
                    _animTimer.Start();
                }

                if (Info.LeftHandEffect > 0)
                {
                    Caster.FixedParticles(0, 10, 5, Info.LeftHandEffect, EffectLayer.LeftHand);
                }

                if (Info.RightHandEffect > 0)
                {
                    Caster.FixedParticles(0, 10, 5, Info.RightHandEffect, EffectLayer.RightHand);
                }
            }
        }

        //checks mana, status before targeting/casting
        public bool Cast()
        {
            Caster.Spell = this;
            logger.Debug("Checking if can cast, then will choose target");
            // if (Core.AOS && Caster.Spell is Spell spell && spell.State == SpellState.Sequencing)
            // {
            //     spell.Disturb(DisturbType.NewCast);
            //     //take part of mana to feint? add only one feint?
            // }
            //Alive
            if (!Caster.CheckAlive())
            {
            }
            //Casting Already
            else if (Caster.Spell?.IsCasting == true)
            {
                Caster.SendLocalizedMessage(502642); // You are already casting a spell.
            }
            /*
            //Polymorph block
            if (BlockedByHorrificBeast &&
                     TransformationSpellHelper.UnderTransformation(Caster, typeof(HorrificBeastSpell)) ||
                     BlockedByAnimalForm && AnimalForm.UnderTransformation(Caster))
            {
                Caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
                return false;
            }
            */
            else if (!CheckIfEnoughSkill())
            {
            }

            //Paralyzed
            else if (Caster.Paralyzed && !Fists.HasFreeHands(Caster) || Caster.Frozen)
            {
                Caster.SendLocalizedMessage(502643); // You can not cast a spell while frozen. +added "and equipped weapon"
            }
            //Check if recovered from casting spell
            else if (CheckNextSpellTime && Core.TickCount - Caster.NextSpellTime < 0)
            {
                Caster.SendLocalizedMessage(502644); // You have not yet recovered from casting a spell.
            }
            //Peaced
            else if (Caster is PlayerMobile mobile && mobile.PeacedUntil > Core.Now)
            {
                mobile.SendLocalizedMessage(1072060); // You cannot cast a spell while calmed.
            }
            //DuelContext Allows SpellCast
            else if ((Caster as PlayerMobile)?.DuelContext?.AllowSpellCast(Caster, this) == false)
            {
            }
            //Enough mana
            else if (Caster.Mana <= ScaleMana(GetMana()))
            {
                Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana
            }
            //Region
            else if (!Caster.Region.OnBeginSpellCast(Caster, this))
            {
            }

            //Scroll and wand checks
            //reagents checks now done only with consumption CheckSequence()
            else if (Scroll != null &&
                     Scroll is not Runebook &&
                     (Scroll.Amount <= 0 ||
                      Scroll.Deleted ||
                      Scroll.RootParent != Caster ||
                      Scroll is BaseWand baseWand &&
                      (baseWand.Charges <= 0 || baseWand.Parent != Caster)))
            {
            }

            /*NOT FAILED OK SCENARIOS */
            else if (IsTargetingSpell(this))
            {
                State = SpellState.SelectingTarget;
                OnCast();
                logger.Debug("Targeting spell is being cast. It passed checks. Calling OnCast() of spell - creating target and waiting to player chosing target");
                return true;
            }
            //for regular non targeted spell
            else
            {
                logger.Debug("regular, non-targeting spell passed check and continue to cast");
                ContinueCast();
                return true;
            }

            FinishSequence();
            return false;
        }


        public bool ContinueCast()
        {
            logger.Debug("ContinueCast() called, so target class instance was created or non-target spell being cast");
            //Start actual casting and take recourses

            State = SpellState.Casting;
            StartCastTime = Core.TickCount;

            Caster.RevealingAction();
            SayMantra();

            //take mana
            if (Caster.Mana < ScaleMana(GetMana()))
            {
                Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana for this spell.
                Disturb(DisturbType.NotValidCast);
                return false;
            }
            else
            {
                logger.Debug("mana taken");
                Caster.Mana -= ScaleMana(GetMana());
            }

            //take reagents
            if (!ConsumeReagents())
            {
                logger.Debug("reagents taken");
                Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502630); // More reagents are needed for this spell.
                Disturb(DisturbType.NotValidCast);
                return false;
            }

            //check magery, take mana, sroll/wand
            if (!CheckIfEnoughSkill())
            {
                Disturb(DisturbType.NotValidCast);
                logger.Debug("not enough magery skill");
                return false;
            }
            //consume scroll
            if (Scroll is SpellScroll)
            {
                logger.Debug("scroll consumed");
                Scroll.Consume();
            }
            //consume wand
            else if (Scroll is BaseWand wand)
            {
                logger.Debug("wand charge consumed");
                wand.ConsumeCharge(Caster);
            }

            OnBeginCast();
            StartAnimation();

            _castTimer = new CastTimer(this, GetCastDelay());

            logger.Debug("castTimer created");
            if (GetCastDelay() > TimeSpan.Zero)
            {
                _castTimer.Start();
                logger.Debug("castTimer started");
            }
            else
            {
                _castTimer.Tick();
            }

            return true;
        }
        public abstract void OnCast();

        public virtual void OnBeginCast()
        {
        }

        public virtual void GetCastSkills(out double min, out double max)
        {
            min = max = 0; // Intended but not required for overriding.
        }

        public virtual bool CheckIfEnoughSkill()
        {
            if (Scroll is BaseWand)
            {
                return true;
            }

            GetCastSkills(out var minSkill, out var maxSkill);

            if (DamageSkill != CastSkill)
            {
                Caster.CheckSkill(DamageSkill, 0.0, Caster.Skills[DamageSkill].Cap);
            }

            return Caster.CheckSkill(CastSkill, minSkill, maxSkill);
        }

        public abstract int GetMana();

        public virtual int ScaleMana(int mana)
        {
            var scalar = 1.0;

            if (!MindRotSpell.GetMindRotScalar(Caster, ref scalar))
            {
                scalar = 1.0;
            }

            // Lower Mana Cost = 40%
            var lmc = AosAttributes.GetValue(Caster, AosAttribute.LowerManaCost);
            if (lmc > 40)
            {
                lmc = 40;
            }

            scalar -= (double)lmc / 100;

            return (int)(mana * scalar);
        }

        public virtual TimeSpan GetDisturbRecovery()
        {
            if (Core.AOS)
            {
                return TimeSpan.Zero;
            }

            var delay = Math.Max(
                1.0 - Math.Sqrt((Core.TickCount - StartCastTime) / 1000.0 / GetCastDelay().TotalSeconds),
                0.2
            );

            return TimeSpan.FromSeconds(delay);
        }

        public virtual TimeSpan GetCastRecovery()
        {
            if (!Core.AOS)
            {
                return NextSpellDelay;
            }

            var fcr = AosAttributes.GetValue(Caster, AosAttribute.CastRecovery) -
                      ThunderstormSpell.GetCastRecoveryMalus(Caster);

            var fcrDelay = -(CastRecoveryFastScalar * fcr);

            var delay = CastRecoveryBase + fcrDelay;

            if (delay < CastRecoveryMinimum)
            {
                delay = CastRecoveryMinimum;
            }

            return TimeSpan.FromSeconds((double)delay / CastRecoveryPerSecond);
        }

        public virtual TimeSpan GetCastDelay()
        {
            if (Scroll is BaseWand)
            {
                return Core.ML ? CastDelayBase : TimeSpan.Zero; // TODO: Should FC apply to wands?
            }

            // Faster casting cap of 2 (if not using the protection spell)
            // Faster casting cap of 0 (if using the protection spell)
            // Paladin spells are subject to a faster casting cap of 4
            // Paladins with magery of 70.0 or above are subject to a faster casting cap of 2
            var fcMax = 4;

            if (CastSkill is SkillName.Magery or SkillName.Necromancy ||
                CastSkill == SkillName.Chivalry && Caster.Skills.Magery.Value >= 70.0)
            {
                fcMax = 2;
            }

            var fc = Math.Min(AosAttributes.GetValue(Caster, AosAttribute.CastSpeed), fcMax);

            if (ProtectionSpell.Registry.ContainsKey(Caster))
            {
                fc -= 2;
            }

            if (EssenceOfWindSpell.IsDebuffed(Caster))
            {
                fc -= EssenceOfWindSpell.GetFCMalus(Caster);
            }

            if (Core.SA)
            {
                // At some point OSI added 0.25s to every spell. This makes the minimum 0.5s
                // Note: This is done after multiplying for summon creature & blade spirits.
                fc--;
            }

            var fcDelay = TimeSpan.FromSeconds(-(CastDelayFastScalar * fc * CastDelaySecondsPerTick));

            return Utility.Max(CastDelayBase + fcDelay, CastDelayMinimum);
        }

        public virtual int ComputeKarmaAward() => 0;

        public virtual bool CheckSequence()
        {
            if (Caster.Deleted || !Caster.Alive || Caster.Spell != this || State != SpellState.ApplyingCast)
            {
                DoFizzle();
                return false;
            }

            //now set to 0 - can be added karma on cast
            var karma = ComputeKarmaAward();
            if (karma != 0)
            {
                Titles.AwardKarma(Caster, karma, true);
            }
            //if garlic was in reagents - hurt caster
            if (TransformationSpellHelper.UnderTransformation(Caster, typeof(VampiricEmbraceSpell)))
            {
                var garlic = false;

                for (var i = 0; !garlic && i < Info.Reagents.Length; ++i)
                {
                    garlic = Info.Reagents[i] == Reagent.Garlic;
                }

                if (garlic)
                {
                    Caster.SendLocalizedMessage(1061651); // The garlic burns you!
                    AOS.Damage(Caster, Utility.RandomMinMax(17, 23), 100, 0, 0, 0, 0);
                }

                return true;
            }
            DoFizzle();
            return false;
        }

        public bool CheckBSequence(Mobile target, bool allowDead = false)
        {
            if (!target.Alive && !allowDead)
            {
                Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }

            if ((Caster as PlayerMobile)?.Young == true && (target as PlayerMobile)?.Young == false)
            {
                Caster.SendLocalizedMessage(500278); // As a young player, you may not cast beneficial spells onto older players.
                return false;
            }

            if (Caster.CanBeBeneficial(target, true, allowDead) && CheckSequence())
            {
                Caster.DoBeneficial(target);
                return true;
            }

            return false;
        }

        public bool CheckHSequence(Mobile target)
        {
            if (!target.Alive)
            {
                Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }

            if (Caster.CanBeHarmful(target) && CheckSequence())
            {
                Caster.DoHarmful(target);
                return true;
            }

            return false;
        }

        private class DelayedDamageContextWrapper
        {
            private readonly Dictionary<Mobile, Timer> m_Contexts = new();

            public void Add(Mobile m, Timer t)
            {
                if (m_Contexts.Remove(m, out var oldTimer))
                {
                    oldTimer.Stop();
                }

                m_Contexts.Add(m, t);
            }

            public bool Contains(Mobile m) => m_Contexts.ContainsKey(m);

            public void Remove(Mobile m)
            {
                if (m_Contexts.Remove(m, out var t))
                {
                    t.Stop();
                }
            }
        }

        private class AnimTimer : Timer
        {
            private readonly Spell m_Spell;

            public AnimTimer(Spell spell, int count) : base(TimeSpan.Zero, AnimateDelay, count)
            {
                m_Spell = spell;
            }

            protected override void OnTick()
            {
                var caster = m_Spell.Caster;

                if (m_Spell.State != SpellState.Casting || caster.Spell != m_Spell)
                {
                    Stop();
                    return;
                }

                if (!caster.Mounted && m_Spell.Info.Action >= 0)
                {
                    if (caster.Body.IsHuman)
                    {
                        caster.Animate(m_Spell.Info.Action, 7, 1, true, false, 0);
                    }
                    else if (caster.Player && caster.Body.IsMonster)
                    {
                        caster.Animate(12, 7, 1, true, false, 0);
                    }
                }

                if (!Running)
                {
                    m_Spell._animTimer = null;
                }
            }
        }

        private class CastTimer : Timer
        {
            private readonly Spell m_Spell;

            public CastTimer(Spell spell, TimeSpan castDelay) : base(castDelay)
            {
                m_Spell = spell;
            }

            //IMMEDIATE APPLY OF SPELL
            protected override void OnTick()
            {
                var caster = m_Spell?.Caster;
                logger.Debug("OnTick() of casttimer executed");
                logger.Debug($"caster is: {caster}");
                logger.Debug($"spell state is: {m_Spell?.State}");

                if (caster == null)
                {
                    return;
                }
                //if player got new target between mantra and apply. OR spell was disturb - not State Casting anymore
                if (m_Spell.State == SpellState.Casting && caster.Spell == m_Spell)
                {
                    //self queue to GC
                    m_Spell._castTimer = null;
                    caster.Region?.OnSpellCast(caster, m_Spell);
                    //set recovery, prohibits from new casts
                    caster.NextSpellTime =
                        Core.TickCount + (int)m_Spell.GetCastRecovery().TotalMilliseconds; // when next spell allowed (each spell has it) Spell.NextSpellTime;

                    caster.Delta(MobileDelta.Flags); // Update paralyze

                    var originalTarget = caster.Target;

                    if (IsTargetingSpell(m_Spell))
                    {
                        caster.Target?.ApplySpellOnTarget();
                    }
                    else
                    {
                        m_Spell?.OnCast();
                    }
                    //if player got new target between mantra and apply of spell
                    if (caster.Player && caster.Target != originalTarget)
                    {

                        logger.Debug("new target was selected before spell applied"); // idk if remove it or make disturb
                                                                                      //  apply bandages (new target)- should disturb?
                                                                                      // new casting is already blocked
                                                                                      // by calling disturb and setting recovery
                        //caster.Target?.BeginTimeout(caster, 30000); // 30 seconds to apply cast
                    }
                    //finally remove this timer from spell = timout
                    if (m_Spell != null) m_Spell._castTimer = null;
                }
            }

            public void Tick()
            {
                OnTick();
            }
        }
    }
}
