namespace HypnoTools.API.Models.ERP;

public class UnidadeDetalhadaModel
{
    public string CodigoUnidade { get; set; } = string.Empty;
    public string DescricaoUnidade { get; set; } = string.Empty;
    public string CodigoObra { get; set; } = string.Empty;
    public string NomeObra { get; set; } = string.Empty;
    public decimal? AreaPrivativa { get; set; }
    public decimal? AreaTotal { get; set; }
    public decimal? ValorVenda { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TipoUnidade { get; set; }
    public int? Andar { get; set; }
}