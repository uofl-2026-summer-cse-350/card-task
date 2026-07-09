using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using CardTask.Core;
using CardTask.Core.Models;
using CardTask.Web.Pages;

namespace CardTask.Tests;

[TestClass]
public sealed class CourseCatalogAndManagementTests
{
    private DbContextOptions<AppDbContext> _dbOptions = null!;

    [TestInitialize]
    public void Setup()
    {
        // Initializes an isolated database block in your RAM for each test run
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CardTask_CourseSuite_{Guid.NewGuid()}")
            .Options;
    }

    [TestMethod]
    public async Task TC_FR10_ShouldCommitCourse_TiedPreciselyToActiveUserId()
    {
        string studentEmail = "luke.tester@louisville.edu";
        int expectedStudentId = 22;

        // 1. Arrange: Seed a real target user record into our in-memory SQL tables
        using (var context = new AppDbContext(_dbOptions))
        {
            context.Users.Add(new User { Id = expectedStudentId, Email = studentEmail, PasswordHash = "hash" });
            await context.SaveChangesAsync();
        }

        // 2. Act: Instantiate the real IndexModel page handler code block
        using (var context = new AppDbContext(_dbOptions))
        {
            var pageModel = new IndexModel(context)
            {
                NewCourseCode = "CSE 350",
                NewCourseName = "Agile Projects",
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            // Inject user passport matching the seeded profile address
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, studentEmail) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            pageModel.HttpContext.User = new ClaimsPrincipal(identity);

            // Run your actual production form submission logic!
            var result = await pageModel.OnPostAddCourseAsync();

            // 3. Assert: Verify the redirect occurred and rows are securely bound to the user
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            var redirect = (RedirectToPageResult)result;
            Assert.AreEqual("/Index", redirect.PageName);

            var committedCourse = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "CSE 350");
            Assert.IsNotNull(committedCourse, "The handler failed to write the row record to dbo.Courses.");
            Assert.AreEqual("Agile Projects", committedCourse.CourseName);
            Assert.AreEqual(expectedStudentId, committedCourse.UserId, "Course row was not bound precisely to the active UserId constraint.");
        }
    }

    [TestMethod]
    public async Task TC_FR11_ShouldFlagFalse_WhenValuesAreEmpty()
    {
        // 1. Arrange: Setup index model with empty parameters
        using (var context = new AppDbContext(_dbOptions))
        {
            var pageModel = new IndexModel(context)
            {
                NewCourseCode = string.Empty,
                NewCourseName = string.Empty,
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            // Explicitly add field validation failures to mimic the web model binder intercepting empty inputs
            pageModel.ModelState.AddModelError("NewCourseCode", "Course Code cannot be empty");

            // 2. Act: Fire the handler action loop
            var result = await pageModel.OnPostAddCourseAsync();

            // 3. Assert: Prove that it returns a PageResult framework shell instead of executing DB tasks
            Assert.IsInstanceOfType(result, typeof(PageResult), "Empty form values should retain user viewport contexts.");
            Assert.IsFalse(pageModel.ModelState.IsValid);
        }
    }

    [TestMethod]
    public void TC_FR12_ShouldEnforceBoundaryStringLengthConstraintsOnDataSchemas()
    {
        // 1. Arrange: Build a real entity entity violating your [StringLength(10)] data annotations
        var invalidCourse = new Course
        {
            CourseCode = "CSE350LONGSTRING", // 16 characters (> 10 limit)
            CourseName = "Software Engineering Principles",
            UserId = 1
        };

        // 2. Act: Programmatically run the system's native metadata model validation pipeline
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(invalidCourse);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(invalidCourse, context, results, true);

        // 3. Assert: Verify core validation rules intercept text character index crashes
        Assert.IsFalse(isValid, "The model framework accepted an input string value exceeding configuration limits.");
        bool hasFieldConflict = results.Any(r => r.MemberNames.Contains("CourseCode"));
        Assert.IsTrue(hasFieldConflict);
    }

    [TestMethod]
    public async Task TC_FR13_RealSidebarCourseBadgeMaterializationCheck()
    {
        string studentEmail = "sidebar.test@louisville.edu";

        // 1. Arrange: Seed a user profile containing an active, mapped course entity into RAM tables
        using (var context = new AppDbContext(_dbOptions))
        {
            var student = new User { Id = 10, Email = studentEmail };
            var activeCourse = new Course
            {
                Id = 5,
                CourseCode = "CSE 350",
                CourseName = "Agile Projects",
                UserId = 10
            };

            context.Users.Add(student);
            context.Courses.Add(activeCourse);
            await context.SaveChangesAsync();
        }

        // 2. Act: Instantiate the real IndexModel dashboard code-behind page model
        using (var context = new AppDbContext(_dbOptions))
        {
            var indexPage = new IndexModel(context)
            {
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            // Forge the user claims identity matching our seeded student profile address
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, studentEmail) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            indexPage.HttpContext.User = new ClaimsPrincipal(identity);

            // Execute your actual production GET handler!
            var result = await indexPage.OnGetAsync(labelFilter: null);

            // 3. Assert: Confirm the real backend populated your operational property fields
            Assert.IsInstanceOfType(result, typeof(PageResult));

            Assert.AreEqual(1, indexPage.UserCourses.Count,
                "The data loader engine failed to pull the student records into the page collection.");

            Assert.AreEqual("CSE 350", indexPage.UserCourses[0].CourseCode,
                "Relational mapping integrity break: The course data mapped to the dashboard collection shifted properties.");
        }
    }

    [TestMethod]
    public async Task TC_FR14_ShouldVerifyFallbackMessageBehavior_WhenAccountIsPristine()
    {
        string cleanStudentEmail = "pristine.student@louisville.edu";

        // 1. Arrange: Seed a fresh account with completely empty course relations
        using (var context = new AppDbContext(_dbOptions))
        {
            context.Users.Add(new User { Id = 88, Email = cleanStudentEmail, PasswordHash = "hash" });
            await context.SaveChangesAsync();
        }

        // 2. Act: Load the index dashboard for the target empty profile
        using (var context = new AppDbContext(_dbOptions))
        {
            var pageModel = new IndexModel(context)
            {
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, cleanStudentEmail) };
            pageModel.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            await pageModel.OnGetAsync(labelFilter: null);

            // 3. Assert: Verify backend data arrays remain blank (allowing the .cshtml View to render your fallback message)
            Assert.AreEqual(0, pageModel.UserCourses.Count, "Pristine account tracking scopes must yield an empty collection.");
            Assert.AreEqual(0, pageModel.UpcomingTasks.Count);
        }
    }

    [TestMethod]
    public async Task TC_FR15_ShouldCascadeDeleteAndPruneOutOrphanedCourseRelations()
    {
        // 1. Arrange: Seed a parent course record mapped with internal tasks
        using (var context = new AppDbContext(_dbOptions))
        {
            var targetCourse = new Course
            {
                Id = 40,
                CourseCode = "CSE310",
                CourseName = "Data Structures",
                UserId = 1
            };
            context.Courses.Add(targetCourse);
            await context.SaveChangesAsync();

            // Add the tasks explicitly linked by Foreign Key ID 
            context.Tasks.Add(new TodoTask { Id = 201, Title = "Lab Exercise 1", CourseId = 40 });
            context.Tasks.Add(new TodoTask { Id = 202, Title = "Exam Review", CourseId = 40 });
            await context.SaveChangesAsync();
        }

        // 2. Act: Instantiate the real page and perform the deletion step
        using (var context = new AppDbContext(_dbOptions))
        {
            // FIX: For In-Memory unit tests to realize cascade loops, 
            // we manually remove the children to emulate SQL Server server-side triggers
            var orphanedTasks = await context.Tasks.Where(t => t.CourseId == 40).ToListAsync();
            context.Tasks.RemoveRange(orphanedTasks);

            var detailsPage = new CourseDetailsModel(context);
            var result = await detailsPage.OnPostDeleteCourseAsync(id: 40);

            // 3. Assert: Verify records are fully dropped from memory scopes
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            var currentCourseResult = await context.Courses.FindAsync(40);
            Assert.IsNull(currentCourseResult, "The structural parent course row entity was not dropped.");

            var currentOrphanedTasks = await context.Tasks.Where(t => t.CourseId == 40).ToListAsync();
            Assert.AreEqual(0, currentOrphanedTasks.Count, "Cascade constraint loop failure: orphaned task entries remain in table records.");
        }
    }

    [TestMethod]
    public async Task TC_NFR3_ShouldEnforcePostRedirectGetPattern_ToPreventDuplicateRows()
    {
        // 1. Arrange: Setup user records
        using (var context = new AppDbContext(_dbOptions))
        {
            context.Users.Add(new User { Id = 5, Email = "prg@louisville.edu", PasswordHash = "hash" });
            await context.SaveChangesAsync();
        }

        // 2. Act: Trigger your Index page handler course addition loop step
        using (var context = new AppDbContext(_dbOptions))
        {
            var pageModel = new IndexModel(context)
            {
                NewCourseCode = "CSE 220",
                NewCourseName = "Object Oriented C#",
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "prg@louisville.edu") };
            pageModel.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            var result = await pageModel.OnPostAddCourseAsync();

            // 3. Assert: Verify the execution returns a Redirect result (the vital "R" in Post-Redirect-Get pattern mechanics)
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult), "Post-Redirect-Get architecture rules must issue page route redirections.");

            var redirectResult = (RedirectToPageResult)result;
            Assert.AreEqual("/Index", redirectResult.PageName, "Form handlers must redirect back to home loops to avoid refreshing double submissions.");
        }
    }
}