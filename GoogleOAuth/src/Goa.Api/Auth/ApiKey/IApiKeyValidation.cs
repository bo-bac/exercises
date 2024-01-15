namespace Goa.Api.Auth.ApiKey
{
    public interface IApiKeyValidation
    {
        bool IsValidApiKey(string keyToValidate);
    }
}
