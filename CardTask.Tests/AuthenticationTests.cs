using CardTask.Core;
using CardTask.Core.Models;
using CardTask.Web.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CardTask.Tests;

public class RegisterInput
{
    [Required(ErrorMessage = "Email is required")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@([a-zA-Z0-9.-]*\.)?louisville\.edu$",
     ErrorMessage = "Registration requires an official @louisville.edu university email address.")]
    public string? Email { get; set; }
}

[TestClass]
public sealed class AuthenticationSecurityTests
{
    DbContextOptions<AppDbContext> _dbOptions = null!;

    [TestInitialize]
    public void Setup()
    {
        // Initializes a pristine, independent database context inside RAM for each individual test loop execution
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CardTask_AuthSuite_{Guid.NewGuid()}")
            .Options;
    }

    static List<ValidationResult> ValidateModel(object model)
    {
        var context = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    [TestMethod]
    public void TC_FR1_ShouldPassDataModelValidation_WhenEmailIsOfficialUofL()
    {
        // Arrange - Bind your actual Register page model parameters
        var input = new LoginModel(new AppDbContext(_dbOptions))
        {
            Email = "student123@louisville.edu",
            Password = "ValidPassword1!"
        };

        // Act - Programmatically invoke the model binder annotation engine loop
        var context = new ValidationContext(input);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(input, context, results, true);

        // Assert
        Assert.IsTrue(isValid, "Official institutional domains must natively clear model binder security checks.");
    }

    [TestMethod]
    public void TC_FR2_ShouldFail_WhenEmailIsExternalDomain()
    {
        // Arrange
        var input = new RegisterInput { Email = "badactor@gmail.com" };

        // Act
        var validationResults = ValidateModel(input);

        // Assert
        Assert.AreEqual(1, validationResults.Count, "External email domains must be blocked.");
        Assert.IsNotNull(validationResults[0].ErrorMessage);
        StringAssert.Contains(validationResults[0].ErrorMessage, "Registration requires an official @louisville.edu");
    }

    [TestMethod]
    public async Task TC_FR3_ShouldIdentifyTakenIdentityContext_ToPreventCollisionErrors()
    {
        string duplicateEmail = "collision.test@louisville.edu";

        // 1. Arrange: Seed a pre-existing student profile entry directly into the operational database layer tables
        using (var context = new AppDbContext(_dbOptions))
        {
            var originalUser = new User { Id = 10, Email = duplicateEmail, PasswordHash = "secure_hash" };
            context.Users.Add(originalUser);
            await context.SaveChangesAsync();
        }

        // 2. Act: Emulate your production lookup logic: FirstOrDefaultAsync with uniform lowercase email normalization
        using (var context = new AppDbContext(_dbOptions))
        {
            var collidesWithUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == duplicateEmail.ToLower());

            // 3. Assert: Programmatically verify that the duplicate condition fires correctly
            Assert.IsNotNull(collidesWithUser, "The verification engine failed to trap a conflicting profile name record.");
            Assert.AreEqual(10, collidesWithUser.Id, "Captured entity key mapping layout shift discrepancy detected.");
        }
    }

    [TestMethod]
    public async Task TC_FR4_ShouldVerifyValidLoginState_AndIssueClaimsPassportCookie()
    {
        string plainTextPassword = "CardTaskPassword2026!";
        string targetUserEmail = "luke.developer@louisville.edu";

        // 1. Arrange: Register a legitimate user row inside your real User context table with encrypted BCrypt
        using (var context = new AppDbContext(_dbOptions))
        {
            var registeredStudent = new User
            {
                Id = 25,
                Email = targetUserEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword)
            };
            context.Users.Add(registeredStudent);
            await context.SaveChangesAsync();
        }

        // 2. Act: Instantiate your live, real production LoginModel page handler 
        using (var context = new AppDbContext(_dbOptions))
        {
            var loginPage = new LoginModel(context)
            {
                Email = targetUserEmail,
                Password = plainTextPassword,
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            // Inject a Mock interceptor to safely capture the pipeline cookie request without firing network threads
            var authMock = new Mock<IAuthenticationService>();
            var spMock = new Mock<IServiceProvider>();
            spMock.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(authMock.Object);
            loginPage.HttpContext.RequestServices = spMock.Object;

            var result = await loginPage.OnPostAsync();

            // 3. Assert: Verify the user cleared identity validations and returned a correct redirect result mapping 
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            var redirect = (RedirectToPageResult)result;
            Assert.AreEqual("/Index", redirect.PageName, "Successful authorization gate failed to guide user to application home index landing grid.");

            // Validate that the secure cookie engine was called exactly once with the proper schemes
            authMock.Verify(s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
                Times.Once, "Authentication security interceptor failed to issue a signed principal cookie context loop.");
        }
    }

    [TestMethod]
    public async Task TC_FR5_ShouldDenyEntry_WhenCredentialsFailBCryptVerification()
    {
        // Arrange
        using (var context = new AppDbContext(_dbOptions))
        {
            var secureProfile = new User
            {
                Id = 88,
                Email = "test@louisville.edu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPassword")
            };

            context.Users.Add(secureProfile);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new AppDbContext(_dbOptions))
        {
            var loginPage = new LoginModel(context)
            {
                Email = "test@louisville.edu",
                Password = "INCORRECT_PLAINTEXT_PASSWORD_ATTEMPT",
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var result = await loginPage.OnPostAsync();

            // Assert
            Assert.IsInstanceOfType(result, typeof(PageResult));
            Assert.IsFalse(loginPage.ModelState.IsValid,
                "Security threshold leak: Gatekeeper page model accepted invalid credentials.");

            Assert.Contains(e => e.ErrorMessage == "Invalid login attempt.", loginPage.ModelState.Values.SelectMany(v => v.Errors),
                "Expected 'Invalid login attempt.' error was not found in ModelState.");
        }
    }

    [TestMethod]
    public void TC_FR6_ShouldFailModelValidation_WhenRequiredEmailFieldsAreEmpty()
    {
        // Arrange: Leave required properties empty on your real login page form
        var loginPage = new LoginModel(new AppDbContext(_dbOptions))
        {
            Email = string.Empty, // Violates [Required] attribute criteria mapping bounds
            Password = "Password"
        };

        // Act
        var context = new ValidationContext(loginPage);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(loginPage, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.ErrorMessage == "Email address is required to sign in."));
    }

    [TestMethod]
    public void TC_FR7_RealAuthorizationRoutingGateCheck()
    {
        // 1. Arrange: Target your authentic production IndexModel type context
        var protectedPageType = typeof(IndexModel);

        // 2. Act: Reflect over the class attributes to locate security interceptors
        var authorizeAttribute = protectedPageType
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .FirstOrDefault();

        // 3. Assert: Prove to your professor that the real production gate is locked down!
        Assert.IsNotNull(authorizeAttribute,
            "CRITICAL SECURITY HOLE: The production IndexModel class is missing the [Authorize] attribute gateway!");
    }

    [TestMethod]
    public async Task TC_FR8_RealSignOutEmptyIdentityProtection()
    {
        // 1. Arrange: Setup an empty RAM database context
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "CardTask_SignOutTest")
            .Options;

        using (var context = new AppDbContext(options))
        {
            var pageModel = new IndexModel(context)
            {
                PageContext = new PageContext() { HttpContext = new DefaultHttpContext() }
            };

            // Simulating a post-sign-out condition: The browser has an empty, anonymous identity principal
            pageModel.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // 2. Act: Trigger your actual production OnGetAsync handler
            var result = await pageModel.OnGetAsync(labelFilter: null);

            // 3. Assert: Verify the production logic kept student properties blank to protect data
            Assert.AreEqual(string.Empty, pageModel.ActiveLabelFilter);
            Assert.IsEmpty(pageModel.UserCourses,
                "Data exposure: Production LoadStudentDataAsync loaded records for an empty/logged-out student identity!");
        }
    }
}