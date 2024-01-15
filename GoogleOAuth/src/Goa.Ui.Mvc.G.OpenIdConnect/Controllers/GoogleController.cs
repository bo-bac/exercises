using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Goa.Ui.Mvc.Controllers
{
    public class GoogleController : Controller
    {
        const string Home_URL = "/";

        public IActionResult Login(string? returnUrl = Home_URL)
        {
            return new RedirectResult(returnUrl);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect(Home_URL);
        }
    }
}
