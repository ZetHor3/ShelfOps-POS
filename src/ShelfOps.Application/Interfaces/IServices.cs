using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;

namespace ShelfOps.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    }

    public interface IProductService
    {
        Task<PagedResult<ProductResponse>> GetProductsAsync(string? search, Guid? categoryId, bool? isActive,
            string? sortBy, string? sortDirection, int page, int pageSize, CancellationToken ct = default);
        Task<Result<ProductResponse>> GetProductByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
        Task<Result<ProductResponse>> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
        Task<Result> DeactivateProductAsync(Guid id, CancellationToken ct = default);
        Task<List<ProductStockResponse>> GetProductStockAsync(Guid id, CancellationToken ct = default);
    }

    public interface ISaleService
    {
        Task<PagedResult<SaleResponse>> GetSalesAsync(string? search, Guid? cashierId,
            DateTime? from, DateTime? toDate, int page, int pageSize, CancellationToken ct = default);
        Task<Result<SaleResponse>> CreateSaleAsync(CreateSaleRequest request, CancellationToken ct = default);
        Task<Result<SaleResponse>> CompleteSaleAsync(CompleteSaleRequest request, Guid cashierId, CancellationToken ct = default);
    }

    public interface IStockService
    {
        Task<Result> ProcessReceiptAsync(ReceiptRequest request, Guid userId, CancellationToken ct = default);
        Task<PagedResult<StockMovementResponse>> GetMovementsAsync(Guid? productId, Guid? warehouseId,
            int page, int pageSize, CancellationToken ct = default);
    }

    public interface IReturnService
    {
        Task<Result<ReturnResponse>> CreateReturnAsync(CreateReturnRequest request, Guid userId, CancellationToken ct = default);
    }

    public interface IUserManagementService
    {
        Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default);
        Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
        Task<Result> DeactivateUserAsync(Guid id, CancellationToken ct = default);
        Task<Result> ChangeUserRoleAsync(Guid id, string newRole, CancellationToken ct = default);
    }

    public interface IDashboardService
    {
        Task<DashboardResponse> GetDashboardAsync(DateTime? from, DateTime? toDate, CancellationToken ct = default);
    }

    public record DashboardResponse(
        decimal TodayRevenue,
        decimal PeriodRevenue,
        int SalesCount,
        decimal AverageReceipt,
        int ActiveProducts,
        decimal StockValue,
        int LowStockCount,
        List<decimal> RevenueDynamics,
        List<SaleResponse> RecentSales);
}