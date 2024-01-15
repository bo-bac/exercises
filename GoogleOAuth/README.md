# GoogleOAuth
This ejercicio was requested by Gamanet

>How we can use Google OAuth in our ASP.NET applications?

**In theory** https://developers.google.com/identity/protocols/oauth2

**In practice** 
1. Setup your OAuth Clients and API Keys https://console.developers.google.com/
2. Configure your client app 

|								| Description	|
| -----							| -----         |
| Goa.Ui.Mvc.G.OpenIdConnect	| use Google.Apis.Auth.AspNetCore3 lib, generates id_token, access_token, refresh_token. Must be an option if you use Google APIs |
| Goa.Ui.Mvc.Ms.OAuth			| use Microsoft.AspNetCore.Authentication.Google lib. Light but generates access_token only that is not JWT so can not be used for Bearer header. Or it must be I just cannot figure out how to configure it :)  |
| Goa.Ui.Mvc.Ms.OpenIdConnect   | use Microsoft.AspNetCore.Authentication.OpenIdConnect lib. **My choise**. Flexible, well documented, can be used not for Google only |
| Goa.Ui.BlazorWebAssembly      | use Microsoft.AspNetCore.Components.WebAssembly.Authentication lib. Just a little of Blazor magic :) |

secrets.json
```
"Authentication": {
        "Google": {
            "Authority": "https://accounts.google.com/",
            "ClientId": "...",
            "ClientSecret": "...",
            "ApiKey": "..."
        }  
    }
```
3. Configure your API app `Goa.Api`


## Tags
`.net core` `c#` `asp.net core` `web api` `oauth` `openid` `google oauth` `asp.net mvc` `blazor wasm` `authorization` `authentication` `api key authorization` `authorization policy`

## Go Ahead
