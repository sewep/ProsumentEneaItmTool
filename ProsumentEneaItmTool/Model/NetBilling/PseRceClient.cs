using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.NetBilling
{
    internal interface IRcePriceSource
    {
        /// <summary>
        /// Hourly RCE prices for the given business days (inclusive). Days not published yet are missing.
        /// </summary>
        Task<List<RcePrice>> GetHourlyPricesAsync(DateTime dayFrom, DateTime dayTo, IProgress<int>? quartersLoaded, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Reads RCE prices from the public PSE API (report rce-pln).
    /// </summary>
    internal class PseRceClient : IRcePriceSource
    {
        /// <summary>
        /// The first business day available in the rce-pln report.
        /// </summary>
        public static readonly DateTime FirstAvailableDay = new(2024, 6, 14);

        private const string BaseUrl = "https://api.raporty.pse.pl/api/rce-pln";

        private readonly HttpClient _httpClient;

        public PseRceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<RcePrice>> GetHourlyPricesAsync(DateTime dayFrom, DateTime dayTo, IProgress<int>? quartersLoaded, CancellationToken cancellationToken)
        {
            var filter = $"business_date ge '{dayFrom:yyyy-MM-dd}' and business_date le '{dayTo:yyyy-MM-dd}'";
            string? url = $"{BaseUrl}?$filter={Uri.EscapeDataString(filter)}&$select=business_date,period,rce_pln";

            var quarters = new List<QuarterPrice>();

            // The API returns 100 records per page and a link to the next one.
            while (url is not null)
            {
                var json = await _httpClient.GetStringAsync(url, cancellationToken);
                var page = ParsePage(json);
                quarters.AddRange(page.Quarters);
                quartersLoaded?.Report(quarters.Count);
                url = page.NextLink;
            }

            return AggregateToHours(quarters);
        }

        internal record QuarterPrice(DateTime BusinessDate, string Period, double PricePlnMwh);

        internal record Page(List<QuarterPrice> Quarters, string? NextLink);

        internal static Page ParsePage(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var quarters = new List<QuarterPrice>();
            foreach (var item in root.GetProperty("value").EnumerateArray())
            {
                quarters.Add(new QuarterPrice(
                    DateTime.ParseExact(item.GetProperty("business_date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    item.GetProperty("period").GetString()!,
                    item.GetProperty("rce_pln").GetDouble()));
            }

            var nextLink = root.TryGetProperty("nextLink", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;

            return new Page(quarters, nextLink);
        }

        /// <summary>
        /// The hourly price is the average of the quarter-hour prices in that local hour.
        /// Before October 2025 PSE published hourly prices, which the report repeats for all four quarters.
        /// </summary>
        internal static List<RcePrice> AggregateToHours(IEnumerable<QuarterPrice> quarters)
        {
            return quarters
                .GroupBy(x => x.BusinessDate.AddHours(PeriodStartHour(x.Period)))
                .Select(x => new RcePrice { Hour = x.Key, PricePlnMwh = x.Average(q => q.PricePlnMwh) })
                .OrderBy(x => x.Hour)
                .ToList();
        }

        /// <summary>
        /// "13:45 - 14:00" -> 13.
        /// </summary>
        private static int PeriodStartHour(string period)
        {
            return int.Parse(period.AsSpan(0, 2), CultureInfo.InvariantCulture);
        }
    }
}
