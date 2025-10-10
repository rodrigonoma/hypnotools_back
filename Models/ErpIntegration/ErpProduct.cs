using System.ComponentModel.DataAnnotations;

namespace HypnoTools.API.Models.ErpIntegration;

/// <summary>
/// Modelo base para representar dados de produto vindos do ERP
/// </summary>
public class ErpProduct : BaseEntity
{
    [Required]
    public int ExternalId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ErpProductStatus Status { get; set; } = ErpProductStatus.Active;

    [Required]
    public int ClientId { get; set; }

    // Navigation properties
    public virtual Client Client { get; set; } = null!;
    public virtual ICollection<ErpTower> Towers { get; set; } = new List<ErpTower>();
    public virtual ICollection<ErpTypology> Typologies { get; set; } = new List<ErpTypology>();
    public virtual ICollection<ErpUnit> Units { get; set; } = new List<ErpUnit>();

    // Integration tracking
    public DateTime? LastSyncDate { get; set; }
    public bool IsImported { get; set; } = false;
    public string? ImportNotes { get; set; }
}

public enum ErpProductStatus
{
    Active = 1,
    Inactive = 0,
    Planning = 2,
    Construction = 3,
    Delivered = 4
}