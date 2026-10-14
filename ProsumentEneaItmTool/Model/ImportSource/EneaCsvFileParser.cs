using System.Globalization;
using System.IO;
using System.Text;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal record ParseResult(IReadOnlyList<ImportFileRecord> Records, IReadOnlyList<string> Warnings);

    internal static class EneaCsvFileParser
    {
        private static readonly string[] _dateFormats = ["yyyy.MM.dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm"];
        private static readonly CultureInfo _culture = new("pl-PL");

        /// <summary>
        /// Decodes file content. Enea reports are saved in UTF-8 or in UTF-16, sometimes without BOM.
        /// </summary>
        public static string Decode(byte[] content)
        {
            ArgumentNullException.ThrowIfNull(content);

            using var reader = new StreamReader(new MemoryStream(content), LooksLikeUtf16(content) ? Encoding.Unicode : Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        public static ParseResult Parse(string data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var records = new List<ImportFileRecord>();
            var warnings = new List<string>();

            // The first row is a header.
            var rows = ClearFromUnwantedChars(data)
                .Split('\n')
                .Skip(1);

            foreach (var row in rows)
            {
                // Some reports have cells like ="value".
                var cells = row
                    .Replace("=", "")
                    .Split(';')
                    .Select(c => c.Trim())
                    .ToArray();

                if (cells.Any(string.IsNullOrEmpty) || cells.Any(c => c.Contains("---")))
                {
                    continue;
                }

                if (TryCellsToRecord(cells, out var record))
                {
                    records.Add(record);
                }
                else
                {
                    warnings.Add(row.Trim());
                }
            }

            return new ParseResult(records, warnings);
        }

        private static bool LooksLikeUtf16(byte[] content)
        {
            // ASCII text in UTF-16 LE has every second byte equal to zero.
            var sample = content.AsSpan(0, Math.Min(content.Length, 200));
            var zeros = 0;
            for (var i = 1; i < sample.Length; i += 2)
            {
                if (sample[i] == 0)
                {
                    zeros++;
                }
            }

            return sample.Length >= 2 && zeros > sample.Length / 4;
        }

        private static string ClearFromUnwantedChars(string data)
        {
            return data
                .Replace("\0", "")
                .Replace("\"", "");
        }

        private static bool TryParseDate(string dateTime, out DateTime date)
        {
            foreach (var format in _dateFormats)
            {
                // The date can be followed by additional text, so only the beginning is taken.
                var candidate = dateTime[..Math.Min(format.Length, dateTime.Length)];
                if (DateTime.TryParseExact(candidate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            // Lenient fallback, which older versions used for all reports.
            foreach (var format in _dateFormats)
            {
                var candidate = dateTime[..Math.Min(format.Length, dateTime.Length)];
                if (DateTime.TryParse(candidate, CultureInfo.InvariantCulture, out date))
                {
                    return true;
                }
            }

            date = default;
            return false;
        }

        private static bool TryCellsToRecord(string[] cells, out ImportFileRecord record)
        {
            record = new ImportFileRecord();

            if (cells.Length < 5 || !TryParseDate(cells[0], out var date))
            {
                return false;
            }

            if (!double.TryParse(cells[1], NumberStyles.Float | NumberStyles.AllowThousands, _culture, out var takenBefore)
                || !double.TryParse(cells[2], NumberStyles.Float | NumberStyles.AllowThousands, _culture, out var fedBefore)
                || !double.TryParse(cells[3], NumberStyles.Float | NumberStyles.AllowThousands, _culture, out var takenAfter)
                || !double.TryParse(cells[4], NumberStyles.Float | NumberStyles.AllowThousands, _culture, out var fedAfter))
            {
                return false;
            }

            record.Date = date;
            record.TakenVolumeBeforeBanancing = takenBefore;
            record.FedVolumeBeforeBanancing = fedBefore;
            record.TakenVolumeAfterBanancing = takenAfter;
            record.FedVolumeAfterBanancing = fedAfter;
            return true;
        }
    }
}
