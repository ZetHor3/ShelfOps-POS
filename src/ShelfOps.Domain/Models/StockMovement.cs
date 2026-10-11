using ShelfOps.Domain.Enums;

namespace ShelfOps.Domain.Models
{
    public class StockMovement
    {
        public Guid Id { get; private set; }
        public Guid WarehouseId { get; private set; }
        public Guid ProductId { get; private set; }
        public MovementType MovementType { get; private set; }
        public int QuantityDelta { get; private set; }
        public ReferenceType ReferenceType { get; private set; }
        public Guid ReferenceId { get; private set; }
        public string? Reason { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        public Warehouse? Warehouse { get; private set; }
        public Product? Product { get; private set; }

        private StockMovement() { }

        public StockMovement(
            Guid warehouseId,
            Guid productId,
            MovementType movementType,
            int quantityDelta,
            ReferenceType referenceType,
            Guid referenceId,
            Guid createdByUserId,
            string? reason = null)
        {
            if (quantityDelta == 0)
                throw new ArgumentException("Quantity delta cannot be zero.", nameof(quantityDelta));

            Id = Guid.NewGuid();
            WarehouseId = warehouseId;
            ProductId = productId;
            MovementType = movementType;
            QuantityDelta = quantityDelta;
            ReferenceType = referenceType;
            ReferenceId = referenceId;
            CreatedByUserId = createdByUserId;
            Reason = reason;
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}