using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;
using ProsumentEneaItmTool.UI;
using ProsumentEneaItmTool.UI.Utils;

namespace ProsumentEneaItmTool.Tests.UI
{
    internal class MainWindowVMTests
    {
        private sealed class FakeLoader : IFileEneaCsvLoader
        {
            public ParseResult? Result { get; set; }
            public ParseResult? Load() => Result;
        }

        private sealed class FakeData : IEnergyDataExtractor, IEnergyDataUpdater
        {
            public List<ImportFileRecord> Stored { get; } = [];

            public Task<List<ImportFileRecord>> GetAllItemsAsync() => Task.FromResult(Stored.ToList());

            public Task<(DateTime, DateTime)> GetDateRangesAsync() =>
                Task.FromResult(Stored.Count == 0
                    ? (DateTime.Today, DateTime.Today)
                    : (Stored.Min(x => x.Date), Stored.Max(x => x.Date)));

            public Task<List<ImportFileRecord>> GetItemsByDateRangesAsync(DateTime dateFrom, DateTime dateTo) =>
                Task.FromResult(Stored.Where(x => x.Date.Date >= dateFrom.Date && x.Date.Date <= dateTo.Date).OrderBy(x => x.Date).ToList());

            public Task AddOrUpdateDataAsync(List<ImportFileRecord> records)
            {
                Stored.RemoveAll(x => records.Any(r => r.Date == x.Date));
                Stored.AddRange(records);
                return Task.CompletedTask;
            }

            public Task ClearAllDataAsync()
            {
                Stored.Clear();
                return Task.CompletedTask;
            }
        }

        private sealed class FakeDialogs : IUserDialogs
        {
            public bool ConfirmAnswer { get; set; }
            public List<string> Errors { get; } = [];
            public List<string> Warnings { get; } = [];
            public void ShowError(string message) => Errors.Add(message);
            public void ShowWarning(string message) => Warnings.Add(message);
            public bool Confirm(string message) => ConfirmAnswer;
        }

        private FakeLoader _loader = null!;
        private FakeData _data = null!;
        private FakeDialogs _dialogs = null!;

        [SetUp]
        public void SetUp()
        {
            _loader = new FakeLoader();
            _data = new FakeData();
            _dialogs = new FakeDialogs();
        }

        private async Task<MainWindowVM> CreateAsync()
        {
            var vm = new MainWindowVM(_loader, _data, _data, new PowerCalculation(), _dialogs);
            await vm.Initialization;
            return vm;
        }

        private static ImportFileRecord Record(DateTime date, double taken) => new()
        {
            Date = date,
            TakenVolumeAfterBanancing = taken,
            TakenVolumeBeforeBanancing = taken
        };

        [Test]
        public async Task Initialization_SelectsWholeStoredPeriod()
        {
            _data.Stored.Add(Record(new DateTime(2024, 1, 1, 10, 0, 0), 1));
            _data.Stored.Add(Record(new DateTime(2024, 3, 1, 10, 0, 0), 2));

            var vm = await CreateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(vm.DateFrom, Is.EqualTo(new DateTime(2024, 1, 1, 10, 0, 0)));
                Assert.That(vm.DateTo, Is.EqualTo(new DateTime(2024, 3, 1, 10, 0, 0)));
                Assert.That(vm.Records, Has.Count.EqualTo(2));
                Assert.That(vm.CalculationEnergyResults.ConsumedAfterBalancing, Is.EqualTo(3));
                Assert.That(vm.IsBusy, Is.False);
            });
        }

        [Test]
        public async Task ClearAllData_DoesNothingWhenNotConfirmed()
        {
            _data.Stored.Add(Record(new DateTime(2024, 1, 1), 1));
            var vm = await CreateAsync();
            _dialogs.ConfirmAnswer = false;

            await vm.ClearAllDataCommand.ExecuteAsync(null);

            Assert.That(_data.Stored, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ClearAllData_RemovesDataWhenConfirmed()
        {
            _data.Stored.Add(Record(new DateTime(2024, 1, 1), 1));
            var vm = await CreateAsync();
            _dialogs.ConfirmAnswer = true;

            await vm.ClearAllDataCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_data.Stored, Is.Empty);
                Assert.That(vm.Records, Is.Empty);
            });
        }

        [Test]
        public async Task LoadFromFile_StoresRecordsAndShowsWarnings()
        {
            var vm = await CreateAsync();
            _loader.Result = new ParseResult([Record(new DateTime(2024, 5, 1, 1, 0, 0), 4)], ["zły wiersz"]);

            await vm.LoadFromFileCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_data.Stored, Has.Count.EqualTo(1));
                Assert.That(vm.Records, Has.Count.EqualTo(1));
                Assert.That(_dialogs.Warnings, Has.Count.EqualTo(1));
                Assert.That(_dialogs.Warnings[0], Does.Contain("zły wiersz"));
                Assert.That(_dialogs.Errors, Is.Empty);
            });
        }

        [Test]
        public async Task LoadFromFile_CancelledDialogDoesNothing()
        {
            var vm = await CreateAsync();
            _loader.Result = null;

            await vm.LoadFromFileCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_data.Stored, Is.Empty);
                Assert.That(_dialogs.Warnings, Is.Empty);
                Assert.That(_dialogs.Errors, Is.Empty);
            });
        }

        [Test]
        public async Task DateFrom_AfterDateTo_MovesDateTo()
        {
            var vm = await CreateAsync();
            vm.DateTo = new DateTime(2024, 1, 10);

            vm.DateFrom = new DateTime(2024, 2, 1);

            Assert.That(vm.DateTo, Is.EqualTo(new DateTime(2024, 2, 1)));
        }

        [Test]
        public async Task DateTo_BeforeDateFrom_MovesDateFrom()
        {
            var vm = await CreateAsync();
            vm.DateFrom = new DateTime(2024, 2, 1);

            vm.DateTo = new DateTime(2024, 1, 10);

            Assert.That(vm.DateFrom, Is.EqualTo(new DateTime(2024, 1, 10)));
        }

        [TestCase(-0.1)]
        [TestCase(1.1)]
        public async Task FreeEnergyCoefficient_OutOfRangeIsRejected(double value)
        {
            var vm = await CreateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(() => vm.FreeEnergyCoefficient = value, Throws.InstanceOf<ArgumentOutOfRangeException>());
                Assert.That(vm.FreeEnergyCoefficient, Is.EqualTo(0.8));
            });
        }

        [Test]
        public async Task FreeEnergyCoefficient_RecalculatesResults()
        {
            _data.Stored.Add(new ImportFileRecord { Date = new DateTime(2024, 1, 1), FedVolumeAfterBanancing = 10 });
            var vm = await CreateAsync();

            vm.FreeEnergyCoefficient = 0.7;

            Assert.That(vm.CalculationEnergyResults.FreeToUseAfterBalancing, Is.EqualTo(7).Within(1e-9));
        }
    }
}
