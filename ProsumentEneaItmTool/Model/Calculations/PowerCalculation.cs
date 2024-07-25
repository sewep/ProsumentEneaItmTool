using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.Calculations
{
    internal class PowerCalculation : IPowerCalculation
    {
        private readonly CalculationEnergyResults _calculationEnergyResults = new();
        private readonly List<CalculationEnergyResults> _calculationEnergyChart = [];

        public PowerCalculation()
        {
            
        }

        public double FreeEnergyCoefficient { get; set; } = 0.8;

        public CalculationEnergyResults CalculationEnergyResults => _calculationEnergyResults;
        public List<CalculationEnergyResults> CalculationEnergyChart => _calculationEnergyChart;

        public void Calculate(List<ImportFileRecord> records)
        {
            _calculationEnergyResults.ConsumedBeforeBalancing = records.Sum(x => x.TakenVolumeBeforeBanancing);
            _calculationEnergyResults.ConsumedAfterBalancing = records.Sum(x => x.TakenVolumeAfterBanancing);
            _calculationEnergyResults.FedBeforeBalancing = records.Sum(x => x.FedVolumeBeforeBanancing);
            _calculationEnergyResults.FedAfterBalancing = records.Sum(x => x.FedVolumeAfterBanancing);
            _calculationEnergyResults.DifferenceBeforeBalancing = _calculationEnergyResults.FedBeforeBalancing - _calculationEnergyResults.ConsumedBeforeBalancing;
            _calculationEnergyResults.DifferenceAfterBalancing = _calculationEnergyResults.FedAfterBalancing - _calculationEnergyResults.ConsumedAfterBalancing;
            _calculationEnergyResults.FreeToUseBeforeBalancing = _calculationEnergyResults.FedBeforeBalancing * FreeEnergyCoefficient - _calculationEnergyResults.ConsumedBeforeBalancing;
            _calculationEnergyResults.FreeToUseAfterBalancing = _calculationEnergyResults.FedAfterBalancing * FreeEnergyCoefficient - _calculationEnergyResults.ConsumedAfterBalancing;
        }
    }
}
