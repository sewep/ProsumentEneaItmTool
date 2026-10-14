using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;
using ProsumentEneaItmTool.Model.DataBase;

namespace ProsumentEneaItmTool.Model.NetBilling
{
    internal interface IMarketPriceStore
    {
        Task<List<RcePrice>> GetRcePricesAsync(DateTime from, DateTime to);

        /// <summary>
        /// Days (date part) in the range which have at least one stored RCE price.
        /// </summary>
        Task<HashSet<DateTime>> GetDaysWithRcePricesAsync(DateTime dayFrom, DateTime dayTo);

        /// <summary>
        /// Adds prices, replacing the stored ones for the same hours.
        /// </summary>
        Task SaveRcePricesAsync(IReadOnlyCollection<RcePrice> prices);

        Task<List<RcemPrice>> GetRcemPricesAsync();

        /// <summary>
        /// Sets the RCEm price of a month; null removes it.
        /// </summary>
        Task SetRcemPriceAsync(DateTime month, double? pricePlnMwh);
    }

    internal class MarketPriceStore : IMarketPriceStore
    {
        private readonly IDbContextFactory<DataContext> _dataContextFactory;

        public MarketPriceStore(IDbContextFactory<DataContext> dataContextFactory)
        {
            _dataContextFactory = dataContextFactory;
        }

        public async Task<List<RcePrice>> GetRcePricesAsync(DateTime from, DateTime to)
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            return await dataContext.RcePrices
                .AsNoTracking()
                .Where(x => x.Hour >= from && x.Hour <= to)
                .OrderBy(x => x.Hour)
                .ToListAsync();
        }

        public async Task<HashSet<DateTime>> GetDaysWithRcePricesAsync(DateTime dayFrom, DateTime dayTo)
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            var hours = await dataContext.RcePrices
                .AsNoTracking()
                .Where(x => x.Hour >= dayFrom.Date && x.Hour < dayTo.Date.AddDays(1))
                .Select(x => x.Hour)
                .ToListAsync();

            return hours.Select(x => x.Date).ToHashSet();
        }

        public async Task SaveRcePricesAsync(IReadOnlyCollection<RcePrice> prices)
        {
            if (prices.Count == 0)
            {
                return;
            }

            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();
            await using var transaction = await dataContext.Database.BeginTransactionAsync();

            var hours = prices.Select(x => x.Hour).ToList();
            await dataContext.RcePrices.Where(x => hours.Contains(x.Hour)).ExecuteDeleteAsync();

            await dataContext.RcePrices.AddRangeAsync(prices.Select(x => new RcePrice { Hour = x.Hour, PricePlnMwh = x.PricePlnMwh }));
            await dataContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        public async Task<List<RcemPrice>> GetRcemPricesAsync()
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            return await dataContext.RcemPrices.AsNoTracking().OrderBy(x => x.Month).ToListAsync();
        }

        public async Task SetRcemPriceAsync(DateTime month, double? pricePlnMwh)
        {
            month = new DateTime(month.Year, month.Month, 1);

            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            await dataContext.RcemPrices.Where(x => x.Month == month).ExecuteDeleteAsync();

            if (pricePlnMwh is double price)
            {
                dataContext.RcemPrices.Add(new RcemPrice { Month = month, PricePlnMwh = price });
                await dataContext.SaveChangesAsync();
            }
        }
    }
}
