using ShelfOps.Domain.Enums;

namespace ShelfOps.Domain.Models
{
    public class CustomerReturn
    {
        public Guid Id { get; private set; }
        public string ReturnNumber { get; private set; } = string.Empty;
        public Guid OriginalSaleId { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public ReturnStatus Status { get; private set; }
        public string Reason { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        public Sale? OriginalSale { get; private set; }
        public ICollection<ReturnItem> Items { get; private set; } = new List<ReturnItem>();

        private CustomerReturn() { }

        public CustomerReturn(Guid originalSaleId, Guid createdByUserId, string reason)
        {
            Id = Guid.NewGuid();
            ReturnNumber = $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
            OriginalSaleId = originalSaleId;
            CreatedByUserId = createdByUserId;
            Status = ReturnStatus.Draft;
            Reason = reason;
            TotalAmount = 0;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void AddItem(ReturnItem item)
        {
            if (Status != ReturnStatus.Draft)
                throw new InvalidOperationException("Cannot modify a completed return.");
            Items.Add(item);
        }

        public void Complete(decimal totalAmount)
        {
            if (Status != ReturnStatus.Draft)
                throw new InvalidOperationException("Return is already completed.");
            TotalAmount = totalAmount;
            Status = ReturnStatus.Completed;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }
}