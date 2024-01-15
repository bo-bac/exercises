using System.Security.Authentication;

namespace Goa.Api.Auth.ApiKey
{
    internal class ExampleApiKeyValidation : IApiKeyValidation
    {
        private const string KEY_KEY = "Authentication:Google:ApiKey";

        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public ExampleApiKeyValidation(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration.GetValue<string>(KEY_KEY);

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidCredentialException($"Api key '{KEY_KEY}' is misconfigured. Add api key to configuration");
            }
        }

        public bool IsValidApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            if (_apiKey == null || _apiKey != apiKey)
            {
                return false;
            }

            return true;
        }
    }
}
