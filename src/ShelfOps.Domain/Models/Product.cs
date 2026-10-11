namespace ShelfOps.Domain.Models
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Sku { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public Guid CategoryId { get; private set; }
        public string? Barcode { get; private set; }
        public decimal PurchasePrice { get; private set; }
        public decimal SalePrice { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }
        public Guid RowVersion { get; private set; }

        public Category? Category { get; private set; }

        private Product() { }

        public Product(
            string sku,
            string name,
            Guid categoryId,
            decimal purchasePrice,
            decimal salePrice,
            string? description = null,
            string? barcode = null)
        {
            Id = Guid.NewGuid();
            SetSku(sku);
            SetName(name);
            CategoryId = categoryId;
            Description = description;
            Barcode = barcode;
            SetPurchasePrice(purchasePrice);
            SetSalePrice(salePrice);
            IsActive = true;
            CreatedAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = CreatedAtUtc;
            RowVersion = Guid.NewGuid();
        }

        public void SetSku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU cannot be empty.", nameof(sku));
            Sku = sku.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty.", nameof(name));
            Name = name.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void SetPurchasePrice(decimal price)
        {
            if (price < 0)
                throw new ArgumentException("Purchase price cannot be negative.", nameof(price));
            PurchasePrice = price;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void SetSalePrice(decimal price)
        {
            if (price <= 0)
                throw new ArgumentException("Sale price must be greater than zero.", nameof(price));
            SalePrice = price;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void SetCategory(Guid categoryId)
        {
            CategoryId = categoryId;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void SetBarcode(string? barcode)
        {
            Barcode = barcode;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void SetDescription(string? description)
        {
            Description = description;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAtUtc = DateTime.UtcNow;
            RowVersion = Guid.NewGuid();
        }
    }
}