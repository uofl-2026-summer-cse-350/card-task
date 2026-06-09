using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTask.Core.Models;

public class TodoTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    // Example: "Read Chapter 4 and complete exercises"
    public string Title { get; set; } = string.Empty;

    [Required]
    // This holds task labels like "Assignment", "Homework", "Exam"
    [StringLength(50)]
    public string Label { get; set; } = "Assignment";

    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);

    public bool IsCompleted { get; set; } = false;

    // Foreign Key linking this task back to its parent Course
    [Required]
    public int CourseId { get; set; }

    [ForeignKey("CourseId")]
    public Course? Course { get; set; }
}