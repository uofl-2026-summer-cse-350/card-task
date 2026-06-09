using System.ComponentModel.DataAnnotations;

namespace CardTask.Core.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}