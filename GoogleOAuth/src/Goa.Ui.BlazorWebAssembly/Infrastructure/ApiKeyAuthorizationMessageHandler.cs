using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components;

namespace Goa.Ui.BlazorWebAssembly.Infrastructure
{
    public class ApiKeyAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        private readonly string _apiKey;

        public ApiKeyAuthorizationMessageHandler(
            IAccessTokenProvider provider,
            NavigationManager navigationManager,
            IConfiguration configuration)
        : base(provider, navigationManager)
        {
            _apiKey = configuration["Authentication:Google:ApiKey"]!;

            ConfigureHandler(
                authorizedUrls: new[] { configuration["Api:BaseAddress"]! }
                /*,scopes: new[] { "email", "profile", "roles", "openid", "apis" }*/);
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("x-api-key", _apiKey);

            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {            
            request.Headers.Add("x-api-key", _apiKey);  

            return base.SendAsync(request, cancellationToken);
        }
    }
}
