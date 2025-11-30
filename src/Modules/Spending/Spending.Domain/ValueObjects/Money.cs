using SpendBear.SharedKernel;

namespace Spending.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Result.Failure<Money>(new Error("Money.InvalidCurrency", "Currency is required."));

        return Result.Success(new Money(amount, currency));
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
