namespace ShelfOps.Application.DTOs
{
    public record CreateReturnRequest(Guid OriginalSaleId, string Reason, List<ReturnItemRequest> Items);

    public record ReturnItemRequest(Guid OriginalSaleItemId, int Quantity, string RestockDisposition);

    public record ReturnResponse(
        Guid Id,
        string ReturnNumber,
        Guid OriginalSaleId,
        string Status,
        string Reason,
        decimal TotalAmount,
        DateTime CreatedAtUtc,
        DateTime? CompletedAtUtc);
}