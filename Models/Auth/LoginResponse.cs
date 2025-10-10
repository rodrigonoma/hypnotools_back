namespace HypnoTools.API.Models.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UsuarioInfo? Usuario { get; set; }
}

public class UsuarioInfo
{
    public int IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public string Empresa { get; set; } = string.Empty;
}