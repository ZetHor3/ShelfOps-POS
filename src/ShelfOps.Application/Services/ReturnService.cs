using ShelfOps.Application.Common;
using ShelfOps.Application.DTOs;
using ShelfOps.Application.Interfaces;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;

namespace ShelfOps.Application.Services
{
    public class ReturnService : IReturnService
    {
        private readonly IReturnRepository _returnRepo;
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly IStockBalanceRepository _stockRepo;
        private readonly IStockMovementRepository _movementRepo;
        private readonly IUnitOfWork _unitOfWork;

        public ReturnService(
            IReturnRepository returnRepo,
            ISaleRepository saleRepo,
            IProductRepository productRepo,
            IStockBalanceRepository stockRepo,
            IStockMovementRepository movementRepo,
            IUnitOfWork unitOfWork)
        {
            _returnRepo = returnRepo;
            _saleRepo = saleRepo;
            _productRepo = productRepo;
            _stockRepo = stockRepo;
            _movementRepo = movementRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ReturnResponse>> CreateReturnAsync(CreateReturnRequest request, Guid userId, CancellationToken ct)
        {
            var sale = await _saleRepo.GetByIdAsync(request.OriginalSaleId, ct);
            if (sale is null)
                return Result<ReturnResponse>.Failure("Sale not found.");

            if (sale.Status != SaleStatus.Completed)
                return Result<ReturnResponse>.Failure("Can only return completed sales.");

            var customerReturn = new CustomerReturn(request.OriginalSaleId, userId, request.Reason);
            decimal totalAmount = 0;

            foreach (var item in request.Items)
            {
                var alreadyReturned = await _returnRepo.GetReturnedQuantityAsync(item.OriginalSaleItemId, ct);
                var saleItem = sale.Items.FirstOrDefault(si => si.Id == item.OriginalSaleItemId);
                if (saleItem is null)
                    return Result<ReturnResponse>.Failure($"Sale item {item.OriginalSaleItemId} not found.");

                var availableForReturn = saleItem.Quantity - alreadyReturned;
                if (item.Quantity > availableForReturn)
                    return Result<ReturnResponse>.Failure(
                        $"Cannot return {item.Quantity} of '{saleItem.ProductNameSnapshot}', only {availableForReturn} available.");

                var disposition = item.RestockDisposition switch
                {
                    "Restock" => RestockDisposition.Restock,
                    _ => RestockDisposition.DoNotRestock
                };

                var returnItem = new ReturnItem(
                    customerReturn.Id, item.OriginalSaleItemId,
                    item.Quantity, saleItem.UnitPrice * 0.9m, disposition);

                customerReturn.AddItem(returnItem);
                totalAmount += returnItem.LineTotal;

                if (disposition == RestockDisposition.Restock)
                {
                    var stock = await _stockRepo.GetAsync(sale.WarehouseId, saleItem.ProductId, ct);
                    if (stock is not null)
                    {
                        stock.Increase(item.Quantity);
                        _stockRepo.Update(stock);

                        var movement = new StockMovement(
                            sale.WarehouseId, saleItem.ProductId, MovementType.CustomerReturn,
                            item.Quantity, ReferenceType.Return, customerReturn.Id,
                            userId, "Возврат товара");
                        _movementRepo.Add(movement);
                    }
                }
            }

            customerReturn.Complete(totalAmount);
            _returnRepo.Add(customerReturn);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<ReturnResponse>.Success(Map(customerReturn));
        }

        private static ReturnResponse Map(CustomerReturn r) => new(
            r.Id, r.ReturnNumber, r.OriginalSaleId,
            r.Status.ToString(), r.Reason, r.TotalAmount,
            r.CreatedAtUtc, r.CompletedAtUtc);
    }
}