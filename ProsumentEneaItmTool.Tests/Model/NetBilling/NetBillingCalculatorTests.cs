using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.NetBilling;

namespace ProsumentEneaItmTool.Tests.Model.NetBilling
{
    internal class NetBillingCalculatorTests
    {
        private static ImportFileRecord Fed(DateTime eneaTime, double kwh) => new() { Date = eneaTime, FedVolumeAfterBanancing = kwh, FedVolumeBeforeBanancing = kwh + 1 };

        [Test]
        public void Calculate_ValuesFedEnergyByHourlyPriceWithCoefficient()
        {
            // Enea marks the hour 12:00-13:00 as 12:59.
            var records = new[] { Fed(new DateTime(2025, 7, 1, 12, 59, 0), 2), Fed(new DateTime(2025, 7, 1, 13, 59, 0), 3) };
            var rce = new Dictionary<DateTime, double>
            {
                [new DateTime(2025, 7, 1, 12, 0, 0)] = 200,
                [new DateTime(2025, 7, 1, 13, 0, 0)] = 400,
            };

            var result = NetBillingCalculator.Calculate(records, rce, new Dictionary<DateTime, double>(), Aggregation.Day, 1.23);

            Assert.Multiple(() =>
            {
                // 2 kWh x 0,2 zł + 3 kWh x 0,4 zł = 1,6 zł, x 1,23.
                Assert.That(result.Total.ValueRce, Is.EqualTo(1.6 * 1.23).Within(1e-9));
                Assert.That(result.Total.FedKwh, Is.EqualTo(5));
                Assert.That(result.Total.FedWithoutRceKwh, Is.Zero);
                Assert.That(result.Total.WeightedRcePlnMwh, Is.EqualTo((2 * 200 + 3 * 400) / 5.0).Within(1e-9));
                Assert.That(result.Periods.Single().Time, Is.EqualTo(new DateTime(2025, 7, 1)));
            });
        }

        [Test]
        public void Calculate_CoefficientAppliesFromFebruary2025()
        {
            var records = new[] { Fed(new DateTime(2025, 1, 31, 23, 59, 0), 1), Fed(new DateTime(2025, 2, 1, 0, 59, 0), 1) };
            var rce = new Dictionary<DateTime, double>
            {
                [new DateTime(2025, 1, 31, 23, 0, 0)] = 1000,
                [new DateTime(2025, 2, 1, 0, 0, 0)] = 1000,
            };
            var rcem = new Dictionary<DateTime, double>
            {
                [new DateTime(2025, 1, 1)] = 500,
                [new DateTime(2025, 2, 1)] = 500,
            };

            var result = NetBillingCalculator.Calculate(records, rce, rcem, Aggregation.Month, 1.23);

            Assert.Multiple(() =>
            {
                // January without the coefficient, February with it.
                Assert.That(result.Periods[0].ValueRce, Is.EqualTo(1.0).Within(1e-9));
                Assert.That(result.Periods[1].ValueRce, Is.EqualTo(1.23).Within(1e-9));
                Assert.That(result.Periods[0].ValueRcem, Is.EqualTo(0.5).Within(1e-9));
                Assert.That(result.Periods[1].ValueRcem, Is.EqualTo(0.615).Within(1e-9));
                Assert.That(result.Total.ValueRce, Is.EqualTo(2.23).Within(1e-9));
            });
        }

        [Test]
        public void Calculate_NegativePricesCountAsZero()
        {
            var records = new[] { Fed(new DateTime(2025, 5, 1, 13, 59, 0), 4) };
            var rce = new Dictionary<DateTime, double> { [new DateTime(2025, 5, 1, 13, 0, 0)] = -50 };
            var rcem = new Dictionary<DateTime, double> { [new DateTime(2025, 5, 1)] = -1 };

            var result = NetBillingCalculator.Calculate(records, rce, rcem, Aggregation.Month, 1.23);

            Assert.Multiple(() =>
            {
                Assert.That(result.Total.ValueRce, Is.Zero);
                Assert.That(result.Total.ValueRcem, Is.Zero);
                Assert.That(result.Total.FedWithRceKwh, Is.EqualTo(4));
            });
        }

        [Test]
        public void Calculate_ReportsEnergyWithoutPrices()
        {
            var records = new[]
            {
                Fed(new DateTime(2024, 5, 31, 12, 59, 0), 1),  // before the PSE report, no prices
                Fed(new DateTime(2024, 6, 20, 12, 59, 0), 2),  // RCE only
                Fed(new DateTime(2024, 7, 10, 12, 59, 0), 3),  // RCE and RCEm
            };
            var rce = new Dictionary<DateTime, double>
            {
                [new DateTime(2024, 6, 20, 12, 0, 0)] = 300,
                [new DateTime(2024, 7, 10, 12, 0, 0)] = 100,
            };
            var rcem = new Dictionary<DateTime, double> { [new DateTime(2024, 7, 1)] = 250 };

            var result = NetBillingCalculator.Calculate(records, rce, rcem, Aggregation.Month, 1.0);

            Assert.Multiple(() =>
            {
                Assert.That(result.Total.FedWithoutRceKwh, Is.EqualTo(1));
                Assert.That(result.Total.FedWithoutRcemKwh, Is.EqualTo(3));
                Assert.That(result.Periods.Select(x => x.Time), Is.EqualTo(new[] { new DateTime(2024, 5, 1), new DateTime(2024, 6, 1), new DateTime(2024, 7, 1) }));
                Assert.That(result.Periods[0].WeightedRcePlnMwh, Is.Null);
                Assert.That(result.Periods[2].RcemPricePlnMwh, Is.EqualTo(250));
                // Comparison only where both prices are known: July, 3 kWh x (0,1 - 0,25) zł.
                Assert.That(result.Total.FedWithBothKwh, Is.EqualTo(3));
                Assert.That(result.Total.DifferenceRceMinusRcem, Is.EqualTo(3 * (0.1 - 0.25)).Within(1e-9));
            });
        }

        [Test]
        public void Calculate_UsesEnergyAfterBalancing()
        {
            var records = new[] { Fed(new DateTime(2025, 7, 1, 12, 59, 0), 2) };
            var rce = new Dictionary<DateTime, double> { [new DateTime(2025, 7, 1, 12, 0, 0)] = 1000 };

            var result = NetBillingCalculator.Calculate(records, rce, new Dictionary<DateTime, double>(), Aggregation.Hour, 1.0);

            // FedVolumeBeforeBanancing is 3 kWh, but only 2 kWh after balancing are sold.
            Assert.That(result.Total.ValueRce, Is.EqualTo(2));
        }
    }
}
