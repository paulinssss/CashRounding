using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting;
using Resto.Front.Api.CashRounding.Configuration;
using Resto.Front.Api.CashRounding.Localization;
using Resto.Front.Api.CashRounding.Services;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.CashRounding.UI
{
    internal sealed class PluginsMenuManager : IDisposable
    {
        private readonly RoundingConfig config;
        private readonly OrderRoundingService roundingService;
        private readonly object syncRoot = new object();
        private IDisposable buttonRegistration;

        public PluginsMenuManager(RoundingConfig config, OrderRoundingService roundingService)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.roundingService = roundingService ?? throw new ArgumentNullException(nameof(roundingService));

            RegisterButton();
        }

        public void RefreshButtonCaption()
        {
            lock (syncRoot)
            {
                DisposeButtonRegistration();
                RegisterButton();
            }
        }

        private void RegisterButton()
        {
            buttonRegistration = PluginContext.Operations.AddButtonToPluginsMenu(
                Strings.MenuButtonCaption,
                x => OnMenuButtonClicked(x.vm));
        }

        private void OnMenuButtonClicked(IViewManager vm)
        {
            if (vm == null)
                return;

            try
            {
                var options = new List<string>
                {
                    Strings.MenuSelectDiscount,
                    config.AutoApplyOnEnterPaymentScreen ? Strings.MenuAutoApplyDisable : Strings.MenuAutoApplyEnable,
                    config.Enabled ? Strings.MenuDisable : Strings.MenuEnable,
                    Strings.MenuStatus
                };

                var selectedIndex = vm.ShowChooserPopup(
                    Strings.MenuTitle,
                    options,
                    -1,
                    ButtonWidth.Wide,
                    Strings.CloseButton);

                switch (selectedIndex)
                {
                    case 0:
                        ShowDiscountChooser(vm);
                        break;
                    case 1:
                        ToggleAutoApply(vm);
                        break;
                    case 2:
                        ToggleEnabled(vm);
                        break;
                    case 3:
                        ShowStatus(vm);
                        break;
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: error in Plugins Menu handler: {ex}");
                vm.ShowErrorPopup(ex.Message, Strings.CloseButton);
            }
        }

        private void ShowDiscountChooser(IViewManager vm)
        {
            var flexibleDiscounts = roundingService.GetAvailableFlexibleDiscounts(PluginContext.Operations);
            if (flexibleDiscounts.Count == 0)
            {
                vm.ShowOkPopup(
                    Strings.NoFlexibleDiscountsTitle,
                    Strings.NoFlexibleDiscountsMessage,
                    Strings.OkButton);
                return;
            }

            var currentDiscount = roundingService.ResolveConfiguredDiscount(PluginContext.Operations);
            var currentIndex = currentDiscount != null
                ? flexibleDiscounts.ToList().FindIndex(d => d.Id == currentDiscount.Id)
                : -1;

            var names = flexibleDiscounts.Select(d => d.Name).ToList();
            var chosenIndex = vm.ShowChooserPopup(
                Strings.ChooseDiscountTitle,
                names,
                currentIndex,
                ButtonWidth.Wide,
                Strings.CloseButton);

            if (chosenIndex < 0 || chosenIndex >= flexibleDiscounts.Count)
                return;

            var selectedDiscount = flexibleDiscounts[chosenIndex];
            try
            {
                config.DiscountTypeId = selectedDiscount.Id;
                config.DiscountTypeName = selectedDiscount.Name;
                config.Save();

                vm.ShowOkPopup(
                    Strings.DiscountSavedTitle,
                    string.Format(CultureInfo.CurrentCulture, Strings.DiscountSavedFormat, selectedDiscount.Name),
                    Strings.OkButton);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: failed to save selected discount: {ex}");
                vm.ShowErrorPopup(
                    string.Format(CultureInfo.CurrentCulture, Strings.ConfigSaveErrorFormat, ex.Message),
                    Strings.CloseButton);
            }
        }

        private void ToggleAutoApply(IViewManager vm)
        {
            try
            {
                config.AutoApplyOnEnterPaymentScreen = !config.AutoApplyOnEnterPaymentScreen;
                config.Save();

                vm.ShowOkPopup(
                    Strings.DiscountSavedTitle,
                    config.AutoApplyOnEnterPaymentScreen ? Strings.AutoApplyEnabledMessage : Strings.AutoApplyDisabledMessage,
                    Strings.OkButton);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: failed to save auto-apply state: {ex}");
                vm.ShowErrorPopup(
                    string.Format(CultureInfo.CurrentCulture, Strings.ConfigSaveErrorFormat, ex.Message),
                    Strings.CloseButton);
            }
        }

        private void ToggleEnabled(IViewManager vm)
        {
            try
            {
                config.Enabled = !config.Enabled;
                config.Save();

                vm.ShowOkPopup(
                    Strings.DiscountSavedTitle,
                    config.Enabled ? Strings.RoundingEnabledMessage : Strings.RoundingDisabledMessage,
                    Strings.OkButton);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: failed to save enabled state: {ex}");
                vm.ShowErrorPopup(
                    string.Format(CultureInfo.CurrentCulture, Strings.ConfigSaveErrorFormat, ex.Message),
                    Strings.CloseButton);
            }
        }

        private void ShowStatus(IViewManager vm)
        {
            var resolved = roundingService.ResolveConfiguredDiscount(PluginContext.Operations);
            var configuredName = !string.IsNullOrWhiteSpace(config.DiscountTypeName)
                ? config.DiscountTypeName
                : (config.DiscountTypeId.HasValue ? config.DiscountTypeId.Value.ToString() : Strings.StatusNotConfigured);

            var body = string.Format(
                CultureInfo.CurrentCulture,
                Strings.StatusBodyFormat,
                config.Enabled ? Strings.StatusActive : Strings.StatusInactive,
                config.AutoApplyOnEnterPaymentScreen ? Strings.StatusActive : Strings.StatusInactive,
                configuredName,
                resolved != null ? Strings.StatusValid : Strings.StatusInvalid,
                config.RoundingStep.ToString("0.00", CultureInfo.CurrentCulture));

            vm.ShowOkPopup(Strings.StatusTitle, body, Strings.OkButton);
        }

        private void DisposeButtonRegistration()
        {
            if (buttonRegistration == null)
                return;

            try
            {
                buttonRegistration.Dispose();
            }
            catch (RemotingException)
            {
                // nothing to do with lost connection
            }
            finally
            {
                buttonRegistration = null;
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                DisposeButtonRegistration();
            }
        }
    }
}
