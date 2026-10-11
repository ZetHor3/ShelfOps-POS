using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IProductRepository _productRepo;
        private readonly ISaleRepository _saleRepo;
        private readonly IStockBalanceRepository _stockBalanceRepo;

        public DashboardService(
            IProductRepository productRepo,
            ISaleRepository saleRepo,
            IStockBalanceRepository stockBalanceRepo)
        {
            _productRepo = productRepo;
            _saleRepo = saleRepo;
            _stockBalanceRepo = stockBalanceRepo;
        }

        public async Task<DashboardResponse> GetDashboardAsync(DateTime? from, DateTime? toDate, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;

            var periodStart = from ?? todayStart.AddDays(-30);
            var periodEnd = toDate ?? now;

            // NOTE: EF Core DbContext is not thread-safe, so queries are awaited
            // sequentially. Firing them concurrently on the same context throws
            // "A second operation was started on this context instance...".

            // Period data
            var periodRevenue = await _saleRepo.GetPeriodRevenueAsync(periodStart, periodEnd, ct);

            // Count sales in period via paged with no filters
            var periodSales = await _saleRepo.GetPagedAsync(null, null, periodStart, periodEnd, 1, 1_000_000, ct);

            // Today's revenue
            var todayRevenue = await _saleRepo.GetDailyRevenueAsync(todayStart, ct);

            // Product stats
            var products = await _productRepo.GetPagedAsync(null, null, true, null, null, 1, 1_000_000, ct);
            var activeProducts = products.TotalCount;

            var stockValue = await _stockBalanceRepo.GetTotalValueAsync(ct);
            var lowStockCount = await _stockBalanceRepo.GetLowStockCountAsync(5, ct);

            // Revenue dynamics (last 30 days)
            var revenueDynamics = new List<decimal>();
            for (int i = 29; i >= 0; i--)
            {
                var dayRevenue = await _saleRepo.GetDailyRevenueAsync(todayStart.AddDays(-i), ct);
                revenueDynamics.Add(dayRevenue);
            }

            var salesCount = periodSales.TotalCount;
            var averageReceipt = salesCount > 0 ? periodRevenue / salesCount : 0;

            // Recent sales (top 10)
            var recentSalesPaged = await _saleRepo.GetPagedAsync(null, null, null, null, 1, 10, ct);
            var recentSaleResponses = recentSalesPaged.Items.Select(s => new SaleResponse(
                s.Id, s.SaleNumber, s.WarehouseId, null,
                s.CashierUserId, null, s.Status.ToString(),
                s.Subtotal, s.DiscountTotal, s.GrandTotal,
                s.CreatedAtUtc, s.CompletedAtUtc,
                s.Items.Select(i => new SaleItemResponse(
                    i.Id, i.ProductId, i.ProductSkuSnapshot, i.ProductNameSnapshot,
                    i.Quantity, i.UnitPrice, i.DiscountAmount, i.LineTotal)).ToList()
            )).ToList();

            return new DashboardResponse(
                todayRevenue, periodRevenue, salesCount, averageReceipt,
                activeProducts, stockValue, lowStockCount,
                revenueDynamics, recentSaleResponses);
        }
    }
}