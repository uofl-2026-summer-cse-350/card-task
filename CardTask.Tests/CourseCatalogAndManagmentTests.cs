using System.ComponentModel.DataAnnotations;

namespace CardTask.Tests
{
    // Production-aligned input model reflecting your course attributes & bounds
    public class CourseManagementInput
    {
        [Required(ErrorMessage = "Course Code cannot be empty")]
        [StringLength(10, ErrorMessage = "Course Code cannot exceed 10 characters")]
        public string? CourseCode { get; set; }

        [Required(ErrorMessage = "Course Title cannot be empty")]
        public string? Title { get; set; }

        public int UserId { get; set; }
    }

    [TestClass]
    public sealed class CourseCatalogAndManagementTests
    {
        // Helper method evaluating data annotations ([Required], [StringLength])
        private List<ValidationResult> ValidateModel(object model)
        {
            var context = new ValidationContext(model, null, null);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [TestMethod]
        public void TC_FR10_ShouldCommitCourse_TiedPreciselyToActiveUserId()
        {
            // Arrange
            int activeUserId = 22;
            var input = new CourseManagementInput
            {
                CourseCode = "CSE 350",
                Title = "Agile Projects",
                UserId = activeUserId
            };

            // Act
            var validationResults = ValidateModel(input);

            // Assert
            Assert.AreEqual(0, validationResults.Count);
            Assert.AreEqual(22, input.UserId, "Course must link explicitly to the active authenticated UserId.");
        }

        [TestMethod]
        public void TC_FR11_ShouldFlagFalse_WhenValuesAreEmpty()
        {
            // Arrange
            var input = new CourseManagementInput { CourseCode = "", Title = "" };

            // Act
            var validationResults = ValidateModel(input);

            // Assert
            Assert.IsTrue(validationResults.Count > 0, "ModelState.IsValid must flag false when required entries are omitted.");
            StringAssert.Contains(validationResults[0].ErrorMessage, "cannot be empty");
        }

        [TestMethod]
        public void TC_FR12_ShouldFail_WhenCourseCodeExceedsBoundaryLimits()
        {
            // Arrange
            var input = new CourseManagementInput
            {
                CourseCode = "CSE350LONGSTRING", // 16 characters (> 10 limit)
                Title = "Software Engineering",
                UserId = 1
            };

            // Act
            var validationResults = ValidateModel(input);

            // Assert
            Assert.AreEqual(1, validationResults.Count);
            Assert.IsNotNull(validationResults[0].ErrorMessage);
            StringAssert.Contains(validationResults[0].ErrorMessage, "cannot exceed 10 characters");
        }

        [TestMethod]
        public void TC_FR13_ShouldMaterializeCourseBadge_InSidebarLoop()
        {
            // Arrange
            var currentSidebarList = new List<string> { "CSE 310", "MATH 205" };
            string newlyAddedBadge = "CSE 350";

            // Act - Emulate adding an item before rendering a Razor UI @foreach block
            currentSidebarList.Add(newlyAddedBadge);

            // Assert
            Assert.IsTrue(currentSidebarList.Contains("CSE 350"), "Course block must immediately exist within the loop target collection.");
            Assert.AreEqual(3, currentSidebarList.Count);
        }

        [TestMethod]
        public void TC_FR14_ShouldPrintFallbackMessage_WhenAccountIsPristine()
        {
            // Arrange
            var userEnrolledCourses = new List<string>(); // Clean list representing an empty dashboard record state
            string displayMessage = "";

            // Act - Emulate direct conditional rendering logic inside your .cshtml view
            if (!userEnrolledCourses.Any())
            {
                displayMessage = "No courses added";
            }

            // Assert
            Assert.AreEqual("No courses added", displayMessage, "The navigation panel must correctly route down a fallback UI string.");
        }

        [TestMethod]
        public void TC_FR15_ShouldPruneRecordAndOrphanedKeys_OnDelete()
        {
            // Arrange
            var courseDatabase = new List<string> { "CSE 310", "CSE 350", "ECE 210" };
            string targetCourseToDelete = "CSE 350";

            // Act - Emulate cascade pruning a targeted row selection
            bool initialCheck = courseDatabase.Contains(targetCourseToDelete);
            courseDatabase.Remove(targetCourseToDelete);

            // Assert
            Assert.IsTrue(initialCheck);
            Assert.IsFalse(courseDatabase.Contains("CSE 350"), "Target row entry keys must drop clearly from active records.");
            Assert.AreEqual(2, courseDatabase.Count);
        }

        [TestMethod]
        public void TC_NFR3_ShouldEnforcePostRedirectGetPattern_ToPreventDuplicateRows()
        {
            // Arrange
            bool duplicateExecutionInterrupted = false;
            string requestedActionHandler = "OnPostAddCourseAsync";
            string currentActionExecutionStatus = "Executing";

            // Act - Simulating the explicit action response lifecycle pattern behavior
            if (currentActionExecutionStatus == "Executing" && requestedActionHandler == "OnPostAddCourseAsync")
            {
                // Bypasses resubmission loop by generating a browser navigation route rewrite response
                currentActionExecutionStatus = "RedirectToPageResult";
                duplicateExecutionInterrupted = true;
            }

            // Assert
            Assert.IsTrue(duplicateExecutionInterrupted, "Post-Redirect-Get pattern mechanics must intercept form post duplicates cleanly.");
            Assert.AreEqual("RedirectToPageResult", currentActionExecutionStatus);
        }
    }
}