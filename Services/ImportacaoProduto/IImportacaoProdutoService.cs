using HypnoTools.API.Models.ERP;
using HypnoTools.API.Models.ImportacaoProduto;

namespace HypnoTools.API.Services.ImportacaoProduto;

public interface IImportacaoProdutoService
{
    Task<ImportacaoResultModel> ImportarProdutoERPAsync(string empresa, string codigoObra, List<UnidadeDetalhadaModel> unidades);
    ImportacaoProdutoModel TransformarDadosERP(string codigoObra, string nomeObra, List<UnidadeDetalhadaModel> unidades);
    Task<ImportacaoProdutoResponseModel> ImportarEstruturaProdutoAsync(ImportacaoProdutoRequestModel request);
}