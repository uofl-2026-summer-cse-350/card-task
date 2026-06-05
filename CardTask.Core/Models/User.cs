using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTask.Core.Models;

[Table("Users")] // Explicitly defines the database table name schema
public class User
{
    [Key] // PK
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incrementing identity column
    public int UserId { get; set; }

    [Required(ErrorMessage = "Email schema constraint violated: Field is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password hash schema constraint violated: Field is required.")]
    [StringLength(255)] // Generous length to safely hold the massive BCrypt character string
    public string PasswordHash { get; set; } = string.Empty;
}