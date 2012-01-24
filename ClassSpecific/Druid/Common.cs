using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using TreeSharp;

namespace Singular.ClassSpecific.Druid
{
    public class Common
    {
        public static ShapeshiftForm WantedDruidForm { get; set; }

        [Class(WoWClass.Druid)]
        [Behavior(BehaviorType.OutOfCombat)]
        [Spec(TalentSpec.BalanceDruid)]
        [Spec(TalentSpec.FeralDruid)]
        [Spec(TalentSpec.FeralTankDruid)]
        [Spec(TalentSpec.RestorationDruid)]
        [Spec(TalentSpec.Lowbie)]
        [Context(WoWContext.All)]
        public static Composite CreateDruidOutOfCombatBuffs()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.DisableBuffs && !StyxWoW.Me.HasAura("Prowl")
                           && !StyxWoW.Me.HasAura("Shadowmeld") && StyxWoW.Me.IsAlive,
                    Spell.BuffSelf("Mark of the Wild")),
                Spell.BuffSelf("Cat Form",
                               ret =>
                               !StyxWoW.Me.Mounted && StyxWoW.Me.Shapeshift != ShapeshiftForm.Cat &&
                               SingularSettings.Instance.Druid.Stealth && !SingularSettings.Instance.Druid.ForceBear),
                Spell.BuffSelf("Prowl",
                               ret =>
                               SingularSettings.Instance.Druid.Stealth &&
                               StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat),
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
                     StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat),
                    new Sequence(
                        new Action(ret => Lua.DoString("RunMacroText(\"/Cast !Cat Form\")")))),
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
                     StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear),
                    new Sequence(
                        new Action(ret => Lua.DoString("RunMacroText(\"/Cast !Bear Form\")"))))
                );
        }

        [Class(WoWClass.Druid)]
        [Behavior(BehaviorType.PreCombatBuffs)]
        [Spec(TalentSpec.FeralDruid)]
        [Spec(TalentSpec.Lowbie)]
        [Context(WoWContext.All)]
        public static Composite CreateFeralDruidBuffComposite()
        {
            return new PrioritySelector(
                /*Basic healing while not in raid | dungeon | battleground | arena */
               Spell.BuffSelf("Prowl",
                               ret =>
                               SingularSettings.Instance.Druid.ProwlPounce &&
                               StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat
                               && !StyxWoW.Me.ActiveAuras.ContainsKey("Preparation")),
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.DisableBuffs && !StyxWoW.Me.HasAura("Prowl")
                           && !StyxWoW.Me.HasAura("Shadowmeld") && StyxWoW.Me.IsAlive &&
                           !StyxWoW.Me.IsInInstance && !StyxWoW.Me.IsInRaid
                           && !StyxWoW.Me.IsPvPFlagged,
                    new PrioritySelector(
                        Spell.Cast("Healing Touch",
                                   ctx =>
                                   StyxWoW.Me.IsAlive && StyxWoW.Me.ActiveAuras.ContainsKey("Predator's Swiftness") &&
                                   StyxWoW.Me.HealthPercent < 60),
                        Spell.Cast("Rejuvenation", ctx => StyxWoW.Me.IsAlive && StyxWoW.Me.HealthPercent < 50),
                        Spell.Cast("Regrowth", ctx => StyxWoW.Me.IsAlive && StyxWoW.Me.HealthPercent < 40))),
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.DisableBuffs && !StyxWoW.Me.HasAura("Prowl")
                           && !StyxWoW.Me.HasAura("Shadowmeld") && StyxWoW.Me.IsAlive,
                    new PrioritySelector(
                        Spell.Buff(
                            "Mark of the Wild",
                            ret => StyxWoW.Me,
                            ret =>
                            (Unit.NearbyFriendlyPlayers.Any(
                                unit =>
                                !unit.Dead && !unit.IsGhost && unit.IsInMyPartyOrRaid &&
                                !unit.HasAnyAura("Mark of the Wild", "Embrace of the Shale Spider", "Blessing of Kings")))
                            ||
                            !StyxWoW.Me.HasAnyAura("Mark of the Wild", "Embrace of the Shale Spider",
                                                   "Blessing of Kings"))))
                );
        }

        [Class(WoWClass.Druid)]
        [Behavior(BehaviorType.PreCombatBuffs)]
        [Spec(TalentSpec.BalanceDruid)]
        [Spec(TalentSpec.FeralTankDruid)]
        [Spec(TalentSpec.RestorationDruid)]
        [Context(WoWContext.All)]
        public static Composite CreateDruidBuffComposite()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.DisableBuffs && !StyxWoW.Me.HasAura("Prowl")
                           && !StyxWoW.Me.HasAura("Shadowmeld") && StyxWoW.Me.IsAlive,
                    new PrioritySelector(
                        Spell.Buff(
                            "Mark of the Wild",
                            ret => StyxWoW.Me,
                            ret =>
                            (Unit.NearbyFriendlyPlayers.Any(
                                unit =>
                                !unit.Dead && !unit.IsGhost && unit.IsInMyPartyOrRaid &&
                                !unit.HasAnyAura("Mark of the Wild", "Embrace of the Shale Spider", "Blessing of Kings")))
                            ||
                            !StyxWoW.Me.HasAnyAura("Mark of the Wild", "Embrace of the Shale Spider",
                                                   "Blessing of Kings"))))
                );
        }
    }
}