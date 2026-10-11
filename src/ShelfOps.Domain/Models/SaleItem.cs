namespace ShelfOps.Domain.Models
{
    public class SaleItem
    {
        public Guid Id { get; private set; }
        public Guid SaleId { get; private set; }
        public Guid ProductId { get; private set; }
        public string ProductSkuSnapshot { get; private set; } = string.Empty;
        public string ProductNameSnapshot { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal LineTotal { get; private set; }

        public Sale? Sale { get; private set; }

        private SaleItem() { }

        public SaleItem(
            Guid saleId,
            Guid productId,
            string productSku,
            string productName,
            int quantity,
            decimal unitPrice,
            decimal discountAmount = 0)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));
            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

            Id = Guid.NewGuid();
            SaleId = saleId;
            ProductId = productId;
            ProductSkuSnapshot = productSku;
            ProductNameSnapshot = productName;
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountAmount = discountAmount;
            LineTotal = (unitPrice * quantity) - discountAmount;

            if (LineTotal < 0)
                throw new InvalidOperationException("Line total cannot be negative.");
        }
    }
}