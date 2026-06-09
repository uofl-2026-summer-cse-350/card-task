using CardTask.Core;
using CardTask.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CardTask.Web.Pages;

[Authorize]
sealed public class SettingsModel(AppDbContext context) : PageModel
{
    private readonly AppDbContext _context = context;

    // Password Change Input Binding Models
    [BindProperty]
    [Required(ErrorMessage = "New password hash allocation target required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Confirmation credential string required.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Dynamic Tracking Properties for the Front UI 
    public int TotalCoursesCount { get; set; }
    public int PendingTasksCount { get; set; }
    public int CompletedTasksCount { get; set; }

    public List<Course> UserCourses { get; set; } = new List<Course>();

    // UI Toast Notification Variables
    [TempData] public string SuccessMessage { get; set; } = string.Empty;
    [TempData] public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDiagnosticsAndSidebarAsync();
        return Page();
    }

    // ACTION: SECURE PASSWORD MODIFICATION USING BCRYPT
    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        // Ignore structural diagnostics model loops from blocking form states
        ModelState.Remove(nameof(UserCourses));

        if (!ModelState.IsValid)
        {
            await LoadDiagnosticsAndSidebarAsync();
            return Page();
        }

        var studentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(studentEmail)) return RedirectToPage("/Logout");

        var student = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail.ToLower());
        if (student == null) return RedirectToPage("/Logout");

        // Overwrite old password storage using strong cryptographic hashing
        student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        await _context.SaveChangesAsync();

        SuccessMessage = "Security password key updated and hashed safely to SQL Server database.";
        return RedirectToPage();
    }

    // ACTION: PROGRAMMATIC PURGE OF ACTIVE USER DATA (PRESENTATION MODE UTILITY)
    public async Task<IActionResult> OnPostResetDatabaseAsync()
    {
        var studentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(studentEmail)) return RedirectToPage("/Logout");

        var student = await _context.Users
            .Include(u => u.Courses)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail.ToLower());

        if (student != null && student.Courses.Any())
        {
            // Cascade delete user-linked database columns
            _context.Courses.RemoveRange(student.Courses);
            await _context.SaveChangesAsync();

            SuccessMessage = "Profile infrastructure successfully purged to default state entries.";
        }
        else
        {
            ErrorMessage = "No active database data remnants discovered to clean.";
        }

        return RedirectToPage();
    }

    private async Task LoadDiagnosticsAndSidebarAsync()
    {
        var studentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(studentEmail)) return;

        var student = await _context.Users
            .Include(u => u.Courses)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail.ToLower());

        if (student != null)
        {
            UserCourses = student.Courses.ToList();

            // Calculate precise diagnostic table lengths for the system panels
            TotalCoursesCount = student.Courses.Count;
            PendingTasksCount = student.Courses.SelectMany(c => c.Tasks).Count(t => !t.IsCompleted);
            CompletedTasksCount = student.Courses.SelectMany(c => c.Tasks).Count(t => t.IsCompleted);
        }
    }
}