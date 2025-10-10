using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HypnoTools.API.Models;

public class ImplementationTask : BaseEntity
{
    [Required]
    public int ImplementationId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [StringLength(50)]
    public string? AssignedTo { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public int SortOrder { get; set; }

    // Navigation properties
    [ForeignKey("ImplementationId")]
    public virtual Implementation Implementation { get; set; } = null!;
}

public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Blocked = 3,
    Cancelled = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}