using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goa.Ui.Mvc.Controllers
{
    public class GoogleController : Controller
    {
        const string Home_URL = "/";

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = Home_URL)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                return Redirect(Home_URL);
            }

            return new ChallengeResult(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(Callback)),

                    Items =
                    {
                        { "uru", returnUrl },
                        //{ "scheme", "Google" }
                    }
                });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Callback()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                return BadRequest(); // TODO: Handle this better.
            }

            //// get sub and issuer to check if external user is known
            //var sub = result.Principal.FindFirst("sub");
            //var issuer = result.Properties.Items["scheme"];

            // do your customm provisioning logic

            // sign in user
            await HttpContext.SignInAsync(result.Principal, result.Properties);

            return Redirect(result.Properties.Items["uru"] ?? Home_URL);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect(Home_URL);
        }
    }
}
