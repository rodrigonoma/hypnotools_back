namespace HypnoTools.API.Models.ImportacaoProduto;

public class ImportacaoProdutoModel
{
    public string CodigoEmpreendimento { get; set; } = string.Empty;
    public string NomeEmpreendimento { get; set; } = string.Empty;
    public List<UnidadeImportacaoModel> Unidades { get; set; } = new();
}

public class UnidadeImportacaoModel
{
    public string CodigoUnidade { get; set; } = string.Empty;
    public string DescricaoUnidade { get; set; } = string.Empty;
    public decimal? AreaPrivativa { get; set; }
    public decimal? AreaTotal { get; set; }
    public decimal? ValorVenda { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TipoUnidade { get; set; }
    public int? Andar { get; set; }
}

public class ImportacaoResultModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalUnidades { get; set; }
    public int UnidadesImportadas { get; set; }
    public List<string> Erros { get; set; } = new();
}