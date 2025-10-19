using HypnoTools.API.Models.ERP;
using HypnoTools.API.Models.ImportacaoProduto;
using HypnoTools.API.Services.ImportacaoProduto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HypnoTools.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportacaoProdutoController : ControllerBase
{
    private readonly IImportacaoProdutoService _importacaoService;
    private readonly ILogger<ImportacaoProdutoController> _logger;

    public ImportacaoProdutoController(
        IImportacaoProdutoService importacaoService,
        ILogger<ImportacaoProdutoController> logger)
    {
        _importacaoService = importacaoService;
        _logger = logger;
    }

    /// <summary>
    /// Transformar dados do ERP para modelo de importação
    /// </summary>
    /// <param name="request">Dados do ERP para transformação</param>
    /// <returns>Dados transformados para importação</returns>
    [HttpPost("transformar-dados-erp")]
    public ActionResult<ImportacaoProdutoModel> TransformarDadosERP([FromBody] TransformacaoERPRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CodigoObra))
            {
                return BadRequest("Código da obra é obrigatório");
            }

            if (string.IsNullOrEmpty(request.NomeObra))
            {
                return BadRequest("Nome da obra é obrigatório");
            }

            if (request.Unidades == null || !request.Unidades.Any())
            {
                return BadRequest("Lista de unidades não pode estar vazia");
            }

            _logger.LogInformation("Transforming ERP data for project {CodigoObra} with {UnidadeCount} units",
                request.CodigoObra, request.Unidades.Count);

            var dadosTransformados = _importacaoService.TransformarDadosERP(
                request.CodigoObra,
                request.NomeObra,
                request.Unidades);

            return Ok(dadosTransformados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming ERP data for project {CodigoObra}",
                request.CodigoObra);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Importar produto do ERP para o sistema Transacional via HypnoCore
    /// </summary>
    /// <param name="request">Dados do produto para importação</param>
    /// <returns>Resultado da importação</returns>
    [HttpPost("importar-produto-erp")]
    public async Task<ActionResult<ImportacaoResultModel>> ImportarProdutoERP([FromBody] ImportacaoERPRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Empresa))
            {
                return BadRequest(new ImportacaoResultModel
                {
                    Success = false,
                    Message = "Empresa é obrigatória",
                    Erros = new List<string> { "Campo empresa não informado" }
                });
            }

            if (string.IsNullOrEmpty(request.CodigoObra))
            {
                return BadRequest(new ImportacaoResultModel
                {
                    Success = false,
                    Message = "Código da obra é obrigatório",
                    Erros = new List<string> { "Campo codigoObra não informado" }
                });
            }

            if (request.Unidades == null || !request.Unidades.Any())
            {
                return BadRequest(new ImportacaoResultModel
                {
                    Success = false,
                    Message = "Lista de unidades não pode estar vazia",
                    Erros = new List<string> { "Nenhuma unidade informada para importação" }
                });
            }

            _logger.LogInformation("Starting product import for empresa {Empresa}, project {CodigoObra} with {UnidadeCount} units",
                request.Empresa, request.CodigoObra, request.Unidades.Count);

            var resultado = await _importacaoService.ImportarProdutoERPAsync(
                request.Empresa,
                request.CodigoObra,
                request.Unidades);

            if (resultado.Success)
            {
                _logger.LogInformation("Product import completed successfully for project {CodigoObra}. {UnidadesImportadas}/{TotalUnidades} units imported",
                    request.CodigoObra, resultado.UnidadesImportadas, resultado.TotalUnidades);
            }
            else
            {
                _logger.LogWarning("Product import failed for project {CodigoObra}: {Message}",
                    request.CodigoObra, resultado.Message);
            }

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during product import for project {CodigoObra}",
                request.CodigoObra);

            return StatusCode(500, new ImportacaoResultModel
            {
                Success = false,
                Message = "Erro interno do servidor",
                TotalUnidades = request.Unidades?.Count ?? 0,
                UnidadesImportadas = 0,
                Erros = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Importa estrutura completa de produto (Torres, Tipologias, Unidades) via HypnoCore
    /// </summary>
    [HttpPost("importar-estrutura")]
    public async Task<ActionResult<ImportacaoProdutoResponseModel>> ImportarEstruturaProduto(
        [FromBody] ImportacaoProdutoRequestModel request)
    {
        try
        {
            if (request.IdProduto <= 0)
            {
                return BadRequest(new ImportacaoProdutoResponseModel
                {
                    Success = false,
                    Message = "ID do produto é obrigatório"
                });
            }

            if (request.Torres == null || !request.Torres.Any())
            {
                return BadRequest(new ImportacaoProdutoResponseModel
                {
                    Success = false,
                    Message = "Pelo menos uma torre deve ser informada"
                });
            }

            if (request.Tipologias == null || !request.Tipologias.Any())
            {
                return BadRequest(new ImportacaoProdutoResponseModel
                {
                    Success = false,
                    Message = "Pelo menos uma tipologia deve ser informada"
                });
            }

            if (request.Unidades == null || !request.Unidades.Any())
            {
                return BadRequest(new ImportacaoProdutoResponseModel
                {
                    Success = false,
                    Message = "Pelo menos uma unidade deve ser informada"
                });
            }

            _logger.LogInformation("Iniciando importação de estrutura para produto ID {IdProduto}", request.IdProduto);
            _logger.LogInformation("Torres: {TorresCount}, Tipologias: {TipologiasCount}, Unidades: {UnidadesCount}",
                request.Torres.Count, request.Tipologias.Count, request.Unidades.Count);

            var resultado = await _importacaoService.ImportarEstruturaProdutoAsync(request);

            if (resultado.Success)
            {
                _logger.LogInformation("Importação concluída com sucesso para produto ID {IdProduto}", request.IdProduto);
                return Ok(resultado);
            }
            else
            {
                _logger.LogWarning("Importação falhou para produto ID {IdProduto}: {Message}",
                    request.IdProduto, resultado.Message);
                return BadRequest(resultado);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar estrutura do produto ID {IdProduto}", request.IdProduto);

            return StatusCode(500, new ImportacaoProdutoResponseModel
            {
                Success = false,
                Message = "Erro interno ao processar importação",
                Error = ex.Message
            });
        }
    }
}

public class TransformacaoERPRequest
{
    public string CodigoObra { get; set; } = string.Empty;
    public string NomeObra { get; set; } = string.Empty;
    public List<UnidadeDetalhadaModel> Unidades { get; set; } = new();
}

public class ImportacaoERPRequest
{
    public string Empresa { get; set; } = string.Empty;
    public string CodigoObra { get; set; } = string.Empty;
    public List<UnidadeDetalhadaModel> Unidades { get; set; } = new();
}