using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.Calculations
{
    public interface IPowerCalculation
    {
        List<CalculationEnergyResults> CalculationEnergyChart { get; }
        CalculationEnergyResults CalculationEnergyResults { get; }
        double FreeEnergyCoefficient { get; set; }

        event EventHandler? CalculationUpdated;

        void Calculate(List<ImportFileRecord> records);
    }
}