namespace ShelfOps.Domain.Models
{
    public class Warehouse
    {
        public Guid Id { get; private set; }
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string? Address { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        public ICollection<StockBalance> StockBalances { get; private set; } = new List<StockBalance>();

        private Warehouse() { }

        public Warehouse(string code, string name, string? address = null)
        {
            Id = Guid.NewGuid();
            SetCode(code);
            SetName(name);
            Address = address;
            IsActive = true;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void SetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Warehouse code cannot be empty.", nameof(code));
            Code = code.Trim().ToUpperInvariant();
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Warehouse name cannot be empty.", nameof(name));
            Name = name.Trim();
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}