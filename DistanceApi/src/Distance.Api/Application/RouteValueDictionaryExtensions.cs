using Microsoft.AspNetCore.Routing;

namespace Distance.Api
{
    internal static class RouteValueDictionaryExtensions
    {
        public static bool TryGetValue(this RouteValueDictionary values, string key, out IATA code)
        {
            values.TryGetValue(key, out var val);
            code = new IATA(val as string);

            return code.IsValid();
        }
    }
}
