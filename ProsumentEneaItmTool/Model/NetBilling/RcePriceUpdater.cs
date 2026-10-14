namespace ProsumentEneaItmTool.Model.NetBilling
{
    internal interface IRcePriceUpdater
    {
        /// <summary>
        /// Downloads RCE prices for days in the range which are not stored yet.
        /// </summary>
        /// <returns>Number of days for which prices were stored.</returns>
        Task<int> DownloadMissingAsync(DateTime dayFrom, DateTime dayTo, IProgress<string>? progress, CancellationToken cancellationToken);
    }

    internal class RcePriceUpdater : IRcePriceUpdater
    {
        // Longer ranges are split, so the progress moves and a failure does not lose everything.
        private const int MaxDaysPerRequest = 31;

        private readonly IRcePriceSource _rcePriceSource;
        private readonly IMarketPriceStore _marketPriceStore;
        private readonly Func<DateTime> _today;

        public RcePriceUpdater(IRcePriceSource rcePriceSource, IMarketPriceStore marketPriceStore)
            : this(rcePriceSource, marketPriceStore, () => DateTime.Today)
        {
        }

        internal RcePriceUpdater(IRcePriceSource rcePriceSource, IMarketPriceStore marketPriceStore, Func<DateTime> today)
        {
            _rcePriceSource = rcePriceSource;
            _marketPriceStore = marketPriceStore;
            _today = today;
        }

        public async Task<int> DownloadMissingAsync(DateTime dayFrom, DateTime dayTo, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            // Prices for the next day are published in the afternoon; later days do not exist yet.
            var from = Max(dayFrom.Date, PseRceClient.FirstAvailableDay);
            var to = Min(dayTo.Date, _today().AddDays(1));
            if (from > to)
            {
                return 0;
            }

            var stored = await _marketPriceStore.GetDaysWithRcePricesAsync(from, to);
            var missingRanges = MissingRanges(from, to, stored).ToList();
            var totalDays = missingRanges.Sum(x => (x.To - x.From).Days + 1);

            var savedDays = 0;
            var processedDays = 0;
            foreach (var (rangeFrom, rangeTo) in missingRanges)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var prices = await _rcePriceSource.GetHourlyPricesAsync(rangeFrom, rangeTo, null, cancellationToken);
                await _marketPriceStore.SaveRcePricesAsync(prices);

                savedDays += prices.Select(x => x.Hour.Date).Distinct().Count();
                processedDays += (rangeTo - rangeFrom).Days + 1;
                progress?.Report($"Pobieranie cen RCE z PSE: {processedDays} z {totalDays} dni");
            }

            return savedDays;
        }

        /// <summary>
        /// Continuous ranges of days without prices, each at most <see cref="MaxDaysPerRequest"/> long.
        /// </summary>
        internal static IEnumerable<(DateTime From, DateTime To)> MissingRanges(DateTime from, DateTime to, IReadOnlySet<DateTime> storedDays)
        {
            DateTime? rangeStart = null;

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                var missing = !storedDays.Contains(day);

                if (missing && rangeStart is null)
                {
                    rangeStart = day;
                }

                var rangeIsFull = rangeStart is not null && (day - rangeStart.Value).Days + 1 == MaxDaysPerRequest;
                var rangeEnds = rangeStart is not null && (!missing || day == to || rangeIsFull);

                if (rangeEnds)
                {
                    yield return (rangeStart!.Value, missing ? day : day.AddDays(-1));
                    rangeStart = null;
                }
            }
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    }
}
