using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Distance.Api
{
    internal static class HttpResponseExtensions
    {
        public static Task BadRequest(this HttpResponse response, string message = null)
        {
            response.StatusCode = 400;
            return response.WriteAsync(message);
        }
    }
}
