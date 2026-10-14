using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;

namespace ProsumentEneaItmTool.Model.Export
{
    internal interface ICsvExporter
    {
        void Export(string filePath, IPowerCalculation calculation);
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

        public void Export(string filePath, IPowerCalculation calculation)
        {
            // BOM, so Excel recognizes UTF-8 and shows Polish letters.
            _fileSystem.File.WriteAllText(filePath, BuildCsv(calculation), new UTF8Encoding(true));
        }

        public static string BuildCsv(IPowerCalculation calculation)
        {
            var coefficient = calculation.FreeEnergyCoefficient.ToString("0.00", _culture);
            var dateFormat = calculation.Aggregation switch
            {
                Aggregation.Day => "yyyy-MM-dd",
                Aggregation.Month => "yyyy-MM",
                _ => "yyyy-MM-dd HH:mm"
            };

            var csv = new StringBuilder();
            csv.AppendLine(string.Join(";",
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
                $"Po opuście ({coefficient}) po bil. narastająco [kWh]"));

            var periods = calculation.PeriodResults;
            var cumulative = calculation.CalculationEnergyChart;

            for (var i = 0; i < periods.Count; i++)
            {
                var period = periods[i];
                var total = cumulative[i];

                csv.AppendLine(string.Join(";",
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
                    Format(total.FreeToUseAfterBalancing)));
            }

            return csv.ToString();
        }

        private static string Format(double value)
        {
            var rounded = Math.Round(value, 3);

            // Avoids "-0" for tiny negative values.
            return (rounded == 0 ? 0 : rounded).ToString("0.###", _culture);
        }
    }
}
