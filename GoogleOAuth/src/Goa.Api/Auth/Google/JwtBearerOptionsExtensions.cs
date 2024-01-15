﻿using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Goa.Api.Auth.Google
{
    public static class JwtBearerOptionsExtensions
    {
        /// <summary>
        /// Configures the JWT Bearer authentication handler to validate Google OpenID Connect tokens
        /// for a specified <paramref name="clientId"/>.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <param name="clientId">The client ID string that you obtain from the API Console.</param>
        /// <exception cref="ArgumentException">If the <paramref name="clientId"/> argument is empty.</exception>
        public static JwtBearerOptions UseGoogle(this JwtBearerOptions options, string clientId)
        {
            return options.AddGoogle(clientId, null);
        }

        /// <summary>
        /// Configures the JWT Bearer authentication handler to validate Google OpenID Connect tokens
        /// for a specified <paramref name="clientId"/> and <paramref name="hostedDomain"/>.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <param name="clientId">The client ID string that you obtain from the API Console.</param>
        /// <param name="hostedDomain">The hd (hosted domain) parameter streamlines the login process for G Suite hosted accounts.</param>
        /// <exception cref="ArgumentException">If the <paramref name="clientId"/> argument is empty.</exception>
        public static JwtBearerOptions AddGoogle(this JwtBearerOptions options, string clientId, string? hostedDomain = default)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("{0} cannot be null or empty.", nameof(clientId));
            }

            options.Audience = clientId;
            options.Authority = GoogleJwtBearerDefaults.Authority;
            options.IncludeErrorDetails = true;

            options.SecurityTokenValidators.Clear();
            options.SecurityTokenValidators.Add(new GoogleJwtSecurityTokenHandler());

            options.TokenValidationParameters = new GoogleTokenValidationParameters
            {
                // Verify the integrity of the ID token according to
                // https://developers.google.com/identity/sign-in/web/backend-auth#verify-the-integrity-of-the-id-token.
                //
                // After you receive the ID token, you must verify its integrity.
                // To verify that the token is valid, ensure that the following criteria are satisfied:

                // - The ID token is properly signed by Google. Use Google's public keys to verify the token's signature.
                ValidateIssuerSigningKey = true,

                // - The value of aud in the ID token is equal to one of your app's client IDs.
                ValidateAudience = true,
                ValidAudience = clientId,

                // - The value of iss in the ID token is equal to accounts.google.com or https://accounts.google.com.
                ValidateIssuer = true,
                ValidIssuers = new[] { GoogleJwtBearerDefaults.Authority, "accounts.google.com" },

                // - The expiry time (exp) of the ID token has not passed.
                ValidateLifetime = true,

                // - If you want to restrict access to only members of your G Suite domain, verify that the ID token has an hd claim that matches your G Suite domain name.
                ValidateHostedDomain = !string.IsNullOrEmpty(hostedDomain),
                HostedDomain = hostedDomain,

                NameClaimType = GoogleClaimTypes.Name,
                AuthenticationType = GoogleJwtBearerDefaults.AuthenticationScheme,
            };

            return options;
        }
    }
}
