using System;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReQuest_backend.Server.Auth;

public sealed record AuthProfile(string Name, string Email, DateTimeOffset IssuedAt);

public class AuthTokenService : IAuthTokenService
{
    private const string ProtectorPurpose = "ReQuest_backend.AuthTokenService.v1";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);
    private readonly IDataProtector _protector;

    public AuthTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    public string IssueToken(string name, string email)
    {
        var profile = new AuthProfile(name.Trim(), email.Trim(), DateTimeOffset.UtcNow);
        return _protector.Protect(JsonSerializer.Serialize(profile));
    }

    public bool TryValidateToken(string token, out AuthProfile? profile)
    {
        profile = null;

        try
        {
            var payload = JsonSerializer.Deserialize<AuthProfile>(_protector.Unprotect(token));
            if (payload == null) return false;
            if (payload.IssuedAt < DateTimeOffset.UtcNow.Subtract(TokenLifetime)) return false;

            profile = payload;
            return true;
        }
        catch
        {
            return false;
        }
    }

}