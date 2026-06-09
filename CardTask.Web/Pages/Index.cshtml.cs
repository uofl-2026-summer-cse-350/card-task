using CardTask.Core;
using CardTask.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardTask.Web.Pages;

[Authorize] // Enforces secure global cookie checking automatically
sealed public class IndexModel(AppDbContext context) : PageModel
{
    private readonly AppDbContext _context = context;

    // Bind Properties for the Quick Add HTML Form Card
    [BindProperty]
    public string NewCourseCode { get; set; } = string.Empty;

    [BindProperty]
    public string NewCourseName { get; set; } = string.Empty;

    // View Collections to stream active database rows to the HTML interface Canvas
    public List<Course> UserCourses { get; set; } = new List<Course>();
    public List<TodoTask> UpcomingTasks { get; set; } = new List<TodoTask>();

    // 1. Fires naturally when visiting/loading the dashboard home route
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadStudentDataAsync();
        return Page();
    }

    // 2. Named Handler Interceptor: Fires exactly when clicking 'Add to Profile'
    public async Task<IActionResult> OnPostAddCourseAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStudentDataAsync();
            return Page();
        }

        var studentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(studentEmail)) return RedirectToPage("/Logout");

        // Locate the matching User row to fetch the unique foreign key target ID
        var currentStudent = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == studentEmail.ToLower());

        if (currentStudent == null) return RedirectToPage("/Logout");

        // Construct the tracking database object schema instance 
        var courseEntry = new Course
        {
            CourseCode = NewCourseCode.Trim().ToUpper(),
            CourseName = NewCourseName.Trim(),
            UserId = currentStudent.Id
        };

        // Inject the entity object row into our EF context tracker container
        _context.Courses.Add(courseEntry);
        await _context.SaveChangesAsync();

        // Perform a clean post-redirect-get refresh state
        return RedirectToPage("/Index");
    }

    // Core helper utility module to keep data fetching centralized and clean
    private async Task LoadStudentDataAsync()
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

            UpcomingTasks = currentUser.Courses
                .SelectMany(c => c.Tasks)
                .Where(t => !t.IsCompleted)
                .OrderBy(t => t.DueDate)
                .ToList();
        }
    }
}