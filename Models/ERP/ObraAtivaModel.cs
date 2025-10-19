namespace HypnoTools.API.Models.ERP;

public class ObraAtivaModel
{
    public string CodigoObra { get; set; } = string.Empty;
    public int EmpresaObra { get; set; }
    public string NomeObra { get; set; } = string.Empty;
    public string StatusObra { get; set; } = string.Empty;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataPrevisaoTermino { get; set; }
    public string? Dtfim_obr { get; set; } // Data de término da obra do ERP
    public int? IdProduto { get; set; } // ID do produto no sistema Transacional (tower products)
}