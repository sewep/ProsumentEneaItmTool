using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.Calculations
{
    internal class PowerCalculation : IPowerCalculation
    {
        private readonly CalculationEnergyResults _calculationEnergyResults = new();
        private readonly List<CalculationEnergyResults> _calculationEnergyChart = [];

        public event EventHandler? CalculationUpdated;

        public PowerCalculation()
        {
            
        }

        public double FreeEnergyCoefficient { get; set; } = 0.8;

        public CalculationEnergyResults CalculationEnergyResults => _calculationEnergyResults;
        public List<CalculationEnergyResults> CalculationEnergyChart => _calculationEnergyChart;

        public void Calculate(List<ImportFileRecord> records)
        {
            _calculationEnergyResults.Time = DateTime.Now;
            _calculationEnergyResults.ConsumedBeforeBalancing = records.Sum(x => x.TakenVolumeBeforeBanancing);
            _calculationEnergyResults.ConsumedAfterBalancing = records.Sum(x => x.TakenVolumeAfterBanancing);
            _calculationEnergyResults.FedBeforeBalancing = records.Sum(x => x.FedVolumeBeforeBanancing);
            _calculationEnergyResults.FedAfterBalancing = records.Sum(x => x.FedVolumeAfterBanancing);
            _calculationEnergyResults.DifferenceBeforeBalancing = _calculationEnergyResults.FedBeforeBalancing - _calculationEnergyResults.ConsumedBeforeBalancing;
            _calculationEnergyResults.DifferenceAfterBalancing = _calculationEnergyResults.FedAfterBalancing - _calculationEnergyResults.ConsumedAfterBalancing;
            _calculationEnergyResults.FreeToUseBeforeBalancing = _calculationEnergyResults.FedBeforeBalancing * FreeEnergyCoefficient - _calculationEnergyResults.ConsumedBeforeBalancing;
            _calculationEnergyResults.FreeToUseAfterBalancing = _calculationEnergyResults.FedAfterBalancing * FreeEnergyCoefficient - _calculationEnergyResults.ConsumedAfterBalancing;

            _calculationEnergyChart.Clear();
            CalculationEnergyResults? lastAdded = null;

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];

                var pos = new CalculationEnergyResults()
                {
                    Time = record.Date,
                    ConsumedBeforeBalancing = record.TakenVolumeBeforeBanancing + lastAdded?.ConsumedBeforeBalancing ?? 0,
                    ConsumedAfterBalancing = record.TakenVolumeAfterBanancing + lastAdded?.ConsumedAfterBalancing ?? 0,
                    FedBeforeBalancing = record.FedVolumeBeforeBanancing + lastAdded?.FedBeforeBalancing ?? 0,
                    FedAfterBalancing = record.FedVolumeAfterBanancing + lastAdded?.FedAfterBalancing ?? 0
                };
                pos.DifferenceBeforeBalancing = pos.FedBeforeBalancing - pos.ConsumedBeforeBalancing;
                pos.DifferenceAfterBalancing = pos.FedAfterBalancing - pos.ConsumedAfterBalancing;
                pos.FreeToUseBeforeBalancing = pos.FedBeforeBalancing * FreeEnergyCoefficient - pos.ConsumedBeforeBalancing;
                pos.FreeToUseAfterBalancing = pos.FedAfterBalancing * FreeEnergyCoefficient - pos.ConsumedAfterBalancing;

                _calculationEnergyChart.Add(pos);

                lastAdded = pos;
            }

            CalculationUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
