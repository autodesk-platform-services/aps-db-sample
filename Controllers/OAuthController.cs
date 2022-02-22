/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Autodesk.Forge;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace forgeSample.Controllers
{
    public class OAuthController : ControllerBase
    {
        private static dynamic InternalToken { get; set; }
        private static dynamic PublicToken { get; set; }

        /// <summary>
        /// Get access token with public (viewables:read) scope
        /// </summary>
        [HttpGet]
        [Route("api/forge/oauth/oss/token")]
        public async Task<dynamic> GetPublicAsync()
        {
            if (PublicToken == null || PublicToken.ExpiresAt < DateTime.UtcNow)
            {
                PublicToken = await Get2LeggedTokenAsync(new Scope[] { Scope.ViewablesRead });
                PublicToken.ExpiresAt = DateTime.UtcNow.AddSeconds(PublicToken.expires_in);
            }
            return PublicToken;
        }

        [HttpGet]
        [Route("api/forge/oauth/token")]
        public async Task<AccessToken> GetPublicTokenAsync()
        {
            Credentials credentials = await Credentials.FromSessionAsync(Request.Cookies, Response.Cookies);

            if (credentials == null)
            {
                base.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return new AccessToken();
            }

            // return the public (viewables:read) access token
            return new AccessToken()
            {
                access_token = credentials.TokenPublic,
                expires_in = (int)credentials.ExpiresAt.Subtract(DateTime.Now).TotalSeconds
            };
        }

        /// <summary>
        /// Get access token with internal (write) scope
        /// </summary>
        public static async Task<dynamic> GetInternalAsync()
        {
            if (InternalToken == null || InternalToken.ExpiresAt < DateTime.UtcNow)
            {
                InternalToken = await Get2LeggedTokenAsync(new Scope[] { Scope.BucketCreate, Scope.BucketRead, Scope.BucketDelete, Scope.DataRead, Scope.DataWrite, Scope.DataCreate, Scope.CodeAll });
                InternalToken.ExpiresAt = DateTime.UtcNow.AddSeconds(InternalToken.expires_in);
            }

            return InternalToken;
        }

        /// <summary>
        /// Get the access token from Autodesk
        /// </summary>
        private static async Task<dynamic> Get2LeggedTokenAsync(Scope[] scopes)
        {
            TwoLeggedApi oauth = new TwoLeggedApi();
            string grantType = "client_credentials";
            dynamic bearer = await oauth.AuthenticateAsync(
              GetAppSetting("FORGE_CLIENT_ID"),
              GetAppSetting("FORGE_CLIENT_SECRET"),
              grantType,
              scopes);
            return bearer;
        }

        /// <summary>
        /// Response for GetPublicToken
        /// </summary>
        public struct AccessToken
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
        }

        [HttpGet]
        [Route("api/forge/oauth/signout")]
        public IActionResult Singout()
        {
            // finish the session
            Credentials.Signout(base.Response.Cookies);

            return Redirect("/");
        }

        [HttpGet]
        [Route("api/forge/oauth/url")]
        public string GetOAuthURL()
        {
            // prepare the sign in URL
            Scope[] scopes = { Scope.DataRead };
            ThreeLeggedApi _threeLeggedApi = new ThreeLeggedApi();
            string oauthUrl = _threeLeggedApi.Authorize(
              Credentials.GetAppSetting("FORGE_CLIENT_ID"),
              oAuthConstants.CODE,
              Credentials.GetAppSetting("FORGE_CALLBACK_URL"),
              new Scope[] { Scope.DataRead, Scope.DataCreate, Scope.DataWrite, Scope.ViewablesRead });

            return oauthUrl;
        }

        [HttpGet]
        [Route("api/forge/callback/oauth")] // see Web.Config FORGE_CALLBACK_URL variable
        public async Task<IActionResult> OAuthCallbackAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Redirect("/");
            // create credentials form the oAuth CODE
            Credentials credentials = await Credentials.CreateFromCodeAsync(code, Response.Cookies);

            return Redirect("/");
        }

        [HttpGet]
        [Route("api/forge/clientid")] // see Web.Config FORGE_CALLBACK_URL variable
        public dynamic GetClientID()
        {
            return new { id = Credentials.GetAppSetting("FORGE_CLIENT_ID") };
        }

        /// <summary>
        /// Reads appsettings from web.config
        /// </summary>
        public static string GetAppSetting(string settingKey)
        {
            return Environment.GetEnvironmentVariable(settingKey);
        }
    }

    /// <summary>
    /// Store data in session
    /// </summary>
    public class Credentials
    {
        private const string FORGE_COOKIE = "ForgeApp";

        private Credentials() { }

        public string TokenInternal { get; set; }
        public string TokenPublic { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Perform the OAuth authorization via code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static async Task<Credentials> CreateFromCodeAsync(string code, IResponseCookies cookies)
        {
            ThreeLeggedApi oauth = new ThreeLeggedApi();

            dynamic credentialInternal = await oauth.GettokenAsync(
              GetAppSetting("FORGE_CLIENT_ID"), GetAppSetting("FORGE_CLIENT_SECRET"),
              oAuthConstants.AUTHORIZATION_CODE, code, GetAppSetting("FORGE_CALLBACK_URL"));

            dynamic credentialPublic = await oauth.RefreshtokenAsync(
              GetAppSetting("FORGE_CLIENT_ID"), GetAppSetting("FORGE_CLIENT_SECRET"),
              "refresh_token", credentialInternal.refresh_token, new Scope[] { Scope.ViewablesRead });

            Credentials credentials = new Credentials();
            credentials.TokenInternal = credentialInternal.access_token;
            credentials.TokenPublic = credentialPublic.access_token;
            credentials.RefreshToken = credentialPublic.refresh_token;
            credentials.ExpiresAt = DateTime.Now.AddSeconds(credentialInternal.expires_in);

            cookies.Append(FORGE_COOKIE, JsonConvert.SerializeObject(credentials));

            return credentials;
        }

        /// <summary>
        /// Restore the credentials from the session object, refresh if needed
        /// </summary>
        /// <returns></returns>
        public static async Task<Credentials> FromSessionAsync(IRequestCookieCollection requestCookie, IResponseCookies responseCookie)
        {
            if (requestCookie == null || !requestCookie.ContainsKey(FORGE_COOKIE)) return null;

            Credentials credentials = JsonConvert.DeserializeObject<Credentials>(requestCookie[FORGE_COOKIE]);
            if (credentials.ExpiresAt < DateTime.Now)
            {
                await credentials.RefreshAsync();
                responseCookie.Delete(FORGE_COOKIE);
                responseCookie.Append(FORGE_COOKIE, JsonConvert.SerializeObject(credentials));
            }

            return credentials;
        }

        public static void Signout(IResponseCookies cookies)
        {
            cookies.Delete(FORGE_COOKIE);
        }

        /// <summary>
        /// Refresh the credentials (internal & external)
        /// </summary>
        /// <returns></returns>
        private async Task RefreshAsync()
        {
            ThreeLeggedApi oauth = new ThreeLeggedApi();

            dynamic credentialInternal = await oauth.RefreshtokenAsync(
              GetAppSetting("FORGE_CLIENT_ID"), GetAppSetting("FORGE_CLIENT_SECRET"),
              "refresh_token", RefreshToken, new Scope[] { Scope.DataRead, Scope.DataCreate, Scope.DataWrite, Scope.ViewablesRead });

            dynamic credentialPublic = await oauth.RefreshtokenAsync(
              GetAppSetting("FORGE_CLIENT_ID"), GetAppSetting("FORGE_CLIENT_SECRET"),
              "refresh_token", credentialInternal.refresh_token, new Scope[] { Scope.ViewablesRead });

            TokenInternal = credentialInternal.access_token;
            TokenPublic = credentialPublic.access_token;
            RefreshToken = credentialPublic.refresh_token;
            ExpiresAt = DateTime.Now.AddSeconds(credentialInternal.expires_in);
        }

        /// <summary>
        /// Reads appsettings from web.config
        /// </summary>
        public static string GetAppSetting(string settingKey)
        {
            return Environment.GetEnvironmentVariable(settingKey);
        }
    }
}