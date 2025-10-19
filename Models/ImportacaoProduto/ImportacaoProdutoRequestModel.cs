using System.Collections.Generic;

namespace HypnoTools.API.Models.ImportacaoProduto;

public class TipologyPropertyModel
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class TorreModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Qty_Floors { get; set; }
    public int Qty_Columns { get; set; }
    public string Delivery_Date { get; set; } = string.Empty; // yyyy-MM-dd
    public string Status { get; set; } = string.Empty;
    public string Ground_Floor { get; set; } = string.Empty;
    public string Unit_Pattern { get; set; } = string.Empty;
    public string? Id_Externo { get; set; }
}

public class TipologyModel
{
    public string Name { get; set; } = string.Empty;
    public string Tipology { get; set; } = string.Empty;
    public string Usable_Area { get; set; } = string.Empty;
    public string Total_Area { get; set; } = string.Empty;
    public int Padrao { get; set; }
    public string? Id_Externo { get; set; }
    public List<TipologyPropertyModel> Properties { get; set; } = new();
}

public class UnidadeModel
{
    public string Id_Torre { get; set; } = string.Empty; // Índice da torre
    public string Id_Tipologia { get; set; } = string.Empty; // Índice da tipologia
    public int Floor { get; set; }
    public string Unity_Number { get; set; } = string.Empty;
    public string? Unity_Number_Custom { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Cadastrar { get; set; } = true;
    public string? Percentage_Unity { get; set; }
    public string? Fase { get; set; }
    public string? Vaga { get; set; }
    public string? Deposito { get; set; }
    public string? Area_Deposito { get; set; }
    public string? Fracao_Ideal { get; set; }
    public string? Id_Externo { get; set; }
}

public class ImportacaoProdutoRequestModel
{
    public int IdProduto { get; set; }
    public List<TorreModel> Torres { get; set; } = new();
    public List<TipologyModel> Tipologias { get; set; } = new();
    public List<UnidadeModel> Unidades { get; set; } = new();
}

public class ImportacaoProdutoResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TotalUnidades { get; set; }
    public string? Error { get; set; }
}
