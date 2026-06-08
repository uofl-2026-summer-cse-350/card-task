using CardTask.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CardTask.Web.Pages;

public class LoginModel : PageModel
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        // --- SUCCESS ENGINE: ISSUE MIDDLEWARE COOKIE ---

        // 1. Create the user's identity passport claims data
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("UserId", user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // 2. Write the encrypted secure cookie tracking block directly into the browser
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        // 3. Route them directly into the application layout shell
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostDevBypassAsync()
    {
        #if DEBUG
        // 1. Forge an immediate, valid deployment identity passport
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "dev.bypass@louisville.edu"),
            new("UserId", "0")
        };

        // FIX: Change 'Identity' to 'ClaimsIdentity' right here!
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // 2. Drop the cookie passport directly into the browser pipeline 
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        // 3. Teleport straight to the dashboard workspace shell
        return RedirectToPage("/Index");
        #else
        return BadRequest("Developer tools are restricted.");
        #endif
    }
}