using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IWarehouseRepository _warehouseRepo;
        private readonly IProductRepository _productRepo;
        private readonly IStockBalanceRepository _stockRepo;
        private readonly IStockMovementRepository _movementRepo;
        private readonly IUnitOfWork _unitOfWork;

        public StockService(
            IWarehouseRepository warehouseRepo,
            IProductRepository productRepo,
            IStockBalanceRepository stockRepo,
            IStockMovementRepository movementRepo,
            IUnitOfWork unitOfWork)
        {
            _warehouseRepo = warehouseRepo;
            _productRepo = productRepo;
            _stockRepo = stockRepo;
            _movementRepo = movementRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> ProcessReceiptAsync(ReceiptRequest request, Guid userId, CancellationToken ct)
        {
            var warehouse = await _warehouseRepo.GetByIdAsync(request.WarehouseId, ct);
            if (warehouse is null)
                return Result.Failure("Warehouse not found.");

            Guid receiptId = Guid.NewGuid();
            try
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    foreach (var item in request.Items)
                    {
                        var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
                        if (product is null)
                            throw new InvalidOperationException($"Product {item.ProductId} not found.");

                        var stock = await _stockRepo.GetAsync(request.WarehouseId, item.ProductId, ct);
                        if (stock is null)
                        {
                            stock = new StockBalance(request.WarehouseId, item.ProductId, item.Quantity);
                            _stockRepo.Add(stock);
                        }
                        else
                        {
                            stock.Increase(item.Quantity);
                            _stockRepo.Update(stock);
                        }

                        var movement = new StockMovement(
                            request.WarehouseId, item.ProductId, MovementType.Receipt,
                            item.Quantity, ReferenceType.Receipt, receiptId, userId);
                        _movementRepo.Add(movement);
                    }
                }, ct);

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public async Task<PagedResult<StockMovementResponse>> GetMovementsAsync(
            Guid? productId, Guid? warehouseId, int page, int pageSize, CancellationToken ct)
        {
            var result = await _movementRepo.GetPagedAsync(productId, warehouseId, page, pageSize, ct);
            var items = result.Items.Select(m => new StockMovementResponse(
                m.Id, m.ProductId, m.Product?.Name, m.Product?.Sku,
                m.QuantityDelta, m.MovementType.ToString(),
                m.ReferenceId, m.Reason, m.CreatedAtUtc)).ToList();
            return new PagedResult<StockMovementResponse>(items, result.TotalCount, result.Page, result.PageSize);
        }
    }
}