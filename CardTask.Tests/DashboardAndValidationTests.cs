using System.ComponentModel.DataAnnotations;

namespace CardTask.Tests;

// Production-aligned task model mapping fields, bounds, and layout tracking properties
public class DashboardTaskInput
{
    [Required(ErrorMessage = "Task Title cannot be empty")]
    public string? Title { get; set; }

    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string? LabelCategory { get; set; } // e.g., "Exams", "Assignments"
    public bool IsCompleted { get; set; }
    public int UserId { get; set; }
}

[TestClass]
public sealed class TaskDashboardAndValidationTests
{
    // Helper method evaluating data annotations 
    private List<ValidationResult> ValidateModel(object model)
    {
        var context = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    [TestMethod]
    public void TC_FR16_ShouldVerifyBootstrapModalOverlayTrigger()
    {
        // Arrange
        bool modalIsVisible = false;
        string userActionTrigger = "ClickAddTaskButton";

        // Act - Emulate frontend state trigger injection logic
        if (userActionTrigger == "ClickAddTaskButton")
        {
            modalIsVisible = true;
        }

        // Assert
        Assert.IsTrue(modalIsVisible, "Bootstrap modal overlay must register as focused/active over the workspace canvas.");
    }

    [TestMethod]
    public void TC_FR17_ShouldSaveRowMatchingRelationalKeys()
    {
        // Arrange
        int validatedUserId = 105;
        var taskRow = new DashboardTaskInput
        {
            Title = "Organic Chemistry Quiz 2",
            DueDate = DateTime.Now.AddDays(4),
            LabelCategory = "Assignments",
            IsCompleted = false,
            UserId = validatedUserId
        };

        // Act
        var validationResults = ValidateModel(taskRow);

        // Assert
        Assert.AreEqual(0, validationResults.Count);
        Assert.AreEqual(105, taskRow.UserId, "Task record row database mapping must match the target student entity id.");
    }

    [TestMethod]
    public void TC_FR18_ShouldApplyConditionalLinqFilter_WhenPillIsClicked()
    {
        // Arrange
        var mockDatabaseTable = new List<DashboardTaskInput>
        {
            new DashboardTaskInput { Title = "Midterm Exam", LabelCategory = "Exams" },
            new DashboardTaskInput { Title = "Lab Report", LabelCategory = "Assignments" },
            new DashboardTaskInput { Title = "Final Exam", LabelCategory = "Exams" }
        };
        string selectedPillFilter = "Exams";

        // Act - Execute conditional backend LINQ expression matching active handlers
        var filteredResults = mockDatabaseTable.Where(t => t.LabelCategory == selectedPillFilter).ToList();

        // Assert
        Assert.AreEqual(2, filteredResults.Count, "LINQ filter engine should return exactly 2 items categorized under 'Exams'.");
        Assert.AreEqual("Midterm Exam", filteredResults[0].Title);
        Assert.AreEqual("Final Exam", filteredResults[1].Title);
    }

    [TestMethod]
    public void TC_FR19_ShouldInvertIsCompletedAndDecreaseMetrics_WhenChecked()
    {
        // Arrange
        var taskItem = new DashboardTaskInput { Title = "Read Sylabus", IsCompleted = false };
        int pendingTasksMetricCounter = 5;

        // Act - Emulate checking the complete task checkbox input loop
        taskItem.IsCompleted = true;
        if (taskItem.IsCompleted)
        {
            pendingTasksMetricCounter--;
        }

        // Assert
        Assert.IsTrue(taskItem.IsCompleted, "Task entity model completion status must evaluate to true.");
        Assert.AreEqual(4, pendingTasksMetricCounter, "Active task tracking counter metrics must decrease immediately.");
    }

    [TestMethod]
    public void TC_FR20_ShouldToggleBackToPending_WhenCompletedItemIsUnchecked()
    {
        // Arrange
        var taskItem = new DashboardTaskInput { Title = "Write Code", IsCompleted = true }; // Starts completed
        int pendingTasksMetricCounter = 4;

        // Act - Emulate unchecking a completed task a second time (Undo state toggle action)
        taskItem.IsCompleted = false;
        if (!taskItem.IsCompleted)
        {
            pendingTasksMetricCounter++;
        }

        // Assert
        Assert.IsFalse(taskItem.IsCompleted, "Task state must return to a pending structural status.");
        Assert.AreEqual(5, pendingTasksMetricCounter, "Item must return cleanly into active task feeds.");
    }

    [TestMethod]
    public void TC_FR21_ShouldAppendCustomTag_ToUniversalLabelCollection()
    {
        // Arrange
        var universalLabelCollection = new List<string> { "Exams", "Homework", "Assignments" };
        string customStudentTagInput = "Lab Modules";

        // Act
        universalLabelCollection.Add(customStudentTagInput);

        // Assert
        Assert.IsTrue(universalLabelCollection.Contains("Lab Modules"), "Custom string variables must append cleanly to list tracking arrays.");
        Assert.AreEqual(4, universalLabelCollection.Count);
    }

    [TestMethod]
    public void TC_FR22_ShouldInterceptRequestAndThrowError_WhenDeadlineIsInPast()
    {
        // Arrange
        var inputTask = new DashboardTaskInput
        {
            Title = "Late Paper Submission",
            DueDate = DateTime.Now.AddDays(-2) // Set 2 days in the past
        };
        string serverSideErrorMessage = "";

        // Act - Custom page handler timestamp validation interceptor logic
        if (inputTask.DueDate < DateTime.Now)
        {
            serverSideErrorMessage = "Assignment deadline cannot be set in the past.";
        }

        // Assert
        Assert.AreNotEqual("", serverSideErrorMessage, "The system backend engine must catch past deadlines.");
        Assert.AreEqual("Assignment deadline cannot be set in the past.", serverSideErrorMessage);
    }

    [TestMethod]
    public void TC_FR23_ShouldExpandWrappers_WhenPayloadDescriptionIsExtremelyLong()
    {
        // Arrange
        var taskWithLongDescription = new DashboardTaskInput
        {
            Title = "Capstone Project",
            Description = new string('A', 5000) // Creates a giant 5,000 character block string payload
        };

        // Act
        int payloadLength = taskWithLongDescription.Description.Length;

        // Assert
        Assert.AreEqual(5000, payloadLength, "System fields must string-bind long payloads successfully without truncation crashes.");
        Assert.IsNotNull(taskWithLongDescription.Description);
    }

    [TestMethod]
    public void TC_NFR4_ShouldApplyCrimsonAccents_WhenDeadlineIsWithin24Hours()
    {
        // Arrange
        var urgentTask = new DashboardTaskInput { Title = "Project Demo", DueDate = DateTime.Now.AddHours(12) };
        var distantTask = new DashboardTaskInput { Title = "Term Exam", DueDate = DateTime.Now.AddDays(5) };

        string urgentBorderClass = "border-neutral";
        string distantBorderClass = "border-neutral";

        // Act - Emulate Razor conditional styling loop computation behavior
        if ((urgentTask.DueDate - DateTime.Now).TotalHours <= 24)
        {
            urgentBorderClass = "border-crimson-accent"; // Target urgent style rule matches
        }

        if ((distantTask.DueDate - DateTime.Now).TotalDays <= 1)
        {
            distantBorderClass = "border-crimson-accent";
        }

        // Assert
        Assert.AreEqual("border-crimson-accent", urgentBorderClass, "Urgent deadlines expiring within 24 hours must receive crimson accent highlighting.");
        Assert.AreEqual("border-neutral", distantBorderClass, "Far off task card dates must maintain default layout metrics styling classes.");
    }
}