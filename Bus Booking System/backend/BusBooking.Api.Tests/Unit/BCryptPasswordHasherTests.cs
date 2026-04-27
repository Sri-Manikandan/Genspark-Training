using BusBooking.Api.Infrastructure.Auth;

namespace BusBooking.Api.Tests.Unit;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_produces_verifiable_hash()
    {
        var hash = _hasher.Hash("CorrectHorse!Battery4");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("CorrectHorse!Battery4");
        _hasher.Verify("CorrectHorse!Battery4", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_on_wrong_password()
    {
        var hash = _hasher.Hash("CorrectHorse!Battery4");

        _hasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_uses_work_factor_eleven()
    {
        var hash = _hasher.Hash("anything");

        // BCrypt hashes encode the work factor after the first `$`.
        // Format: $2a$11$... (or $2b$11$... depending on variant).
        hash.Should().MatchRegex(@"^\$2[aby]\$11\$");
    }
}
