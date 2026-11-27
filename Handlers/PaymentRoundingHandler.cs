using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using Resto.Front.Api.CashRounding.Configuration;
using Resto.Front.Api.CashRounding.Localization;
using Resto.Front.Api.CashRounding.Services;
using Resto.Front.Api.CashRounding.UI;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.CashRounding.Handlers
{
    internal sealed class PaymentRoundingHandler : IDisposable
    {
        private readonly RoundingConfig config;
        private readonly OrderRoundingService roundingService;
        private readonly PluginsMenuManager menuManager;
        private readonly PaymentScreenButtonManager paymentButtonManager;
        private readonly Stack<IDisposable> subscriptions = new Stack<IDisposable>();

        public PaymentRoundingHandler(
            RoundingConfig config,
            OrderRoundingService roundingService,
            PluginsMenuManager menuManager,
            PaymentScreenButtonManager paymentButtonManager)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.roundingService = roundingService ?? throw new ArgumentNullException(nameof(roundingService));
            this.menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
            this.paymentButtonManager = paymentButtonManager ?? throw new ArgumentNullException(nameof(paymentButtonManager));

            // NavigatingToPaymentScreen provides the scoped IOperationService (x.os) while the order
            // is still in OrderStatus.New (before any payment is added on screen).
            subscriptions.Push(
                PluginContext.Notifications.NavigatingToPaymentScreen.Subscribe(
                    x => OnNavigatingToPaymentScreen(x.order, x.os, x.vm)));

            subscriptions.Push(
                PluginContext.Notifications.CurrentCultureChanged.Subscribe(
                    x => OnCultureChanged(x.uiCulture ?? x.culture)));
        }

        private void OnNavigatingToPaymentScreen(IOrder order, IOperationService os, IViewManager vm)
        {
            if (order == null || os == null)
                return;

            try
            {
                if (config.Enabled && config.AutoApplyOnEnterPaymentScreen)
                {
                    roundingService.ApplyRounding(order, os, vm, allowCancelBillPrompt: false);
                    var updated = os.TryGetOrderById(order.Id) ?? order;
                    paymentButtonManager.SyncButtonForOrder(updated, os);
                }
                else
                {
                    paymentButtonManager.SyncButtonForOrder(order, os);
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: error on NavigatingToPaymentScreen for order {order.Id}: {ex}");
            }
        }

        private void OnCultureChanged(System.Globalization.CultureInfo newCulture)
        {
            try
            {
                Strings.SetCulture(newCulture);
                menuManager.RefreshButtonCaption();
                paymentButtonManager.RefreshLanguage();
            }
            catch (Exception ex)
            {
                PluginContext.Log.Warn($"CashRounding: failed to refresh culture: {ex.Message}");
            }
        }

        public void Dispose()
        {
            while (subscriptions.Count > 0)
            {
                var subscription = subscriptions.Pop();
                try
                {
                    subscription.Dispose();
                }
                catch (RemotingException)
                {
                    // nothing to do with lost connection
                }
            }
        }
    }
}
