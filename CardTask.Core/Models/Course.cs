using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTask.Core.Models;

public class Course
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    public string Labels { get; set; } = "Exam,Homework,Assignment";

    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
}