using Api.Data;
using Api.Features.Auth;
using Api.Shared.Options;
using Api.Shared.Security;
using Api.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Api.Tests.Auth;

public class AuthServiceTests
{
    private static (AuthService Service, PasswordHasher Hasher) Create(AppDbContext db)
    {
        var hasher = new PasswordHasher();
        var tokenHasher = new TokenHasher();
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "Test",
            Audience = "Test",
            SigningKey = "THIS_IS_A_TEST_SIGNING_KEY_32_CHARS",
            AccessTokenMinutes = 5,
            RefreshTokenDays = 1
        });
        var jwtTokenService = new JwtTokenService(jwtOptions);
        var service = new AuthService(db, hasher, tokenHasher, jwtTokenService, jwtOptions);
        return (service, hasher);
    }

    [Fact]
    public async Task Register_Succeeds_ForNewEmail()
    {
        using var db = TestDb.CreateDbContext();
        var (service, _) = Create(db);
        var request = new RegisterRequest("new@test.com", "Password1!");

        var result = await service.RegisterAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Users.Count().Should().Be(1);
        db.RefreshTokens.Count().Should().Be(1);
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value!.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_Fails_WhenEmailExists()
    {
        using var db = TestDb.CreateDbContext();
        db.AddUser("dup@test.com");
        var (service, _) = Create(db);
        var request = new RegisterRequest("dup@test.com", "Password1!");

        var result = await service.RegisterAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.email_exists");
    }

    [Fact]
    public async Task Login_Fails_WhenPasswordInvalid()
    {
        using var db = TestDb.CreateDbContext();
        var (service, hasher) = Create(db);
        var user = db.AddUser("user@test.com");
        user.SetPasswordHash(hasher.Hash("Correct1!"));
        db.SaveChanges();

        var result = await service.LoginAsync(new LoginRequest("user@test.com", "Wrong1!"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");
    }
}
