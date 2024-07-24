using System.Globalization;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal class EneaCsvFileParser
    {
        public static IEnumerable<ImportFileRecord> Parse(string data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var result = new List<ImportFileRecord>();
            
            data = ClearFromUnwantedChars(data);

            var rows = data.Split('\n').ToList();
            rows.RemoveAt(0);

            string dateFormat = "yyyy.MM.dd HH:mm:ss";
            CultureInfo culture = new("pl-PL");
            foreach (var row in rows)
            {
                var cells = row.Split(';');

                if (string.IsNullOrEmpty(cells[0]))
                {
                    continue;
                }

                if (cells.Any(c => c.Contains("---") || string.IsNullOrEmpty(c)))
                {
                    continue;
                }

                result.Add(new ImportFileRecord() {
                    Date = DateTime.ParseExact(cells[0].Trim(), dateFormat, CultureInfo.InvariantCulture),
                    TakenVolumeBeforeBanancing = double.Parse(cells[1], culture),
                    FedVolumeBeforeBanancing = double.Parse(cells[2], culture),
                    TakenVolumeAfterBanancing = double.Parse(cells[3], culture),
                    FedVolumeAfterBanancing = double.Parse(cells[4], culture)
                });
            }

            return result;
        }

        private static string ClearFromUnwantedChars(string data)
        {
            data = data.Replace("\0", "");
            data = data.Replace("\"", "");
            return data;
        }
    }
}
