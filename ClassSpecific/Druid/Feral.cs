using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace Singular.ClassSpecific.Druid
{
    public class Feral
    {
        private const int FeralT13ItemSetId = 1058;

        private static DruidSettings Settings
        {
            get { return SingularSettings.Instance.Druid; }
        }

        private static int Finisherhealth
        {
            get
            {
                int health = 120;
                if (StyxWoW.Me.Level == 85 && StyxWoW.Me.IsInRaid && StyxWoW.Me.NumRaidMembers >= 11)
                    health = 500000;
                if (StyxWoW.Me.Level == 85 && StyxWoW.Me.IsInRaid && StyxWoW.Me.NumRaidMembers <= 10)
                    health = 250000;
                if (StyxWoW.Me.Level == 85 && StyxWoW.Me.IsInInstance)
                    health = 100000;
                if (StyxWoW.Me.Level >= 80 && StyxWoW.Me.Level <= 81 && !StyxWoW.Me.IsInInstance && !StyxWoW.Me.IsInRaid)
                    health = 1000;
                if (StyxWoW.Me.Level >= 82 && StyxWoW.Me.Level <= 84 && !StyxWoW.Me.IsInInstance && !StyxWoW.Me.IsInRaid)
                    health = 2500;
                if (StyxWoW.Me.Level >= 80 && StyxWoW.Me.Level <= 84 && StyxWoW.Me.IsInInstance)
                    health = 4000;
                if (StyxWoW.Me.Level >= 70 && StyxWoW.Me.Level <= 79)
                    health = StyxWoW.Me.Level*30;
                if (StyxWoW.Me.Level >= 10 && StyxWoW.Me.Level <= 69)
                    health = StyxWoW.Me.Level*20;
                return health;
            }
        }

        private static int NumTier13Pieces
        {
            get
            {
                int
                    count = StyxWoW.Me.Inventory.Equipped.Hands != null &&
                            StyxWoW.Me.Inventory.Equipped.Hands.ItemInfo.ItemSetId == FeralT13ItemSetId
                                ? 1
                                : 0;
                count += StyxWoW.Me.Inventory.Equipped.Legs != null &&
                         StyxWoW.Me.Inventory.Equipped.Legs.ItemInfo.ItemSetId == FeralT13ItemSetId
                             ? 1
                             : 0;
                count += StyxWoW.Me.Inventory.Equipped.Chest != null &&
                         StyxWoW.Me.Inventory.Equipped.Chest.ItemInfo.ItemSetId == FeralT13ItemSetId
                             ? 1
                             : 0;
                count += StyxWoW.Me.Inventory.Equipped.Shoulder != null &&
                         StyxWoW.Me.Inventory.Equipped.Shoulder.ItemInfo.ItemSetId == FeralT13ItemSetId
                             ? 1
                             : 0;
                count += StyxWoW.Me.Inventory.Equipped.Head != null &&
                         StyxWoW.Me.Inventory.Equipped.Head.ItemInfo.ItemSetId == FeralT13ItemSetId
                             ? 1
                             : 0;
                return count;
            }
        }

        private static bool HasTeir13Bonus
        {
            get { return NumTier13Pieces >= 2; }
        }

        private static bool HasTeir13Bonus2
        {
            get { return NumTier13Pieces >= 4; }
        }

        private static bool IsrakeTfed
        {
            get
            {
                if (StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                    StyxWoW.Me.GetAuraTimeLeft("Tiger's Fury", true).TotalSeconds >= 6)
                    return false;

                if ((StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                     StyxWoW.Me.GetAuraTimeLeft("Tiger's Fury", true).TotalSeconds > 5) &&
                    (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 14)
                    )
                    return false;

                if ((StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                     StyxWoW.Me.GetAuraTimeLeft("Tiger's Fury", true).TotalSeconds > 4) &&
                    (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 13)
                    )
                    return false;

                if ((StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                     StyxWoW.Me.GetAuraTimeLeft("Tiger's Fury", true).TotalSeconds > 3) &&
                    (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 11)
                    )
                    return false;

                if ((StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                     StyxWoW.Me.GetAuraTimeLeft("Tiger's Fury", true).TotalSeconds > 1) &&
                    (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 10)
                    )
                    return false;

                return (!StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") ||
                        StyxWoW.Me.GetAuraTimeLeft("Tiger's Fury", true).TotalSeconds < 1)
                       ||
                       (!StyxWoW.Me.CurrentTarget.HasMyAura("Rip") ||
                        StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds >= 9);
            }
        }

        private static float EnergyRegen
        {
            get { return Lua.GetReturnVal<float>("return GetPowerRegen()", 1); }
        }


        private static int CurrentEnergy
        {
            get { return Lua.GetReturnVal<int>("return UnitMana(\"player\");", 0); }
        }

        public static List<WoWUnit> EnemyUnits
        {
            get
            {
                return
                    ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                        .Where(unit =>
                               !unit.IsFriendly
                               && (unit.IsTargetingMeOrPet
                                   || unit.IsTargetingMyPartyMember
                                   || unit.IsTargetingMyRaidMember
                                   || unit.IsPlayer)
                               && !unit.IsNonCombatPet
                               && !unit.IsCritter
                               && unit.DistanceSqr
                               <= 15*15).ToList();
            }
        }

        [Spec(TalentSpec.FeralDruid)]
        [Spec(TalentSpec.FeralTankDruid)]
        [Behavior(BehaviorType.Pull)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.All)]
        public static Composite CreateFeralPull()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => SingularSettings.Instance.Druid.ForceBear && (!StyxWoW.Me.IsInInstance ||
                                                                         (Group.Tank != null &&
                                                                          (Group.Tank.IsMe || !Group.Tank.IsAlive))) ||
                           StyxWoW.Me.ActiveAuras.ContainsKey("Frenzied Regeneration") ||
                           StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Druid.BearLife ||
                           EnemyUnits.Count >= SingularSettings.Instance.Druid.BearCount,
                    CreateBearTankCombat()
                    ),
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.ForceBear ||
                           StyxWoW.Me.IsInInstance && Group.Tank != null && !Group.Tank.IsMe && Group.Tank.IsAlive,
                    CreateFeralCatPull())
                );
        }


        [Spec(TalentSpec.FeralDruid)]
        [Spec(TalentSpec.FeralTankDruid)]
        [Behavior(BehaviorType.Combat)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.All)]
        public static Composite CreateFeralCombat()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => SingularSettings.Instance.Druid.ForceBear && (!StyxWoW.Me.IsInInstance ||
                                                                         (Group.Tank != null &&
                                                                          (Group.Tank.IsMe || !Group.Tank.IsAlive))) ||
                           StyxWoW.Me.ActiveAuras.ContainsKey("Frenzied Regeneration") ||
                           StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Druid.BearLife ||
                           EnemyUnits.Count >= SingularSettings.Instance.Druid.BearCount,
                    CreateBearTankCombat()
                    ),
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.ForceBear ||
                           StyxWoW.Me.IsInInstance && Group.Tank != null && !Group.Tank.IsMe && Group.Tank.IsAlive,
                    CreateFeralCatCombat()
                    ));
        }

        #region Cat

        public static Composite CreateFeralCatCombat()
        {
            return new PrioritySelector(
                CreateFeralCatManualForms(),
                CreateFeralCatActualCombat());
        }

        public static Composite CreateFeralCatPull()
        {
            return new PrioritySelector(
                CreateFeralCatManualForms(),
                CreateFeralCatActualPull());
        }

        public static Composite CreateFeralCatManualForms()
        {
            return new PrioritySelector(
                Spell.WaitForCast(),
                // If we're in caster form, and not casting anything (tranq), then fucking switch to cat.
                new Decorator(
                    ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Normal,
                    Spell.BuffSelf("Cat Form")),
                // We don't want to get stuck in Aquatic form. We won't be able to cast shit
                new Decorator(
                    ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Aqua,
                    Spell.BuffSelf("Cat Form")),
                new Decorator(
                    ret => !Settings.ManualForms && StyxWoW.Me.Shapeshift != ShapeshiftForm.Cat,
                    Spell.BuffSelf("Cat Form")),
                //// If the user has manual forms enabled. Automatically switch to cat combat if they switch forms.
                new Decorator(
                    ret => Settings.ManualForms && StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear,
                    new PrioritySelector(
                        CreateBearTankActualCombat(),
                        new ActionAlwaysSucceed())));
        }

        public static Composite CreateFeralCatActualPull()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                new Decorator(
                    ret =>
                    !StyxWoW.Me.IsInRaid && !StyxWoW.Me.IsInParty && SingularSettings.Instance.Druid.FeralHeal,
                    Resto.CreateRestoDruidHealOnlyBehavior(true)),
                //based on Ej
                //http://elitistjerks.com/f73/t127445-feral_cat_cataclysm_4_3_dragon_soul/#Rotation
                Spell.Cast("Pounce",
                           ret =>
                           StyxWoW.Me.HasAura("Prowl") &&
                           SingularSettings.Instance.Druid.ProwlPounce),
                Spell.Cast("Faerie Fire (Feral)", ret => SingularSettings.Instance.Druid.PullFff),
                Spell.Cast(
                    "Feral Charge (Cat)",
                    ret =>
                    SingularSettings.Instance.Druid.UseFeralChargeCatPull && StyxWoW.Me.CurrentTarget.Distance >= 10 &&
                    StyxWoW.Me.CurrentTarget.Distance <= 23),
                Movement.CreateMoveToMeleeBehavior(true));
        }


        public static Composite CreateFeralCatActualCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                new Decorator(
                    ret => !StyxWoW.Me.IsInRaid && !StyxWoW.Me.IsInParty && SingularSettings.Instance.Druid.FeralHeal,
                    Resto.CreateRestoDruidHealOnlyBehavior(true)),
                new Decorator(ret => !SingularSettings.Instance.Druid.DisableInterrupts,
                              Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget)),
                Movement.CreateMoveBehindTargetBehavior(),
                new Decorator(
                    ret =>
                    (SingularSettings.Instance.Druid.Shift && StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Rooted) &&
                     StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat),
                    new Sequence(
                        new Action(ret => Lua.DoString("RunMacroText(\"/Cast !Cat Form\")")))),
                new Decorator(
                    ret =>
                    (SingularSettings.Instance.Druid.ShiftSlow &&
                     StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Snared) &&
                     !StyxWoW.Me.ActiveAuras.ContainsKey("Crippling Poison") &&
                     StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat),
                    new Sequence(
                        new Action(ret => Lua.DoString("RunMacroText(\"/Cast !Cat Form\")")))),
                Spell.Cast(
                    "Feral Charge (Cat)",
                    ret =>
                    Settings.UseFeralChargeCat && StyxWoW.Me.CurrentTarget.Distance >= 10 &&
                    StyxWoW.Me.CurrentTarget.Distance <= 23),
                /*Bases on Mew!*/

                /*Tiger's Fury!*/
                // #1
                Spell.BuffSelf("Tiger's Fury", ret => CurrentEnergy <= 26 &&
                                                      !SpellManager.GlobalCooldown &&
                                                      StyxWoW.Me.CurrentTarget.Level < 85),
                // #2
                Spell.BuffSelf("Tiger's Fury", ret => CurrentEnergy <= 35 &&
                                                      !StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") &&
                                                      !SpellManager.GlobalCooldown &&
                                                      StyxWoW.Me.CurrentTarget.Level >= 85),
                // #3
                Spell.BuffSelf("Tiger's Fury", ret => CurrentEnergy <= 45 &&
                                                      HasTeir13Bonus2 &&
                                                      !StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") &&
                                                      !SpellManager.GlobalCooldown &&
                                                      !StyxWoW.Me.ActiveAuras.ContainsKey("Stampede")
                    ),
                /*Berserk!*/

                //#4
                Spell.Cast("Berserk",
                           ret =>
                           SingularSettings.Instance.Druid.UseBerserkCat &&
                           StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") && !SpellManager.GlobalCooldown),
                //#5 
                Spell.Cast("Berserk",
                           ret =>
                           SingularSettings.Instance.Druid.UseBerserkCat && !SpellManager.GlobalCooldown &&
                           StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth < 25 &&
                           SpellManager.HasSpell("Tiger's Fury") &&
                           SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds > 6),
                /*AOE*/
                new Decorator(
                    ret => EnemyUnits.Count >=
                           SingularSettings.Instance.Druid.SwipeCount,
                    new PrioritySelector(
                        new Decorator(
                            ret =>
                            (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") &&
                             StyxWoW.Me.GetAuraTimeLeft("Stampede", true).TotalSeconds <= 2.0),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        Spell.Cast("Swipe (Cat)"),
                        new Decorator(
                            ret => (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") &&
                                    !StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") &&
                                    StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                                    CurrentEnergy <= (EnergyRegen)),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        Spell.Cast("Faerie Fire (Feral)",
                                   ret =>
                                   StyxWoW.Me.CurrentTarget.IsBoss() &&
                                   (!StyxWoW.Me.CurrentTarget.HasSunders() ||
                                    (StyxWoW.Me.CurrentTarget.HasMyAura("Faerie Fire") &&
                                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Faerie Fire", true).TotalSeconds <= 1)
                                   )
                            )
                        )
                    ),
                /*Debuffs!*/

                new Decorator(
                    ret => EnemyUnits.Count <
                           SingularSettings.Instance.Druid.SwipeCount,
                    new PrioritySelector(
                        //#6
                        Spell.Cast("Faerie Fire (Feral)",
                                   ret =>
                                   StyxWoW.Me.CurrentTarget.IsBoss() &&
                                   (!StyxWoW.Me.CurrentTarget.HasSunders() ||
                                    (StyxWoW.Me.CurrentTarget.HasMyAura("Faerie Fire") &&
                                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Faerie Fire", true).TotalSeconds <= 1)
                                   )
                            ),
                        //#7
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   !StyxWoW.Me.CurrentTarget.HasBleedDebuff() ||
                                   (StyxWoW.Me.CurrentTarget.HasMyAura("Mangle") &&
                                    StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Mangle", true).TotalSeconds <= 2)
                            ),
                        /*Ravage!*/

                        //#8
                        new Decorator(
                            ret =>
                            (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") &&
                             StyxWoW.Me.GetAuraTimeLeft("Stampede", true).TotalSeconds <= 2.0),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    //SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        //#grinding
                        new Decorator(
                            ret =>
                            (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") && SingularSettings.Instance.Druid.Ravage),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        //#Ferocious Bite for grinding
                        Spell.Cast("Ferocious Bite",
                                   ret =>
                                   StyxWoW.Me.CurrentTarget.HealthPercent <= StyxWoW.Me.ComboPoints*10 &&
                                   SingularSettings.Instance.Druid.Bite),
                        /*Blood in the Water!*/

                        //#9
                        Spell.Cast("Ferocious Bite",
                                   ret => StyxWoW.Me.ComboPoints > 0 &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent <= (HasTeir13Bonus ? 60 : 25) &&
                                          StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds <= 2.1 &&
                                          StyxWoW.Me.CurrentTarget.HasMyAura("Rip")
                            ),
                        //#10
                        Spell.Cast("Ferocious Bite",
                                   ret => StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent <= (HasTeir13Bonus ? 60 : 25) &&
                                          StyxWoW.Me.CurrentTarget.HasMyAura("Rip")
                            ),
                        //Missing Glyph of Bloodletting

                        //#Ferocious Bite for low lvls 
                        Spell.Cast("Ferocious Bite",
                                   ret => StyxWoW.Me.Level <= 60 &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent <= StyxWoW.Me.ComboPoints*10),
                        //#rip for low lvls 
                        Spell.Cast("Rip",
                                   ret => StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.Level <= 60 &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent > 50 &&
                                          !StyxWoW.Me.CurrentTarget.HasMyAura("Rip") ||
                                          (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds < 2.0)
                            ),
                        //#rake for low lvls
                        Spell.Cast("Rake",
                                   ret => (!StyxWoW.Me.CurrentTarget.HasMyAura("Rake") ||
                                           (StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                            StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 3.0)
                                          ) &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent > 50
                                          && StyxWoW.Me.Level <= 84
                            ),
                        // FB / Rip / Rake while pvping


                        Spell.Cast("Ferocious Bite",
                                   ret => StyxWoW.Me.CurrentTarget.IsPlayer && StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent <= 40),
                        Spell.Cast("Rip",
                                   ret => StyxWoW.Me.CurrentTarget.IsPlayer && StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.HealthPercent > 40 &&
                                          SpellManager.HasSpell("Tiger's Fury") &&
                                          (!StyxWoW.Me.CurrentTarget.HasMyAura("Rip") ||
                                           (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                                            StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds < 2.0)
                                          )
                                          && (StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") ||
                                              StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds + 2 <=
                                              SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds)
                            ),
                        Spell.Cast("Rake",
                                   ret =>
                                   StyxWoW.Me.CurrentTarget.IsPlayer && StyxWoW.Me.CurrentTarget.HealthPercent > 40 &&
                                   StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                                   StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                   !IsrakeTfed),
                        Spell.Cast("Rake",
                                   ret =>
                                   StyxWoW.Me.CurrentTarget.IsPlayer && StyxWoW.Me.CurrentTarget.HealthPercent > 40 &&
                                   (!StyxWoW.Me.CurrentTarget.HasMyAura("Rake") ||
                                    (StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 3.0)
                                   ) &&
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") ||
                                    (StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                     SpellManager.HasSpell("Tiger's Fury") &&
                                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds <=
                                     SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds) ||
                                    CurrentEnergy >= 71)
                            ),
                        /*Regular Rotation*/

                        //#11
                        Spell.Cast("Rip",
                                   ret => StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth >= 6 &&
                                          SpellManager.HasSpell("Tiger's Fury") &&
                                          (!StyxWoW.Me.CurrentTarget.HasMyAura("Rip") ||
                                           (StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                                            StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds < 2.0)
                                          )
                                          && (StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") ||
                                              StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds + 2 <=
                                              SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds)
                            ),
                        //#12       
                        Spell.Cast("Ferocious Bite",
                                   ret => StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") &&
                                          StyxWoW.Me.ComboPoints == 5 &&
                                          CurrentEnergy >= 60 &&
                                          StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds > 5 &&
                                          StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds >= 3),
                        //#13
                        Spell.Cast("Rake",
                                   ret => StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth >= 8.5 &&
                                          StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                                          StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                          !IsrakeTfed),
                        //#14
                        Spell.Cast("Rake",
                                   ret => StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth >= 8.5 &&
                                          (!StyxWoW.Me.CurrentTarget.HasMyAura("Rake") ||
                                           (StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                            StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 3.0)
                                          ) &&
                                          (StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") ||
                                           (StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                                            SpellManager.HasSpell("Tiger's Fury") &&
                                            StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds <=
                                            SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds) ||
                                           CurrentEnergy >= 71)
                            ),
                        //#15
                        Spell.Cast("Shred",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseShred &&
                                   StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) &&
                                   StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting")),
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   (!StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) ||
                                    !SingularSettings.Instance.Druid.UseShred) &&
                                   StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting")
                            ),
                        //#16
                        Spell.BuffSelf("Savage Roar",
                                       ret =>
                                       SingularSettings.Instance.Druid.UseSavageRoarCat && StyxWoW.Me.ComboPoints > 0 &&
                                       (!StyxWoW.Me.ActiveAuras.ContainsKey("Savage Roar") ||
                                        StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds <= 2)
                            ),
                        //#17
                        new Decorator(
                            ret => (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") &&
                                    SpellManager.HasSpell("Tiger's Fury") &&
                                    SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds < 1.0 &&
                                    HasTeir13Bonus2),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        //#new
                        Spell.BuffSelf("Savage Roar",
                                       ret =>
                                       SingularSettings.Instance.Druid.UseSavageRoarCat && StyxWoW.Me.ComboPoints == 5 &&
                                       StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth >= 9 &&
                                       StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                                       StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds <= 12
                                       &&
                                       (StyxWoW.Me.ActiveAuras.ContainsKey("Savage Roar") &&
                                        StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds
                                        <= StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds + 6)
                            ),
                        //#18
                        Spell.Cast("Ferocious Bite",
                                   ret => StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth <= 7),
                        //#19
                        Spell.Cast("Ferocious Bite",
                                   ret => (!StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") ||
                                           CurrentEnergy < 25) &&
                                          StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds >= 8 &&
                                          StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds >= 4 &&
                                          StyxWoW.Me.CurrentTarget.Level < 85),
                        Spell.Cast("Ferocious Bite",
                                   ret => (!StyxWoW.Me.ActiveAuras.ContainsKey("Berserk") ||
                                           CurrentEnergy < 25) &&
                                          StyxWoW.Me.ComboPoints == 5 &&
                                          StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds >= 12 &&
                                          StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds >= 10 &&
                                          StyxWoW.Me.CurrentTarget.Level >= 85),
                        //#20 
                        new Decorator(
                            ret => (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") &&
                                    HasTeir13Bonus2 &&
                                    !StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") &&
                                    CurrentEnergy <= (EnergyRegen)
                                   ),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        //#21
                        new Decorator(
                            ret => (StyxWoW.Me.ActiveAuras.ContainsKey("Stampede") &&
                                    !StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") &&
                                    StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") &&
                                    CurrentEnergy <= (EnergyRegen)
                                   ),
                            new Sequence(
                                new Action(ret => SpellManager.Cast(WoWSpell.FromId(81170), StyxWoW.Me.CurrentTarget)
                                    )
                                )
                            ),
                        //Ignore 4x T11 Bonus
                        //#22
                        Spell.Cast("Shred",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseShred &&
                                   StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) &&
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") ||
                                    StyxWoW.Me.ActiveAuras.ContainsKey("Berserk")
                                   )
                            ),
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   (!StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) ||
                                    !SingularSettings.Instance.Druid.UseShred) &&
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Tiger's Fury") ||
                                    StyxWoW.Me.ActiveAuras.ContainsKey("Berserk")
                                   )
                            ),
                        //#23
                        Spell.Cast("Shred",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseShred &&
                                   StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) &&
                                   (StyxWoW.Me.ComboPoints < 5 &&
                                    StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds <= 3)
                                   || SingularSettings.Instance.Druid.UseShred &&
                                   (StyxWoW.Me.ComboPoints == 0 &&
                                    StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds <= 2)
                            ),
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   (!StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) ||
                                    !SingularSettings.Instance.Druid.UseShred) &&
                                   (StyxWoW.Me.ComboPoints < 5 &&
                                    StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).
                                        TotalSeconds <= 3)
                                   ||
                                   (StyxWoW.Me.ComboPoints == 0 &&
                                    StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds <=
                                    2)
                            ),
                        //#24
                        Spell.Cast("Shred",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseShred &&
                                   StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) &&
                                   SpellManager.HasSpell("Tiger's Fury") &&
                                   SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().TotalSeconds <= 3),
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   (!StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) ||
                                    !SingularSettings.Instance.Druid.UseShred) &&
                                   SpellManager.HasSpell("Tiger's Fury") &&
                                   SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft().
                                       TotalSeconds <= 3),
                        //#25
                        Spell.Cast("Shred",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseShred &&
                                   StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) &&
                                   StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth <= 8.5),
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   (!StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) ||
                                    !SingularSettings.Instance.Druid.UseShred) &&
                                   StyxWoW.Me.CurrentTarget.CurrentHealth/Finisherhealth <= 8.5),
                        //#26
                        Spell.Cast("Shred",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseShred &&
                                   StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) &&
                                   CurrentEnergy >= (EnergyRegen)
                            ),
                        Spell.Cast("Mangle (Cat)",
                                   ret =>
                                   (!StyxWoW.Me.IsSafelyBehind(StyxWoW.Me.CurrentTarget) ||
                                    !SingularSettings.Instance.Druid.UseShred) &&
                                   CurrentEnergy >= (EnergyRegen)
                            )
                        )
                    ),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Bear

        public static Composite CreateBearTankCombat()
        {
            return new PrioritySelector(
                CreateBearTankManualForms(),
                CreateBearTankActualCombat());
        }

        private static Composite CreateBearTankManualForms()
        {
            return new PrioritySelector(
                Spell.WaitForCast(),
                // If we're in caster form, and not casting anything (tranq), then fucking switch to bear.
                new Decorator(
                    ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Normal,
                    Spell.BuffSelf("Bear Form")),
                new Decorator(
                    ret => !Settings.ManualForms && StyxWoW.Me.Shapeshift != ShapeshiftForm.Bear,
                    Spell.BuffSelf("Bear Form")),
                // If the user has manual forms enabled. Automatically switch to cat combat if they switch forms.
                new Decorator(
                    ret => Settings.ManualForms && StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat,
                    new PrioritySelector(
                        CreateFeralCatActualCombat(),
                        new ActionAlwaysSucceed()))
                );
        }

        public static Composite CreateBearTankActualCombat()
        {
            TankManager.NeedTankTargeting = true;
            return new PrioritySelector(
                ctx => TankManager.Instance.FirstUnit ?? StyxWoW.Me.CurrentTarget,
                //((WoWUnit)ret)

                Spell.WaitForCast(),
                // If we're in caster form, and not casting anything (tranq), then fucking switch to bear.
                new Decorator(
                    ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Normal,
                    Spell.BuffSelf("Bear Form")),
                // We don't want to get stuck in Aquatic form. We won't be able to cast shit
                new Decorator(
                    ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Aqua,
                    Spell.BuffSelf("Bear Form")),
                new Decorator(
                    ret => !Settings.ManualForms && StyxWoW.Me.Shapeshift != ShapeshiftForm.Bear,
                    Spell.BuffSelf("Bear Form")),
                // If the user has manual forms enabled. Automatically switch to cat combat if they switch forms.
                new Decorator(
                    ret => Settings.ManualForms && StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat,
                    new PrioritySelector(
                        CreateFeralCatActualCombat(),
                        new ActionAlwaysSucceed())),
                Safers.EnsureTarget(),
                Movement.CreateFaceTargetBehavior(),
                new Decorator(
                    ret =>
                    (SingularSettings.Instance.Druid.Shift && StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Rooted) &&
                     StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear),
                    new Sequence(
                        new Action(ret => Lua.DoString("RunMacroText(\"/Cast !Bear Form\")")))),
                new Decorator(
                    ret =>
                    (SingularSettings.Instance.Druid.ShiftSlow &&
                     StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Snared) &&
                     !StyxWoW.Me.ActiveAuras.ContainsKey("Crippling Poison") &&
                     StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear),
                    new Sequence(
                        new Action(ret => Lua.DoString("RunMacroText(\"/Cast !Bear Form\")")))),
                Spell.Cast("Faerie Fire (Feral)", ret => SingularSettings.Instance.Druid.PullFff),
                new Decorator(
                    ret =>
                    Settings.UseFeralChargeBear && ((WoWUnit) ret).Distance > 8f && ((WoWUnit) ret).Distance < 25f,
                    Spell.Cast("Feral Charge (Bear)", ret => ((WoWUnit) ret))),
                // Defensive CDs are hard to 'roll' from this type of logic, so we'll simply use them more as 'oh shit' buttons, than anything.
                // Barkskin should be kept on CD, regardless of what we're tanking
                Spell.BuffSelf("Barkskin", ret => StyxWoW.Me.HealthPercent < Settings.FeralBarkskin),
                // Since Enrage no longer makes us take additional damage, just keep it on CD. Its a rage boost, and coupled with King of the Jungle, a DPS boost for more threat.
                Spell.BuffSelf("Enrage"),
                // Only pop SI if we're taking a bunch of damage.
                Spell.BuffSelf("Survival Instincts", ret => StyxWoW.Me.HealthPercent < Settings.SurvivalInstinctsHealth),
                // We only want to pop FR < 30%. Users should not be able to change this value, as FR automatically pushes us to 30% hp.
                Spell.BuffSelf("Frenzied Regeneration",
                               ret => StyxWoW.Me.HealthPercent < Settings.FrenziedRegenerationHealth),
                // Make sure we deal with interrupts...
                //Spell.Cast(80964 /*"Skull Bash (Bear)"*/, ret => (WoWUnit)ret, ret => ((WoWUnit)ret).IsCasting
                new Decorator(ret => !SingularSettings.Instance.Druid.DisableInterrupts,
                              Helpers.Common.CreateInterruptSpellCast(ret => ((WoWUnit) ret))),
                // If we have 3+ units not targeting us, and are within 10yds, then pop our AOE taunt. (These are ones we have 'no' threat on, or don't hold solid threat on)
                Spell.Cast(
                    "Challenging Roar", ret => TankManager.Instance.NeedToTaunt.First(),
                    ret =>
                    SingularSettings.Instance.EnableTaunting &&
                    TankManager.Instance.NeedToTaunt.Count(u => u.Distance <= 10) >= 3),
                // If there's a unit that needs taunting, do it.
                Spell.Cast(
                    "Growl", ret => TankManager.Instance.NeedToTaunt.First(),
                    ret =>
                    SingularSettings.Instance.EnableTaunting &&
                    TankManager.Instance.NeedToTaunt.FirstOrDefault() != null),
                new Decorator(
                    ret =>
                    EnemyUnits.Count >= 2 &&
                    EnemyUnits.Count <= 3 &&
                    StyxWoW.Me.ActiveAuras.ContainsKey("Berserk"),
                    new PrioritySelector(
                        Spell.Cast("Maul",
                                   ret =>
                                   SpellManager.HasSpell("Maul") && !SpellManager.Spells["Maul"].Cooldown &&
                                   StyxWoW.Me.CurrentRage > 45),
                        Spell.Cast("Demoralizing Roar",
                                   ret =>
                                   SingularSettings.Instance.Druid.DebuffRoar &&
                                   Unit.NearbyUnfriendlyUnits.Any(u => u.Distance <= 10 && !u.HasDemoralizing()
                                       )
                            ),
                        Spell.Cast("Mangle (Bear)"),
                        Movement.CreateMoveToMeleeBehavior(true)
                        )
                    ),
                new Decorator(
                    ret =>
                    EnemyUnits.Count >= 2,
                    new PrioritySelector(
                        Spell.Cast("Berserk",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseBerserkBear && SpellManager.HasSpell("Berserk") &&
                                   !SpellManager.Spells["Berserk"].Cooldown && !SpellManager.GlobalCooldown &&
                                   (
                                       ((WoWUnit) ret).HasSunders() ||
                                       StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Faerie Fire", true).TotalSeconds >
                                       (TalentManager.HasGlyph("Berserk") ? 25 : 15)
                                   ) &&
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                    StyxWoW.Me.GetAuraTimeLeft("Pulverize", true).TotalSeconds > 8)
                            ),
                        Spell.Cast("Maul",
                                   ret =>
                                   SpellManager.HasSpell("Maul") && !SpellManager.Spells["Maul"].Cooldown &&
                                   StyxWoW.Me.CurrentRage > 45),
                        Spell.Cast("Thrash",
                                   ret =>
                                   SpellManager.HasSpell("Thrash") && !SpellManager.Spells["Thrash"].Cooldown &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 25)
                            ),
                        Spell.Cast("Swipe (Bear)",
                                   ret =>
                                   SpellManager.HasSpell("Swipe (Bear)") &&
                                   !SpellManager.Spells["Swipe (Bear)"].Cooldown &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 25)
                            ),
                        Spell.Cast("Demoralizing Roar",
                                   ret =>
                                   SingularSettings.Instance.Druid.DebuffRoar &&
                                   Unit.NearbyUnfriendlyUnits.Any(u => u.Distance <= 10 && !u.HasDemoralizing()
                                       )
                            ),
                        new Decorator(
                            ret =>
                            (SpellManager.HasSpell("Thrash") &&
                             SpellManager.Spells["Thrash"].CooldownTimeLeft.TotalSeconds > 1
                             && SpellManager.HasSpell("Swipe (Bear)") &&
                             SpellManager.Spells["Swipe (Bear)"].CooldownTimeLeft.TotalSeconds > 1)
                            ||
                            (!SpellManager.HasSpell("Thrash") && SpellManager.HasSpell("Swipe (Bear)") &&
                             SpellManager.Spells["Swipe (Bear)"].CooldownTimeLeft.TotalSeconds > 1)
                            ||
                            (!SpellManager.HasSpell("Swipe (Bear)") && SpellManager.HasSpell("Thrash") &&
                             SpellManager.Spells["Thrash"].CooldownTimeLeft.TotalSeconds > 1)
                            || (!SpellManager.HasSpell("Swipe (Bear)") && !SpellManager.HasSpell("Thrash")),
                            new PrioritySelector(
                                Spell.Cast("Mangle (Bear)",
                                           ret =>
                                           SpellManager.HasSpell("Mangle (Bear)") &&
                                           !SpellManager.Spells["Mangle (Bear)"].Cooldown &&
                                           StyxWoW.Me.CurrentRage >=
                                           (
                                               (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ||
                                                StyxWoW.Me.ActiveAuras.ContainsKey("Berserk")
                                               )
                                                   ? 0
                                                   : 15) && !StyxWoW.Me.CurrentTarget.HasAura("Infected Wounds")
                                    ),
                                Spell.Cast("Lacerate",
                                           ret =>
                                           HasTeir13Bonus && !StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                           !StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    ),
                                Spell.Cast("Pulverize",
                                           ret =>
                                           HasTeir13Bonus && !StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                           StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    ),
                                Spell.Cast("Faerie Fire (Feral)",
                                           ret =>
                                           SpellManager.HasSpell("Faerie Fire (Feral)") &&
                                           !SpellManager.Spells["Faerie Fire (Feral)"].Cooldown &&
                                           (!StyxWoW.Me.CurrentTarget.HasSunders() ||
                                            (StyxWoW.Me.CurrentTarget.HasMyAura("Faerie Fire") &&
                                             StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Faerie Fire", true).TotalSeconds <=
                                             1.5)
                                           )
                                    ),
                                Spell.Cast("Mangle (Bear)",
                                           ret =>
                                           SpellManager.HasSpell("Mangle (Bear)") &&
                                           !SpellManager.Spells["Mangle (Bear)"].Cooldown &&
                                           StyxWoW.Me.CurrentRage >=
                                           ((StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ||
                                             StyxWoW.Me.ActiveAuras.ContainsKey("Berserk")
                                            )
                                                ? 0
                                                : 15)
                                    ),
                                Spell.Cast("Lacerate",
                                           ret =>
                                           SpellManager.HasSpell("Lacerate") && HasTeir13Bonus &&
                                           StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                           !StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                           StyxWoW.Me.GetAuraTimeLeft("Pulverize", true).TotalSeconds <
                                           (1.5 + 0.33)*(3 - StyxWoW.Me.CurrentTarget.Auras["Lacerate"].StackCount + 1) &&
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    ),
                                Spell.Cast("Lacerate",
                                           ret =>
                                           !StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate") &&
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    ),
                                Spell.Cast("Pulverize",
                                           ret =>
                                           StyxWoW.Me.GetAuraTimeLeft("Pulverize", true).TotalSeconds <= 3 &&
                                           StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    ),
                                Spell.Cast("Lacerate",
                                           ret =>
                                           SpellManager.HasSpell("Lacerate") &&
                                           StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate") &&
                                           StyxWoW.Me.CurrentTarget.Auras["Lacerate"].StackCount < 3 &&
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    ),
                                Spell.Cast("Faerie Fire (Feral)",
                                           ret =>
                                           SpellManager.HasSpell("Faerie Fire (Feral)") &&
                                           !SpellManager.Spells["Faerie Fire (Feral)"].Cooldown),
                                Spell.Cast("Lacerate",
                                           ret =>
                                           StyxWoW.Me.CurrentRage >=
                                           (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                                    )
                                )
                            ),
                        Movement.CreateMoveToMeleeBehavior(true)
                        )
                    ),
                new Decorator(
                    ret =>
                    EnemyUnits.Count == 1,
                    new PrioritySelector(
                        Spell.Cast("Berserk",
                                   ret =>
                                   SingularSettings.Instance.Druid.UseBerserkBear && SpellManager.HasSpell("Berserk") &&
                                   !SpellManager.Spells["Berserk"].Cooldown && !SpellManager.GlobalCooldown &&
                                   (
                                       ((WoWUnit) ret).HasSunders() ||
                                       StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Faerie Fire", true).TotalSeconds >
                                       (TalentManager.HasGlyph("Berserk") ? 25 : 15)
                                   ) &&
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                    StyxWoW.Me.GetAuraTimeLeft("Pulverize", true).TotalSeconds > 8)
                            ),
                        Spell.Cast("Maul",
                                   ret =>
                                   SpellManager.HasSpell("Maul") && !SpellManager.Spells["Maul"].Cooldown &&
                                   StyxWoW.Me.CurrentRage > 45),
                        Spell.Cast("Demoralizing Roar",
                                   ret =>
                                   SingularSettings.Instance.Druid.DebuffRoar &&
                                   Unit.NearbyUnfriendlyUnits.Any(u => u.Distance <= 10 && !u.HasDemoralizing()
                                       )
                            ),
                        Spell.Cast("Mangle (Bear)",
                                   ret =>
                                   SpellManager.HasSpell("Mangle (Bear)") &&
                                   !SpellManager.Spells["Mangle (Bear)"].Cooldown &&
                                   StyxWoW.Me.CurrentRage >=
                                   (
                                       (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ||
                                        StyxWoW.Me.ActiveAuras.ContainsKey("Berserk")
                                       )
                                           ? 0
                                           : 15) && !StyxWoW.Me.CurrentTarget.HasAura("Infected Wounds")
                            ),
                        Spell.Cast("Lacerate",
                                   ret =>
                                   HasTeir13Bonus && !StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                   !StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Spell.Cast("Pulverize",
                                   ret =>
                                   HasTeir13Bonus && !StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                   StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Spell.Cast("Faerie Fire (Feral)",
                                   ret =>
                                   SpellManager.HasSpell("Faerie Fire (Feral)") &&
                                   !SpellManager.Spells["Faerie Fire (Feral)"].Cooldown &&
                                   (!StyxWoW.Me.CurrentTarget.HasSunders() ||
                                    (StyxWoW.Me.CurrentTarget.HasMyAura("Faerie Fire") &&
                                     StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Faerie Fire", true).TotalSeconds <= 1.5)
                                   )
                            ),
                        Spell.Cast("Mangle (Bear)",
                                   ret =>
                                   SpellManager.HasSpell("Mangle (Bear)") &&
                                   !SpellManager.Spells["Mangle (Bear)"].Cooldown &&
                                   StyxWoW.Me.CurrentRage >=
                                   ((StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ||
                                     StyxWoW.Me.ActiveAuras.ContainsKey("Berserk")
                                    )
                                        ? 0
                                        : 15)
                            ),
                        Spell.Cast("Lacerate",
                                   ret =>
                                   SpellManager.HasSpell("Lacerate") && HasTeir13Bonus &&
                                   StyxWoW.Me.ActiveAuras.ContainsKey("Pulverize") &&
                                   !StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                   StyxWoW.Me.GetAuraTimeLeft("Pulverize", true).TotalSeconds <
                                   (1.5 + 0.33)*(3 - StyxWoW.Me.CurrentTarget.Auras["Lacerate"].StackCount + 1) &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Spell.Cast("Lacerate",
                                   ret =>
                                   !StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate") &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Spell.Cast("Thrash",
                                   ret =>
                                   SpellManager.HasSpell("Thrash") && !SpellManager.Spells["Thrash"].Cooldown
                                   /*&& !Unit.NearbyUnfriendlyUnits.Any(u => u.Distance < 8 && u.IsCrowdControlled())*/&&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 25)
                            ),
                        Spell.Cast("Pulverize",
                                   ret =>
                                   StyxWoW.Me.GetAuraTimeLeft("Pulverize", true).TotalSeconds <= 3 &&
                                   StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate", 3) &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Spell.Cast("Lacerate",
                                   ret =>
                                   SpellManager.HasSpell("Lacerate") && StyxWoW.Me.CurrentTarget.HasMyAura("Lacerate") &&
                                   StyxWoW.Me.CurrentTarget.Auras["Lacerate"].StackCount < 3 &&
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Spell.Cast("Faerie Fire (Feral)",
                                   ret =>
                                   SpellManager.HasSpell("Faerie Fire (Feral)") &&
                                   !SpellManager.Spells["Faerie Fire (Feral)"].Cooldown),
                        Spell.Cast("Lacerate",
                                   ret =>
                                   StyxWoW.Me.CurrentRage >=
                                   (StyxWoW.Me.ActiveAuras.ContainsKey("Clearcasting") ? 0 : 15)
                            ),
                        Movement.CreateMoveToMeleeBehavior(true)
                        )
                    )
                );
        }

        #endregion
    }
}