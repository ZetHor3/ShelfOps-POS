namespace ShelfOps.Application.DTOs
{
    public record StockMovementResponse(
        Guid Id,
        Guid ProductId,
        string? ProductName,
        string? ProductSku,
        int QuantityDelta,
        string MovementType,
        Guid? ReferenceId,
        string? Reason,
        DateTime CreatedAtUtc);

    public record ReceiptRequest(Guid WarehouseId, List<ReceiptItemRequest> Items);

    public record ReceiptItemRequest(Guid ProductId, int Quantity, decimal UnitCost);

    public record StockBalanceResponse(Guid ProductId, Guid WarehouseId, string? WarehouseName, int QuantityOnHand);
}