
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using Intuit.Ipp.DataService;

namespace MvcCodeFlowClientManual.Controllers
{
    

    [Authorize]
    public class AppController : Controller
    {
        public static string mod;
        public static string expo;

        public static string clientid = ConfigurationManager.AppSettings["clientid"];
        public static string clientsecret = ConfigurationManager.AppSettings["clientsecret"];
        public static string redirectUrl = ConfigurationManager.AppSettings["redirectUrl"];
        public static string stateCSRFToken = "";

        public static string authorizeUrl = "";
        public static string tokenEndpoint = "";
        public static string revocationEndpoint = "";
        public static string userinfoEndpoint = "";
        public static string issuerEndpoint = "";
        public static string code = "";

        public static string access_token = "";
        public static string refresh_token = "";
        public static string identity_token = "";
        public static IList<JsonWebKey> keys;


        


        public ActionResult Index()
        {
            return View();
        }

       


        /// <summary>
        /// Refresh the token 
        /// </summary>
        /// <returns></returns>
        protected async Task<ActionResult> RefreshToken()
        {
            //Refresh Token call to tokenendpoint
            var tokenClient = new TokenClient(AppController.tokenEndpoint, AppController.clientid, AppController.clientsecret);
            var principal = User as ClaimsPrincipal;
            var refreshToken = principal.FindFirst("refresh_token").Value;

            TokenResponse response = await tokenClient.RequestRefreshTokenAsync(refreshToken);
            UpdateCookie(response);

            return RedirectToAction("Index");
        }

       
        /// <summary>
        /// Intialize servicecontext
        /// </summary>
        /// <param name="realmId"></param>
        /// <returns></returns>
        public ServiceContext IntializeContext(string realmId)
        {
            var principal = User as ClaimsPrincipal;
            OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(principal.FindFirst("access_token").Value);
            ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
            //Enable minorversion 
            serviceContext.IppConfiguration.MinorVersion.Qbo = "23";
            //Enable logging
            //serviceContext.IppConfiguration.Logger.RequestLog.EnableRequestResponseLogging = true;
            //serviceContext.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = @"C:\IdsLogs";//Create a folder in your drive first
            return serviceContext;
        }

        /// <summary>
        /// Revoke access tokens
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> RevokeAccessToken()
        {
            var accessToken = (User as ClaimsPrincipal).FindFirst("access_token").Value;

            //Revoke Access token call
            var revokeClient = new TokenRevocationClient(AppController.revocationEndpoint, clientid, clientsecret);

            //Revoke access token
            TokenRevocationResponse revokeAccessTokenResponse = await revokeClient.RevokeAccessTokenAsync(accessToken);
            if (revokeAccessTokenResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                Session.Abandon();
                Request.GetOwinContext().Authentication.SignOut();
                
            }//delete claims and cookies
           
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Revoke refresh tokens
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> RevokeRefreshToken()
        {
            var refreshToken = (User as ClaimsPrincipal).FindFirst("refresh_token").Value;
            
            //Revoke Refresh token call
            var revokeClient = new TokenRevocationClient(AppController.revocationEndpoint, clientid, clientsecret);

            //Revoke refresh token
            TokenRevocationResponse revokeAccessTokenResponse = await revokeClient.RevokeAccessTokenAsync(refreshToken);
            if (revokeAccessTokenResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                Session.Abandon();
                Request.GetOwinContext().Authentication.SignOut();
            }
            //return RedirectToAction("Index");
            return RedirectToAction("Index");
        }

        //Update cookie with new claim indfo/tokens for logged in user
        private void UpdateCookie(TokenResponse response)
        {
            if (response.IsError)
            {
                throw new Exception(response.Error);
            }

            var identity = (User as ClaimsPrincipal).Identities.First();
            var result = from c in identity.Claims
                         where c.Type != "access_token" &&
                               c.Type != "refresh_token" &&
                               c.Type != "access_token_expires_at" &&
                               c.Type != "access_token_expires_at" 
                         select c;

            var claims = result.ToList();

            claims.Add(new Claim("access_token", response.AccessToken));
           
            claims.Add(new Claim("access_token_expires_at", (DateTime.Now.AddSeconds(response.AccessTokenExpiresIn)).ToString()));
            claims.Add(new Claim("refresh_token", response.RefreshToken));
           
            claims.Add(new Claim("refresh_token_expires_at", (DateTime.UtcNow.ToEpochTime() + response.RefreshTokenExpiresIn).ToDateTimeFromEpoch().ToString()));
           
            var newId = new ClaimsIdentity(claims, "Cookies");
            Request.GetOwinContext().Authentication.SignIn(newId);
        }
        
    }
}