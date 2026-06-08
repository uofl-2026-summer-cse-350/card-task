using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CardTask.Core;
using CardTask.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace CardTask.Web.Pages;

public class RegisterModel(AppDbContext context) : PageModel
{
    readonly AppDbContext _context = context;

    [BindProperty]
    [Required(ErrorMessage = "UofL email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid formatting style.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@([a-zA-Z0-9.-]*\.)?louisville\.edu$",
            ErrorMessage = "Registration requires an official @louisville.edu university email address.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "A password must be assigned.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Passwords require at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == Email.ToLower());
        if (emailExists)
        {
            ModelState.AddModelError(string.Empty, "This email is already registered.");
            return Page();
        }

        // 2. Hash the plain text password seamlessly
        string securePasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);

        var newUser = new User
        {
            Email = Email.Trim(),
            PasswordHash = securePasswordHash // 3. Save the scrambled hash to the DB
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Login");
    }
}