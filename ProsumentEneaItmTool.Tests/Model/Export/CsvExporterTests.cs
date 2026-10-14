using System.IO.Abstractions.TestingHelpers;
using System.Text;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.Export;

namespace ProsumentEneaItmTool.Tests.Model.Export
{
    internal class CsvExporterTests
    {
        private static PowerCalculation CalculateMonthly()
        {
            var calculation = new PowerCalculation { Aggregation = Aggregation.Month, FreeEnergyCoefficient = 0.8 };
            calculation.Calculate(
            [
                new() { Date = new DateTime(2024, 1, 10, 12, 0, 0), TakenVolumeBeforeBanancing = 10.5, FedVolumeBeforeBanancing = 2, TakenVolumeAfterBanancing = 9.25, FedVolumeAfterBanancing = 1 },
                new() { Date = new DateTime(2024, 1, 20, 12, 0, 0), TakenVolumeBeforeBanancing = 1, FedVolumeBeforeBanancing = 0, TakenVolumeAfterBanancing = 1, FedVolumeAfterBanancing = 0 },
                new() { Date = new DateTime(2024, 2, 5, 12, 0, 0), TakenVolumeBeforeBanancing = 0, FedVolumeBeforeBanancing = 30, TakenVolumeAfterBanancing = 0, FedVolumeAfterBanancing = 25.125 },
            ]);
            return calculation;
        }

        [Test]
        public void BuildCsv_WritesPeriodAndCumulativeValuesInPolishFormat()
        {
            var lines = CsvExporter.BuildCsv(CalculateMonthly())
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() =>
            {
                Assert.That(lines, Has.Length.EqualTo(3));
                Assert.That(lines[0], Does.StartWith("Okres;Pobrana przed bil. [kWh];"));
                Assert.That(lines[0], Does.Contain("Po opuście (0,80) po bil. [kWh]"));
                // January: taken after 10,25, fed after 1, difference -9,25, after discount 0,8 - 10,25 = -9,45.
                Assert.That(lines[1], Is.EqualTo("2024-01;11,5;2;10,25;1;-9,25;-9,45;10,25;1;-9,25;-9,45"));
                // February cumulative: taken 10,25, fed 26,125, difference 15,875, after discount 20,9 - 10,25 = 10,65.
                Assert.That(lines[2], Is.EqualTo("2024-02;0;30;0;25,125;25,125;20,1;10,25;26,125;15,875;10,65"));
            });
        }

        [Test]
        public void Export_WritesUtf8FileWithBom()
        {
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory(@"C:\eksport");

            new CsvExporter(fileSystem).Export(@"C:\eksport\dane.csv", CalculateMonthly());

            var bytes = fileSystem.File.ReadAllBytes(@"C:\eksport\dane.csv");
            Assert.Multiple(() =>
            {
                Assert.That(bytes.Take(3), Is.EqualTo(Encoding.UTF8.GetPreamble()));
                Assert.That(Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), Does.Contain("Wysłana"));
            });
        }
    }
}
