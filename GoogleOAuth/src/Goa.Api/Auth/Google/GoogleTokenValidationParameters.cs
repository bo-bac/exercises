using Microsoft.IdentityModel.Tokens;

namespace Goa.Api.Auth.Google
{
    public class GoogleTokenValidationParameters : TokenValidationParameters
    {
        public GoogleTokenValidationParameters()
        {
        }

        protected GoogleTokenValidationParameters(GoogleTokenValidationParameters other) : base(other)
        {
            HostedDomain = other.HostedDomain;
            ValidateHostedDomain = other.ValidateHostedDomain;
        }

        public string? HostedDomain { get; set; }

        public bool ValidateHostedDomain { get; set; }

        public override TokenValidationParameters Clone()
        {
            return new GoogleTokenValidationParameters(this);
        }
    }
}
