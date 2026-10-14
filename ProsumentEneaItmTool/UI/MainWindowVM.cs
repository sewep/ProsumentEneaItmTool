using System.Windows;
using System.Windows.Input;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;

namespace ProsumentEneaItmTool.UI
{
    internal class MainWindowVM : ObservedObject
    {
        private readonly IFileEneaCsvLoader _fileEneaCsvLoader;
        private readonly IEnergyDataExtractor _energyDataExtractor;
        private readonly IEnergyDataUpdater _energyDataUpdater;
        private readonly IPowerCalculation _powerCalculation;

        // Database operations run one at a time, so an import is finished before the data is reloaded.
        private readonly SemaphoreSlim _dataBaseLock = new(1, 1);

        private DateTime _dateFrom;
        private DateTime _dateTo;
        private List<ImportFileRecord> _records = [];
        private int _isBusyCounter = 0;
        private int _recordsRequestId = 0;

        public MainWindowVM(IFileEneaCsvLoader fileEneaCsvLoader,
                            IEnergyDataExtractor energyDataExtractor,
                            IEnergyDataUpdater energyDataUpdater,
                            IPowerCalculation powerCalculation)
        {
            _fileEneaCsvLoader = fileEneaCsvLoader;
            _energyDataExtractor = energyDataExtractor;
            _energyDataUpdater = energyDataUpdater;
            _powerCalculation = powerCalculation;

            LoadFromFile = new RelayCommand(async _ => await LoadFromFileAsync(), _ => !IsBusy);
            ClearAllData = new RelayCommand(async _ => await RunAsync(ClearAllDataAsync), _ => !IsBusy);
            SetLastYear = new RelayCommand(async _ => await RunAsync(SetLastYearAsync), _ => !IsBusy);
            SetEntireEnteredTimePeriod = new RelayCommand(async _ => await RunAsync(SelectFullDateRangesAsync), _ => !IsBusy);

            _ = RunAsync(SelectFullDateRangesAsync);
        }

        public DateTime DateFrom
        {
            get => _dateFrom;
            set
            {
                _dateFrom = value;
                OnPropertyChanged();
                _ = RunAsync(UseSelectedRangeAsync);
            }
        }
        public DateTime DateTo
        {
            get => _dateTo;
            set
            {
                _dateTo = value;
                OnPropertyChanged();
                _ = RunAsync(UseSelectedRangeAsync);
            }
        }

        public bool IsBusy => _isBusyCounter > 0;

        public CalculationEnergyResults CalculationEnergyResults => _powerCalculation.CalculationEnergyResults;

        public double FreeEnergyCoefficient
        {
            get => _powerCalculation.FreeEnergyCoefficient;
            set
            {
                _powerCalculation.FreeEnergyCoefficient = value;
                OnPropertyChanged();
                Recalculate();
            }
        }

        public IPowerCalculation PowerCalculation => _powerCalculation;

        public List<ImportFileRecord> Records
        {
            get => _records;
            set
            {
                _records = value;
                OnPropertyChanged();
                Recalculate();
            }
        }

        public ICommand LoadFromFile { get; }

        public ICommand ClearAllData { get; }

        public ICommand SetLastYear { get; }

        public ICommand SetEntireEnteredTimePeriod { get; }

        private async Task LoadFromFileAsync()
        {
            List<ImportFileRecord> list;
            try
            {
                list = _fileEneaCsvLoader.Load().ToList();
            }
            catch (Exception e)
            {
                ShowError(e);
                return;
            }

            if (list.Count == 0)
            {
                return;
            }

            await RunAsync(async () =>
            {
                await RunDataBaseAsync(() => _energyDataUpdater.AddOrUpdateDataAsync(list));
                await SelectFullDateRangesAsync();
            });
        }

        private async Task ClearAllDataAsync()
        {
            await RunDataBaseAsync(_energyDataUpdater.ClearAllDataAsync);
            await SelectFullDateRangesAsync();
        }

        private async Task SetLastYearAsync()
        {
            var (_, dateTo) = await RunDataBaseAsync(_energyDataExtractor.GetDateRangesAsync);
            SetDateRange(dateTo.AddYears(-1), dateTo);
            await UseSelectedRangeAsync();
        }

        private async Task SelectFullDateRangesAsync()
        {
            var (dateFrom, dateTo) = await RunDataBaseAsync(_energyDataExtractor.GetDateRangesAsync);
            SetDateRange(dateFrom, dateTo);
            await UseSelectedRangeAsync();
        }

        private async Task UseSelectedRangeAsync()
        {
            var requestId = ++_recordsRequestId;
            var records = await RunDataBaseAsync(() => _energyDataExtractor.GetItemsByDateRangesAsync(DateFrom, DateTo));

            // A newer request has been started in the meantime, its result wins.
            if (requestId == _recordsRequestId)
            {
                Records = records;
            }
        }

        /// <summary>
        /// Sets both dates without triggering a separate reload for each of them.
        /// </summary>
        private void SetDateRange(DateTime dateFrom, DateTime dateTo)
        {
            _dateFrom = dateFrom;
            _dateTo = dateTo;
            OnPropertyChanged(nameof(DateFrom), nameof(DateTo));
        }

        private void Recalculate()
        {
            _powerCalculation.Calculate(_records);
            OnPropertyChanged(nameof(CalculationEnergyResults));
        }

        /// <summary>
        /// Runs a database operation off the UI thread, one at a time.
        /// </summary>
        private async Task<T> RunDataBaseAsync<T>(Func<Task<T>> operation)
        {
            await _dataBaseLock.WaitAsync();
            try
            {
                return await Task.Run(operation);
            }
            finally
            {
                _dataBaseLock.Release();
            }
        }

        private Task RunDataBaseAsync(Func<Task> operation)
        {
            return RunDataBaseAsync(async () =>
            {
                await operation();
                return true;
            });
        }

        /// <summary>
        /// Shows the busy indicator for the time of the action and reports its errors.
        /// </summary>
        private async Task RunAsync(Func<Task> action)
        {
            SetBusy(true);
            try
            {
                await action();
            }
            catch (Exception e)
            {
                ShowError(e);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _isBusyCounter += busy ? 1 : -1;
            OnPropertyChanged(nameof(IsBusy));
            CommandManager.InvalidateRequerySuggested();
        }

        private static void ShowError(Exception e)
        {
            MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
