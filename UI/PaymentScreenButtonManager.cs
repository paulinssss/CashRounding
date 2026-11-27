using System;
using System.Globalization;
using System.Runtime.Remoting;
using Resto.Front.Api.CashRounding.Configuration;
using Resto.Front.Api.CashRounding.Localization;
using Resto.Front.Api.CashRounding.Services;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.CashRounding.UI
{
    internal sealed class PaymentScreenButtonManager : IDisposable
    {
        // Simple coin / discount icon geometry (WPF Path syntax)
        private const string RoundingIconGeometry =
            "M12,2A10,10 0 1,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 1,1 4,12A8,8 0 0,1 12,4M11,7V9H9V11H11V13H9V15H11V17H13V15H15V13H13V11H15V9H13V7H11Z";

        private readonly RoundingConfig config;
        private readonly OrderRoundingService roundingService;
        private readonly object syncRoot = new object();
        private Guid buttonId;
        private IDisposable buttonRegistration;

        public PaymentScreenButtonManager(RoundingConfig config, OrderRoundingService roundingService)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.roundingService = roundingService ?? throw new ArgumentNullException(nameof(roundingService));

            RegisterButton();
        }

        public void SyncButtonForOrder(IOrder order, IOperationService os)
        {
            if (buttonId == Guid.Empty || order == null || os == null)
                return;

            try
            {
                if (!config.Enabled)
                {
                    os.UpdatePaymentScreenButtonState(buttonId, Strings.PaymentButtonDefault, false, false, RoundingIconGeometry);
                    return;
                }

                var state = roundingService.InspectOrderState(order, os);
                ApplyButtonState(state, os);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Warn($"CashRounding: failed to sync payment screen button state: {ex.Message}");
            }
        }

        public void RefreshLanguage()
        {
            lock (syncRoot)
            {
                if (buttonId == Guid.Empty)
                    return;

                try
                {
                    PluginContext.Operations.UpdatePaymentScreenButtonState(
                        buttonId,
                        Strings.PaymentButtonDefault,
                        null,
                        config.Enabled,
                        RoundingIconGeometry);
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Warn($"CashRounding: failed to refresh payment button caption: {ex.Message}");
                }
            }
        }

        private void RegisterButton()
        {
            var result = PluginContext.Operations.AddButtonToPaymentScreen(
                Strings.PaymentButtonDefault,
                false,
                config.Enabled,
                x => OnPaymentScreenButtonClicked(x.order, x.os, x.vm),
                RoundingIconGeometry);

            buttonId = result.buttonId;
            buttonRegistration = result.buttonRegistration;
        }

        private void OnPaymentScreenButtonClicked(IOrder order, IOperationService os, IViewManager vm)
        {
            if (order == null || os == null)
                return;

            try
            {
                var updatedState = roundingService.ToggleRounding(order, os, vm);
                ApplyButtonState(updatedState, os);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: error on payment screen button click: {ex}");
                vm?.ShowErrorPopup(ex.Message, Strings.CloseButton);
            }
        }

        private void ApplyButtonState(OrderRoundingState state, IOperationService os)
        {
            if (buttonId == Guid.Empty || state == null || os == null)
                return;

            if (!config.Enabled)
            {
                os.UpdatePaymentScreenButtonState(buttonId, Strings.PaymentButtonDefault, false, false, RoundingIconGeometry);
                return;
            }

            if (!state.IsConfigured)
            {
                os.UpdatePaymentScreenButtonState(buttonId, Strings.PaymentButtonDefault, false, true, RoundingIconGeometry);
                return;
            }

            if (state.IsApplied)
            {
                var caption = string.Format(CultureInfo.CurrentCulture, Strings.PaymentButtonAppliedFormat, state.AppliedSum);
                os.UpdatePaymentScreenButtonState(buttonId, caption, true, true, RoundingIconGeometry);
                return;
            }

            if (state.ExpectedDiscount > 0m)
            {
                var caption = string.Format(CultureInfo.CurrentCulture, Strings.PaymentButtonApplyFormat, state.ExpectedDiscount);
                os.UpdatePaymentScreenButtonState(buttonId, caption, false, true, RoundingIconGeometry);
            }
            else
            {
                os.UpdatePaymentScreenButtonState(buttonId, Strings.PaymentButtonAlreadyRounded, false, false, RoundingIconGeometry);
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
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
                    buttonId = Guid.Empty;
                }
            }
        }
    }
}
