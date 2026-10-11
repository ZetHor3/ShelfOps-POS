using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Services
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly IStockBalanceRepository _stockRepo;
        private readonly IStockMovementRepository _movementRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SaleService(
            ISaleRepository saleRepo,
            IProductRepository productRepo,
            IStockBalanceRepository stockRepo,
            IStockMovementRepository movementRepo,
            IAuditLogRepository auditRepo,
            IUnitOfWork unitOfWork)
        {
            _saleRepo = saleRepo;
            _productRepo = productRepo;
            _stockRepo = stockRepo;
            _movementRepo = movementRepo;
            _auditRepo = auditRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<SaleResponse>> GetSalesAsync(string? search, Guid? cashierId,
            DateTime? from, DateTime? toDate, int page, int pageSize, CancellationToken ct)
        {
            var result = await _saleRepo.GetPagedAsync(search, cashierId, from, toDate, page, pageSize, ct);
            var items = result.Items.Select(MapSale).ToList();
            return new PagedResult<SaleResponse>(items, result.TotalCount, result.Page, result.PageSize);
        }

        public async Task<Result<SaleResponse>> CreateSaleAsync(CreateSaleRequest request, CancellationToken ct)
        {
            var sale = new Sale(request.WarehouseId, Guid.Empty, request.IdempotencyKey);
            sale.SetSaleNumber(await _saleRepo.NextSaleNumberAsync(ct));
            _saleRepo.Add(sale);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<SaleResponse>.Success(MapSale(sale));
        }

        public async Task<Result<SaleResponse>> CompleteSaleAsync(CompleteSaleRequest request, Guid cashierId, CancellationToken ct)
        {
            if (request.Items.Count == 0)
                return Result<SaleResponse>.Failure("Sale must have at least one item.");

            // Idempotency check
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existing = await _saleRepo.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
                if (existing is not null)
                    return Result<SaleResponse>.Success(MapSale(existing));
            }

            Sale? completedSale = null;
            try
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var sale = new Sale(request.WarehouseId, cashierId, request.IdempotencyKey);
                    sale.SetSaleNumber(await _saleRepo.NextSaleNumberAsync(ct));

                    foreach (var item in request.Items)
                    {
                        var product = await _productRepo.GetByIdAsync(item.ProductId, ct)
                            ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

                        if (!product.IsActive)
                            throw new InvalidOperationException($"Product '{product.Name}' is not active.");

                        var stock = await _stockRepo.GetAsync(request.WarehouseId, item.ProductId, ct);
                        if (stock is null || stock.QuantityOnHand < item.Quantity)
                            throw new InvalidOperationException($"Insufficient stock for '{product.Name}'.");

                        var saleItem = new SaleItem(sale.Id, product.Id, product.Sku, product.Name,
                            item.Quantity, product.SalePrice, item.DiscountAmount);

                        sale.AddItem(saleItem);
                        stock.Decrease(item.Quantity);
                        _stockRepo.Update(stock);

                        var movement = new StockMovement(request.WarehouseId, product.Id, MovementType.Sale,
                            -item.Quantity, ReferenceType.Sale, sale.Id, cashierId);
                        _movementRepo.Add(movement);
                    }

                    var subtotal = sale.CalculateSubtotal();
                    var discountTotal = 0m;
                    sale.Complete(subtotal, discountTotal, subtotal - discountTotal);
                    _saleRepo.Add(sale);
                    completedSale = sale;
                }, ct);

                return Result<SaleResponse>.Success(MapSale(completedSale!));
            }
            catch (InvalidOperationException ex)
            {
                return Result<SaleResponse>.Failure(ex.Message);
            }
        }

        private static SaleResponse MapSale(Sale s) => new(
            s.Id, s.SaleNumber, s.WarehouseId, null,
            s.CashierUserId, null, s.Status.ToString(),
            s.Subtotal, s.DiscountTotal, s.GrandTotal,
            s.CreatedAtUtc, s.CompletedAtUtc,
            s.Items.Select(i => new SaleItemResponse(
                i.Id, i.ProductId, i.ProductSkuSnapshot, i.ProductNameSnapshot,
                i.Quantity, i.UnitPrice, i.DiscountAmount, i.LineTotal)).ToList());
    }
}