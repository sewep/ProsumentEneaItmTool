using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;
using ProsumentEneaItmTool.UI.Utils;

namespace ProsumentEneaItmTool.UI
{
    internal class MainWindowVM : ObservableObject
    {
        private const int MaxWarningRowsShown = 20;

        private readonly IFileEneaCsvLoader _fileEneaCsvLoader;
        private readonly IEnergyDataExtractor _energyDataExtractor;
        private readonly IEnergyDataUpdater _energyDataUpdater;
        private readonly IPowerCalculation _powerCalculation;
        private readonly IUserDialogs _userDialogs;

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
                            IPowerCalculation powerCalculation,
                            IUserDialogs userDialogs)
        {
            _fileEneaCsvLoader = fileEneaCsvLoader;
            _energyDataExtractor = energyDataExtractor;
            _energyDataUpdater = energyDataUpdater;
            _powerCalculation = powerCalculation;
            _userDialogs = userDialogs;

            LoadFromFileCommand = new AsyncRelayCommand(LoadFromFileAsync, () => !IsBusy);
            ClearAllDataCommand = new AsyncRelayCommand(ClearAllDataAsync, () => !IsBusy);
            SetLastYearCommand = new AsyncRelayCommand(() => RunAsync(SetLastYearAsync), () => !IsBusy);
            SetEntireEnteredTimePeriodCommand = new AsyncRelayCommand(() => RunAsync(SelectFullDateRangesAsync), () => !IsBusy);

            Initialization = RunAsync(SelectFullDateRangesAsync);
        }

        /// <summary>
        /// Loading of the initial data started in the constructor.
        /// </summary>
        public Task Initialization { get; }

        public DateTime DateFrom
        {
            get => _dateFrom;
            set
            {
                if (!SetProperty(ref _dateFrom, value))
                {
                    return;
                }

                if (_dateTo < _dateFrom)
                {
                    SetProperty(ref _dateTo, _dateFrom, nameof(DateTo));
                }

                _ = RunAsync(UseSelectedRangeAsync);
            }
        }

        public DateTime DateTo
        {
            get => _dateTo;
            set
            {
                if (!SetProperty(ref _dateTo, value))
                {
                    return;
                }

                if (_dateFrom > _dateTo)
                {
                    SetProperty(ref _dateFrom, _dateTo, nameof(DateFrom));
                }

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
                // The binding shows the exception message as a validation error.
                if (value is < 0 or > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Współczynnik musi być z zakresu od 0 do 1.");
                }

                if (_powerCalculation.FreeEnergyCoefficient == value)
                {
                    return;
                }

                _powerCalculation.FreeEnergyCoefficient = value;
                OnPropertyChanged();
                Recalculate();
            }
        }

        public IPowerCalculation PowerCalculation => _powerCalculation;

        public List<ImportFileRecord> Records
        {
            get => _records;
            private set
            {
                _records = value;
                OnPropertyChanged();
                Recalculate();
            }
        }

        public IAsyncRelayCommand LoadFromFileCommand { get; }

        public IAsyncRelayCommand ClearAllDataCommand { get; }

        public IAsyncRelayCommand SetLastYearCommand { get; }

        public IAsyncRelayCommand SetEntireEnteredTimePeriodCommand { get; }

        private async Task LoadFromFileAsync()
        {
            ParseResult? parseResult;
            try
            {
                parseResult = _fileEneaCsvLoader.Load();
            }
            catch (Exception e)
            {
                _userDialogs.ShowError($"Nie udało się wczytać pliku.\r\n\r\n{e.Message}");
                return;
            }

            if (parseResult is null)
            {
                return;
            }

            if (parseResult.Warnings.Count > 0)
            {
                ShowParseWarnings(parseResult);
            }

            if (parseResult.Records.Count == 0)
            {
                return;
            }

            await RunAsync(async () =>
            {
                await RunDataBaseAsync(() => _energyDataUpdater.AddOrUpdateDataAsync(parseResult.Records.ToList()));
                await SelectFullDateRangesAsync();
            });
        }

        private async Task ClearAllDataAsync()
        {
            if (!_userDialogs.Confirm("Czy na pewno usunąć wszystkie zaimportowane dane?\r\n\r\nTej operacji nie można cofnąć."))
            {
                return;
            }

            await RunAsync(async () =>
            {
                await RunDataBaseAsync(_energyDataUpdater.ClearAllDataAsync);
                await SelectFullDateRangesAsync();
            });
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
            SetProperty(ref _dateFrom, dateFrom, nameof(DateFrom));
            SetProperty(ref _dateTo, dateTo, nameof(DateTo));
        }

        private void Recalculate()
        {
            _powerCalculation.Calculate(_records);
            OnPropertyChanged(nameof(CalculationEnergyResults));
        }

        private void ShowParseWarnings(ParseResult parseResult)
        {
            var rows = string.Join("\r\n", parseResult.Warnings.Take(MaxWarningRowsShown));
            var more = parseResult.Warnings.Count > MaxWarningRowsShown
                ? $"\r\n… i {parseResult.Warnings.Count - MaxWarningRowsShown} więcej."
                : string.Empty;

            _userDialogs.ShowWarning(
                $"Nie udało się odczytać {parseResult.Warnings.Count} wierszy, zostaną pominięte. " +
                $"Poprawnie odczytano {parseResult.Records.Count} rekordów.\r\n\r\n{rows}{more}");
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
                _userDialogs.ShowError(e.ToString());
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

            LoadFromFileCommand?.NotifyCanExecuteChanged();
            ClearAllDataCommand?.NotifyCanExecuteChanged();
            SetLastYearCommand?.NotifyCanExecuteChanged();
            SetEntireEnteredTimePeriodCommand?.NotifyCanExecuteChanged();
        }
    }
}
