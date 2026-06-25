using CardTask.Core;
using CardTask.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardTask.Web.Pages;

[Authorize]
sealed public class IndexModel(AppDbContext context) : PageModel
{
    private readonly AppDbContext _context = context;

    [BindProperty]
    public string NewCourseCode { get; set; } = string.Empty;

    [BindProperty]
    public string NewCourseName { get; set; } = string.Empty;

    // Track the currently active custom label filter selection
    public string ActiveLabelFilter { get; set; } = string.Empty;

    // Holds the dynamic list of available custom labels found on active tasks
    public List<string> AvailableCustomLabels { get; set; } = new List<string>();

    public List<Course> UserCourses { get; set; } = new List<Course>();
    public List<TodoTask> UpcomingTasks { get; set; } = new List<TodoTask>();

    public async Task<IActionResult> OnGetAsync(string? labelFilter)
    {
        ActiveLabelFilter = labelFilter ?? string.Empty;
        await LoadStudentDataAsync(ActiveLabelFilter);
        return Page();
    }

    public async Task<IActionResult> OnPostAddCourseAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStudentDataAsync(ActiveLabelFilter);
            return Page();
        }

        var studentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(studentEmail)) return RedirectToPage("/Logout");

        var currentStudent = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail.ToLower());

        if (currentStudent == null) return RedirectToPage("/Logout");

        var courseEntry = new Course
        {
            CourseCode = NewCourseCode.Trim().ToUpper(),
            CourseName = NewCourseName.Trim(),
            UserId = currentStudent.Id
        };

        _context.Courses.Add(courseEntry);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Index");
    }

    private async Task LoadStudentDataAsync(string labelFilter)
    {
        var studentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(studentEmail)) return;

        var currentUser = await _context.Users
            .Include(u => u.Courses)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail.ToLower());

        if (currentUser != null)
        {
            UserCourses = currentUser.Courses.ToList();

            // 1. Flatten all pending tasks into an initial queryable collection
            var allActiveTasks = currentUser.Courses
                .SelectMany(c => c.Tasks)
                .Where(t => !t.IsCompleted)
                .ToList();

            // 2. DYNAMICALLY EXTRACTION ENGINE: Pull all unique custom labels currently in use 
            AvailableCustomLabels = allActiveTasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Label))
                .Select(t => t.Label.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(l => l)
                .ToList();

            // 3. Apply the data filter constraint if a pill selection is active
            if (!string.IsNullOrEmpty(labelFilter))
            {
                UpcomingTasks = allActiveTasks
                    .Where(t => t.Label.Equals(labelFilter, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.DueDate)
                    .ToList();
            }
            else
            {
                UpcomingTasks = allActiveTasks
                    .OrderBy(t => t.DueDate)
                    .ToList();
            }
        }
    }
}