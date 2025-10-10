using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HypnoTools.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnidadeController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UnidadeController> _logger;

    public UnidadeController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UnidadeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Atualiza o id_externo das unidades na tabela tower_units
    /// Este endpoint atua como proxy, encaminhando a requisição para o TRS-API
    /// </summary>
    /// <param name="request">Requisição contendo IdProduto e lista de unidades com propriedade Identificador_unid (ex: "AP 15018")</param>
    /// <returns>Resultado da operação de atualização com total de unidades atualizadas</returns>
    [HttpPost("atualizar-id-externo")]
    [ProducesResponseType(typeof(AtualizarIdExternoResponse), 200)]
    [ProducesResponseType(typeof(AtualizarIdExternoResponse), 400)]
    [ProducesResponseType(typeof(AtualizarIdExternoResponse), 500)]
    [ProducesResponseType(typeof(AtualizarIdExternoResponse), 503)]
    public async Task<ActionResult<AtualizarIdExternoResponse>> AtualizarIdExterno(
        [FromBody] AtualizarIdExternoRequest request)
    {
        try
        {
            _logger.LogInformation("Starting unit external ID update for product {IdProduto} with {Count} units",
                request?.IdProduto ?? 0, request?.Unidades?.Count ?? 0);

            // Validar requisição
            if (request == null || request.Unidades == null || request.Unidades.Count == 0)
            {
                return BadRequest(new AtualizarIdExternoResponse
                {
                    Success = false,
                    Message = "Lista de unidades não pode ser vazia",
                    TotalAtualizados = 0,
                    TotalRecebidos = 0,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            if (request.IdProduto <= 0)
            {
                return BadRequest(new AtualizarIdExternoResponse
                {
                    Success = false,
                    Message = "IdProduto é obrigatório e deve ser maior que zero",
                    TotalAtualizados = 0,
                    TotalRecebidos = request.Unidades.Count,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Obter URL do TRS-API da configuração
            var trsApiUrl = _configuration["HypnoCore:TRSAPI"] ??
                           _configuration["HypnoCore:BaseUrl"] ??
                           "http://localhost:5055"; // fallback URL

            var httpClient = _httpClientFactory.CreateClient();

            // Configurar timeout para a requisição
            httpClient.Timeout = TimeSpan.FromMinutes(3);

            // Obter token de autenticação da requisição atual e extrair empresa
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            string? companyAlias = null;

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);

                // Extrair empresa do token JWT
                try
                {
                    var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);

                    // Empresa é armazenada como ClaimTypes.Role no HypnoCore Auth API
                    companyAlias = jwtToken.Claims.FirstOrDefault(x =>
                        x.Type == ClaimTypes.Role ||
                        x.Type == "role" ||
                        x.Type == "empresa" ||
                        x.Type == "Empresa")?.Value;

                    _logger.LogDebug("Extracted company from JWT token (ClaimTypes.Role): {Company}", companyAlias ?? "NOT FOUND");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract company from JWT token");
                }
            }

            if (string.IsNullOrEmpty(companyAlias))
            {
                _logger.LogError("Company information not found in JWT token. Cannot proceed without company identification.");
                return StatusCode(500, new AtualizarIdExternoResponse
                {
                    Success = false,
                    Message = "Informação da empresa não encontrada no token de autenticação",
                    TotalAtualizados = 0,
                    TotalRecebidos = request.Unidades.Count,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Preparar o payload da requisição
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Adicionar headers de correlação e empresa para rastreamento e isolamento de banco
            var correlationId = Guid.NewGuid().ToString();
            httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

            // CRITICAL: Add "Empresa" header - required for TRS-API to forward to Transacional-Proposta
            httpClient.DefaultRequestHeaders.Add("Empresa", companyAlias);

            _logger.LogDebug("Sending request to TRS-API for company '{Company}' with correlation ID {CorrelationId}",
                companyAlias, correlationId);

            // Encaminhar requisição para o endpoint AtualizarIdExterno do TRS-API
            var response = await httpClient.PostAsync(
                $"{trsApiUrl}/api/unidade/atualizar-id-externo",
                content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Tentar fazer parse da resposta para obter informações detalhadas
                var responseData = JsonSerializer.Deserialize<AtualizarIdExternoResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (responseData != null)
                {
                    _logger.LogInformation("Successfully updated {TotalAtualizados} out of {TotalRecebidos} units with correlation ID {CorrelationId}",
                        responseData.TotalAtualizados, responseData.TotalRecebidos, correlationId);

                    return Ok(responseData);
                }

                // Fallback se não conseguir deserializar
                return Ok(new AtualizarIdExternoResponse
                {
                    Success = true,
                    Message = "Processamento concluído com sucesso",
                    TotalAtualizados = 0,
                    TotalRecebidos = request.Unidades.Count,
                    ProcessedAt = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Failed to update unit external IDs. Status: {StatusCode}, Error: {Error}, Correlation ID: {CorrelationId}",
                    response.StatusCode, responseContent, correlationId);

                return StatusCode((int)response.StatusCode, new AtualizarIdExternoResponse
                {
                    Success = false,
                    Message = "Erro ao atualizar id_externo das unidades",
                    Error = responseContent,
                    StatusCode = (int)response.StatusCode,
                    TotalAtualizados = 0,
                    TotalRecebidos = request.Unidades.Count,
                    ProcessedAt = DateTime.UtcNow
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during unit external ID update");

            return StatusCode(503, new AtualizarIdExternoResponse
            {
                Success = false,
                Message = "Erro de comunicação com o sistema TRS-API",
                Error = ex.Message,
                StatusCode = 503,
                TotalAtualizados = 0,
                TotalRecebidos = request?.Unidades?.Count ?? 0,
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout during unit external ID update");

            return StatusCode(408, new AtualizarIdExternoResponse
            {
                Success = false,
                Message = "Timeout na requisição para o sistema TRS-API",
                Error = ex.Message,
                StatusCode = 408,
                TotalAtualizados = 0,
                TotalRecebidos = request?.Unidades?.Count ?? 0,
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during unit external ID update");

            return StatusCode(500, new AtualizarIdExternoResponse
            {
                Success = false,
                Message = "Erro interno do servidor",
                Error = ex.Message,
                StatusCode = 500,
                TotalAtualizados = 0,
                TotalRecebidos = request?.Unidades?.Count ?? 0,
                ProcessedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Health check endpoint para verificar conectividade com TRS-API
    /// </summary>
    /// <returns>Status de saúde</returns>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            var trsApiUrl = _configuration["HypnoCore:TRSAPI"] ??
                           _configuration["HypnoCore:BaseUrl"] ??
                           "http://localhost:5055";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Tentar alcançar o endpoint de Unidade para verificar conectividade
            var response = await httpClient.GetAsync($"{trsApiUrl}/api/unidade/health");

            return Ok(new
            {
                Service = "HypnoTools.API Unidade",
                TrsApiConnected = response.IsSuccessStatusCode,
                TrsApiStatus = response.StatusCode.ToString(),
                TrsApiUrl = trsApiUrl,
                CheckedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                Service = "HypnoTools.API Unidade",
                TrsApiConnected = false,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Modelo de unidade para atualização de ID externo
/// </summary>
public class AtualizarIdExternoUnidade
{
    public string Identificador_unid { get; set; } = string.Empty;
}

/// <summary>
/// Modelo de requisição para atualização de ID externo
/// </summary>
public class AtualizarIdExternoRequest
{
    public int IdProduto { get; set; }
    public List<AtualizarIdExternoUnidade> Unidades { get; set; } = new();
}

/// <summary>
/// Modelo de resposta para atualização de ID externo
/// </summary>
public class AtualizarIdExternoResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalAtualizados { get; set; }
    public int TotalRecebidos { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
}
