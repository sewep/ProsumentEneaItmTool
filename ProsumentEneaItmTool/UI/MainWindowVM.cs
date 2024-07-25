using System.Diagnostics;
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

        private DateTime _dateFrom;
        private DateTime _dateTo;
        private List<ImportFileRecord> _records = [];
        private int _isBusyCounter = 0;

        public MainWindowVM(IFileEneaCsvLoader fileEneaCsvLoader,
                            IEnergyDataExtractor energyDataExtractor,
                            IEnergyDataUpdater energyDataUpdater,
                            IPowerCalculation powerCalculation)
        {
            _fileEneaCsvLoader = fileEneaCsvLoader;
            _energyDataExtractor = energyDataExtractor;
            _energyDataUpdater = energyDataUpdater;
            _powerCalculation = powerCalculation;

            _ = SelectFullDateRangesAsync();
        }

        public DateTime DateFrom
        {
            get => _dateFrom;
            set
            {
                _dateFrom = value;
                OnPropertyChanged();
                _ = UseSelectedRangeAsync();
            }
        }
        public DateTime DateTo
        {
            get => _dateTo;
            set
            {
                _dateTo = value;
                OnPropertyChanged();
                _ = UseSelectedRangeAsync();
            }
        }

        public bool IsBusy
        {
            get => _isBusyCounter > 0;
            set
            {
                if (value)
                {
                    _isBusyCounter++;
                    Debug.WriteLine($"Busy increase {_isBusyCounter}");
                }
                else
                {
                    _isBusyCounter--;
                    Debug.WriteLine($"Busy decrease {_isBusyCounter}");
                }

                OnPropertyChanged();
            }
        }

        public CalculationEnergyResults CalculationEnergyResults => _powerCalculation.CalculationEnergyResults;

        public List<ImportFileRecord> Records
        {
            get => _records;
            set
            {
                _records = value;
                _powerCalculation.Calculate(_records);
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationEnergyResults));
            }
        }

        public ICommand LoadFromFile => new RelayCommand(async (o) =>
        {
            try
            {
                var list = _fileEneaCsvLoader.Load().ToList();

                IsBusy = true;
                await Task.Yield();

                await Task.Run(async () =>
                {
                    await _energyDataUpdater.AddOrUpdateDataAsync(list);
                });

                IsBusy = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await SelectFullDateRangesAsync();
        });

        public ICommand ClearAllData => new RelayCommand(async (o) =>
        {
            IsBusy = true;
            await Task.Run(async () =>
            {
                await _energyDataUpdater.ClearAllDataAsync();
                await SelectFullDateRangesAsync();
            });
            IsBusy = false;
        });

        public ICommand SetEntireEnteredTimePeriod => new RelayCommand(async (o) =>
        {
            await SelectFullDateRangesAsync();
        });

        private async Task SelectFullDateRangesAsync()
        {
            IsBusy = true;
            await Task.Run(async () =>
            {
                (DateFrom, DateTo) = await _energyDataExtractor.GetDateRangesAsync();
                await UseSelectedRangeAsync();
            });
            IsBusy = false;
        }

        private async Task UseSelectedRangeAsync()
        {
            IsBusy = true;
            Records = await _energyDataExtractor.GetItemsByDateRangesAsync(DateFrom, DateTo);
            IsBusy = false;
        }
    }
}
