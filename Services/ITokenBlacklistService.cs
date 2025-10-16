using System;

namespace SafeScribe.Api.Services;

public interface ITokenBlacklistService
{
    Task AddAsync(string jti, DateTimeOffset expiresAt);
    bool IsBlacklisted(string jti);
}
