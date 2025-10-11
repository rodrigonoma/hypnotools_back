using HypnoTools.API.Models.ERP;
using HypnoTools.API.Models.ImportacaoProduto;
using HypnoTools.API.Services.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HypnoTools.API.Services.ImportacaoProduto;

public class ImportacaoProdutoService : IImportacaoProdutoService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImportacaoProdutoService> _logger;
    private readonly IHypnoCoreAuthService _authService;

    public ImportacaoProdutoService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ImportacaoProdutoService> logger,
        IHypnoCoreAuthService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authService = authService;

        var hypnoCoreBaseUrl = _configuration["HypnoCore:TRSAPI"] ?? "http://localhost:5055";
        _httpClient.BaseAddress = new Uri($"{hypnoCoreBaseUrl}/");
    }

    public ImportacaoProdutoModel TransformarDadosERP(string codigoObra, string nomeObra, List<UnidadeDetalhadaModel> unidades)
    {
        _logger.LogInformation("Transforming ERP data for project {CodigoObra} with {UnidadeCount} units",
            codigoObra, unidades.Count);

        var importacaoModel = new ImportacaoProdutoModel
        {
            CodigoEmpreendimento = codigoObra,
            NomeEmpreendimento = nomeObra,
            Unidades = unidades.Select(u => new UnidadeImportacaoModel
            {
                CodigoUnidade = u.CodigoUnidade,
                DescricaoUnidade = u.DescricaoUnidade,
                AreaPrivativa = u.AreaPrivativa,
                AreaTotal = u.AreaTotal,
                ValorVenda = u.ValorVenda,
                Status = u.Status,
                TipoUnidade = u.TipoUnidade,
                Andar = u.Andar
            }).ToList()
        };

        _logger.LogInformation("Data transformation completed. {UnidadeCount} units ready for import",
            importacaoModel.Unidades.Count);

        return importacaoModel;
    }

    public async Task<ImportacaoResultModel> ImportarProdutoERPAsync(string empresa, string codigoObra, List<UnidadeDetalhadaModel> unidades)
    {
        try
        {
            var token = _authService.GetCurrentUserToken();
            if (string.IsNullOrEmpty(token))
            {
                return new ImportacaoResultModel
                {
                    Success = false,
                    Message = "Token de autenticação não encontrado",
                    Erros = new List<string> { "Usuário não autenticado" }
                };
            }

            // Extrair empresa do token JWT
            string? companyAlias = null;
            try
            {
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

            if (string.IsNullOrEmpty(companyAlias))
            {
                _logger.LogError("Company information not found in JWT token");
                return new ImportacaoResultModel
                {
                    Success = false,
                    Message = "Informação da empresa não encontrada no token de autenticação",
                    Erros = new List<string> { "Claim 'empresa' não encontrada no token JWT" }
                };
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // CRITICAL: Add "Empresa" header - required for TRS-API to forward to Transacional-Proposta
            _httpClient.DefaultRequestHeaders.Add("Empresa", companyAlias);

            _logger.LogInformation("Sending import request for company '{Company}'", companyAlias);

            // Transform ERP data to ImportacaoProdutoRequestModel format
            var importacaoData = TransformarDadosERP(codigoObra, $"Obra_{codigoObra}", unidades);

            // Convert to the format expected by HypnoCore TRS-API (matching Transacional-Proposta structure)
            var requestModel = new
            {
                CodigoEmpreendimento = importacaoData.CodigoEmpreendimento,
                NomeEmpreendimento = importacaoData.NomeEmpreendimento,
                Unidades = importacaoData.Unidades.Select(u => new
                {
                    CodigoUnidade = u.CodigoUnidade,
                    DescricaoUnidade = u.DescricaoUnidade,
                    AreaPrivativa = u.AreaPrivativa,
                    AreaTotal = u.AreaTotal,
                    ValorVenda = u.ValorVenda,
                    Status = u.Status,
                    TipoUnidade = u.TipoUnidade,
                    Andar = u.Andar,
                    // Additional fields that might be required by Transacional-Proposta
                    DataCriacao = DateTime.Now,
                    Ativo = true
                }).ToList(),
                // Additional metadata
                EmpresaOrigem = empresa,
                DataImportacao = DateTime.Now,
                OrigemImportacao = "HypnoTools_ERP_UAU"
            };

            var json = JsonSerializer.Serialize(requestModel, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending import request to HypnoCore TRS-API for project {CodigoObra} with {UnidadeCount} units",
                codigoObra, unidades.Count);

            var response = await _httpClient.PostAsync("api/Produto/ImportarEstruturaProduto", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Product import successful for project {CodigoObra}", codigoObra);

                // Try to parse response to get detailed results
                try
                {
                    var apiResponse = JsonSerializer.Deserialize<dynamic>(responseContent);
                    return new ImportacaoResultModel
                    {
                        Success = true,
                        Message = "Importação realizada com sucesso",
                        TotalUnidades = unidades.Count,
                        UnidadesImportadas = unidades.Count
                    };
                }
                catch
                {
                    // If parsing fails, return success anyway
                    return new ImportacaoResultModel
                    {
                        Success = true,
                        Message = "Importação realizada com sucesso",
                        TotalUnidades = unidades.Count,
                        UnidadesImportadas = unidades.Count
                    };
                }
            }
            else
            {
                _logger.LogWarning("Product import failed for project {CodigoObra}. Status: {StatusCode}, Response: {Response}",
                    codigoObra, response.StatusCode, responseContent);

                return new ImportacaoResultModel
                {
                    Success = false,
                    Message = $"Falha na importação. Status: {response.StatusCode}",
                    TotalUnidades = unidades.Count,
                    UnidadesImportadas = 0,
                    Erros = new List<string> { responseContent }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during product import for project {CodigoObra}", codigoObra);

            return new ImportacaoResultModel
            {
                Success = false,
                Message = "Erro interno durante a importação",
                TotalUnidades = unidades.Count,
                UnidadesImportadas = 0,
                Erros = new List<string> { ex.Message }
            };
        }
    }
}