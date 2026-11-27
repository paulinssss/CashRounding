using System;

namespace Resto.Front.Api.CashRounding.Services
{
    public static class RoundingCalculator
    {
        public const decimal DefaultStep = 0.05m;

        /// <summary>
        /// Calculates the discount amount required to round <paramref name="amount"/> down
        /// (in favor of the customer) to the nearest multiple of <paramref name="step"/>.
        /// Examples (step = 0.05):
        ///   0.17 -> 0.02 (final 0.15)
        ///   0.24 -> 0.04 (final 0.20)
        ///   1.01 -> 0.01 (final 1.00)
        ///   1.05 -> 0.00 (final 1.05)
        /// </summary>
        public static decimal CalculateDiscount(decimal amount, decimal step = DefaultStep)
        {
            if (amount <= 0m || step <= 0m)
                return 0m;

            var normalizedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            var targetTotal = Math.Floor(normalizedAmount / step) * step;
            var discount = Math.Round(normalizedAmount - targetTotal, 2, MidpointRounding.AwayFromZero);

            return discount > 0m ? discount : 0m;
        }

        /// <summary>
        /// Calculates the final rounded total after applying the downward rounding discount.
        /// </summary>
        public static decimal CalculateRoundedTotal(decimal amount, decimal step = DefaultStep)
        {
            if (amount <= 0m)
                return amount;

            var normalizedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            return normalizedAmount - CalculateDiscount(normalizedAmount, step);
        }
    }
}
