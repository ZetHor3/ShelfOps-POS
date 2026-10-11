using ShelfOps.Application.Common;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<PagedResult<Product>> GetPagedAsync(string? search, Guid? categoryId, bool? isActive,
            string? sortBy, string? sortDirection, int page, int pageSize, CancellationToken ct = default);
        Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
        Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeId = null, CancellationToken ct = default);
        void Add(Product product);
        void Update(Product product);
    }

    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(CancellationToken ct = default);
        Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
        void Add(Category category);
        void Update(Category category);
    }

    public interface IWarehouseRepository
    {
        Task<List<Warehouse>> GetAllAsync(CancellationToken ct = default);
        Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }

    public interface IStockBalanceRepository
    {
        Task<StockBalance?> GetAsync(Guid warehouseId, Guid productId, CancellationToken ct = default);
        Task<List<StockBalance>> GetByProductAsync(Guid productId, CancellationToken ct = default);
        Task<Dictionary<Guid, int>> GetTotalsByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
        Task<decimal> GetTotalValueAsync(CancellationToken ct = default);
        Task<int> GetLowStockCountAsync(int threshold = 5, CancellationToken ct = default);
        void Add(StockBalance balance);
        void Update(StockBalance balance);
    }

    public interface IStockMovementRepository
    {
        Task<PagedResult<StockMovement>> GetPagedAsync(Guid? productId, Guid? warehouseId,
            int page, int pageSize, CancellationToken ct = default);
        void Add(StockMovement movement);
    }

    public interface ISaleRepository
    {
        Task<PagedResult<Sale>> GetPagedAsync(string? search, Guid? cashierId,
            DateTime? from, DateTime? toDate, int page, int pageSize, CancellationToken ct = default);
        Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Sale?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
        Task<decimal> GetPeriodRevenueAsync(DateTime from, DateTime toDate, CancellationToken ct = default);
        Task<decimal> GetDailyRevenueAsync(DateTime day, CancellationToken ct = default);
        Task<string> NextSaleNumberAsync(CancellationToken ct = default);
        void Add(Sale sale);
    }

    public interface IReturnRepository
    {
        Task<PagedResult<CustomerReturn>> GetPagedAsync(Guid? saleId, int page, int pageSize, CancellationToken ct = default);
        Task<CustomerReturn?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<int> GetReturnedQuantityAsync(Guid saleItemId, CancellationToken ct = default);
        void Add(CustomerReturn customerReturn);
    }

    public interface IUserRepository
    {
        Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<bool> IsUsernameUniqueAsync(string username, Guid? excludeId = null, CancellationToken ct = default);
        void Add(User user);
        void Update(User user);
    }

    public interface IAuditLogRepository
    {
        Task<PagedResult<AuditLog>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        void Add(AuditLog log);
    }
}