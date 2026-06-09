using CardTask.Core;
using CardTask.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CardTask.Web.Pages;

[Authorize]
public class CourseDetailsModel(AppDbContext context) : PageModel
{
    private readonly AppDbContext _context = context;

    [BindProperty]
    [Required, StringLength(200)]
    public string NewTaskTitle { get; set; } = string.Empty;

    [BindProperty]
    public string NewTaskLabel { get; set; } = "Assignment";

    [BindProperty]
    public DateTime NewTaskDueDate { get; set; } = DateTime.Now.AddDays(7);

    [BindProperty]
    public string CustomLabelName { get; set; } = string.Empty;

    [BindProperty]
    public string LabelToDelete { get; set; } = string.Empty;

    [BindProperty]
    public int? EditingTaskId { get; set; }

    public Course? CurrentCourse { get; set; }
    public List<Course> UserCourses { get; set; } = new();

    // Database-backed labels property
    public List<string> AvailableLabels => string.IsNullOrEmpty(CurrentCourse?.Labels)
        ? new List<string> { "Exam", "Homework", "Assignment" }
        : CurrentCourse.Labels.Split(',').ToList();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadCourseWorkspaceAsync(id);
        if (CurrentCourse == null) return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAddTaskAsync(int id)
    {
        ModelState.Remove(nameof(CustomLabelName));
        ModelState.Remove(nameof(LabelToDelete));

        if (!ModelState.IsValid)
        {
            await LoadCourseWorkspaceAsync(id);
            return Page();
        }

        TodoTask task;
        if (EditingTaskId.HasValue && EditingTaskId.Value > 0)
        {
            task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == EditingTaskId.Value && t.CourseId == id)
                   ?? new TodoTask { CourseId = id };
        }
        else
        {
            task = new TodoTask { CourseId = id };
            _context.Tasks.Add(task);
        }

        task.Title = NewTaskTitle.Trim();
        task.Label = NewTaskLabel;
        task.DueDate = NewTaskDueDate.ToUniversalTime();

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleCompleteAsync(int id, int taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.CourseId == id);
        if (task != null)
        {
            task.IsCompleted = !task.IsCompleted;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddLabelAsync(int id)
    {
        if (!string.IsNullOrWhiteSpace(CustomLabelName))
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                var labelList = string.IsNullOrEmpty(course.Labels) ? new List<string>() : course.Labels.Split(',').ToList();
                if (!labelList.Contains(CustomLabelName.Trim()))
                {
                    labelList.Add(CustomLabelName.Trim());
                    course.Labels = string.Join(",", labelList);
                    await _context.SaveChangesAsync();
                }
            }
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteLabelAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course != null && !string.IsNullOrWhiteSpace(LabelToDelete) && LabelToDelete != "Assignment")
        {
            var labelList = course.Labels.Split(',').ToList();
            if (labelList.Contains(LabelToDelete))
            {
                labelList.Remove(LabelToDelete);
                course.Labels = string.Join(",", labelList);
                await _context.SaveChangesAsync();
            }
        }
        return RedirectToPage(new { id });
    }

    private async Task LoadCourseWorkspaceAsync(int courseId)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrEmpty(email)) return;

        var student = await _context.Users
            .Include(u => u.Courses)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (student != null)
        {
            UserCourses = student.Courses.ToList();
            CurrentCourse = student.Courses.FirstOrDefault(c => c.Id == courseId);
        }
    }
}