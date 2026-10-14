using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;

namespace ProsumentEneaItmTool.Model.NetBilling
{
    /// <summary>
    /// Value of the energy fed to the grid in one period, in net-billing.
    /// Values are in PLN and already include the deposit coefficient.
    /// </summary>
    public class NetBillingPeriod
    {
        public DateTime Time { get; init; }

        /// <summary>
        /// Energy fed to the grid after hourly balancing, kWh.
        /// </summary>
        public double FedKwh { get; set; }

        /// <summary>
        /// Fed energy in hours with a known RCE price, kWh.
        /// </summary>
        public double FedWithRceKwh { get; set; }

        public double ValueRce { get; set; }

        /// <summary>
        /// Fed energy in months with a known RCEm price, kWh.
        /// </summary>
        public double FedWithRcemKwh { get; set; }

        public double ValueRcem { get; set; }

        /// <summary>
        /// Fed energy valued by both methods, so they can be compared, kWh.
        /// </summary>
        public double FedWithBothKwh { get; set; }

        public double ValueRceOfBoth { get; set; }

        public double ValueRcemOfBoth { get; set; }

        /// <summary>
        /// Monthly price (negative as zero) of the month the period lies in, PLN/MWh.
        /// </summary>
        public double? RcemPricePlnMwh { get; set; }

        public double FedWithoutRceKwh => FedKwh - FedWithRceKwh;

        public double FedWithoutRcemKwh => FedKwh - FedWithRcemKwh;

        /// <summary>
        /// RCE prices (negative as zero) weighted by the fed energy, PLN/MWh, without the deposit coefficient.
        /// </summary>
        public double? WeightedRcePlnMwh { get; set; }

        /// <summary>
        /// Positive when the hourly prices give more than the monthly price.
        /// </summary>
        public double DifferenceRceMinusRcem => ValueRceOfBoth - ValueRcemOfBoth;
    }

    public class NetBillingResult
    {
        public List<NetBillingPeriod> Periods { get; init; } = [];

        public NetBillingPeriod Total { get; init; } = new();
    }

    /// <summary>
    /// Values the energy fed to the grid by hourly RCE and by monthly RCEm prices.
    /// The value goes to the prosumer deposit: price x energy x coefficient (from 2025-02-01), negative prices count as zero.
    /// </summary>
    public static class NetBillingCalculator
    {
        /// <summary>
        /// Coefficient increasing the deposit value (amendment of the RES act).
        /// </summary>
        public const double DefaultDepositCoefficient = 1.23;

        /// <summary>
        /// The coefficient applies to energy fed from this day; earlier energy is valued without it.
        /// </summary>
        public static readonly DateTime DepositCoefficientFrom = new(2025, 2, 1);

        public static NetBillingResult Calculate(
            IReadOnlyList<ImportFileRecord> records,
            IReadOnlyDictionary<DateTime, double> rceByHour,
            IReadOnlyDictionary<DateTime, double> rcemByMonth,
            Aggregation aggregation,
            double depositCoefficient,
            DateTime? depositCoefficientFrom = null)
        {
            var coefficientFrom = depositCoefficientFrom ?? DepositCoefficientFrom;

            var periods = new SortedDictionary<DateTime, NetBillingPeriod>();
            var weightedPriceSums = new Dictionary<DateTime, double>();
            var total = new NetBillingPeriod { Time = records.Count > 0 ? records.Min(x => x.Date) : default };
            var totalWeightedPriceSum = 0.0;

            foreach (var record in records)
            {
                var fedKwh = record.FedVolumeAfterBanancing;

                // Enea marks the hour 13:00-14:00 as 13:59, prices are stored by the start of the hour.
                var hour = new DateTime(record.Date.Year, record.Date.Month, record.Date.Day, record.Date.Hour, 0, 0);
                var month = new DateTime(hour.Year, hour.Month, 1);

                var coefficient = hour >= coefficientFrom ? depositCoefficient : 1.0;

                double? rce = rceByHour.TryGetValue(hour, out var rcePrice) ? Math.Max(0, rcePrice) : null;
                double? rcem = rcemByMonth.TryGetValue(month, out var rcemPrice) ? Math.Max(0, rcemPrice) : null;

                var periodTime = PowerCalculation.PeriodStart(record.Date, aggregation);
                if (!periods.TryGetValue(periodTime, out var period))
                {
                    period = new NetBillingPeriod { Time = periodTime };
                    periods.Add(periodTime, period);
                }

                foreach (var target in new[] { period, total })
                {
                    target.FedKwh += fedKwh;

                    if (rce is double r)
                    {
                        target.FedWithRceKwh += fedKwh;
                        target.ValueRce += Value(fedKwh, r, coefficient);
                    }

                    if (rcem is double m)
                    {
                        target.FedWithRcemKwh += fedKwh;
                        target.ValueRcem += Value(fedKwh, m, coefficient);
                    }

                    if (rce is double rb && rcem is double mb)
                    {
                        target.FedWithBothKwh += fedKwh;
                        target.ValueRceOfBoth += Value(fedKwh, rb, coefficient);
                        target.ValueRcemOfBoth += Value(fedKwh, mb, coefficient);
                    }
                }

                if (rce is double weighted)
                {
                    weightedPriceSums[periodTime] = weightedPriceSums.GetValueOrDefault(periodTime) + weighted * fedKwh;
                    totalWeightedPriceSum += weighted * fedKwh;
                }

                // Hours, days and months always lie within one month.
                period.RcemPricePlnMwh ??= rcem;
            }

            foreach (var period in periods.Values)
            {
                period.WeightedRcePlnMwh = period.FedWithRceKwh > 0
                    ? weightedPriceSums.GetValueOrDefault(period.Time) / period.FedWithRceKwh
                    : null;
            }

            total.WeightedRcePlnMwh = total.FedWithRceKwh > 0 ? totalWeightedPriceSum / total.FedWithRceKwh : null;

            return new NetBillingResult { Periods = periods.Values.ToList(), Total = total };
        }

        /// <summary>
        /// kWh x PLN/MWh -> PLN.
        /// </summary>
        private static double Value(double kwh, double pricePlnMwh, double coefficient) => kwh * pricePlnMwh / 1000 * coefficient;
    }
}
