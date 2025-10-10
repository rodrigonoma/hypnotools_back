using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HypnoTools.API.Models.ErpIntegration;

public class ErpTypology : BaseEntity
{
    [Required]
    public int ErpProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Tipology { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UsableArea { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalArea { get; set; }

    public int IsDefault { get; set; } = 0; // padrao field

    // ERP specific fields
    public int? ExternalId { get; set; }
    public string? ExternalCode { get; set; }

    // Navigation properties
    [ForeignKey("ErpProductId")]
    public virtual ErpProduct ErpProduct { get; set; } = null!;
    public virtual ICollection<ErpTypologyProperty> Properties { get; set; } = new List<ErpTypologyProperty>();
}

public class ErpTypologyProperty : BaseEntity
{
    [Required]
    public int ErpTypologyId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    // Navigation properties
    [ForeignKey("ErpTypologyId")]
    public virtual ErpTypology ErpTypology { get; set; } = null!;
}