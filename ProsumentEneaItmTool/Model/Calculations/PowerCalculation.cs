using ProsumentEneaItmTool.Model.DataBase;

namespace ProsumentEneaItmTool.Model.Calculations
{
    internal class PowerCalculation
    {
        private readonly DataContext _dataContext;

        public PowerCalculation(DataContext dataContext)
        {
            _dataContext = dataContext;
        }
    }
}
