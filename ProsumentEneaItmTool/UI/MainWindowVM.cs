using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;

namespace ProsumentEneaItmTool.UI
{
    internal class MainWindowVM : ObservedObject
    {
        private readonly DataContext _dataContext;
        private readonly IFileEneaCsvLoader _fileEneaCsvLoader;

        private DateTime _dateFrom;
        private DateTime _dateTo;
        private List<ImportFileRecord> _records = [];
        private int _isBusyCounter = 0;

        public MainWindowVM(DataContext dataContext, IFileEneaCsvLoader fileEneaCsvLoader)
        {
            _dataContext = dataContext;
            _fileEneaCsvLoader = fileEneaCsvLoader;

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
                    await AddOrUpdateDataAsync(list);
                    await _dataContext.SaveChangesAsync();
                });

                IsBusy = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await SelectFullDateRangesAsync();
        });

        private async Task AddOrUpdateDataAsync(List<ImportFileRecord> list)
        {

            list.ForEach(async x =>
            {
                var toRemove = await _dataContext.ImportedRecords.Where(y => y.Date.Equals(x.Date)).ToListAsync();
                _dataContext.ImportedRecords.RemoveRange(toRemove);
            });

            await _dataContext.ImportedRecords.AddRangeAsync(list);
        }

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
                if (!await _dataContext.ImportedRecords.AnyAsync())
                {
                    DateFrom = DateTo = DateTime.Now;
                    IsBusy = false;
                    return;
                }

                DateFrom = _dataContext.ImportedRecords.Min(x => x.Date);
                DateTo = _dataContext.ImportedRecords.Max(x => x.Date);

                await UseSelectedRangeAsync();
            });
            IsBusy = false;
        }

        private async Task UseSelectedRangeAsync()
        {
            IsBusy = true;
            Records = await _dataContext.ImportedRecords
                .Where(x => x.Date >= DateFrom && x.Date <= DateTo)
                .ToListAsync();
            IsBusy = false;
        }
    }
}
