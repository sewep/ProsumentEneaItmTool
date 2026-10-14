using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.NetBilling;

namespace ProsumentEneaItmTool.Model.Export
{
    internal interface ICsvExporter
    {
        void Export(string filePath, IPowerCalculation calculation, NetBillingResult? netBilling);
    }

    /// <summary>
    /// Saves calculated values in a CSV file readable by Excel with Polish settings.
    /// </summary>
    internal class CsvExporter : ICsvExporter
    {
        private static readonly CultureInfo _culture = new("pl-PL");

        private readonly IFileSystem _fileSystem;

        public CsvExporter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void Export(string filePath, IPowerCalculation calculation, NetBillingResult? netBilling)
        {
            // BOM, so Excel recognizes UTF-8 and shows Polish letters.
            _fileSystem.File.WriteAllText(filePath, BuildCsv(calculation, netBilling), new UTF8Encoding(true));
        }

        /// <param name="netBilling">Values calculated with the same aggregation, added as extra columns.</param>
        public static string BuildCsv(IPowerCalculation calculation, NetBillingResult? netBilling = null)
        {
            var coefficient = calculation.FreeEnergyCoefficient.ToString("0.00", _culture);
            var dateFormat = calculation.Aggregation switch
            {
                Aggregation.Day => "yyyy-MM-dd",
                Aggregation.Month => "yyyy-MM",
                _ => "yyyy-MM-dd HH:mm"
            };

            var csv = new StringBuilder();
            var header = new List<string>
            {
                "Okres",
                "Pobrana przed bil. [kWh]",
                "Wysłana przed bil. [kWh]",
                "Pobrana po bil. [kWh]",
                "Wysłana po bil. [kWh]",
                "Różnica po bil. [kWh]",
                $"Po opuście ({coefficient}) po bil. [kWh]",
                "Pobrana po bil. narastająco [kWh]",
                "Wysłana po bil. narastająco [kWh]",
                "Różnica po bil. narastająco [kWh]",
                $"Po opuście ({coefficient}) po bil. narastająco [kWh]"
            };

            if (netBilling is not null)
            {
                header.AddRange(["Net-billing: wartość wg RCE [zł]", "Net-billing: bez ceny RCE [kWh]", "Net-billing: wartość wg RCEm [zł]", "Net-billing: bez ceny RCEm [kWh]"]);
            }

            csv.AppendLine(string.Join(";", header));

            // A repeated hour (DST change) is one net-billing period, it is written to the first of the two rows.
            var netBillingPeriods = netBilling?.Periods.ToDictionary(x => x.Time) ?? [];

            var periods = calculation.PeriodResults;
            var cumulative = calculation.CalculationEnergyChart;

            for (var i = 0; i < periods.Count; i++)
            {
                var period = periods[i];
                var total = cumulative[i];

                var row = new List<string>
                {
                    period.Time.ToString(dateFormat, CultureInfo.InvariantCulture),
                    Format(period.ConsumedBeforeBalancing),
                    Format(period.FedBeforeBalancing),
                    Format(period.ConsumedAfterBalancing),
                    Format(period.FedAfterBalancing),
                    Format(period.DifferenceAfterBalancing),
                    Format(period.FreeToUseAfterBalancing),
                    Format(total.ConsumedAfterBalancing),
                    Format(total.FedAfterBalancing),
                    Format(total.DifferenceAfterBalancing),
                    Format(total.FreeToUseAfterBalancing)
                };

                if (netBilling is not null)
                {
                    if (netBillingPeriods.Remove(period.Time, out var nb))
                    {
                        row.AddRange([
                            FormatValue(nb.ValueRce, nb.FedKwh, nb.FedWithRceKwh), Format(nb.FedWithoutRceKwh),
                            FormatValue(nb.ValueRcem, nb.FedKwh, nb.FedWithRcemKwh), Format(nb.FedWithoutRcemKwh)]);
                    }
                    else
                    {
                        row.AddRange(["", "", "", ""]);
                    }
                }

                csv.AppendLine(string.Join(";", row));
            }

            return csv.ToString();
        }

        /// <summary>
        /// Empty when energy was fed but none of it has a price, so missing price is not shown as zero value.
        /// </summary>
        private static string FormatValue(double value, double fedKwh, double fedWithPriceKwh) =>
            fedKwh > 0 && fedWithPriceKwh <= 0 ? "" : FormatMoney(value);

        private static string FormatMoney(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", _culture);

        private static string Format(double value)
        {
            var rounded = Math.Round(value, 3);

            // Avoids "-0" for tiny negative values.
            return (rounded == 0 ? 0 : rounded).ToString("0.###", _culture);
        }
    }
}
