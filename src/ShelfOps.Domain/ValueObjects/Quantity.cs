namespace ShelfOps.Domain.ValueObjects
{
    public readonly record struct Quantity(int Value)
    {
        public static readonly Quantity Zero = new(0);

        public bool IsPositive => Value > 0;
        public bool IsNegative => Value < 0;
        public bool IsZero => Value == 0;

        public Quantity Add(Quantity other) => new(Value + other.Value);
        public Quantity Subtract(Quantity other) => new(Value - other.Value);
    }
}