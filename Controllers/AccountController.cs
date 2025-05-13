using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleMvcApp.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SampleMvcApp.Controllers
{
    public class AccountController : Controller 
    {
        public IActionResult Login(string returnUrl = "/")
        {
            var authProperties = new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };

            return Challenge(authProperties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var callbackUrl = Url.Action("Index", "Home", null, Request.Scheme);

            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = callbackUrl
            });

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect(callbackUrl);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var idToken = await HttpContext.GetTokenAsync("id_token");

            return View(new UserProfileViewModel
            {
                Name = User.Identity?.Name,
                EmailAddress = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                ProfileImage = User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value,
                AccessToken = accessToken,
                IdToken = idToken,
                AccessTokenJson = DecodeJwt(accessToken),
                IdTokenJson = DecodeJwt(idToken)
            });
        }

        private string DecodeJwt(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !token.Contains('.'))
                return "Invalid token";

            try
            {
                var parts = token.Split('.');
                var payload = parts[1];
                var jsonBytes = System.Convert.FromBase64String(PadBase64(payload));
                var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
                return System.Text.Json.JsonSerializer.Serialize(
                    System.Text.Json.JsonSerializer.Deserialize<object>(json),
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return "Unable to decode token";
            }
        }

        private string PadBase64(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: return base64 + "==";
                case 3: return base64 + "=";
                default: return base64;
            }
        }


        [Authorize]
        public IActionResult Claims()
        {
            return View(User.Claims);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
