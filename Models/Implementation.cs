using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HypnoTools.API.Models;

public class Implementation : BaseEntity
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ImplementationStatus Status { get; set; } = ImplementationStatus.Planning;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? PlannedEndDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal ProgressPercentage { get; set; } = 0;

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("ClientId")]
    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ImplementationTask> Tasks { get; set; } = new List<ImplementationTask>();
}

public enum ImplementationStatus
{
    Planning = 0,
    InProgress = 1,
    Testing = 2,
    Completed = 3,
    OnHold = 4,
    Cancelled = 5
}