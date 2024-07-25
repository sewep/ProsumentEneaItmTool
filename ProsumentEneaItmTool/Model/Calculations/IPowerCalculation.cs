using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.Calculations
{
    internal interface IPowerCalculation
    {
        List<CalculationEnergyResults> CalculationEnergyChart { get; }
        CalculationEnergyResults CalculationEnergyResults { get; }
        double FreeEnergyCoefficient { get; set; }

        void Calculate(List<ImportFileRecord> records);
    }
}