﻿#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: raphus $
// $Date: 2012-01-02 12:17:17 +0100 (Mo, 02 Jan 2012) $
// $HeadURL: http://svn.apocdev.com/singular/trunk/Singular/Settings/DruidSettings.cs $
// $LastChangedBy: raphus $
// $LastChangedDate: 2012-01-02 12:17:17 +0100 (Mo, 02 Jan 2012) $
// $LastChangedRevision: 530 $
// $Revision: 530 $

#endregion

using System.ComponentModel;

using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    internal class DruidSettings : Styx.Helpers.Settings
    {
        public DruidSettings()
            : base(SingularSettings.SettingsPath + "_Druid.xml")
        {
        }

        [Setting]
        [DefaultValue(40)]
        [Category("Common")]
        [DisplayName("Innervate Mana")]
        [Description("Innervate will be used when your mana drops below this value")]
        public int InnervateMana { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Disable Interrupts")]
        [Description("Disables any automatic interrupt behaviour.")]
        public bool DisableInterrupts { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Disable Buffing")]
        [Description("Disables any automatic buff behaviour.")]
        public bool DisableBuffs { get; set; }

        #region Balance

        [Setting]
        [DefaultValue(false)]
        [Category("Balance")]
        [DisplayName("Starfall")]
        [Description("Use Starfall.")]
        public bool UseStarfall { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Balance")]
        [DisplayName("Diable Healing")]
        [Description("Disables Balance healing, is auto disabled in a party.")]
        public bool NoHealBalance { get; set; }

        [Setting]
        [DefaultValue(40)]
        [Category("Balance")]
        [DisplayName("Healing Touch")]
        [Description("Healing Touch will be used at this value.")]
        public int HealingTouchBalance { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Balance")]
        [DisplayName("Rejuvenation Health")]
        [Description("Rejuvenation will be used at this value")]
        public int RejuvenationBalance { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Balance")]
        [DisplayName("Regrowth Health")]
        [Description("Regrowth will be used at this value")]
        public int RegrowthBalance { get; set; }

        #endregion

        #region Resto

        [Setting]
        [DefaultValue(60)]
        [Category("Restoration")]
        [DisplayName("Tranquility Health")]
        [Description("Tranquility will be used at this value")]
        public int TranquilityHealth { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Restoration")]
        [DisplayName("Tranquility Count")]
        [Description(
            "Tranquility will be used when count of party members whom health is below Tranquility health mets this value "
            )]
        public int TranquilityCount { get; set; }

        [Setting]
        [DefaultValue(65)]
        [Category("Restoration")]
        [DisplayName("Swiftmend Health")]
        [Description("Swiftmend will be used at this value")]
        public int Swiftmend { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Restoration")]
        [DisplayName("Wild Growth Health")]
        [Description("Wild Growth will be used at this value")]
        public int WildGrowthHealth { get; set; }

        [Setting]
        [DefaultValue(2)]
        [Category("Restoration")]
        [DisplayName("Wild Growth Count")]
        [Description(
            "Wild Growth will be used when count of party members whom health is below Wild Growth health mets this value "
            )]
        public int WildGrowthCount { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Restoration")]
        [DisplayName("Regrowth Health")]
        [Description("Regrowth will be used at this value")]
        public int Regrowth { get; set; }

        [Setting]
        [DefaultValue(60)]
        [Category("Restoration")]
        [DisplayName("Healing Touch Health")]
        [Description("Healing Touch will be used at this value")]
        public int HealingTouch { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Restoration")]
        [DisplayName("Nourish Health")]
        [Description("Nourish will be used at this value")]
        public int Nourish { get; set; }

        [Setting]
        [DefaultValue(90)]
        [Category("Restoration")]
        [DisplayName("Rejuvenation Health")]
        [Description("Rejuvenation will be used at this value")]
        public int Rejuvenation { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Restoration")]
        [DisplayName("Tree of Life Health")]
        [Description("Tree of Life will be used at this value")]
        public int TreeOfLifeHealth { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Restoration")]
        [DisplayName("Tree of Life Count")]
        [Description(
            "Tree of Life will be used when count of party members whom health is below Tree of Life health mets this value "
            )]
        public int TreeOfLifeCount { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Restoration")]
        [DisplayName("Barkskin Health")]
        [Description("Barkskin will be used at this value")]
        public int Barkskin { get; set; }

        #endregion

        #region Feral

        [Setting]
        [DefaultValue(true)]
        [Category("Feral Tanking")]
        [DisplayName("Feral Charge")]
        [Description("Use Feral Charge to close gaps.")]
        public bool UseFeralChargeBear { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Feral Tanking")]
        [DisplayName("Automatic Berserk")]
        [Description("Use Beserk automatically.")]
        public bool UseBerserkBear { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Feral Cat")]
        [DisplayName("Use Shred")]
        [Description(
            "Turn on if you want to shred if possible instead of mangle. NOTE: Sometimes the isbehind() check returns wrong values."
            )]
        public bool UseShred { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Feral Cat")]
        [DisplayName("FeralHeal")]
        [Description("Use healing spells in cat spec")]
        public bool FeralHeal { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Feral Cat")]
        [DisplayName("Swipe Count")]
        [Description("Set how many adds to swipe on.")]
        public int SwipeCount { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Feral Cat")]
        [DisplayName("Feral Charge")]
        [Description("Use Feral Charge to close gaps.")]
        public bool UseFeralChargeCat { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Pull")]
        [DisplayName("Feral Charge Pull")]
        [Description("Use Feral Charge to Pull enemies.")]
        public bool UseFeralChargeCatPull { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Feral Cat")]
        [DisplayName("Automatic Berserk")]
        [Description("Use Beserk automatically.")]
        public bool UseBerserkCat { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Feral Cat")]
        [DisplayName("Automatic Savage Roar")]
        [Description("Use Savage Roar automatically.")]
        public bool UseSavageRoarCat { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Feral Cat")]
        [DisplayName("Switch Bear (Adds)")]
        [Description("Set how many adds are needed to switch to bear.")]
        public int BearCount { get; set; }

        [Setting]
        [DefaultValue(30)]
        [Category("Feral Cat")]
        [DisplayName("Switch Bear (Life)")]
        [Description("You will switch to bear at this value.")]
        public int BearLife { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Pull")]
        [DisplayName("Prowl and Pounce")]
        [Description("The cat tries to use Prowl as often as possible and will begin fighting with Pounce")]
        public bool ProwlPounce { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Feral")]
        [DisplayName("Manual Forms")]
        [Description(
            "Disables any automatic form switching. Manually switching to cat form will automatically start the Cat combat cycle, and vice versa for bear."
            )]
        public bool ManualForms { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Pull")]
        [DisplayName("Pull with Fearie Fire")]
        [Description(
            "Tries to pull eniemes with Fearie Fire (Feral)"
            )]
        public bool PullFff { get; set; }


        [Setting]
        [DefaultValue(false)]
        [Category("PvP")]
        [DisplayName("Shift if Rooted")]
        [Description(
            "Tries to powershift when rooted"
            )]
        public bool Shift { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("PvP")]
        [DisplayName("Shift if Slowed")]
        [Description(
            "Tries to powershift when Slowed"
            )]
        public bool ShiftSlow { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("PvP")]
        [DisplayName("Always stealth if not mounted")]
        [Description(
            "Tries to stealth if not mounted"
            )]
        public bool Stealth { get; set; }


        [Setting]
        [DefaultValue(100)]
        [Category("Feral")]
        [DisplayName("Barkskin Health")]
        [Description(
            "Barkskin will be used at this value. Set this to 100 to enable on cooldown usage. (Recommended: 100)")]
        public int FeralBarkskin { get; set; }

        [Setting]
        [DefaultValue(55)]
        [Category("Feral")]
        [DisplayName("Survival Instincts Health")]
        [Description("SI will be used at this value. Set this to 100 to enable on cooldown usage. (Recommended: 55)")]
        public int SurvivalInstinctsHealth { get; set; }

        [Setting]
        [DefaultValue(30)]
        [Category("Feral Tanking")]
        [DisplayName("Frenzied Regeneration Health")]
        [Description(
            "FR will be used at this value. Set this to 100 to enable on cooldown usage. (Recommended: 30 if glyphed. 15 if not.)"
            )]
        public int FrenziedRegenerationHealth { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Feral")]
        [DisplayName("Always fight in Bear Form")]
        [Description(
            "You will always fight in Bear Form if set to true."
            )]
        public bool ForceBear { get; set; }

        #endregion
    }
}