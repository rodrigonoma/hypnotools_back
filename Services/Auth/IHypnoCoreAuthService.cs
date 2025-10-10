using HypnoTools.API.Models.Auth;

namespace HypnoTools.API.Services.Auth;

public interface IHypnoCoreAuthService
{
    Task<LoginResponse> AuthenticateAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    string? GetCurrentUserToken();
    void SetCurrentUserToken(string token);
}