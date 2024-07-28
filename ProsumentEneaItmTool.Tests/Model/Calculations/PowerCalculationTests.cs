using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;

namespace ProsumentEneaItmTool.Tests.Model.Calculations
{
    internal class PowerCalculationTests
    {
        [Test]
        public void PowerCalculationTest_CorectDigitalResults()
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
        public void PowerCalculationTest_CorectChartResults()
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
                Assert.That(chartCalculations.ConsumedBeforeBalancing, Is.EqualTo(105));
                Assert.That(chartCalculations.ConsumedAfterBalancing, Is.EqualTo(95));
                Assert.That(chartCalculations.FedBeforeBalancing, Is.EqualTo(55));
                Assert.That(chartCalculations.FedAfterBalancing, Is.EqualTo(45));
                Assert.That(chartCalculations.DifferenceBeforeBalancing, Is.EqualTo(-50));
                Assert.That(chartCalculations.DifferenceAfterBalancing, Is.EqualTo(-50));
                Assert.That(chartCalculations.FreeToUseBeforeBalancing, Is.EqualTo(-61));
                Assert.That(chartCalculations.FreeToUseAfterBalancing, Is.EqualTo(-59));
            });
        }
    }
}
