using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.NetBilling;
using ProsumentEneaItmTool.UI.Utils;

namespace ProsumentEneaItmTool.UI
{
    /// <summary>
    /// One month in the net-billing table; the RCEm price is entered by the user.
    /// </summary>
    internal class NetBillingMonthRow : ObservableObject
    {
        private readonly Func<DateTime, double?, Task> _rcemChanged;
        private double? _rcemPricePlnMwh;

        private NetBillingPeriod _period;

        public NetBillingMonthRow(NetBillingPeriod period, Func<DateTime, double?, Task> rcemChanged)
        {
            _period = period;
            _rcemPricePlnMwh = period.RcemPricePlnMwh;
            _rcemChanged = rcemChanged;
        }

        /// <summary>
        /// Calculated values of the month; replaced after a recalculation.
        /// </summary>
        public NetBillingPeriod Period
        {
            get => _period;
            set
            {
                _period = value;

                // All values below depend on the period.
                OnPropertyChanged(string.Empty);
            }
        }

        // Empty cells instead of zeros where prices are missing.
        public double? WeightedRcePlnMwh => Period.WeightedRcePlnMwh;

        public double? ValueRce => Period.FedWithRceKwh > 0 ? Period.ValueRce : null;

        public double? FedWithoutRceKwh => Period.FedWithoutRceKwh > 0.0005 ? Period.FedWithoutRceKwh : null;

        public double? ValueRcem => Period.FedWithRcemKwh > 0 ? Period.ValueRcem : null;

        public double? DifferenceRceMinusRcem => Period.FedWithBothKwh > 0 ? Period.DifferenceRceMinusRcem : null;

        public DateTime Month => Period.Time;

        public double? RcemPricePlnMwh
        {
            get => _rcemPricePlnMwh;
            set
            {
                if (value is < 0 or > 10000)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Cena RCEm w zł/MWh, od 0 do 10 000.");
                }

                if (SetProperty(ref _rcemPricePlnMwh, value))
                {
                    _ = _rcemChanged(Month, value);
                }
            }
        }
    }

    internal class NetBillingVM : ObservableObject
    {
        private readonly IMarketPriceStore _marketPriceStore;
        private readonly IRcePriceUpdater _rcePriceUpdater;
        private readonly IUserDialogs _userDialogs;

        private IReadOnlyList<ImportFileRecord> _records = [];
        private DateTime _dateFrom;
        private DateTime _dateTo;
        private Dictionary<DateTime, double> _rceByHour = [];
        private Dictionary<DateTime, double> _rcemByMonth = [];
        private double _depositCoefficient = NetBillingCalculator.DefaultDepositCoefficient;
        private NetBillingResult _result = new();
        private string _status = string.Empty;
        private bool _isDownloading;
        private int _updateRequestId;

        public NetBillingVM(IMarketPriceStore marketPriceStore, IRcePriceUpdater rcePriceUpdater, IUserDialogs userDialogs)
        {
            _marketPriceStore = marketPriceStore;
            _rcePriceUpdater = rcePriceUpdater;
            _userDialogs = userDialogs;

            DownloadRcePricesCommand = new AsyncRelayCommand(DownloadRcePricesAsync, () => !IsDownloading && _records.Count > 0);
        }

        public event EventHandler? ResultUpdated;

        public double DepositCoefficient
        {
            get => _depositCoefficient;
            set
            {
                // The binding shows the exception message as a validation error.
                if (value is < 0 or > 3)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Współczynnik musi być z zakresu od 0 do 3.");
                }

                if (SetProperty(ref _depositCoefficient, value))
                {
                    Recalculate();
                }
            }
        }

        /// <summary>
        /// Monthly values for the selected range.
        /// </summary>
        public NetBillingResult Result
        {
            get => _result;
            private set => SetProperty(ref _result, value);
        }

        public ObservableCollection<NetBillingMonthRow> Months { get; } = [];

        // Totals for the summary, empty where no prices are known.
        public double? TotalValueRce => Result.Total.FedWithRceKwh > 0 ? Result.Total.ValueRce : null;

        public double? TotalValueRcem => Result.Total.FedWithRcemKwh > 0 ? Result.Total.ValueRcem : null;

        public double? TotalDifferenceRceMinusRcem => Result.Total.FedWithBothKwh > 0 ? Result.Total.DifferenceRceMinusRcem : null;

        /// <summary>
        /// Fed energy without RCE price in days, for which PSE publishes prices (can be downloaded), kWh.
        /// </summary>
        public double FedWithoutRceDownloadableKwh { get; private set; }

        /// <summary>
        /// Fed energy before the first day with RCE prices in PSE, kWh.
        /// </summary>
        public double FedBeforeRceKwh { get; private set; }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            private set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    DownloadRcePricesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public IAsyncRelayCommand DownloadRcePricesCommand { get; }

        /// <summary>
        /// Takes the records of the selected range and loads stored prices for it.
        /// </summary>
        public async Task UpdateAsync(IReadOnlyList<ImportFileRecord> records, DateTime dateFrom, DateTime dateTo)
        {
            _records = records;
            _dateFrom = dateFrom.Date;
            _dateTo = dateTo.Date;
            DownloadRcePricesCommand.NotifyCanExecuteChanged();

            var requestId = ++_updateRequestId;
            try
            {
                var (rce, rcem) = await Task.Run(LoadPricesAsync);

                // A newer range has been selected in the meantime.
                if (requestId != _updateRequestId)
                {
                    return;
                }

                _rceByHour = rce;
                _rcemByMonth = rcem;
                Recalculate();
            }
            catch (Exception e)
            {
                _userDialogs.ShowError($"Nie udało się odczytać cen energii.\r\n\r\n{e.Message}");
            }
        }

        /// <summary>
        /// Values for the given aggregation with the loaded prices, e.g. for the export.
        /// </summary>
        public NetBillingResult CalculateFor(Aggregation aggregation)
        {
            return NetBillingCalculator.Calculate(_records, _rceByHour, _rcemByMonth, aggregation, DepositCoefficient);
        }

        private async Task<(Dictionary<DateTime, double> Rce, Dictionary<DateTime, double> Rcem)> LoadPricesAsync()
        {
            var rce = await _marketPriceStore.GetRcePricesAsync(_dateFrom, _dateTo.AddDays(1));
            var rcem = await _marketPriceStore.GetRcemPricesAsync();

            return (rce.ToDictionary(x => x.Hour, x => x.PricePlnMwh), rcem.ToDictionary(x => x.Month, x => x.PricePlnMwh));
        }

        private async Task DownloadRcePricesAsync()
        {
            IsDownloading = true;
            try
            {
                var progress = new Progress<string>(x => Status = x);
                var days = await Task.Run(() => _rcePriceUpdater.DownloadMissingAsync(_dateFrom, _dateTo, progress, CancellationToken.None));

                await UpdateAsync(_records, _dateFrom, _dateTo);

                if (days == 0 && FedWithoutRceDownloadableKwh > 0.5)
                {
                    _userDialogs.ShowInformation(
                        "Nie pobrano nowych cen. PSE publikuje ceny RCE od 14.06.2024, " +
                        "a ceny na kolejny dzień zwykle po godzinie 14.");
                }
            }
            catch (Exception e)
            {
                _userDialogs.ShowError($"Nie udało się pobrać cen RCE z PSE.\r\n\r\n{e.Message}");
                UpdateStatus();
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private async Task SetRcemPriceAsync(DateTime month, double? pricePlnMwh)
        {
            try
            {
                await _marketPriceStore.SetRcemPriceAsync(month, pricePlnMwh);

                if (pricePlnMwh is double price)
                {
                    _rcemByMonth[month] = price;
                }
                else
                {
                    _rcemByMonth.Remove(month);
                }

                Recalculate(rebuildRows: false);
            }
            catch (Exception e)
            {
                _userDialogs.ShowError($"Nie udało się zapisać ceny RCEm.\r\n\r\n{e.Message}");
            }
        }

        private void Recalculate(bool rebuildRows = true)
        {
            Result = CalculateFor(Aggregation.Month);

            // Rows are kept while the user edits RCEm, so the table does not jump.
            if (rebuildRows || Months.Count != Result.Periods.Count)
            {
                Months.Clear();
                foreach (var period in Result.Periods)
                {
                    Months.Add(new NetBillingMonthRow(period, SetRcemPriceAsync));
                }
            }
            else
            {
                for (var i = 0; i < Months.Count; i++)
                {
                    Months[i].Period = Result.Periods[i];
                }
            }

            CountEnergyWithoutRce();
            OnPropertyChanged(nameof(TotalValueRce));
            OnPropertyChanged(nameof(TotalValueRcem));
            OnPropertyChanged(nameof(TotalDifferenceRceMinusRcem));

            UpdateStatus();
            ResultUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void CountEnergyWithoutRce()
        {
            var downloadable = 0.0;
            var before = 0.0;

            foreach (var record in _records)
            {
                var hour = new DateTime(record.Date.Year, record.Date.Month, record.Date.Day, record.Date.Hour, 0, 0);
                if (_rceByHour.ContainsKey(hour))
                {
                    continue;
                }

                if (hour < PseRceClient.FirstAvailableDay)
                {
                    before += record.FedVolumeAfterBanancing;
                }
                else
                {
                    downloadable += record.FedVolumeAfterBanancing;
                }
            }

            FedWithoutRceDownloadableKwh = downloadable;
            FedBeforeRceKwh = before;
        }

        private void UpdateStatus()
        {
            var total = Result.Total;
            var before = FedBeforeRceKwh > 0.5
                ? $" Energia oddana przed 14.06.2024 ({FedBeforeRceKwh:N0} kWh) nie jest wyceniona, PSE nie publikuje wcześniejszych cen RCE."
                : string.Empty;

            if (total.FedKwh <= 0)
            {
                Status = "Brak energii oddanej w wybranym okresie.";
            }
            else if (FedWithoutRceDownloadableKwh > 0.5)
            {
                Status = $"Brak cen RCE dla {FedWithoutRceDownloadableKwh:N0} kWh energii oddanej – pobierz ceny z PSE.{before}";
            }
            else
            {
                Status = $"Ceny RCE są pobrane.{before}";
            }
        }
    }
}
