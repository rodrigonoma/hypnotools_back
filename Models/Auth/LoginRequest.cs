namespace HypnoTools.API.Models.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
}