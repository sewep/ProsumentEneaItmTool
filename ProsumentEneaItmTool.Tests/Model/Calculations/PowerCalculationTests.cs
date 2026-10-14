using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;

namespace ProsumentEneaItmTool.Tests.Model.Calculations
{
    internal class PowerCalculationTests
    {
        [Test]
        public void PowerCalculationTest_CorrectTotals()
        {
            var records = new List<ImportFileRecord>() {
                new() {
                    Id = 1,
                    Date = new DateTime(2024, 1, 1),
                    TakenVolumeBeforeBanancing = 100,
                    TakenVolumeAfterBanancing = 90,
                    FedVolumeBeforeBanancing = 50,
                    FedVolumeAfterBanancing = 40 },
                new() {
                    Id = 1,
                    Date = new DateTime(2024, 1, 2),
                    TakenVolumeBeforeBanancing = 105,
                    TakenVolumeAfterBanancing = 95,
                    FedVolumeBeforeBanancing = 55,
                    FedVolumeAfterBanancing = 45 },
            };

            var powerCalculation = new PowerCalculation();
            powerCalculation.FreeEnergyCoefficient = 0.8;
            powerCalculation.Calculate(records);

            var digitalCalculations = powerCalculation.CalculationEnergyResults;
            Assert.Multiple(() =>
            {
                Assert.That(digitalCalculations.ConsumedBeforeBalancing, Is.EqualTo(205));
                Assert.That(digitalCalculations.ConsumedAfterBalancing, Is.EqualTo(185));
                Assert.That(digitalCalculations.FedBeforeBalancing, Is.EqualTo(105));
                Assert.That(digitalCalculations.FedAfterBalancing, Is.EqualTo(85));
                Assert.That(digitalCalculations.DifferenceBeforeBalancing, Is.EqualTo(-100));
                Assert.That(digitalCalculations.DifferenceAfterBalancing, Is.EqualTo(-100));
                Assert.That(digitalCalculations.FreeToUseBeforeBalancing, Is.EqualTo(-121));
                Assert.That(digitalCalculations.FreeToUseAfterBalancing, Is.EqualTo(-117));
            });
        }

        [Test]
        public void PowerCalculationTest_CorrectCumulativeChart()
        {
            var records = new List<ImportFileRecord>() {
                new() {
                    Id = 1,
                    Date = new DateTime(2024, 1, 1),
                    TakenVolumeBeforeBanancing = 100,
                    TakenVolumeAfterBanancing = 90,
                    FedVolumeBeforeBanancing = 50,
                    FedVolumeAfterBanancing = 40 },
                new() {
                    Id = 1,
                    Date = new DateTime(2024, 1, 2),
                    TakenVolumeBeforeBanancing = 105,
                    TakenVolumeAfterBanancing = 95,
                    FedVolumeBeforeBanancing = 55,
                    FedVolumeAfterBanancing = 45 },
            };

            var powerCalculation = new PowerCalculation();
            powerCalculation.FreeEnergyCoefficient = 0.8;
            powerCalculation.Calculate(records);

            var chartCalculations = powerCalculation.CalculationEnergyChart[1];
            Assert.Multiple(() =>
            {
                Assert.That(chartCalculations.ConsumedBeforeBalancing, Is.EqualTo(205));
                Assert.That(chartCalculations.ConsumedAfterBalancing, Is.EqualTo(185));
                Assert.That(chartCalculations.FedBeforeBalancing, Is.EqualTo(105));
                Assert.That(chartCalculations.FedAfterBalancing, Is.EqualTo(85));
                Assert.That(chartCalculations.DifferenceBeforeBalancing, Is.EqualTo(-100));
                Assert.That(chartCalculations.DifferenceAfterBalancing, Is.EqualTo(-100));
                Assert.That(chartCalculations.FreeToUseBeforeBalancing, Is.EqualTo(-121));
                Assert.That(chartCalculations.FreeToUseAfterBalancing, Is.EqualTo(-117));
            });
        }

        [Test]
        public void PowerCalculationTest_FirstChartPointContainsFirstRecord()
        {
            var records = new List<ImportFileRecord>() {
                new() {
                    Date = new DateTime(2024, 1, 1),
                    TakenVolumeBeforeBanancing = 100,
                    TakenVolumeAfterBanancing = 90,
                    FedVolumeBeforeBanancing = 50,
                    FedVolumeAfterBanancing = 40 },
            };

            var powerCalculation = new PowerCalculation();
            powerCalculation.Calculate(records);

            var firstPoint = powerCalculation.CalculationEnergyChart[0];
            Assert.Multiple(() =>
            {
                Assert.That(firstPoint.ConsumedBeforeBalancing, Is.EqualTo(100));
                Assert.That(firstPoint.ConsumedAfterBalancing, Is.EqualTo(90));
                Assert.That(firstPoint.FedBeforeBalancing, Is.EqualTo(50));
                Assert.That(firstPoint.FedAfterBalancing, Is.EqualTo(40));
            });
        }

        private static List<ImportFileRecord> HourlyRecordsInTwoMonths() =>
        [
            new() { Date = new DateTime(2024, 1, 31, 10, 0, 0), TakenVolumeAfterBanancing = 1, FedVolumeAfterBanancing = 2 },
            new() { Date = new DateTime(2024, 1, 31, 11, 0, 0), TakenVolumeAfterBanancing = 3, FedVolumeAfterBanancing = 4 },
            new() { Date = new DateTime(2024, 2, 1, 0, 0, 0), TakenVolumeAfterBanancing = 5, FedVolumeAfterBanancing = 0 },
            new() { Date = new DateTime(2024, 2, 15, 12, 0, 0), TakenVolumeAfterBanancing = 2, FedVolumeAfterBanancing = 10 },
        ];

        [Test]
        public void PowerCalculationTest_DayAggregationSumsEachDay()
        {
            var powerCalculation = new PowerCalculation { Aggregation = Aggregation.Day };
            powerCalculation.Calculate(HourlyRecordsInTwoMonths());

            var periods = powerCalculation.PeriodResults;
            Assert.Multiple(() =>
            {
                Assert.That(periods.Select(x => x.Time), Is.EqualTo(new[] { new DateTime(2024, 1, 31), new DateTime(2024, 2, 1), new DateTime(2024, 2, 15) }));
                Assert.That(periods.Select(x => x.ConsumedAfterBalancing), Is.EqualTo(new[] { 4.0, 5.0, 2.0 }));
                Assert.That(periods.Select(x => x.FedAfterBalancing), Is.EqualTo(new[] { 6.0, 0.0, 10.0 }));
                Assert.That(periods[0].DifferenceAfterBalancing, Is.EqualTo(2));
                Assert.That(powerCalculation.CalculationEnergyChart.Select(x => x.ConsumedAfterBalancing), Is.EqualTo(new[] { 4.0, 9.0, 11.0 }));
            });
        }

        [Test]
        public void PowerCalculationTest_MonthAggregationSumsEachMonth()
        {
            var powerCalculation = new PowerCalculation { Aggregation = Aggregation.Month, FreeEnergyCoefficient = 0.5 };
            powerCalculation.Calculate(HourlyRecordsInTwoMonths());

            var periods = powerCalculation.PeriodResults;
            Assert.Multiple(() =>
            {
                Assert.That(periods.Select(x => x.Time), Is.EqualTo(new[] { new DateTime(2024, 1, 1), new DateTime(2024, 2, 1) }));
                Assert.That(periods.Select(x => x.ConsumedAfterBalancing), Is.EqualTo(new[] { 4.0, 7.0 }));
                Assert.That(periods.Select(x => x.FreeToUseAfterBalancing), Is.EqualTo(new[] { 6 * 0.5 - 4, 10 * 0.5 - 7 }));
            });
        }

        [TestCase(Aggregation.Hour)]
        [TestCase(Aggregation.Day)]
        [TestCase(Aggregation.Month)]
        public void PowerCalculationTest_AggregationDoesNotChangeTotals(Aggregation aggregation)
        {
            var powerCalculation = new PowerCalculation { Aggregation = aggregation };
            powerCalculation.Calculate(HourlyRecordsInTwoMonths());

            var lastPoint = powerCalculation.CalculationEnergyChart[^1];
            Assert.Multiple(() =>
            {
                Assert.That(powerCalculation.CalculationEnergyResults.ConsumedAfterBalancing, Is.EqualTo(11));
                Assert.That(lastPoint.ConsumedAfterBalancing, Is.EqualTo(11));
                Assert.That(lastPoint.FedAfterBalancing, Is.EqualTo(16));
                Assert.That(powerCalculation.PeriodResults.Sum(x => x.FedAfterBalancing), Is.EqualTo(16));
            });
        }

        [Test]
        public void PowerCalculationTest_LastChartPointEqualsTotals()
        {
            var records = new List<ImportFileRecord>() {
                new() { Date = new DateTime(2024, 1, 1), TakenVolumeAfterBanancing = 3, FedVolumeAfterBanancing = 1 },
                new() { Date = new DateTime(2024, 1, 2), TakenVolumeAfterBanancing = 5, FedVolumeAfterBanancing = 7 },
                new() { Date = new DateTime(2024, 1, 3), TakenVolumeAfterBanancing = 2, FedVolumeAfterBanancing = 4 },
            };

            var powerCalculation = new PowerCalculation();
            powerCalculation.Calculate(records);

            var lastPoint = powerCalculation.CalculationEnergyChart[^1];
            var totals = powerCalculation.CalculationEnergyResults;
            Assert.Multiple(() =>
            {
                Assert.That(lastPoint.ConsumedAfterBalancing, Is.EqualTo(totals.ConsumedAfterBalancing));
                Assert.That(lastPoint.FedAfterBalancing, Is.EqualTo(totals.FedAfterBalancing));
                Assert.That(lastPoint.FreeToUseAfterBalancing, Is.EqualTo(totals.FreeToUseAfterBalancing).Within(1e-9));
            });
        }
    }
}
