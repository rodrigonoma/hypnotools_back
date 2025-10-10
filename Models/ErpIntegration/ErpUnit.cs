using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HypnoTools.API.Models.ErpIntegration;

/// <summary>
/// Modelo para representar unidades vindas do ERP
/// </summary>
public class ErpUnit : BaseEntity
{
    [Required]
    public int ExternalId { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public int? Floor { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PrivateArea { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TotalArea { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? SaleValue { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [StringLength(100)]
    public string? UnitType { get; set; }

    // Foreign Keys
    [Required]
    public int ErpProductId { get; set; }

    public int? ErpTowerId { get; set; }

    public int? ErpTypologyId { get; set; }

    // Navigation properties
    public virtual ErpProduct ErpProduct { get; set; } = null!;
    public virtual ErpTower? ErpTower { get; set; }
    public virtual ErpTypology? ErpTypology { get; set; }

    // Integration tracking
    public DateTime? LastSyncDate { get; set; }
    public bool IsImported { get; set; } = false;
    public string? ImportNotes { get; set; }
}