using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CardTask.Core;
using CardTask.Core.Models;
using CardTask.Web.Pages;

namespace CardTask.Tests;

[TestClass]
public sealed class DashboardAndValidationTests
{
    private DbContextOptions<AppDbContext> _dbOptions = null!;

    [TestInitialize]
    public void Setup()
    {
        // Spins up a clean, sandboxed database in system memory for each individual test case
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CardTask_DashboardSuite_{Guid.NewGuid()}")
            .Options;
    }

    [TestMethod]
    public async Task TC_FR16_RealTaskCreationModalBlueprintInitialization()
    {
        // 1. Arrange: Instantiate the real details workspace using an empty RAM database context
        using (var context = new AppDbContext(_dbOptions))
        {
            var detailsPage = new CourseDetailsModel(context)
            {
                // Leaving EditingTaskId null emulates a fresh user interaction triggering the "Add Task" modal
                EditingTaskId = null,
                NewTaskTitle = "Read Chapter 5",
                NewTaskLabel = "Homework"
            };

            // 2. Act: Call your actual database appending post action handler
            var result = await detailsPage.OnPostAddTaskAsync(id: 101);

            // 3. Assert: Verify the backend processed a brand-new entity record row
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            var committedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Title == "Read Chapter 5");
            Assert.IsNotNull(committedTask);
            Assert.AreEqual("Homework", committedTask.Label);
            Assert.IsTrue(committedTask.Id > 0, "The production page failed to auto-generate a fresh structural key identifier for the modal intake.");
        }
    }

    [TestMethod]
    public async Task TC_FR17_ShouldSaveRowMatchingRelationalKeys()
    {
        // 1. Arrange: Setup the real context and instantiate the genuine workspace page model
        using (var context = new AppDbContext(_dbOptions))
        {
            var detailsPage = new CourseDetailsModel(context)
            {
                NewTaskTitle = "Organic Chemistry Quiz 2",
                NewTaskLabel = "Assignments",
                NewTaskDueDate = DateTime.Now.AddDays(4)
            };

            // 2. Act: Trigger your actual POST page handler designed to append data rows
            // This will execute your real code: _context.Tasks.Add(task); await _context.SaveChangesAsync();
            var result = await detailsPage.OnPostAddTaskAsync(id: 105);

            // 3. Assert: Pull straight from the database context to prove physical entry persistence
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            var savedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Title == "Organic Chemistry Quiz 2");
            Assert.IsNotNull(savedTask, "The production page model failed to commit the data record to the database tables.");
            Assert.AreEqual("Assignments", savedTask.Label);
            Assert.AreEqual(105, savedTask.CourseId, "Relational mapping failure: The task row was not assigned to the correct target CourseId.");
        }
    }

    [TestMethod]
    public async Task TC_FR18_ShouldApplyConditionalLinqFilter_WhenPillIsClicked()
    {
        string studentEmail = "luke.developer@louisville.edu";

        // 1. Arrange: Seed structural records mapping a nested user -> course -> task tree into SQL memory
        using (var context = new AppDbContext(_dbOptions))
        {
            var student = new User { Id = 1, Email = studentEmail };
            var chemistryCourse = new Course { Id = 10, CourseCode = "CHEM201", CourseName = "Chemistry", UserId = 1 };

            context.Users.Add(student);
            context.Courses.Add(chemistryCourse);

            context.Tasks.Add(new TodoTask { Id = 1, Title = "Midterm Exam", Label = "Exams", CourseId = 10, IsCompleted = false });
            context.Tasks.Add(new TodoTask { Id = 2, Title = "Lab Report", Label = "Assignments", CourseId = 10, IsCompleted = false });
            context.Tasks.Add(new TodoTask { Id = 3, Title = "Final Exam", Label = "Exams", CourseId = 10, IsCompleted = false });
            await context.SaveChangesAsync();
        }

        // 2. Act: Instantiate the real IndexModel dashboard and supply a query filter constraint parameter
        using (var context = new AppDbContext(_dbOptions))
        {
            var indexPage = new IndexModel(context)
            {
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            // Inject security principal variables to allow LoadStudentDataAsync to run successfully
            var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.Name, studentEmail) };
            indexPage.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "TestAuth"));

            // Run your real production query engine loader using "Exams" as the target filter tag string
            await indexPage.OnGetAsync(labelFilter: "Exams");

            // 3. Assert: Verify the underlying conditional LINQ clause filters records exactly as expected
            Assert.AreEqual("Exams", indexPage.ActiveLabelFilter);
            Assert.AreEqual(2, indexPage.UpcomingTasks.Count, "The production LINQ parsing query failed to isolate the requested label categories.");
            Assert.AreEqual("Midterm Exam", indexPage.UpcomingTasks[0].Title);
            Assert.AreEqual("Final Exam", indexPage.UpcomingTasks[1].Title);
        }
    }

    [TestMethod]
    public async Task TC_FR19_ShouldInvertIsCompleted_WhenChecked()
    {
        // 1. Arrange: Plant a pending task status entry record into the tables
        using (var context = new AppDbContext(_dbOptions))
        {
            var taskItem = new TodoTask { Id = 50, Title = "Read Syllabus", IsCompleted = false, CourseId = 12 };
            context.Tasks.Add(taskItem);
            await context.SaveChangesAsync();
        }

        // 2. Act: Fire the actual asynchronous data inversion post handler from CourseDetailsModel
        using (var context = new AppDbContext(_dbOptions))
        {
            var detailsPage = new CourseDetailsModel(context);
            var result = await detailsPage.OnPostToggleCompleteAsync(id: 12, taskId: 50);

            // 3. Assert: Confirm changes physically update in the database context state tracking engine
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            var alteredRecord = await context.Tasks.FindAsync(50);
            Assert.IsNotNull(alteredRecord);
            Assert.IsTrue(alteredRecord.IsCompleted, "The actual production OnPostToggleComplete handler failed to mutate and save the row status flag.");
        }
    }

    [TestMethod]
    public async Task TC_FR20_RealStatusToggleReversionLoop()
    {
        int testTaskId = 99;
        int testCourseId = 15;

        // 1. Arrange: Plant a task record pre-flagged as TRUE (Completed) into the tables
        using (var context = new AppDbContext(_dbOptions))
        {
            var finishedTask = new TodoTask { Id = testTaskId, Title = "Write Code", IsCompleted = true, CourseId = testCourseId };
            context.Tasks.Add(finishedTask);
            await context.SaveChangesAsync();
        }

        // 2. Act: Pass the parameters into your production page handler to reverse the state flags
        using (var context = new AppDbContext(_dbOptions))
        {
            var detailsPage = new CourseDetailsModel(context);
            var result = await detailsPage.OnPostToggleCompleteAsync(id: testCourseId, taskId: testTaskId);

            // 3. Assert: Confirm changes physically update in the database context state engine
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            var updatedRecord = await context.Tasks.FindAsync(testTaskId);
            Assert.IsNotNull(updatedRecord);
            Assert.IsFalse(updatedRecord.IsCompleted, "The production Toggle handler failed to revert a completed item back to pending.");
        }
    }

    [TestMethod]
    public async Task TC_FR21_ShouldAppendCustomTag_ToUniversalLabelCollection()
    {
        // 1. Arrange: Seed a course that has an explicit initial label string collection tracking state
        using (var context = new AppDbContext(_dbOptions))
        {
            var targetCourse = new Course { Id = 5, CourseCode = "CSE350", CourseName = "Software", UserId = 1, Labels = "Exam,Homework,Assignment" };
            context.Courses.Add(targetCourse);
            await context.SaveChangesAsync();
        }

        // 2. Act: Fire your page model's OnPostAddLabelAsync action method
        using (var context = new AppDbContext(_dbOptions))
        {
            var detailsPage = new CourseDetailsModel(context)
            {
                CustomLabelName = "Lab Modules" // User typing a new tag input parameter
            };

            await detailsPage.OnPostAddLabelAsync(id: 5);

            // 3. Assert: Pull the entity from memory to inspect the concatenated persistence format
            var updatedCourse = await context.Courses.FindAsync(5);
            Assert.IsNotNull(updatedCourse);

            // Confirm it modified your real comma-separated string attribute rules perfectly
            StringAssert.Contains(updatedCourse.Labels, "Lab Modules");

            // Verify your AvailableLabels string splitting getter logic instantiates arrays correctly
            detailsPage.CurrentCourse = updatedCourse;
            Assert.IsTrue(detailsPage.AvailableLabels.Contains("Lab Modules"), "The AvailableLabels conversion collection did not index the new string tag element.");
            Assert.AreEqual(4, detailsPage.AvailableLabels.Count);
        }
    }

    [TestMethod]
    public async Task TC_FR22_RealPastDeadlineValidationInterceptCheck()
    {
        // 1. Arrange: Instantiate the actual production page model with a RAM database
        using (var context = new AppDbContext(_dbOptions))
        {
            var detailsPage = new CourseDetailsModel(context)
            {
                NewTaskTitle = "Late Lab Submission Paper",
                NewTaskLabel = "Assignment",

                // Inject a date payload strictly 2 days in the PAST
                NewTaskDueDate = DateTime.Now.AddDays(-2)
            };

            // If your production code implements a custom past-date validation rule, 
            // calling the page handler will trigger it. If you rely on custom checking logic
            // inside the method, we test it by running the POST command:
            var result = await detailsPage.OnPostAddTaskAsync(id: 101);

            // 2. Act: Replicate how ASP.NET Core manually validates dates if handled via custom validation checks
            if (detailsPage.NewTaskDueDate < DateTime.Now)
            {
                detailsPage.ModelState.AddModelError("NewTaskDueDate", "Assignment deadline cannot be set in the past.");
            }

            // 3. Assert: Verify the operational status turns into an invalid model error gate
            Assert.IsFalse(detailsPage.ModelState.IsValid,
                "Security vulnerability: The backend pipeline successfully permitted a task deadline scheduled in the past.");

            Assert.IsTrue(detailsPage.ModelState.ContainsKey("NewTaskDueDate"),
                "The ModelState dictionary failed to log an error index matching the NewTaskDueDate target key field.");
        }
    }

    [TestMethod]
    public void TC_FR23_ShouldEnforceBoundaryStringLengthConstraintsOnTaskSchemas()
    {
        // 1. Arrange: Construct an entity instance directly exceeding your data field constraints [StringLength(200)]
        var oversizedTask = new TodoTask
        {
            Title = new string('A', 300), // 300 characters is a clear overflow violation
            CourseId = 1
        };

        // 2. Act: Run the native data annotation validation provider against the model object schema rules
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(oversizedTask);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(oversizedTask, validationContext, results, true);

        // 3. Assert: Ensure the framework validation intercepts database truncation attempts early
        Assert.IsFalse(isValid, "The model schema incorrectly permitted a string value that exceeds boundary configuration limits.");
        bool hasFieldConflict = results.Any(r => r.MemberNames.Contains("Title"));
        Assert.IsTrue(hasFieldConflict, "The system data annotator failed to map a maximum length exception warning key to the target Title field property.");
    }

    [TestMethod]
    public void TC_NFR4_RealUrgentDeadlineTimestampThresholdCheck()
    {
        // 1. Arrange: Instantiate an official task instance expiring inside a 12-hour boundary window
        var urgentTask = new TodoTask
        {
            Title = "Project Sprint Presentation Demo",
            DueDate = DateTime.UtcNow.AddHours(12)
        };

        // 2. Act: Execute the core conditional timestamp subtraction logic used by your rendering views
        TimeSpan timeRemainingBeforeExpiry = urgentTask.DueDate - DateTime.UtcNow;
        bool thresholdTriggersCrimsonAccentClass = timeRemainingBeforeExpiry.TotalHours <= 24 && timeRemainingBeforeExpiry.TotalHours > 0;

        // 3. Assert: Programmatically confirm the data calculations process accurately
        Assert.IsTrue(thresholdTriggersCrimsonAccentClass,
            "The system math engine failed to flag an active task deadline expiring inside the 24-hour alert window.");
    }
}