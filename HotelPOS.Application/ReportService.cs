using HotelPOS.Application.Interface;
using HotelPOS.Domain.Interface;

namespace HotelPOS.Application
{
    public class ReportService : IReportService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IItemRepository _itemRepo;
        private readonly ICategoryRepository _categoryRepo;

        public ReportService(IOrderRepository orderRepo, IItemRepository itemRepo, ICategoryRepository categoryRepo)
        {
            _orderRepo = orderRepo;
            _itemRepo = itemRepo;
            _categoryRepo = categoryRepo;
        }

        public async Task<SalesReportDto> GetSalesReportAsync(
            DateTime? from = null, DateTime? to = null)
        {
            // Standardize bounds to UTC for repository query
            var utcFrom = from?.ToUniversalTime();
            var utcTo = to?.ToUniversalTime();

            // Fetch only relevant orders from database (latest 500 for the dashboard summary)
            var (orders, totalCount) = await _orderRepo.GetPagedWithItemsAsync(1, 500, utcFrom, utcTo);

            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var count = orders.Count;
            var avg = count > 0 ? Math.Round(totalRevenue / count, 2) : 0m;

            var mostPopular = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ItemName)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .FirstOrDefault()?.Key ?? "N/A";

            var byTable = orders
                .GroupBy(o => o.TableNumber)
                .Select(g => new TableSalesRowDto
                {
                    TableNumber = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(t => t.TableNumber)
                .ToList();
            for (int i = 0; i < byTable.Count; i++) byTable[i].SNo = i + 1;

            var recent = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .Select((o, idx) => new RecentOrderRowDto
                {
                    SNo = idx + 1,
                    OrderId = o.Id,
                    TableNumber = o.TableNumber,
                    CreatedAt = o.CreatedAt.ToLocalTime(),
                    Total = o.TotalAmount,
                    DiscountAmount = o.DiscountAmount,
                    ItemCount = o.Items.Count,
                    PaymentMode = string.IsNullOrWhiteSpace(o.PaymentMode) ? "Cash" : o.PaymentMode,
                    CustomerName = o.CustomerName,
                    CustomerPhone = o.CustomerPhone,
                    CustomerGstin = o.CustomerGstin,
                    Items = o.Items ?? new List<HotelPOS.Domain.OrderItem>()
                })
                .ToList();

            var allItems = await _itemRepo.GetAllAsync();
            var allCats = await _categoryRepo.GetAllAsync();

            var categorySales = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => allItems.FirstOrDefault(it => it.Id == i.ItemId)?.CategoryId)
                .Select(g =>
                {
                    var cat = allCats.FirstOrDefault(c => c.Id == g.Key);
                    var rev = g.Sum(i => i.Total);
                    return new CategorySalesRowDto
                    {
                        CategoryName = cat?.Name ?? "Others",
                        Revenue = rev,
                        Percentage = totalRevenue > 0 ? (double)(rev / totalRevenue * 100) : 0
                    };
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();
            for (int i = 0; i < categorySales.Count; i++) categorySales[i].SNo = i + 1;

            var paymentModeSales = orders
                .GroupBy(o => string.IsNullOrWhiteSpace(o.PaymentMode) ? "Cash" : o.PaymentMode)
                .Select(g =>
                {
                    var rev = g.Sum(o => o.TotalAmount);
                    return new PaymentModeSalesRowDto
                    {
                        PaymentMode = g.Key,
                        Revenue = rev,
                        OrderCount = g.Count(),
                        Percentage = totalRevenue > 0 ? (double)(rev / totalRevenue * 100) : 0
                    };
                })
                .OrderByDescending(p => p.Revenue)
                .ToList();
            for (int i = 0; i < paymentModeSales.Count; i++) paymentModeSales[i].SNo = i + 1;

            return new SalesReportDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = count,
                AverageOrderValue = avg,
                MostPopularItem = mostPopular,
                SalesByTable = byTable,
                RecentOrders = recent,
                SalesByCategory = categorySales,
                SalesByPaymentMode = paymentModeSales
            };
        }

        public async Task<List<ItemReportRowDto>> GetItemReportAsync(
            DateTime? from = null, DateTime? to = null)
        {
            var utcFrom = from?.ToUniversalTime();
            var utcTo = to?.ToUniversalTime();

            var (orders, _) = await _orderRepo.GetPagedWithItemsAsync(1, -1, utcFrom, utcTo);

            var result = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ItemName)
                .Select(g => new ItemReportRowDto
                {
                    ItemName = g.Key,
                    TotalQtySold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Total),
                    UnitPrice = g.Count() > 0 ? g.Average(i => i.Price) : 0
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            for (int i = 0; i < result.Count; i++) result[i].SNo = i + 1;
            return result;
        }

        public async Task<List<GstReportRowDto>> GetGstReportAsync(DateTime from, DateTime to)
        {
            // Standardize bounds to UTC
            var utcFrom = from.ToUniversalTime();
            var utcTo = to.ToUniversalTime();

            var (filtered, _) = await _orderRepo.GetPagedWithItemsAsync(1, -1, utcFrom, utcTo);

            var result = filtered
                .GroupBy(o => o.CreatedAt.ToLocalTime().Date)
                .Select(g => new GstReportRowDto
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    GrossRevenue = g.Sum(o => o.TotalAmount),
                    GstAmount = g.Sum(o => o.GstAmount),
                    NetIncome = g.Sum(o => o.Subtotal)
                })
                .OrderBy(r => r.Date)
                .ToList();

            for (int i = 0; i < result.Count; i++) result[i].SNo = i + 1;
            return result;
        }

        public async Task<List<MonthlySalesChartDto>> GetMonthlyChartDataAsync()
        {
            var now = DateTime.Now;

            // Get data for the last 12 months (UTC bounds)
            var startDateLocal = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var startDateUtc = startDateLocal.ToUniversalTime();

            var (orders, _) = await _orderRepo.GetPagedWithItemsAsync(1, -1, startDateUtc);

            var monthlyData = orders
                .GroupBy(o => new { o.CreatedAt.ToLocalTime().Year, o.CreatedAt.ToLocalTime().Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            var result = new List<MonthlySalesChartDto>();
            for (int i = 0; i < 12; i++)
            {
                var target = startDateLocal.AddMonths(i);
                var data = monthlyData.FirstOrDefault(m => m.Year == target.Year && m.Month == target.Month);

                result.Add(new MonthlySalesChartDto
                {
                    MonthName = target.ToString("MMM yy"),
                    Revenue = data?.Revenue ?? 0m
                });
            }

            return result;
        }
    }
}
