using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.Calculations
{
    public interface IPowerCalculation
    {
        /// <summary>
        /// Values summed up from the beginning of the range, one point per period.
        /// </summary>
        List<CalculationEnergyResults> CalculationEnergyChart { get; }

        /// <summary>
        /// Values of each period separately.
        /// </summary>
        List<CalculationEnergyResults> PeriodResults { get; }

        /// <summary>
        /// Totals for the whole range.
        /// </summary>
        CalculationEnergyResults CalculationEnergyResults { get; }

        double FreeEnergyCoefficient { get; set; }

        Aggregation Aggregation { get; set; }

        event EventHandler? CalculationUpdated;

        void Calculate(List<ImportFileRecord> records);
    }
}
