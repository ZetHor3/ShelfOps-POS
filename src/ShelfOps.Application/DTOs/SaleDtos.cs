namespace ShelfOps.Application.DTOs
{
    public record CreateSaleRequest(Guid WarehouseId, string? IdempotencyKey = null);

    public record AddSaleItemRequest(Guid ProductId, int Quantity, decimal DiscountAmount = 0);

    public record CompleteSaleRequest(
        Guid SaleId,
        Guid WarehouseId,
        List<AddSaleItemRequest> Items,
        string? IdempotencyKey = null);

    public record SaleResponse(
        Guid Id,
        string SaleNumber,
        Guid WarehouseId,
        string? WarehouseName,
        Guid CashierUserId,
        string? CashierName,
        string Status,
        decimal Subtotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        DateTime CreatedAtUtc,
        DateTime? CompletedAtUtc,
        List<SaleItemResponse> Items);

    public record SaleItemResponse(
        Guid Id,
        Guid ProductId,
        string ProductSku,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal DiscountAmount,
        decimal LineTotal);
}