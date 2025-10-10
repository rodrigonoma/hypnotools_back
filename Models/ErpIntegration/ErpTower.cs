using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HypnoTools.API.Models.ErpIntegration;

public class ErpTower : BaseEntity
{
    [Required]
    public int ErpProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int QuantityFloors { get; set; }

    public int QuantityColumns { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int Status { get; set; } = 1;

    public int GroundFloor { get; set; } = 0;

    public int UnitPattern { get; set; } = 1;

    // ERP specific fields
    public int? ExternalId { get; set; }
    public string? ExternalCode { get; set; }

    // Navigation properties
    [ForeignKey("ErpProductId")]
    public virtual ErpProduct ErpProduct { get; set; } = null!;
}