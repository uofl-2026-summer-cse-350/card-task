using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardTask.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        var authCheck = TempData["UserAuthenticated"] as string;

        if (string.IsNullOrEmpty(authCheck) || authCheck != "true")
        {
            return RedirectToPage("/Login");
        }

        TempData.Keep("UserAuthenticated");
        return Page();
    }
}