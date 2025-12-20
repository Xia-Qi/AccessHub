using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebClient1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public string Code { get; set; } = "test";
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string code)
        {
            if (!string.IsNullOrEmpty(code))
            {
                Code = code;
            }

        }
        public IActionResult RedirectLogin()
        {
            Redirect("localhost://5700/login")
        }
    }
}
