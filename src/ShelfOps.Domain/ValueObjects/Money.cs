namespace ShelfOps.Domain.ValueObjects
{
    public readonly record struct Money(decimal Amount)
    {
        public static readonly Money Zero = new(0);

        public Money Add(Money other) => new(Amount + other.Amount);
        public Money Subtract(Money other) => new(Amount - other.Amount);
        public Money Multiply(decimal factor) => new(Amount * factor);

        public override string ToString() => Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}