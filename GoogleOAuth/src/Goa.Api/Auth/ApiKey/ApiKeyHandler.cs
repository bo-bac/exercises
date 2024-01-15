using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualBasic;

namespace Goa.Api.Auth.ApiKey
{
    public class ApiKeyHandler : AuthorizationHandler<ApiKeyRequirement>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IApiKeyValidation _apiKeyValidation;
        public ApiKeyHandler(IServiceProvider provider, IApiKeyValidation apiKeyValidation)
        {            
            _httpContextAccessor = provider.GetService<IHttpContextAccessor>();
            _apiKeyValidation = apiKeyValidation;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
        {
            var apiKey = _httpContextAccessor?.HttpContext?.Request.Headers[ApiKeyDefaults.HeaderName].ToString();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (!_apiKeyValidation.IsValidApiKey(apiKey))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
