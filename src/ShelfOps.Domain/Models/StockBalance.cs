namespace ShelfOps.Domain.Models
{
    public class StockBalance
    {
        public Guid WarehouseId { get; private set; }
        public Guid ProductId { get; private set; }
        public int QuantityOnHand { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }
        public Guid RowVersion { get; private set; }

        public Warehouse? Warehouse { get; private set; }
        public Product? Product { get; private set; }

        private StockBalance() { }

        public StockBalance(Guid warehouseId, Guid productId, int initialQuantity = 0)
        {
            if (initialQuantity < 0)
                throw new ArgumentException("Initial quantity cannot be negative.", nameof(initialQuantity));

            WarehouseId = warehouseId;
            ProductId = productId;
            QuantityOnHand = initialQuantity;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void Increase(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Increase quantity must be positive.", nameof(quantity));

            QuantityOnHand += quantity;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void Decrease(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Decrease quantity must be positive.", nameof(quantity));

            if (QuantityOnHand - quantity < 0)
                throw new InvalidOperationException(
                    $"Insufficient stock. Available: {QuantityOnHand}, requested: {quantity}.");

            QuantityOnHand -= quantity;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }
    }
}