using HypnoTools.API.Models.Auth;
using HypnoTools.API.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HypnoTools.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHypnoCoreAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHypnoCoreAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Autenticar usuário via HypnoCore Auth-API
    /// </summary>
    /// <param name="request">Dados de login incluindo empresa</param>
    /// <returns>Token de autenticação e informações do usuário</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Senha))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Email e senha são obrigatórios"
                });
            }

            if (string.IsNullOrEmpty(request.Empresa))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Empresa é obrigatória"
                });
            }

            _logger.LogInformation("Processing login request for user {Email} at company {Empresa}",
                request.Email, request.Empresa);

            var result = await _authService.AuthenticateAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Login successful for user {Email} at company {Empresa}",
                    request.Email, request.Empresa);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Login failed for user {Email} at company {Empresa}: {Message}",
                    request.Email, request.Empresa, result.Message);
                return Unauthorized(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email} at company {Empresa}",
                request.Email, request.Empresa);

            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Validar token de autenticação
    /// </summary>
    /// <param name="token">Token a ser validado</param>
    /// <returns>Status da validação</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<bool>> ValidateToken([FromBody] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(false);
            }

            var isValid = await _authService.ValidateTokenAsync(token);
            return Ok(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, false);
        }
    }

    /// <summary>
    /// Logout do usuário (limpa token local)
    /// </summary>
    /// <returns>Confirmação de logout</returns>
    [HttpPost("logout")]
    public ActionResult Logout()
    {
        try
        {
            _authService.SetCurrentUserToken(string.Empty);
            _logger.LogInformation("User logged out successfully");

            return Ok(new { Success = true, Message = "Logout realizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }
}