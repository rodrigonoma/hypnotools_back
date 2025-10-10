using HypnoTools.API.Models.Auth;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HypnoTools.API.Services.Auth;

public class HypnoCoreAuthService : IHypnoCoreAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HypnoCoreAuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _currentUserToken;

    public HypnoCoreAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HypnoCoreAuthService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        var hypnoCoreAuthUrl = _configuration["HypnoCore:AuthAPI"] ?? "http://localhost:5201";
        _httpClient.BaseAddress = new Uri($"{hypnoCoreAuthUrl}/");
    }

    public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting authentication for user {Email} at company {Empresa}",
                request.Email, request.Empresa);

            var loginModel = new
            {
                Email = request.Email,
                Senha = request.Senha,
                Empresa = request.Empresa
            };

            var json = JsonSerializer.Serialize(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/Login/authenticate", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("HypnoCore Auth response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Raw response from HypnoCore Auth API: {Response}", responseContent);

                try
                {
                    // Primeiro, tentar deserializar como nosso modelo esperado
                    var authResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Deserialized response - Success: {Success}, Token: {HasToken}, Usuario: {HasUsuario}",
                        authResponse?.Success,
                        !string.IsNullOrEmpty(authResponse?.Token),
                        authResponse?.Usuario != null);

                    if (authResponse != null)
                    {
                        // Se a resposta foi bem-sucedida mas não tem o campo Success definido, assumir como true
                        if (!authResponse.Success && !string.IsNullOrEmpty(authResponse.Token))
                        {
                            _logger.LogInformation("Setting Success to true based on presence of token");
                            authResponse.Success = true;
                        }

                        if (authResponse.Success && !string.IsNullOrEmpty(authResponse.Token))
                        {
                            // Se não há dados do usuário, extrair do JWT token
                            if (authResponse.Usuario == null)
                            {
                                _logger.LogInformation("Usuario is null, extracting from JWT token");
                                authResponse.Usuario = ExtractUserFromToken(authResponse.Token);
                            }

                            _currentUserToken = authResponse.Token;
                            _logger.LogInformation("Authentication successful for user {Email}", request.Email);
                            return authResponse;
                        }
                    }

                    // Se chegou até aqui, tentar uma abordagem mais flexível
                    _logger.LogInformation("Trying flexible parsing approach...");

                    using (var doc = JsonDocument.Parse(responseContent))
                    {
                        var root = doc.RootElement;

                        // Tentar extrair token de diferentes campos possíveis
                        string? token = null;
                        if (root.TryGetProperty("token", out var tokenElement))
                            token = tokenElement.GetString();
                        else if (root.TryGetProperty("Token", out tokenElement))
                            token = tokenElement.GetString();
                        else if (root.TryGetProperty("accessToken", out tokenElement))
                            token = tokenElement.GetString();
                        else if (root.TryGetProperty("access_token", out tokenElement))
                            token = tokenElement.GetString();

                        if (!string.IsNullOrEmpty(token))
                        {
                            _logger.LogInformation("Found token using flexible parsing");

                            // Extrair dados do usuário do JWT token
                            var usuario = ExtractUserFromToken(token);

                            // Criar resposta com dados básicos
                            var flexibleResponse = new LoginResponse
                            {
                                Success = true,
                                Token = token,
                                Message = "Login realizado com sucesso",
                                Usuario = usuario
                            };

                            _currentUserToken = token;
                            _logger.LogInformation("Authentication successful using flexible parsing for user {Email}", request.Email);
                            return flexibleResponse;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize response from HypnoCore Auth API");
                }
            }

            _logger.LogWarning("Authentication failed for user {Email}: {Response}", request.Email, responseContent);

            return new LoginResponse
            {
                Success = false,
                Message = "Falha na autenticação. Verifique suas credenciais."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Email}", request.Email);
            return new LoginResponse
            {
                Success = false,
                Message = "Erro interno durante a autenticação."
            };
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync("api/Login/validate");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public string? GetCurrentUserToken()
    {
        if (!string.IsNullOrEmpty(_currentUserToken))
            return _currentUserToken;

        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    public void SetCurrentUserToken(string token)
    {
        _currentUserToken = token;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    private UsuarioInfo ExtractUserFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            _logger.LogInformation("Extracting user data from JWT token");

            // Extrair claims do token
            var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value ??
                            jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ??
                            jwtToken.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value;

            var nameClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "name")?.Value ??
                           jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ??
                           jwtToken.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value;

            var idClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid")?.Value ??
                         jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ??
                         jwtToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;

            // Empresa é armazenada como ClaimTypes.Role
            var empresaClaim = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.Role ||
                x.Type == "role" ||
                x.Type == "empresa" ||
                x.Type == "Empresa")?.Value;

            // Se não encontrou nome, usar parte do email antes do @
            if (string.IsNullOrEmpty(nameClaim) && !string.IsNullOrEmpty(emailClaim))
            {
                nameClaim = emailClaim.Split('@')[0];
            }

            var usuario = new UsuarioInfo
            {
                Email = emailClaim ?? "usuario@hypnobox.com.br",
                Nome = nameClaim ?? "Usuário",
                IdUsuario = int.TryParse(idClaim, out int id) ? id : 1,
                Ativo = true,
                Empresa = empresaClaim ?? string.Empty
            };

            _logger.LogInformation("Extracted user data - Email: {Email}, Nome: {Nome}, Id: {Id}, Empresa: {Empresa}",
                usuario.Email, usuario.Nome, usuario.IdUsuario, usuario.Empresa);

            return usuario;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user data from JWT token");

            // Retornar dados padrão em caso de erro
            return new UsuarioInfo
            {
                Email = "usuario@hypnobox.com.br",
                Nome = "Usuário",
                IdUsuario = 1,
                Ativo = true
            };
        }
    }
}