using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.Calculations
{
    internal class PowerCalculation : IPowerCalculation
    {
        private readonly CalculationEnergyResults _calculationEnergyResults = new();
        private readonly List<CalculationEnergyResults> _calculationEnergyChart = [];
        private readonly List<CalculationEnergyResults> _periodResults = [];

        public event EventHandler? CalculationUpdated;

        public double FreeEnergyCoefficient { get; set; } = 0.8;

        public Aggregation Aggregation { get; set; } = Aggregation.Hour;

        public CalculationEnergyResults CalculationEnergyResults => _calculationEnergyResults;
        public List<CalculationEnergyResults> CalculationEnergyChart => _calculationEnergyChart;
        public List<CalculationEnergyResults> PeriodResults => _periodResults;

        public void Calculate(List<ImportFileRecord> records)
        {
            _calculationEnergyResults.Time = DateTime.Now;
            _calculationEnergyResults.ConsumedBeforeBalancing = records.Sum(x => x.TakenVolumeBeforeBanancing);
            _calculationEnergyResults.ConsumedAfterBalancing = records.Sum(x => x.TakenVolumeAfterBanancing);
            _calculationEnergyResults.FedBeforeBalancing = records.Sum(x => x.FedVolumeBeforeBanancing);
            _calculationEnergyResults.FedAfterBalancing = records.Sum(x => x.FedVolumeAfterBanancing);
            CalculateDerivedValues(_calculationEnergyResults);

            _periodResults.Clear();
            _periodResults.AddRange(AggregateToPeriods(records));

            _calculationEnergyChart.Clear();
            CalculationEnergyResults? lastAdded = null;

            foreach (var period in _periodResults)
            {
                var pos = new CalculationEnergyResults()
                {
                    Time = period.Time,
                    ConsumedBeforeBalancing = period.ConsumedBeforeBalancing + (lastAdded?.ConsumedBeforeBalancing ?? 0),
                    ConsumedAfterBalancing = period.ConsumedAfterBalancing + (lastAdded?.ConsumedAfterBalancing ?? 0),
                    FedBeforeBalancing = period.FedBeforeBalancing + (lastAdded?.FedBeforeBalancing ?? 0),
                    FedAfterBalancing = period.FedAfterBalancing + (lastAdded?.FedAfterBalancing ?? 0)
                };
                CalculateDerivedValues(pos);

                _calculationEnergyChart.Add(pos);

                lastAdded = pos;
            }

            CalculationUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Start of the period the given time belongs to.
        /// </summary>
        public static DateTime PeriodStart(DateTime time, Aggregation aggregation) => aggregation switch
        {
            Aggregation.Day => time.Date,
            Aggregation.Month => new DateTime(time.Year, time.Month, 1),
            _ => time
        };

        private IEnumerable<CalculationEnergyResults> AggregateToPeriods(List<ImportFileRecord> records)
        {
            // Records come ordered by date. Hours are not grouped, so a repeated hour (DST change) stays two points.
            if (Aggregation == Aggregation.Hour)
            {
                return records.Select(x => CreatePeriod(x.Date, [x]));
            }

            return records
                .GroupBy(x => PeriodStart(x.Date, Aggregation))
                .OrderBy(x => x.Key)
                .Select(x => CreatePeriod(x.Key, x.ToList()));
        }

        private CalculationEnergyResults CreatePeriod(DateTime time, IReadOnlyCollection<ImportFileRecord> records)
        {
            var period = new CalculationEnergyResults()
            {
                Time = time,
                ConsumedBeforeBalancing = records.Sum(x => x.TakenVolumeBeforeBanancing),
                ConsumedAfterBalancing = records.Sum(x => x.TakenVolumeAfterBanancing),
                FedBeforeBalancing = records.Sum(x => x.FedVolumeBeforeBanancing),
                FedAfterBalancing = records.Sum(x => x.FedVolumeAfterBanancing)
            };
            CalculateDerivedValues(period);
            return period;
        }

        private void CalculateDerivedValues(CalculationEnergyResults results)
        {
            results.DifferenceBeforeBalancing = results.FedBeforeBalancing - results.ConsumedBeforeBalancing;
            results.DifferenceAfterBalancing = results.FedAfterBalancing - results.ConsumedAfterBalancing;
            results.FreeToUseBeforeBalancing = results.FedBeforeBalancing * FreeEnergyCoefficient - results.ConsumedBeforeBalancing;
            results.FreeToUseAfterBalancing = results.FedAfterBalancing * FreeEnergyCoefficient - results.ConsumedAfterBalancing;
        }
    }
}
