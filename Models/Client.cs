using System.ComponentModel.DataAnnotations;

namespace HypnoTools.API.Models;

public class Client : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [StringLength(50)]
    public string Company { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Domain { get; set; }

    public ClientStatus Status { get; set; } = ClientStatus.Prospect;

    public DateTime? ImplementationStartDate { get; set; }

    public DateTime? GoLiveDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<Implementation> Implementations { get; set; } = new List<Implementation>();
}

public enum ClientStatus
{
    Prospect = 0,
    InImplementation = 1,
    Active = 2,
    Paused = 3,
    Cancelled = 4
}