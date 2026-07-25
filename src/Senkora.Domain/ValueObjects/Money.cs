namespace Senkora.Domain.ValueObjects;

public sealed record Money(decimal Amount, string CurrencyCode = "TRY")
{
    public static Money Zero(string currency = "TRY") => new(0, currency);
    public Money Add(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException("Cannot add different currencies.");
        return this with { Amount = Amount + other.Amount };
    }
    public override string ToString() => $"{Amount:N2} {CurrencyCode}";
}
