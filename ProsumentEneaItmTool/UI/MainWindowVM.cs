using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;

namespace ProsumentEneaItmTool.UI
{
    internal class MainWindowVM : ObservedObject
    {
        private readonly IFileEneaCsvLoader _fileEneaCsvLoader;
        private readonly IEnergyDataExtractor _energyDataExtractor;
        private readonly IEnergyDataUpdater _energyDataUpdater;

        private DateTime _dateFrom;
        private DateTime _dateTo;
        private List<ImportFileRecord> _records = [];
        private int _isBusyCounter = 0;

        public MainWindowVM(IFileEneaCsvLoader fileEneaCsvLoader, IEnergyDataExtractor energyDataExtractor, IEnergyDataUpdater energyDataUpdater)
        {
            _fileEneaCsvLoader = fileEneaCsvLoader;
            _energyDataExtractor = energyDataExtractor;
            _energyDataUpdater = energyDataUpdater;

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

        public double TotalConsumingBB => Records.Sum(x => x.TakenVolumeBeforeBanancing);
        public double TotalConsumingAB => Records.Sum(x => x.TakenVolumeAfterBanancing);
        public double TotalFedBB => Records.Sum(x => x.FedVolumeBeforeBanancing);
        public double TotalFedAB => Records.Sum(x => x.FedVolumeAfterBanancing);
        public double DiffBB => TotalFedBB - TotalConsumingBB;
        public double DiffAB => TotalFedAB - TotalConsumingAB;
        public double DiffBBNetto => TotalFedBB * 0.8 - TotalConsumingBB;
        public double DiffABNetto => TotalFedAB * 0.8 - TotalConsumingAB;

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

        public ICommand SetEntireEnteredTimePeriod => new RelayCommand(async (o) =>
        {
            await SelectFullDateRangesAsync();
        });

        public List<ImportFileRecord> Records
        {
            get => _records;
            set
            {
                _records = value;
                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(TotalConsumingBB),
                    nameof(TotalConsumingAB),
                    nameof(TotalFedBB),
                    nameof(TotalFedAB),
                    nameof(DiffBB),
                    nameof(DiffAB),
                    nameof(DiffBBNetto),
                    nameof(DiffABNetto));
            }
        }

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
