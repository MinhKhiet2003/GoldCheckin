using Base.Entities;

namespace Base.Services.IServices;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) Create(Account account, Guid sessionId);
}
