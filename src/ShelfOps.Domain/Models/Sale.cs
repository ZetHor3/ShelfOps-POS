using ShelfOps.Domain.Enums;

namespace ShelfOps.Domain.Models
{
    public class Sale
    {
        public Guid Id { get; private set; }
        public string SaleNumber { get; private set; } = string.Empty;
        public Guid WarehouseId { get; private set; }
        public Guid CashierUserId { get; private set; }
        public SaleStatus Status { get; private set; }
        public decimal Subtotal { get; private set; }
        public decimal DiscountTotal { get; private set; }
        public decimal GrandTotal { get; private set; }
        public string CurrencyCode { get; private set; } = "RUB";
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public string? IdempotencyKey { get; private set; }

        public Warehouse? Warehouse { get; private set; }
        public ICollection<SaleItem> Items { get; private set; } = new List<SaleItem>();

        private Sale() { }

        public Sale(Guid warehouseId, Guid cashierUserId, string? idempotencyKey = null)
        {
            Id = Guid.NewGuid();
            SaleNumber = GenerateSaleNumber();
            WarehouseId = warehouseId;
            CashierUserId = cashierUserId;
            Status = SaleStatus.Draft;            Subtotal = 0;
            DiscountTotal = 0;
            GrandTotal = 0;
            CreatedAtUtc = DateTime.UtcNow;
            IdempotencyKey = idempotencyKey;
        }

        public void AddItem(SaleItem item)
        {
            if (Status != SaleStatus.Draft)
                throw new InvalidOperationException("Cannot modify a completed sale.");

            Items.Add(item);
 }
        // Temporary in-process placeholder, safe only because every persistable path
        // overwrites it via SetSaleNumber with a DB-sequence value.
        private static int _saleCounter;
        private static string GenerateSaleNumber()
        {
            var counter = Interlocked.Increment(ref _saleCounter);
            return $"SALE-{DateTime.UtcNow:yyyyMMdd}-{counter:D5}";
        }

        public void SetSaleNumber(string saleNumber)
        {
            if (string.IsNullOrWhiteSpace(saleNumber))
                throw new ArgumentException("Sale number cannot be empty.", nameof(saleNumber));

            SaleNumber = saleNumber;
        }

        public void Complete(decimal subtotal, decimal discountTotal, decimal grandTotal)
        {
            if (Status != SaleStatus.Draft)
                throw new InvalidOperationException("Sale is already completed or cancelled.");

            Subtotal = subtotal;
            DiscountTotal = discountTotal;
            GrandTotal = grandTotal;
            Status = SaleStatus.Completed;
            CompletedAtUtc = DateTime.UtcNow;
        }

        public decimal CalculateSubtotal() => Items.Sum(i => i.LineTotal);
        public decimal CalculateGrandTotal() => Subtotal - DiscountTotal;
    }
}