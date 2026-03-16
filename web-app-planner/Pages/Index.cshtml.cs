using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Planner.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Projects/Index");
}
