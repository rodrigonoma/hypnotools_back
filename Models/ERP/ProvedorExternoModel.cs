namespace HypnoTools.API.Models.ERP;

public class ProvedorExternoModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string UrlBase { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public int Provedor { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class ProvedorExternoRequestModel
{
    public string? Empresa { get; set; }
    public List<string>? Empresas { get; set; }
    public int? Provedor { get; set; }
}