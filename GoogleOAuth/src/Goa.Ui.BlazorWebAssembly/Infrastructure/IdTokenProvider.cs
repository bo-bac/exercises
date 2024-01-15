using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Ui.BlazorWebAssembly.Infrastructure
{
    public class IdTokenProvider : IAccessTokenProvider
    {
        private readonly string _idTokenKey;
        
        protected NavigationManager Navigation { get; }

        protected RemoteAuthenticationOptions<OidcProviderOptions> Options { get; }

        public IdTokenProvider(
            IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> options,
            NavigationManager navigation,
            IConfiguration configuration)
        {
            Options = options.Value;
            Navigation = navigation;

            _idTokenKey = $"oidc.user:{Options.ProviderOptions.Authority}:{Options.ProviderOptions.ClientId}";
        }

        public ValueTask<AccessTokenResult> RequestAccessToken()
        {
            //var result = await JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken");
            
            var token = Interop.Session.GetItem(_idTokenKey);
            var result = !string.IsNullOrEmpty(token)
                ? JsonSerializer.Deserialize<InternalTokenResult>(token)
                : new InternalTokenResult();            

            return ValueTask.FromResult(
                new AccessTokenResult(
                    result.Status,
                    new AccessToken 
                    { 
                        Value = result.Token, 
                        GrantedScopes = (result.Scope ?? "").Split(' '),
                        Expires = result.Exp()
                    },
                    result.Status == AccessTokenResultStatus.RequiresRedirect ? Options.AuthenticationPaths.LogInPath : null,
                    result.Status == AccessTokenResultStatus.RequiresRedirect ? new InteractiveRequestOptions
                    {
                        Interaction = InteractionType.GetToken,
                        ReturnUrl = GetReturnUrl(null)
                    } : null));
        }

        public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var token = Interop.Session.GetItem(_idTokenKey);
            var result = !string.IsNullOrEmpty(token)
                ? JsonSerializer.Deserialize<InternalTokenResult>(token)
                : new InternalTokenResult();

            return ValueTask.FromResult(
                new AccessTokenResult(
                    result.Status,
                    new AccessToken
                    {
                        Value = result.Token,
                        GrantedScopes = (result.Scope ?? "").Split(' '),
                        Expires = result.Exp()
                    },
                    result.Status == AccessTokenResultStatus.RequiresRedirect ? Options.AuthenticationPaths.LogInPath : null,
                    result.Status == AccessTokenResultStatus.RequiresRedirect ? new InteractiveRequestOptions
                    {
                        Interaction = InteractionType.GetToken,
                        ReturnUrl = GetReturnUrl(options.ReturnUrl),
                        Scopes = options.Scopes ?? Array.Empty<string>(),
                    } : null));
        }

        private string GetReturnUrl(string? customReturnUrl) =>
            customReturnUrl != null ? Navigation.ToAbsoluteUri(customReturnUrl).AbsoluteUri : Navigation.Uri;
    }

    static partial class Interop
    {
        internal static partial class Session
        {
            [JSImport("globalThis.sessionStorage.getItem")]
            internal static partial string GetItem(string key);
        }        
    }

    internal readonly struct InternalTokenResult
    {
        [JsonIgnore]
        public AccessTokenResultStatus Status => string.IsNullOrEmpty(Token) 
            ? AccessTokenResultStatus.RequiresRedirect 
            : AccessTokenResultStatus.Success;

        [JsonPropertyName("id_token")]
        public string Token { get; init; }

        [JsonPropertyName("scope")]
        public string Scope { get; init; }

        public DateTimeOffset Exp()
        {
            if (string.IsNullOrEmpty(Token))
            {
                return DateTimeOffset.MinValue;
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(Token);
            var expS = jsonToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (!long.TryParse(expS, out var exp))
            {
                return DateTimeOffset.MinValue;
            }

            return DateTimeOffset.FromUnixTimeSeconds(exp).ToLocalTime();
        }
    }
}
