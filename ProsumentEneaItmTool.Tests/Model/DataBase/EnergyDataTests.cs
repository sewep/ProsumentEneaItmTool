using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.DataBase;

namespace ProsumentEneaItmTool.Tests.Model.DataBase
{
    internal class EnergyDataTests
    {
        private SqliteConnection _connection = null!;
        private DataContext _dataContext = null!;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(_connection)
                .Options;
            _dataContext = new DataContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _dataContext.Dispose();
            _connection.Dispose();
        }

        private static ImportFileRecord Record(DateTime date, double taken) => new()
        {
            Date = date,
            TakenVolumeBeforeBanancing = taken,
            TakenVolumeAfterBanancing = taken
        };

        [Test]
        public async Task AddOrUpdateData_ReplacesRecordsWithSameDate()
        {
            var updater = new EnergyDataUpdater(_dataContext);
            var date1 = new DateTime(2024, 1, 1, 10, 0, 0);
            var date2 = new DateTime(2024, 1, 1, 11, 0, 0);
            var date3 = new DateTime(2024, 1, 1, 12, 0, 0);

            await updater.AddOrUpdateDataAsync([Record(date1, 1), Record(date2, 2)]);
            await updater.AddOrUpdateDataAsync([Record(date2, 20), Record(date3, 30)]);

            var stored = await _dataContext.ImportedRecords.AsNoTracking().OrderBy(x => x.Date).ToListAsync();
            Assert.Multiple(() =>
            {
                Assert.That(stored.Select(x => x.Date), Is.EqualTo(new[] { date1, date2, date3 }));
                Assert.That(stored.Select(x => x.TakenVolumeAfterBanancing), Is.EqualTo(new[] { 1.0, 20.0, 30.0 }));
            });
        }

        [Test]
        public async Task AddOrUpdateData_ReimportOfSameFileDoesNotDuplicate()
        {
            var updater = new EnergyDataUpdater(_dataContext);
            var records = Enumerable.Range(0, 48)
                .Select(h => Record(new DateTime(2024, 1, 1).AddHours(h), h))
                .ToList();

            await updater.AddOrUpdateDataAsync(records.Select(x => Record(x.Date, x.TakenVolumeAfterBanancing)).ToList());
            await updater.AddOrUpdateDataAsync(records.Select(x => Record(x.Date, x.TakenVolumeAfterBanancing)).ToList());

            Assert.That(await _dataContext.ImportedRecords.CountAsync(), Is.EqualTo(48));
        }

        [Test]
        public async Task GetItemsByDateRanges_ReturnsRecordsOrderedByDate()
        {
            var updater = new EnergyDataUpdater(_dataContext);
            var extractor = new EnergyDataExtractor(_dataContext);
            var day = new DateTime(2024, 1, 1);

            // Second import inserts earlier hours after later ones.
            await updater.AddOrUpdateDataAsync([Record(day.AddHours(5), 5), Record(day.AddHours(6), 6)]);
            await updater.AddOrUpdateDataAsync([Record(day.AddHours(1), 1), Record(day.AddHours(5), 50)]);

            var result = await extractor.GetItemsByDateRangesAsync(day, day);

            Assert.That(result.Select(x => x.Date), Is.Ordered);
        }
    }
}
