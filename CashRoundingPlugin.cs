using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.CashRounding.Configuration;
using Resto.Front.Api.CashRounding.Handlers;
using Resto.Front.Api.CashRounding.Services;
using Resto.Front.Api.CashRounding.UI;

namespace Resto.Front.Api.CashRounding
{
    [UsedImplicitly]
    [PluginLicenseModuleId(21150218)]
    public sealed class CashRoundingPlugin : IFrontPlugin
    {
        private readonly Stack<IDisposable> subscriptions = new Stack<IDisposable>();

        public CashRoundingPlugin()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            PluginContext.Log.Info($"Initializing CashRoundingPlugin v{version} (API V8)");

            var config = RoundingConfig.Load();
            var roundingService = new OrderRoundingService(config);
            var menuManager = new PluginsMenuManager(config, roundingService);
            var paymentButtonManager = new PaymentScreenButtonManager(config, roundingService);
            var notificationHandler = new PaymentRoundingHandler(config, roundingService, menuManager, paymentButtonManager);

            subscriptions.Push(menuManager);
            subscriptions.Push(paymentButtonManager);
            subscriptions.Push(notificationHandler);

            PluginContext.Log.Info("CashRoundingPlugin started");
        }

        public void Dispose()
        {
            while (subscriptions.Any())
            {
                var subscription = subscriptions.Pop();
                try
                {
                    subscription.Dispose();
                }
                catch (RemotingException)
                {
                    // nothing to do with the lost connection
                }
            }

            PluginContext.Log.Info("CashRoundingPlugin stopped");
        }
    }
}
