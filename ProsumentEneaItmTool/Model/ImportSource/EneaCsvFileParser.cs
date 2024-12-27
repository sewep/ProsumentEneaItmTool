using System.Globalization;
using System.Text;
using System.Windows;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal class EneaCsvFileParser
    {
        private static readonly string[] _dateFormat = { "yyyy.MM.dd HH:mm:ss", "yyyy-MM-dd HH:mm" };
        private static readonly CultureInfo _culture = new("pl-PL");

        public static IEnumerable<ImportFileRecord> Parse(string data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var result = new List<ImportFileRecord>();

            data = ClearFromUnwantedChars(data);

            var rows = data.Split('\n').ToList();

            rows.RemoveAt(0);

            var parseWarnings = new StringBuilder();

            foreach (var row in rows)
            {
                // Remove some strange strings from report.
                var rowClened = row.Replace("=", "");


                var cells = rowClened.Split(';');

                try
                {

                    if (cells.Any(c => string.IsNullOrEmpty(c) || c!.Contains("---")))
                    {
                        continue;
                    }

                    result.Add(CellsToRecord(cells));
                }
                catch (Exception e)
                {
                    parseWarnings.AppendLine($"Not parsed row: {row}");
                }
            }

            if (parseWarnings.Length > 0)
            {
                MessageBox.Show(
                    "Problem z pasrowaniem rekordów: \r\n" + parseWarnings.ToString(),
                    "Uwagi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return result;
        }

        private static string ClearFromUnwantedChars(string data)
        {
            return data
                .Replace("\0", "")
                .Replace("\"", "");
        }

        private static DateTime TryParseDate(string dateTime)
        {
            foreach (var format in _dateFormat)
            {
                var candidate = dateTime[..Math.Min(format.Length, dateTime.Length)];
                if (DateTime.TryParse(candidate, CultureInfo.InvariantCulture, out var date))
                {
                    return date;
                }
            }

            throw new ArgumentException($"Can't parse date time {dateTime}.");
        }

        private static ImportFileRecord CellsToRecord(string[] cells)
        {
            return new ImportFileRecord()
            {
                Date = TryParseDate(cells[0]),
                TakenVolumeBeforeBanancing = double.Parse(cells[1], _culture),
                FedVolumeBeforeBanancing = double.Parse(cells[2], _culture),
                TakenVolumeAfterBanancing = double.Parse(cells[3], _culture),
                FedVolumeAfterBanancing = double.Parse(cells[4], _culture)
            };
        }
    }
}
