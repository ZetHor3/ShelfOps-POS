namespace ShelfOps.Domain.Models
{
    public class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }

        public ICollection<Product> Products { get; private set; } = new List<Product>();

        private Category() { }

        public Category(string name, string? description)
        {
            Id = Guid.NewGuid();
            SetName(name);
            Description = description;
            IsActive = true;
            CreatedAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = CreatedAtUtc;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty.", nameof(name));
            Name = name.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void SetDescription(string? description)
        {
            Description = description;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}