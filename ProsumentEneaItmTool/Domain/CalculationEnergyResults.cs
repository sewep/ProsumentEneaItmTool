namespace ProsumentEneaItmTool.Domain
{
    public class CalculationEnergyResults
    {
        public DateTime Time { get; set; }
        public double ConsumedBeforeBalancing { get; set; }
        public double ConsumedAfterBalancing { get; set; }
        public double FedBeforeBalancing { get; set; }
        public double FedAfterBalancing { get; set; }
        public double DifferenceBeforeBalancing { get; set; }
        public double DifferenceAfterBalancing { get; set; }
        public double FreeToUseBeforeBalancing { get; set; }
        public double FreeToUseAfterBalancing { get; set; }
    }
}
