using HypnoTools.API.Models.ERP;
using HypnoTools.API.Services.Auth;
using System.Text;
using System.Text.Json;

namespace HypnoTools.API.Services.ERP;

// Modelo para mapear dados das obras do HypnoCore

public class HypnoCoreObraModel
{
    public string Cod_obr { get; set; } = string.Empty;
    public int Empresa_obr { get; set; }
    public string Descr_obr { get; set; } = string.Empty;
    public int Status_obr { get; set; }
    public string Ender_obr { get; set; } = string.Empty;
    public string Fone_obr { get; set; } = string.Empty;
    public string Fisc_obr { get; set; } = string.Empty;
    public DateTime DtIni_obr { get; set; }
    public DateTime Dtfim_obr { get; set; }
    public int TipoObra_obr { get; set; }
    public string EnderEntr_obr { get; set; } = string.Empty;
    public string? CEI_obr { get; set; }
    public DateTime DataCad_obr { get; set; }
    public DateTime DataAlt_obr { get; set; }
    public string UsrCad_obr { get; set; } = string.Empty;
}

public class ERPIntegrationService : IERPIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ERPIntegrationService> _logger;
    private readonly IHypnoCoreAuthService _authService;

    public ERPIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ERPIntegrationService> logger,
        IHypnoCoreAuthService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authService = authService;

        var hypnoCoreBaseUrl = _configuration["HypnoCore:CRMAPI"] ?? "http://localhost:5180";
        _httpClient.BaseAddress = new Uri($"{hypnoCoreBaseUrl}/");
    }

    private void SetAuthorizationHeader()
    {
        var token = _authService.GetCurrentUserToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    public async Task<List<ProvedorExternoModel>> GetProvedoresExternosAsync(string empresa, int? provedor = null)
    {
        try
        {
            SetAuthorizationHeader();

            var request = new ProvedorExternoRequestModel
            {
                Empresa = empresa,
                Provedor = provedor ?? 2 // UAU = 2
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Requesting external providers for company {Empresa}", empresa);

            var response = await _httpClient.PostAsync("api/Integracao/GetAllProvedorExterno", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var provedores = JsonSerializer.Deserialize<List<ProvedorExternoModel>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ProvedorExternoModel>();

                _logger.LogInformation("Found {Count} external providers for company {Empresa}", provedores.Count, empresa);
                return provedores;
            }

            _logger.LogWarning("Failed to get external providers. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);

            return new List<ProvedorExternoModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external providers for company {Empresa}", empresa);
            return new List<ProvedorExternoModel>();
        }
    }

    public async Task<List<EmpresaAtivaModel>> ObterEmpresasAtivasAsync(string empresa)
    {
        try
        {
            SetAuthorizationHeader();

            _logger.LogInformation("Requesting active companies for empresa {Empresa}", empresa);

            var response = await _httpClient.GetAsync($"api/ERP/obter-empresas-ativas/{empresa}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var empresasAtivas = JsonSerializer.Deserialize<List<EmpresaAtivaModel>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<EmpresaAtivaModel>();

                _logger.LogInformation("Retrieved {Count} active companies for empresa {Empresa}", empresasAtivas.Count, empresa);
                return empresasAtivas;
            }

            _logger.LogWarning("Failed to get active companies. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);

            return new List<EmpresaAtivaModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active companies for empresa {Empresa}", empresa);
            return new List<EmpresaAtivaModel>();
        }
    }

    public async Task<List<ObraAtivaModel>> ObterObrasAtivasAsync(string empresa)
    {
        try
        {
            SetAuthorizationHeader();

            _logger.LogInformation("Requesting active projects for empresa {Empresa}", empresa);

            var response = await _httpClient.GetAsync($"api/ERP/obter-obras-ativas/{empresa}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // CRM-API agora retorna ObraAtivaDto com camelCase e IdProduto
                var obrasAtivas = JsonSerializer.Deserialize<List<ObraAtivaModel>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ObraAtivaModel>();

                _logger.LogInformation("Retrieved {Count} active projects for empresa {Empresa}", obrasAtivas.Count, empresa);

                // Log para debug
                if (obrasAtivas.Any())
                {
                    var primeira = obrasAtivas.First();
                    _logger.LogInformation("First project mapped: CodigoObra={CodigoObra}, NomeObra={NomeObra}, StatusObra={StatusObra}, EmpresaObra={EmpresaObra}, IdProduto={IdProduto}",
                        primeira.CodigoObra, primeira.NomeObra, primeira.StatusObra, primeira.EmpresaObra, primeira.IdProduto);
                }

                return obrasAtivas;
            }

            _logger.LogWarning("Failed to get active projects. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);

            return new List<ObraAtivaModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active projects for empresa {Empresa}", empresa);
            return new List<ObraAtivaModel>();
        }
    }

    private ObraAtivaModel MapHypnoCoreToObraAtiva(HypnoCoreObraModel hypnoCoreObra)
    {
        return new ObraAtivaModel
        {
            CodigoObra = hypnoCoreObra.Cod_obr,
            EmpresaObra = hypnoCoreObra.Empresa_obr,
            NomeObra = hypnoCoreObra.Descr_obr,
            StatusObra = MapStatusObra(hypnoCoreObra.Status_obr),
            DataInicio = hypnoCoreObra.DtIni_obr,
            DataPrevisaoTermino = hypnoCoreObra.Dtfim_obr
        };
    }

    private string MapStatusObra(int statusObr)
    {
        return statusObr switch
        {
            0 => "Ativo",
            1 => "Inativo",
            2 => "Em Planejamento",
            3 => "Em Andamento",
            4 => "Finalizado",
            5 => "Suspenso",
            _ => "Não Definido"
        };
    }


    public async Task<List<UnidadeDetalhadaModel>> BuscarUnidadesDetalhadasAsync(string empresa, string codigoObra)
    {
        try
        {
            SetAuthorizationHeader();

            _logger.LogInformation("Requesting detailed units for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);

            var response = await _httpClient.GetAsync($"api/ERP/buscar-unidades-detalhadas/{empresa}/{codigoObra}");
            var responseContent = await response.Content.ReadAsStringAsync();


            if (response.IsSuccessStatusCode)
            {
                // O HypnoCore já retorna dados no formato correto, usar diretamente
                var unidadesDetalhadas = JsonSerializer.Deserialize<List<UnidadeDetalhadaModel>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<UnidadeDetalhadaModel>();

                _logger.LogInformation("Retrieved {Count} detailed units for empresa {Empresa}, project {CodigoObra}",
                    unidadesDetalhadas.Count, empresa, codigoObra);
                return unidadesDetalhadas;
            }

            _logger.LogWarning("Failed to get detailed units. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);

            return new List<UnidadeDetalhadaModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed units for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);
            return new List<UnidadeDetalhadaModel>();
        }
    }

    public async Task<List<CampoPersonalizadoModel>> BuscarCamposPersonalizadosAsync(string empresa, string codigoObra)
    {
        try
        {
            SetAuthorizationHeader();

            _logger.LogInformation("Requesting custom fields for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);

            var response = await _httpClient.GetAsync($"api/ERP/buscar-campos-person/{empresa}/{codigoObra}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var camposPersonalizados = JsonSerializer.Deserialize<List<CampoPersonalizadoModel>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CampoPersonalizadoModel>();

                _logger.LogInformation("Retrieved {Count} custom fields for empresa {Empresa}, project {CodigoObra}",
                    camposPersonalizados.Count, empresa, codigoObra);
                return camposPersonalizados;
            }

            _logger.LogWarning("Failed to get custom fields. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);

            return new List<CampoPersonalizadoModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom fields for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);
            return new List<CampoPersonalizadoModel>();
        }
    }
}