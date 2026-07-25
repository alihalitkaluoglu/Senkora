using Xunit;
using FluentAssertions;
using Senkora.Domain.ValueObjects;

namespace Senkora.Tests.Unit.Domain;

public sealed class LogoTokenTests
{
    [Fact]
    public void Token_ShouldBeExpired_WhenExpiresAtIsPast()
    {
        var token = new LogoToken("tok", null, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(-1));
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Token_ShouldBeExpiringSoon_WhenWithinBuffer()
    {
        var token = new LogoToken("tok", null, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(20));
        token.IsExpiringSoon(30).Should().BeTrue();
    }

    [Fact]
    public void Token_ShouldNotBeExpiringSoon_WhenSufficientTimeRemains()
    {
        var token = new LogoToken("tok", null, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        token.IsExpiringSoon(30).Should().BeFalse();
    }
}
