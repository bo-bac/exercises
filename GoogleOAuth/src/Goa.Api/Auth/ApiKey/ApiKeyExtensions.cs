using Microsoft.AspNetCore.Authorization;

namespace Goa.Api.Auth.ApiKey
{
    public static class ApiKeyExtensions
    {
        public static IServiceCollection AddApiKey<TKeyValidation>(this IServiceCollection services) 
            where TKeyValidation : class, IApiKeyValidation
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IApiKeyValidation, TKeyValidation>();
            services.AddSingleton<IAuthorizationHandler, ApiKeyHandler>();

            return services;
        }
    }
}
