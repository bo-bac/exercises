using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
//'http://schemas.microsodt.com/ws/2008/06/identity/clains/role' instead of 'roles'
// This flag ensures that the ClaimsIdentity claims collection will be build from the claims in the token
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// Add services to the container.
services.AddControllersWithViews();

services
    .AddAuthentication(o =>
    {
        // This forces challenge results to be handled by Google OpenID Handler, so there's no
        // need to add an AccountController that emits challenges for Login.
        o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        // This forces forbid results to be handled by Google OpenID Handler, which checks if
        // extra scopes are required and does automatic incremental auth.
        o.DefaultForbidScheme = OpenIdConnectDefaults.AuthenticationScheme;
        // Default scheme that will handle everything else.
        // Once a user is authenticated, the OAuth2 token info is stored in cookies.
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        //options.Cookie.Name = "mvccode";
        options.LoginPath = "/google/login";
        options.Events.OnSigningOut = async e =>
        {
            //await e.HttpContext.RevokeUserRefreshTokenAsync();
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = configuration["Authentication:Google:Authority"];
        options.ClientId = configuration["Authentication:Google:ClientId"];
        //options.ClientSecret = configuration["Authentication:Google:ClientSecret"];

        // https://darutk.medium.com/diagrams-of-all-the-openid-connect-flows-6968e3990660
        options.ResponseType = OpenIdConnectResponseType.IdToken;  // code | id_token
        options.CallbackPath = "/signin-google";

        options.SaveTokens = true;
        //options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        //options.Scope.Add("offline_access");
        //options.Scope.Add("api");

        options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };

#if DEBUG
        options.Events.OnTicketReceived = ctx =>
        {
            // just to check what tokens are received
            List<AuthenticationToken> tokens = ctx.Properties.GetTokens().ToList();

            return Task.CompletedTask;
        };
#endif
    });

// adds global authorization policy to require authenticated users
services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

services.AddHttpClient("GoaApi", c =>
{
    c.BaseAddress = new Uri(configuration["Api:BaseAddress"]);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
