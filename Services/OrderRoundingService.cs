using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Resto.Front.Api.CashRounding.Configuration;
using Resto.Front.Api.CashRounding.Localization;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Extensions;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.CashRounding.Services
{
    public sealed class OrderRoundingState
    {
        public bool IsConfigured { get; set; }
        public bool IsApplied { get; set; }
        public decimal AppliedSum { get; set; }
        public decimal ExpectedDiscount { get; set; }
    }

    public sealed class OrderRoundingService
    {
        private readonly RoundingConfig config;

        public OrderRoundingService(RoundingConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Returns only active, non-deleted discount types configured as flexible sum in Syrve BackOffice.
        /// </summary>
        public IReadOnlyList<IDiscountType> GetAvailableFlexibleDiscounts(IOperationService os)
        {
            if (os == null)
                throw new ArgumentNullException(nameof(os));

            return os.GetDiscountTypes()
                .Where(IsValidFlexibleDiscount)
                .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public IDiscountType ResolveConfiguredDiscount(IOperationService os)
        {
            if (os == null)
                throw new ArgumentNullException(nameof(os));

            if (config.DiscountTypeId.HasValue)
            {
                var byId = os.TryGetDiscountTypeById(config.DiscountTypeId.Value);
                if (IsValidFlexibleDiscount(byId))
                    return byId;
            }

            if (!string.IsNullOrWhiteSpace(config.DiscountTypeName))
            {
                var targetName = config.DiscountTypeName.Trim();
                var byName = os.GetDiscountTypes()
                    .FirstOrDefault(d => IsValidFlexibleDiscount(d)
                        && string.Equals(d.Name?.Trim(), targetName, StringComparison.OrdinalIgnoreCase));

                if (byName != null)
                    return byName;
            }

            return null;
        }

        public OrderRoundingState InspectOrderState(IOrder order, IOperationService os)
        {
            var state = new OrderRoundingState();
            if (order == null || os == null)
                return state;

            var discountType = ResolveConfiguredDiscount(os);
            if (discountType == null)
                return state;

            state.IsConfigured = true;

            var existingDiscount = order.Discounts
                .FirstOrDefault(d => d.DiscountType != null && d.DiscountType.Id == discountType.Id);

            var appliedSum = order.AppliedDiscounts
                .Where(a => a.Discount?.DiscountType != null && a.Discount.DiscountType.Id == discountType.Id)
                .Sum(a => a.DiscountSum);

            if (existingDiscount != null)
            {
                state.IsApplied = true;
                state.AppliedSum = appliedSum;
                state.ExpectedDiscount = RoundingCalculator.CalculateDiscount(order.ResultSum + appliedSum, config.RoundingStep);
            }
            else
            {
                state.IsApplied = false;
                state.AppliedSum = 0m;
                state.ExpectedDiscount = RoundingCalculator.CalculateDiscount(order.ResultSum, config.RoundingStep);
            }

            return state;
        }

        public OrderRoundingState ApplyRounding(IOrder order, IOperationService os, IViewManager vm, bool allowCancelBillPrompt)
        {
            if (!config.Enabled || order == null || os == null)
                return InspectOrderState(order, os);

            try
            {
                var discountType = ResolveConfiguredDiscount(os);
                if (discountType == null)
                {
                    PluginContext.Log.Warn(
                        $"CashRounding: configured flexible discount not found or invalid " +
                        $"(Id={config.DiscountTypeId}, Name='{config.DiscountTypeName}').");

                    os.AddWarningMessage(Strings.DiscountNotConfiguredWarning, Strings.PluginSender, TimeSpan.FromSeconds(5));
                    return new OrderRoundingState { IsConfigured = false };
                }

                var credentials = os.GetDefaultCredentials();
                order = EnsureOrderEditable(order, os, vm, credentials, allowCancelBillPrompt);
                if (order == null)
                    return new OrderRoundingState { IsConfigured = true };

                var existingDiscount = order.Discounts
                    .FirstOrDefault(d => d.DiscountType != null && d.DiscountType.Id == discountType.Id);

                var existingAppliedSum = order.AppliedDiscounts
                    .Where(a => a.Discount?.DiscountType != null && a.Discount.DiscountType.Id == discountType.Id)
                    .Sum(a => a.DiscountSum);

                var baseTotal = order.ResultSum + existingAppliedSum;
                var expectedDiscount = RoundingCalculator.CalculateDiscount(baseTotal, config.RoundingStep);

                // Already in the desired state
                if (existingDiscount == null && expectedDiscount <= 0m)
                {
                    if (allowCancelBillPrompt && config.ShowNotification)
                    {
                        os.AddNotificationMessage(Strings.NoRoundingNeededMessage, Strings.PluginSender, TimeSpan.FromSeconds(3));
                    }
                    return InspectOrderState(order, os);
                }

                if (existingDiscount != null && existingAppliedSum == expectedDiscount)
                {
                    return InspectOrderState(order, os);
                }

                // Remove outdated rounding discount before recalculating on exact Syrve ResultSum
                if (existingDiscount != null)
                {
                    os.DeleteDiscount(existingDiscount, order, credentials);
                    order = os.TryGetOrderById(order.Id);
                    if (order == null || order.Status != OrderStatus.New)
                        return new OrderRoundingState { IsConfigured = true };

                    expectedDiscount = RoundingCalculator.CalculateDiscount(order.ResultSum, config.RoundingStep);
                }

                if (expectedDiscount <= 0m)
                    return InspectOrderState(order, os);

                var oldTotal = order.ResultSum;
                os.AddFlexibleSumDiscount(expectedDiscount, discountType, order, credentials);

                var updatedOrder = os.TryGetOrderById(order.Id) ?? order;
                var resetCount = ResetUnprocessedPayments(updatedOrder, os, credentials);
                if (resetCount > 0)
                {
                    updatedOrder = os.TryGetOrderById(order.Id) ?? updatedOrder;
                }

                var newTotal = updatedOrder.ResultSum;

                PluginContext.Log.Info(
                    $"CashRounding: applied -{expectedDiscount.ToString("0.00", CultureInfo.InvariantCulture)} " +
                    $"('{discountType.Name}') to order #{order.Number} ({order.Id}). " +
                    $"OldTotal={oldTotal.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                    $"NewTotal={newTotal.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                    $"ResetPayments={resetCount}.");

                if (config.ShowNotification)
                {
                    var template = resetCount > 0
                        ? Strings.RoundingAppliedPaymentsResetFormat
                        : Strings.RoundingAppliedFormat;

                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        template,
                        expectedDiscount,
                        newTotal);

                    os.AddNotificationMessage(message, Strings.PluginSender, TimeSpan.FromSeconds(4));
                }

                return InspectOrderState(updatedOrder, os);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: error applying rounding to order {order?.Id}: {ex}");
                return InspectOrderState(order, os);
            }
        }

        public OrderRoundingState RemoveRounding(IOrder order, IOperationService os, IViewManager vm, bool allowCancelBillPrompt)
        {
            if (order == null || os == null)
                return new OrderRoundingState();

            try
            {
                var discountType = ResolveConfiguredDiscount(os);
                if (discountType == null)
                    return new OrderRoundingState { IsConfigured = false };

                var existingDiscount = order.Discounts
                    .FirstOrDefault(d => d.DiscountType != null && d.DiscountType.Id == discountType.Id);

                if (existingDiscount == null)
                    return InspectOrderState(order, os);

                var credentials = os.GetDefaultCredentials();
                order = EnsureOrderEditable(order, os, vm, credentials, allowCancelBillPrompt);
                if (order == null)
                    return InspectOrderState(os.TryGetOrderById(order?.Id ?? Guid.Empty), os);

                existingDiscount = order.Discounts
                    .FirstOrDefault(d => d.DiscountType != null && d.DiscountType.Id == discountType.Id);

                if (existingDiscount != null)
                {
                    os.DeleteDiscount(existingDiscount, order, credentials);

                    var afterDeleteOrder = os.TryGetOrderById(order.Id) ?? order;
                    var resetCount = ResetUnprocessedPayments(afterDeleteOrder, os, credentials);

                    PluginContext.Log.Info(
                        $"CashRounding: removed rounding discount from order #{order.Number} ({order.Id}). ResetPayments={resetCount}.");

                    if (config.ShowNotification)
                    {
                        var message = resetCount > 0
                            ? Strings.RoundingRemovedPaymentsResetMessage
                            : Strings.RoundingRemovedMessage;

                        os.AddNotificationMessage(message, Strings.PluginSender, TimeSpan.FromSeconds(3));
                    }
                }

                var updatedOrder = os.TryGetOrderById(order.Id) ?? order;
                return InspectOrderState(updatedOrder, os);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"CashRounding: error removing rounding from order {order?.Id}: {ex}");
                return InspectOrderState(order, os);
            }
        }

        public OrderRoundingState ToggleRounding(IOrder order, IOperationService os, IViewManager vm)
        {
            if (!config.Enabled || order == null || os == null)
                return InspectOrderState(order, os);

            var currentState = InspectOrderState(order, os);
            if (!currentState.IsConfigured)
            {
                os.AddWarningMessage(Strings.DiscountNotConfiguredWarning, Strings.PluginSender, TimeSpan.FromSeconds(5));
                return currentState;
            }

            return currentState.IsApplied
                ? RemoveRounding(order, os, vm, allowCancelBillPrompt: true)
                : ApplyRounding(order, os, vm, allowCancelBillPrompt: true);
        }

        private static IOrder EnsureOrderEditable(
            IOrder order,
            IOperationService os,
            IViewManager vm,
            Data.Security.ICredentials credentials,
            bool allowCancelBillPrompt)
        {
            if (order.Status == OrderStatus.New)
                return order;

            if (order.Status == OrderStatus.Bill && allowCancelBillPrompt && vm != null)
            {
                var confirm = vm.ShowYesNoPopup(
                    Strings.BillCancelConfirmTitle,
                    Strings.BillCancelConfirmMessage,
                    Strings.YesButton,
                    Strings.NoButton);

                if (!confirm)
                    return null;

                os.CancelBill(order, credentials);
                var reloaded = os.TryGetOrderById(order.Id);
                return reloaded != null && reloaded.Status == OrderStatus.New ? reloaded : null;
            }

            PluginContext.Log.Info(
                $"CashRounding: order #{order.Number} ({order.Id}) has status {order.Status}; skipping modification.");
            return null;
        }

        private static int ResetUnprocessedPayments(
            IOrder order,
            IOperationService os,
            Data.Security.ICredentials credentials)
        {
            if (order?.Payments == null || order.Payments.Count == 0)
                return 0;

            var unprocessedPayments = order.Payments
                .Where(p => p != null
                    && p.Status == PaymentStatus.New
                    && !p.IsPrepay
                    && !p.IsPreliminary
                    && !p.IsProcessedExternally
                    && !p.IsFiscalizedExternally)
                .ToList();

            if (unprocessedPayments.Count == 0)
                return 0;

            try
            {
                var editSession = os.CreateEditSession();
                foreach (var payment in unprocessedPayments)
                {
                    if (payment.IsExternal)
                        editSession.DeleteExternalPaymentItem(payment, order);
                    else
                        editSession.DeletePaymentItem(payment, order);
                }

                os.SubmitChanges(editSession, credentials);

                PluginContext.Log.Info(
                    $"CashRounding: reset {unprocessedPayments.Count} unprocessed payment item(s) on order #{order.Number} ({order.Id}).");

                return unprocessedPayments.Count;
            }
            catch (Exception ex)
            {
                PluginContext.Log.Warn(
                    $"CashRounding: failed to reset unprocessed payment items on order #{order.Number} ({order.Id}): {ex.Message}");
                return 0;
            }
        }

        private static bool IsValidFlexibleDiscount(IDiscountType discount)
        {
            return discount != null
                && discount.IsActive
                && !discount.Deleted
                && discount.DiscountByFlexibleSum;
        }
    }
}
