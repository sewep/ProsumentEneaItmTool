using System.Globalization;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal class EneaCsvFileParser
    {
        private static readonly string _dateFormat = "yyyy.MM.dd HH:mm:ss";
        private static readonly CultureInfo _culture = new("pl-PL");

        public static IEnumerable<ImportFileRecord> Parse(string data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var result = new List<ImportFileRecord>();

            data = ClearFromUnwantedChars(data);

            var rows = data.Split('\n').ToList();

            rows.RemoveAt(0);

            foreach (var row in rows)
            {
                var cells = row.Split(';');

                if (cells.Any(c => string.IsNullOrEmpty(c) || c!.Contains("---")))
                {
                    continue;
                }

                result.Add(CellsToRecord(cells));
            }

            return result;
        }

        private static string ClearFromUnwantedChars(string data)
        {
            return data
                .Replace("\0", "")
                .Replace("\"", "");
        }

        private static ImportFileRecord CellsToRecord(string[] cells)
        {
            return new ImportFileRecord()
            {
                Date = DateTime.ParseExact(cells[0].Trim(), _dateFormat, CultureInfo.InvariantCulture),
                TakenVolumeBeforeBanancing = double.Parse(cells[1], _culture),
                FedVolumeBeforeBanancing = double.Parse(cells[2], _culture),
                TakenVolumeAfterBanancing = double.Parse(cells[3], _culture),
                FedVolumeAfterBanancing = double.Parse(cells[4], _culture)
            };
        }
    }
}
