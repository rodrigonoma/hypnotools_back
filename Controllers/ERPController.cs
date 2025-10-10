using HypnoTools.API.Models.ERP;
using HypnoTools.API.Services.ERP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HypnoTools.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ERPController : ControllerBase
{
    private readonly IERPIntegrationService _erpService;
    private readonly ILogger<ERPController> _logger;

    public ERPController(IERPIntegrationService erpService, ILogger<ERPController> logger)
    {
        _erpService = erpService;
        _logger = logger;
    }

    /// <summary>
    /// Obter provedores externos configurados para uma empresa
    /// </summary>
    /// <param name="empresa">Nome/alias da empresa</param>
    /// <param name="provedor">ID do provedor (opcional, padrão = 2 para UAU)</param>
    /// <returns>Lista de provedores externos</returns>
    [HttpGet("provedores-externos/{empresa}")]
    public async Task<ActionResult<List<ProvedorExternoModel>>> GetProvedoresExternos(
        string empresa,
        [FromQuery] int? provedor = null)
    {
        try
        {
            if (string.IsNullOrEmpty(empresa))
            {
                return BadRequest("Empresa é obrigatória");
            }

            _logger.LogInformation("Getting external providers for company {Empresa}", empresa);

            var provedores = await _erpService.GetProvedoresExternosAsync(empresa, provedor);
            return Ok(provedores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external providers for company {Empresa}", empresa);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obter empresas ativas do ERP UAU
    /// </summary>
    /// <param name="empresa">Nome/alias da empresa</param>
    /// <returns>Lista de empresas ativas no ERP</returns>
    [HttpGet("empresas-ativas/{empresa}")]
    public async Task<ActionResult<List<EmpresaAtivaModel>>> GetEmpresasAtivas(string empresa)
    {
        try
        {
            if (string.IsNullOrEmpty(empresa))
            {
                return BadRequest("Empresa é obrigatória");
            }

            _logger.LogInformation("Getting active companies from ERP for empresa {Empresa}", empresa);

            var empresasAtivas = await _erpService.ObterEmpresasAtivasAsync(empresa);
            return Ok(empresasAtivas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active companies for empresa {Empresa}", empresa);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obter obras/projetos ativos do ERP UAU
    /// </summary>
    /// <param name="empresa">Nome/alias da empresa</param>
    /// <returns>Lista de obras ativas no ERP</returns>
    [HttpGet("obter-obras-ativas/{empresa}")]
    public async Task<ActionResult<List<ObraAtivaModel>>> GetObrasAtivas(string empresa)
    {
        try
        {
            if (string.IsNullOrEmpty(empresa))
            {
                return BadRequest("Empresa é obrigatória");
            }

            _logger.LogInformation("Getting active projects from ERP for empresa {Empresa}", empresa);

            var obrasAtivas = await _erpService.ObterObrasAtivasAsync(empresa);

            // Log para debug
            if (obrasAtivas.Any())
            {
                var primeira = obrasAtivas.First();
                _logger.LogInformation("Controller returning first project: CodigoObra={CodigoObra}, NomeObra={NomeObra}, StatusObra={StatusObra}",
                    primeira.CodigoObra, primeira.NomeObra, primeira.StatusObra);
            }

            return Ok(obrasAtivas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active projects for empresa {Empresa}", empresa);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Buscar unidades detalhadas de uma obra específica
    /// </summary>
    /// <param name="empresa">Nome/alias da empresa</param>
    /// <param name="codigoObra">Código da obra no ERP</param>
    /// <returns>Lista de unidades detalhadas da obra</returns>
    [HttpGet("unidades-detalhadas/{empresa}/{codigoObra}")]
    public async Task<ActionResult<List<UnidadeDetalhadaModel>>> GetUnidadesDetalhadas(
        string empresa,
        string codigoObra)
    {
        try
        {
            if (string.IsNullOrEmpty(empresa))
            {
                return BadRequest("Empresa é obrigatória");
            }

            if (string.IsNullOrEmpty(codigoObra))
            {
                return BadRequest("Código da obra é obrigatório");
            }

            _logger.LogInformation("Getting detailed units from ERP for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);

            var unidadesDetalhadas = await _erpService.BuscarUnidadesDetalhadasAsync(empresa, codigoObra);
            return Ok(unidadesDetalhadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed units for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Buscar campos personalizados de uma obra específica
    /// </summary>
    /// <param name="empresa">Nome/alias da empresa</param>
    /// <param name="codigoObra">Código da obra no ERP</param>
    /// <returns>Lista de campos personalizados da obra</returns>
    [HttpGet("campos-personalizados/{empresa}/{codigoObra}")]
    public async Task<ActionResult<List<CampoPersonalizadoModel>>> GetCamposPersonalizados(string empresa, string codigoObra)
    {
        try
        {
            if (string.IsNullOrEmpty(empresa))
            {
                return BadRequest("Empresa é obrigatória");
            }

            if (string.IsNullOrEmpty(codigoObra))
            {
                return BadRequest("Código da obra é obrigatório");
            }

            _logger.LogInformation("Getting custom fields from ERP for empresa {Empresa}, project {CodigoObra}",
                empresa, codigoObra);

            var camposPersonalizados = await _erpService.BuscarCamposPersonalizadosAsync(empresa, codigoObra);
            return Ok(camposPersonalizados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom fields for empresa {Empresa}, project {CodigoObra}", empresa, codigoObra);
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}