using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FoodEntity.Models;

namespace Repository.Security
{
    public static class DependencyInjection
    {
        public static bool SignIn(this User us, HttpContext httpContext, bool KeepMeLoggedin = true)
        {
            try
            {
                var claims = new List<Claim>()
                    {
                      new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "" + us.FullName ),
                      new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "" + us.FullName),
                      new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "Food"),
                      new Claim("Name",  "" + us.FullName) ,
                      new Claim("UserName",  "" + us.FullName) ,


                      new Claim("Id", us.Id.ToString()),

                   };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties() { IsPersistent = KeepMeLoggedin };
                httpContext.SignOutAsync();
                httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                return true;
            }
            catch
            {
                return false;
                throw;
            }
        }

        public static string GetUserName(this ClaimsPrincipal ob)
        {
            var identity = ob;

            if (identity == null)
            {
                return "";
            }

            var identityClaims = identity.Claims.FirstOrDefault(c => c.Type == "UserName")?.Value;
            return "" + identityClaims;
        }

        public static bool SignOut(this User us, HttpContext httpContext)
        {
            try
            {
                httpContext.SignOutAsync(

            //authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
                return true;
            }
            catch
            {
                return false;
                throw;
            }
        }
    }
}
