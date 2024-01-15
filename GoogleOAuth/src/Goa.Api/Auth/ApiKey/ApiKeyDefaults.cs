namespace Goa.Api.Auth.ApiKey
{
    public static class ApiKeyDefaults
    {
        // https://www.stum.de/2009/01/14/const-strings-a-very-convenient-way-to-shoot-yourself-in-the-foot/
        public const string PolicyName = "ApiKeyPolicy";

        public static readonly string HeaderName = "X-Api-Key";        
    }
}
