using Goa.Ui.BlazorWebAssembly;
using Goa.Ui.BlazorWebAssembly.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped<IAccessTokenProvider, IdTokenProvider>();
builder.Services.AddScoped<ApiKeyAuthorizationMessageHandler>();
builder.Services.AddHttpClient<GoaApiClient>().AddHttpMessageHandler<ApiKeyAuthorizationMessageHandler>();

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Authentication:Google", options.ProviderOptions);
});



await builder.Build().RunAsync();