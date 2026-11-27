using System;
using System.Globalization;

namespace Resto.Front.Api.CashRounding.Localization
{
    internal static class Strings
    {
        private static CultureInfo currentCultureOverride;

        public static void SetCulture(CultureInfo culture)
        {
            currentCultureOverride = culture;
        }

        private static bool IsItalian
        {
            get
            {
                var culture = currentCultureOverride ?? CultureInfo.CurrentUICulture ?? CultureInfo.CurrentCulture;
                if (culture != null && culture.TwoLetterISOLanguageName.Equals("it", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (currentCultureOverride == null)
                {
                    var current = CultureInfo.CurrentCulture;
                    return current != null && current.TwoLetterISOLanguageName.Equals("it", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
        }

        private static string T(string it, string en) => IsItalian ? it : en;

        // Plugin identity & Plugins Menu
        public static string PluginSender => T("Arrotondamento Contanti", "Cash Rounding");
        public static string MenuButtonCaption => T("Arrotondamento Contanti", "Cash Rounding");
        public static string MenuTitle => T("Arrotondamento Contanti - Impostazioni", "Cash Rounding - Settings");
        public static string MenuSelectDiscount => T("Seleziona sconto arrotondamento", "Select rounding discount");
        public static string MenuDisable => T("Disattiva plugin", "Disable plugin");
        public static string MenuEnable => T("Attiva plugin", "Enable plugin");
        public static string MenuAutoApplyDisable => T("Disattiva applicazione automatica in cassa", "Disable auto-apply on payment screen");
        public static string MenuAutoApplyEnable => T("Attiva applicazione automatica in cassa", "Enable auto-apply on payment screen");
        public static string MenuStatus => T("Mostra stato configurazione", "Show configuration status");
        public static string CloseButton => T("Chiudi", "Close");
        public static string OkButton => T("OK", "OK");
        public static string YesButton => T("Sì", "Yes");
        public static string NoButton => T("No", "No");

        // Payment Screen button
        public static string PaymentButtonDefault => T("Arrotonda", "Round");
        public static string PaymentButtonApplyFormat => T("Arrotonda (-{0:0.00})", "Round (-{0:0.00})");
        public static string PaymentButtonAppliedFormat => T("Arrotondato (-{0:0.00})", "Rounded (-{0:0.00})");
        public static string PaymentButtonAlreadyRounded => T("Già arrotondato", "Already rounded");

        // Bill status confirmation
        public static string BillCancelConfirmTitle => T("Preconto già stampato", "Bill already printed");
        public static string BillCancelConfirmMessage => T(
            "L'ordine ha un preconto stampato. Vuoi annullare il preconto per modificare l'arrotondamento?",
            "This order has a printed bill. Do you want to cancel the bill to update rounding?");

        // Discount chooser
        public static string ChooseDiscountTitle => T(
            "Seleziona lo sconto libero per l'arrotondamento",
            "Select the flexible-sum discount for rounding");

        public static string NoFlexibleDiscountsTitle => T("Nessuno sconto libero trovato", "No flexible discount found");
        public static string NoFlexibleDiscountsMessage => T(
            "Non è stato trovato alcuno sconto attivo con importo libero (Flexible Sum) in Syrve BackOffice.\n\nCrea uno sconto a importo libero su Syrve Office e riprova.",
            "No active flexible-sum discount was found in Syrve BackOffice.\n\nPlease create a flexible-sum discount in Syrve Office and try again.");

        public static string DiscountSavedTitle => T("Configurazione salvata", "Configuration saved");
        public static string DiscountSavedFormat => T(
            "Sconto arrotondamento impostato su:\n{0}",
            "Rounding discount set to:\n{0}");

        public static string RoundingEnabledMessage => T(
            "Plugin Arrotondamento Contanti ATTIVATO.",
            "Cash Rounding plugin ENABLED.");

        public static string RoundingDisabledMessage => T(
            "Plugin Arrotondamento Contanti DISATTIVATO.",
            "Cash Rounding plugin DISABLED.");

        public static string AutoApplyEnabledMessage => T(
            "Applicazione automatica all'ingresso in pagamento ATTIVATA.",
            "Auto-apply on entering payment screen ENABLED.");

        public static string AutoApplyDisabledMessage => T(
            "Applicazione automatica all'ingresso in pagamento DISATTIVATA (usa il tasto sulla schermata di pagamento).",
            "Auto-apply on entering payment screen DISABLED (use the button on the payment screen).");

        public static string ConfigSaveErrorFormat => T(
            "Impossibile salvare la configurazione:\n{0}",
            "Failed to save configuration:\n{0}");

        // Status popup
        public static string StatusTitle => T("Stato Arrotondamento Contanti", "Cash Rounding Status");
        public static string StatusActive => T("Attivo", "Enabled");
        public static string StatusInactive => T("Disattivo", "Disabled");
        public static string StatusNotConfigured => T("Non configurato", "Not configured");
        public static string StatusValid => T("Valido (importo libero)", "Valid (flexible sum)");
        public static string StatusInvalid => T("Non valido o non trovato", "Invalid or not found");

        public static string StatusBodyFormat => T(
            "Stato: {0}\nApplicazione automatica in cassa: {1}\nSconto configurato: {2}\nVerifica sconto: {3}\nPasso arrotondamento: {4}",
            "Status: {0}\nAuto-apply on payment screen: {1}\nConfigured discount: {2}\nDiscount check: {3}\nRounding step: {4}");

        // Runtime notifications
        public static string RoundingAppliedFormat => T(
            "Arrotondamento applicato: -{0:0.00} (nuovo totale: {1:0.00})",
            "Rounding applied: -{0:0.00} (new total: {1:0.00})");

        public static string RoundingAppliedPaymentsResetFormat => T(
            "Arrotondamento applicato: -{0:0.00} (nuovo totale: {1:0.00}). Pagamento azzerato: reinserire il pagamento.",
            "Rounding applied: -{0:0.00} (new total: {1:0.00}). Payment cleared: please re-enter the payment.");

        public static string RoundingRemovedMessage => T(
            "Arrotondamento rimosso.",
            "Rounding removed.");

        public static string RoundingRemovedPaymentsResetMessage => T(
            "Arrotondamento rimosso. Pagamento azzerato: reinserire il pagamento.",
            "Rounding removed. Payment cleared: please re-enter the payment.");

        public static string NoRoundingNeededMessage => T(
            "Il totale è già arrotondato al multiplo di 0,05.",
            "The total is already rounded to a 0.05 multiple.");

        public static string DiscountNotConfiguredWarning => T(
            "Arrotondamento non applicato: seleziona uno sconto libero nel menu Plugin.",
            "Rounding not applied: select a flexible-sum discount from the Plugins menu.");
    }
}
