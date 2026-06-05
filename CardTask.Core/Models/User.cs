using System.ComponentModel.DataAnnotations;

namespace CardTask.Core.Models;

sealed public class User
{
    [Key]
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}