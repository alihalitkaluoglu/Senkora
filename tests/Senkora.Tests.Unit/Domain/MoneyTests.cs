using Xunit;
using FluentAssertions;
using Senkora.Domain.ValueObjects;

namespace Senkora.Tests.Unit.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Add_ShouldSumAmounts_WhenSameCurrency()
    {
        var a = new Money(100, "TRY");
        var b = new Money(50, "TRY");
        a.Add(b).Amount.Should().Be(150);
    }

    [Fact]
    public void Add_ShouldThrow_WhenDifferentCurrencies()
    {
        var act = () => new Money(100, "TRY").Add(new Money(50, "USD"));
        act.Should().Throw<InvalidOperationException>();
    }
}
