using FluentAssertions;
using Spending.Domain.ValueObjects;

namespace Spending.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act
        var result = Money.Create(amount, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(amount);
        result.Value.Currency.Should().Be(currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCurrency_ShouldFail(string? currency)
    {
        // Arrange
        var amount = 100.50m;

        // Act
        var result = Money.Create(amount, currency!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrency");
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        // Act
        var money = Money.Zero();

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_WithCustomCurrency_ShouldReturnZeroAmountWithCurrency()
    {
        // Act
        var money = Money.Zero("EUR");

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(100m, "USD").Value;

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentAmounts_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(200m, "USD").Value;

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(100m, "EUR").Value;

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100.50)]
    [InlineData(-50.25)]
    [InlineData(999999.99)]
    public void Create_WithVariousAmounts_ShouldSucceed(decimal amount)
    {
        // Act
        var result = Money.Create(amount, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(amount);
    }
}
