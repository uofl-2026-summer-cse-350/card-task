using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CardTask.Core;
using System.ComponentModel.DataAnnotations;

namespace CardTask.Web.Pages;

sealed public class LoginModel : PageModel
{
    readonly AppDbContext _context;

    public LoginModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email address is required to sign in.")]
    [EmailAddress(ErrorMessage = "Invalid email formatting layout.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password configuration required.")]
    public string Password { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // 2. Find the user by email first
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == Email.ToLower());

        // 3. Use BCrypt.Verify to check if the typed password matches the stored hash
        if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        // Success! Route them to the dashboard
        return RedirectToPage("/Index");
    }
}