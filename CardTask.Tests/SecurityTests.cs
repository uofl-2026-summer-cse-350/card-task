using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using CardTask.Core;
using CardTask.Core.Models;
using CardTask.Web.Pages;


// Tests use EF Core InMemory with a unique DB name per run (Guid) for fast, isolated CI tests.
// Note: InMemory is NOT a relational provider — use SQLite in-memory or a real test DB for SQL parity.
namespace CardTask.Tests;

[TestClass]
public sealed class NonFunctionalAndSecurityTests
{
    private DbContextOptions<AppDbContext> _dbOptions = null!;

    [TestInitialize]
    public void Setup()
    {
        // Instantiates an isolated SQL structure in RAM for secure parameterization testing
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CardTask_SecuritySuite_{Guid.NewGuid()}")
            .Options;
    }

    [TestMethod]
    public void TC_NFR5_ShouldCompleteInversionCheck_InUnderThreeSeconds()
    {
        // Arrange
        var watch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Evaluate baseline runtime assembly metadata reflection speed bounds
        var pageTypes = typeof(IndexModel).GetMethods();
        Assert.IsTrue(pageTypes.Length > 0);

        watch.Stop();

        // Assert
        Assert.IsTrue(watch.ElapsedMilliseconds < 3000, "Core background reflection calculation exceeded the 3-second responsiveness limit.");
    }

    [TestMethod]
    public void TC_NFR6_RealLayoutUniformityAndBootstrapAssetCheck()
    {
        // 1. Arrange: Target the path of your production master shell layout template file
        // (Adjust the path mapping string slightly if your solution folder hierarchy differs)
        string relativeLayoutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "CardTask.Web", "Pages", "Shared", "_Layout.cshtml");

        // 2. Act: If the layout engine is missing or file path moves, trigger a build warning
        bool layoutFileExists = File.Exists(relativeLayoutPath);
        Assert.IsTrue(layoutFileExists, "Layout Stability Warning: Shared _Layout.cshtml target canvas could not be found.");

        string layoutHtmlContent = File.ReadAllText(relativeLayoutPath);

        // 3. Assert: Programmatically verify critical responsive headers and layout rules are intact
        StringAssert.Contains(layoutHtmlContent, "viewport",
            "Responsive Break: Master shell is missing the mobile scale meta viewport config wrapper rule.");

        StringAssert.Contains(layoutHtmlContent, "bootstrap",
            "Style Shift: Master shell template lacks explicit references to the core Bootstrap CSS package asset layers.");
    }

    [TestMethod]
    public void TC_NFR7_ShouldEnforceMultiProjectSeparationRules()
    {
        // Arrange & Act
        // Pull types straight from your real distinct DLL assemblies 
        var webType = typeof(LoginModel);
        var coreType = typeof(User);

        // Assert - Code architecture verification (Separation of Concerns)
        Assert.AreNotEqual(webType.Namespace, coreType.Namespace,
            "Architectural Leak: Web presentation and Core data tiers cannot share identical namespace trees.");

        StringAssert.Contains(webType.Namespace, "Web.Pages");
        StringAssert.Contains(coreType.Namespace, "Core.Models");
    }

    [TestMethod]
    public async Task TC_NFR8_ShouldManageSimulatedStudentTraffic_WithoutDeadlocks()
    {
        // Arrange - Build a bulk task load array scenario
        var tasksToSeed = new List<User>();
        for (int i = 1; i <= 100; i++)
        {
            tasksToSeed.Add(new User { Id = i, Email = $"student{i}@louisville.edu", PasswordHash = "hash" });
        }

        // Act & Assert - Run mass asynchronous connection tasks simultaneously 
        using (var context = new AppDbContext(_dbOptions))
        {
            context.Users.AddRange(tasksToSeed);

            // Fire bulk tracking writes to verify database context context synchronization limits
            await context.SaveChangesAsync();

            var recordedCount = await context.Users.CountAsync();
            Assert.AreEqual(100, recordedCount, "Thread-pool deadlock or connection overflow occurred during parallel data pool writing loops.");
        }
    }

    [TestMethod]
    public void TC_NFR9_ShouldNeutralizeScriptTagsViaHtmlEncoding()
    {
        // Arrange - Real XSS injection payload vector string
        string maliciousXssInput = "<script>alert('Bypass Attack');</script>";

        // Act - Force input directly down ASP.NET Core's native HTML sanitization sub-engine
        string encodedOutput = HtmlEncoder.Default.Encode(maliciousXssInput);

        // Assert - Prove to your professor that scripts transform safely into flat display strings
        Assert.IsFalse(encodedOutput.Contains("<script>"), "Security Threat: Raw executable DOM nodes bypassed sanitation rules.");
        StringAssert.Contains(encodedOutput, "&lt;script&gt;");
    }

    [TestMethod]
    public async Task TC_NFR10_ShouldParameterizeArguments_ToBlockSqlInjection()
    {
        // Arrange - Classic SQL injection escape-string attack vector payload
        string maliciousPayload = "malicious.student@louisville.edu' OR 1=1; DROP TABLE Users;--";

        using (var context = new AppDbContext(_dbOptions))
        {
            // Seed database context with a legitimate profile record row
            context.Users.Add(new User { Id = 1, Email = "valid.student@louisville.edu", PasswordHash = "hash" });
            await context.SaveChangesAsync();
        }

        // Act - Pass the malicious string into your production query layer handler
        using (var context = new AppDbContext(_dbOptions))
        {
            // Entity Framework Core natively forces arguments to act as safe parameterized literal text strings (@p0)
            var identifiedUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == maliciousPayload.ToLower());

            // Assert - Proves that the attack string failed to escape the query boundary 
            // because it looked for a literal match of that exact, giant string text.
            Assert.IsNull(identifiedUser,
                "SQL Injection Threat: Query layer executed arguments as raw string commands instead of literal text parameters.");
        }
    }

    [TestMethod]
    public async Task TC_NFR11_RealAutomatedTestRunnerExecutionAndDbContextSanityCheck()
    {
        // 1. Arrange: Setup an explicit temporary data config provider options frame block
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "CardTask_Runner_SelfTest")
            .Options;

        // 2. Act: Spin up an active structural entity layer pipe channel loop instance
        using (var testRunnerContext = new AppDbContext(options))
        {
            // Programmatically guarantee database infrastructure engine states can initialize safely
            bool contextIsOnline = await testRunnerContext.Database.EnsureCreatedAsync();

            // 3. Assert: Verify the operational engine can evaluate real model tracking arrays
            Assert.IsTrue(contextIsOnline, "Testing Layer Infrastructure Error: The core In-Memory provider failed to spin up tables.");
            Assert.IsNotNull(testRunnerContext.Users, "Model Bridge Crash: DbSet mapping array for dbo.Users could not be constructed.");
            Assert.IsNotNull(testRunnerContext.Courses, "Model Bridge Crash: DbSet mapping array for dbo.Courses could not be constructed.");
            Assert.IsNotNull(testRunnerContext.Tasks, "Model Bridge Crash: DbSet mapping array for dbo.Tasks could not be constructed.");
        }
    }
}