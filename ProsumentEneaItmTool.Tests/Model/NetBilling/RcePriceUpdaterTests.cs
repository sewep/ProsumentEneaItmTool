using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.NetBilling;

namespace ProsumentEneaItmTool.Tests.Model.NetBilling
{
    internal class RcePriceUpdaterTests
    {
        private sealed class TestDataContextFactory(DbContextOptions<DataContext> options) : IDbContextFactory<DataContext>
        {
            public DataContext CreateDbContext() => new(options);
        }

        /// <summary>
        /// Returns 24 prices for each requested day, except days listed as not published.
        /// </summary>
        private sealed class FakeSource : IRcePriceSource
        {
            public List<(DateTime From, DateTime To)> Requests { get; } = [];
            public HashSet<DateTime> NotPublished { get; } = [];

            public Task<List<RcePrice>> GetHourlyPricesAsync(DateTime dayFrom, DateTime dayTo, IProgress<int>? quartersLoaded, CancellationToken cancellationToken)
            {
                Requests.Add((dayFrom, dayTo));
                var prices = new List<RcePrice>();
                for (var day = dayFrom; day <= dayTo; day = day.AddDays(1))
                {
                    if (NotPublished.Contains(day))
                    {
                        continue;
                    }

                    prices.AddRange(Enumerable.Range(0, 24).Select(h => new RcePrice { Hour = day.AddHours(h), PricePlnMwh = day.Day * 10 + h }));
                }

                return Task.FromResult(prices);
            }
        }

        private SqliteConnection _connection = null!;
        private MarketPriceStore _store = null!;
        private FakeSource _source = null!;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            var factory = new TestDataContextFactory(new DbContextOptionsBuilder<DataContext>().UseSqlite(_connection).Options);
            using (var dataContext = factory.CreateDbContext())
            {
                dataContext.Database.Migrate();
            }

            _store = new MarketPriceStore(factory);
            _source = new FakeSource();
        }

        [TearDown]
        public void TearDown() => _connection.Dispose();

        private RcePriceUpdater CreateUpdater(DateTime today) => new(_source, _store, () => today);

        [Test]
        public void MissingRanges_SplitsByStoredDaysAndMaxLength()
        {
            var stored = new HashSet<DateTime> { new(2025, 1, 3), new(2025, 1, 4) };

            var ranges = RcePriceUpdater.MissingRanges(new DateTime(2025, 1, 1), new DateTime(2025, 3, 10), stored).ToList();

            Assert.That(ranges, Is.EqualTo(new[]
            {
                (new DateTime(2025, 1, 1), new DateTime(2025, 1, 2)),
                (new DateTime(2025, 1, 5), new DateTime(2025, 2, 4)),
                (new DateTime(2025, 2, 5), new DateTime(2025, 3, 7)),
                (new DateTime(2025, 3, 8), new DateTime(2025, 3, 10)),
            }));
        }

        [Test]
        public async Task DownloadMissing_StoresPricesAndSkipsStoredDaysNextTime()
        {
            var updater = CreateUpdater(new DateTime(2025, 1, 20));

            var first = await updater.DownloadMissingAsync(new DateTime(2025, 1, 1), new DateTime(2025, 1, 5), null, CancellationToken.None);
            var second = await updater.DownloadMissingAsync(new DateTime(2025, 1, 3), new DateTime(2025, 1, 7), null, CancellationToken.None);

            var prices = await _store.GetRcePricesAsync(new DateTime(2025, 1, 1), new DateTime(2025, 1, 7, 23, 0, 0));
            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(5));
                Assert.That(second, Is.EqualTo(2));
                Assert.That(_source.Requests[1], Is.EqualTo((new DateTime(2025, 1, 6), new DateTime(2025, 1, 7))));
                Assert.That(prices, Has.Count.EqualTo(7 * 24));
            });
        }

        [Test]
        public async Task DownloadMissing_LimitsRangeToAvailableDays()
        {
            var updater = CreateUpdater(new DateTime(2024, 6, 20));

            await updater.DownloadMissingAsync(new DateTime(2022, 7, 1), new DateTime(2024, 12, 31), null, CancellationToken.None);

            // From the first day in the PSE report to tomorrow.
            Assert.That(_source.Requests, Is.EqualTo(new[] { (new DateTime(2024, 6, 14), new DateTime(2024, 6, 21)) }));
        }

        [Test]
        public async Task DownloadMissing_RequestsNotPublishedDayAgainLater()
        {
            var updater = CreateUpdater(new DateTime(2025, 1, 10));
            _source.NotPublished.Add(new DateTime(2025, 1, 11));

            await updater.DownloadMissingAsync(new DateTime(2025, 1, 9), new DateTime(2025, 1, 11), null, CancellationToken.None);
            _source.NotPublished.Clear();
            await updater.DownloadMissingAsync(new DateTime(2025, 1, 9), new DateTime(2025, 1, 11), null, CancellationToken.None);

            Assert.That(_source.Requests[1], Is.EqualTo((new DateTime(2025, 1, 11), new DateTime(2025, 1, 11))));
        }

        [Test]
        public async Task RcemPrices_CanBeSetChangedAndRemoved()
        {
            await _store.SetRcemPriceAsync(new DateTime(2025, 5, 17), 250);
            await _store.SetRcemPriceAsync(new DateTime(2025, 6, 1), 300);
            await _store.SetRcemPriceAsync(new DateTime(2025, 6, 1), 310);
            await _store.SetRcemPriceAsync(new DateTime(2025, 5, 1), null);

            var prices = await _store.GetRcemPricesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(prices.Select(x => x.Month), Is.EqualTo(new[] { new DateTime(2025, 6, 1) }));
                Assert.That(prices[0].PricePlnMwh, Is.EqualTo(310));
            });
        }
    }
}
