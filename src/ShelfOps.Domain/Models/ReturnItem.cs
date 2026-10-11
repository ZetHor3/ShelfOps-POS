using ShelfOps.Domain.Enums;

namespace ShelfOps.Domain.Models
{
    public class ReturnItem
    {
        public Guid Id { get; private set; }
        public Guid ReturnId { get; private set; }
        public Guid OriginalSaleItemId { get; private set; }
        public int Quantity { get; private set; }
        public decimal RefundUnitPrice { get; private set; }
        public decimal LineTotal { get; private set; }
        public RestockDisposition RestockDisposition { get; private set; }

        public CustomerReturn? Return { get; private set; }

        private ReturnItem() { }

        public ReturnItem(
            Guid returnId,
            Guid originalSaleItemId,
            int quantity,
            decimal refundUnitPrice,
            RestockDisposition restockDisposition)
        {
            if (quantity <= 0)
                throw new ArgumentException("Return quantity must be positive.", nameof(quantity));
            if (refundUnitPrice < 0)
                throw new ArgumentException("Refund price cannot be negative.", nameof(refundUnitPrice));

            Id = Guid.NewGuid();
            ReturnId = returnId;
            OriginalSaleItemId = originalSaleItemId;
            Quantity = quantity;
            RefundUnitPrice = refundUnitPrice;
            LineTotal = refundUnitPrice * quantity;
            RestockDisposition = restockDisposition;
        }
    }
}