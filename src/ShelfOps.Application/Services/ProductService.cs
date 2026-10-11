using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Models;
using ShelfOps.Domain.Enums;

namespace ShelfOps.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IStockBalanceRepository _stockRepo;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            IStockBalanceRepository stockRepo,
            IUnitOfWork unitOfWork)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _stockRepo = stockRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<ProductResponse>> GetProductsAsync(string? search, Guid? categoryId, bool? isActive,
            string? sortBy, string? sortDirection, int page, int pageSize, CancellationToken ct)
        {
            var result = await _productRepo.GetPagedAsync(search, categoryId, isActive,
                sortBy, sortDirection, page, pageSize, ct);

            var productIds = result.Items.Select(p => p.Id).ToList();
            var totals = await _stockRepo.GetTotalsByProductIdsAsync(productIds, ct);

            var items = result.Items.Select(p => new ProductResponse(
                p.Id, p.Sku, p.Name, p.CategoryId, p.Category?.Name,
                p.Description, p.Barcode, p.PurchasePrice, p.SalePrice,
                p.IsActive,
                totals.TryGetValue(p.Id, out var total) ? total : 0,
                p.CreatedAtUtc, p.UpdatedAtUtc)).ToList();

            return new PagedResult<ProductResponse>(items, result.TotalCount, result.Page, result.PageSize);
        }

        public async Task<Result<ProductResponse>> GetProductByIdAsync(Guid id, CancellationToken ct)
        {
            var product = await _productRepo.GetByIdAsync(id, ct);
            if (product is null)
                return Result<ProductResponse>.Failure("Product not found.");

            var stock = await _stockRepo.GetByProductAsync(id, ct);
            return Result<ProductResponse>.Success(Map(product, stock.Sum(b => b.QuantityOnHand)));
        }

        public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
        {
            var category = await _categoryRepo.GetByIdAsync(request.CategoryId, ct);
            if (category is null)
                return Result<ProductResponse>.Failure("Category not found.");

            if (!await _productRepo.IsSkuUniqueAsync(request.Sku, null, ct))
                return Result<ProductResponse>.Failure("SKU already exists.");

            var product = new Product(request.Sku, request.Name, request.CategoryId,
                request.PurchasePrice, request.SalePrice, request.Description, request.Barcode);

            _productRepo.Add(product);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<ProductResponse>.Success(Map(product));
        }

        public async Task<Result<ProductResponse>> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
        {
            var product = await _productRepo.GetByIdAsync(id, ct);
            if (product is null)
                return Result<ProductResponse>.Failure("Product not found.");

            product.SetName(request.Name);
            product.SetPurchasePrice(request.PurchasePrice);
            product.SetSalePrice(request.SalePrice);
            product.SetCategory(request.CategoryId);
            product.SetDescription(request.Description);
            product.SetBarcode(request.Barcode);

            _productRepo.Update(product);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<ProductResponse>.Success(Map(product));
        }

        public async Task<Result> DeactivateProductAsync(Guid id, CancellationToken ct)
        {
            var product = await _productRepo.GetByIdAsync(id, ct);
            if (product is null)
                return Result.Failure("Product not found.");

            product.Deactivate();
            _productRepo.Update(product);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }

        public async Task<List<ProductStockResponse>> GetProductStockAsync(Guid id, CancellationToken ct)
        {
            var balances = await _stockRepo.GetByProductAsync(id, ct);
            return balances.Select(b => new ProductStockResponse(
                b.ProductId, b.WarehouseId, b.Warehouse?.Name, b.QuantityOnHand)).ToList();
        }

        private static ProductResponse Map(Product p, int stockTotal = 0) => new(
            p.Id, p.Sku, p.Name, p.CategoryId, p.Category?.Name,
            p.Description, p.Barcode, p.PurchasePrice, p.SalePrice,
            p.IsActive, stockTotal, p.CreatedAtUtc, p.UpdatedAtUtc);
    }
}