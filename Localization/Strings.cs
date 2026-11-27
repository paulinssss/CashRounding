using System;
using System.Globalization;
using System.Resources;

namespace Resto.Front.Api.CashRounding.Localization
{
    internal static class Strings
    {
        private static readonly ResourceManager ResourceManager = new ResourceManager(
            "Resto.Front.Api.CashRounding.Localization.Resources",
            typeof(Strings).Assembly);

        private static CultureInfo currentCultureOverride;

        public static void SetCulture(CultureInfo culture)
        {
            currentCultureOverride = culture;
        }

        private static CultureInfo ResolveCulture()
        {
            var culture = currentCultureOverride ?? CultureInfo.CurrentUICulture ?? CultureInfo.CurrentCulture;
            if (culture != null && culture.TwoLetterISOLanguageName.Equals("it", StringComparison.OrdinalIgnoreCase))
            {
                return culture.Name.Equals("it-IT", StringComparison.OrdinalIgnoreCase)
                    ? culture
                    : CultureInfo.GetCultureInfo("it-IT");
            }

            return culture ?? CultureInfo.InvariantCulture;
        }

        private static string Get(string key) => ResourceManager.GetString(key, ResolveCulture()) ?? key;

        // Plugin identity & Plugins Menu
        public static string PluginSender => Get(nameof(PluginSender));
        public static string MenuButtonCaption => Get(nameof(MenuButtonCaption));
        public static string MenuTitle => Get(nameof(MenuTitle));
        public static string MenuSelectDiscount => Get(nameof(MenuSelectDiscount));
        public static string MenuDisable => Get(nameof(MenuDisable));
        public static string MenuEnable => Get(nameof(MenuEnable));
        public static string MenuAutoApplyDisable => Get(nameof(MenuAutoApplyDisable));
        public static string MenuAutoApplyEnable => Get(nameof(MenuAutoApplyEnable));
        public static string MenuStatus => Get(nameof(MenuStatus));
        public static string CloseButton => Get(nameof(CloseButton));
        public static string OkButton => Get(nameof(OkButton));
        public static string YesButton => Get(nameof(YesButton));
        public static string NoButton => Get(nameof(NoButton));

        // Payment Screen button
        public static string PaymentButtonDefault => Get(nameof(PaymentButtonDefault));
        public static string PaymentButtonApplyFormat => Get(nameof(PaymentButtonApplyFormat));
        public static string PaymentButtonAppliedFormat => Get(nameof(PaymentButtonAppliedFormat));
        public static string PaymentButtonAlreadyRounded => Get(nameof(PaymentButtonAlreadyRounded));

        // Bill status confirmation
        public static string BillCancelConfirmTitle => Get(nameof(BillCancelConfirmTitle));
        public static string BillCancelConfirmMessage => Get(nameof(BillCancelConfirmMessage));

        // Discount chooser
        public static string ChooseDiscountTitle => Get(nameof(ChooseDiscountTitle));
        public static string NoFlexibleDiscountsTitle => Get(nameof(NoFlexibleDiscountsTitle));
        public static string NoFlexibleDiscountsMessage => Get(nameof(NoFlexibleDiscountsMessage));
        public static string DiscountSavedTitle => Get(nameof(DiscountSavedTitle));
        public static string DiscountSavedFormat => Get(nameof(DiscountSavedFormat));
        public static string RoundingEnabledMessage => Get(nameof(RoundingEnabledMessage));
        public static string RoundingDisabledMessage => Get(nameof(RoundingDisabledMessage));
        public static string AutoApplyEnabledMessage => Get(nameof(AutoApplyEnabledMessage));
        public static string AutoApplyDisabledMessage => Get(nameof(AutoApplyDisabledMessage));
        public static string ConfigSaveErrorFormat => Get(nameof(ConfigSaveErrorFormat));

        // Status popup
        public static string StatusTitle => Get(nameof(StatusTitle));
        public static string StatusActive => Get(nameof(StatusActive));
        public static string StatusInactive => Get(nameof(StatusInactive));
        public static string StatusNotConfigured => Get(nameof(StatusNotConfigured));
        public static string StatusValid => Get(nameof(StatusValid));
        public static string StatusInvalid => Get(nameof(StatusInvalid));
        public static string StatusBodyFormat => Get(nameof(StatusBodyFormat));

        // Runtime notifications
        public static string RoundingAppliedFormat => Get(nameof(RoundingAppliedFormat));
        public static string RoundingAppliedPaymentsResetFormat => Get(nameof(RoundingAppliedPaymentsResetFormat));
        public static string RoundingRemovedMessage => Get(nameof(RoundingRemovedMessage));
        public static string RoundingRemovedPaymentsResetMessage => Get(nameof(RoundingRemovedPaymentsResetMessage));
        public static string NoRoundingNeededMessage => Get(nameof(NoRoundingNeededMessage));
        public static string DiscountNotConfiguredWarning => Get(nameof(DiscountNotConfiguredWarning));
    }
}
