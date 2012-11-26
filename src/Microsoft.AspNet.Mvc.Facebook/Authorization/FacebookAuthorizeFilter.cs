﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Facebook;
using Microsoft.AspNet.Mvc.Facebook.Client;

namespace Microsoft.AspNet.Mvc.Facebook.Authorization
{
    public class FacebookAuthorizeFilter : IAuthorizationFilter
    {
        private FacebookConfiguration _config;

        public FacebookAuthorizeFilter(FacebookConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _config = config;
        }

        public virtual void OnAuthorization(AuthorizationContext filterContext)
        {
            // TODO, set the state parameter to protect against cross-site request forgery (https://developers.facebook.com/docs/howtos/login/server-side-login/).
            // This will require session state to be used so we have to fall back if session is disabled (https://developers.facebook.com/docs/reference/dialogs/oauth/#parameters).
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            IEnumerable<object> authorizeAttributes = filterContext.ActionDescriptor.GetCustomAttributes(typeof(FacebookAuthorizeAttribute), inherit: true)
                .Union(filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(FacebookAuthorizeAttribute), inherit: true));
            if (!authorizeAttributes.Any())
            {
                return;
            }

            FacebookClient client = _config.ClientProvider.CreateClient();
            dynamic signedRequest = client.ParseSignedRequest(filterContext.HttpContext.Request);
            string userId = null;
            string accessToken = null;
            if (signedRequest != null)
            {
                userId = signedRequest.user_id;
                accessToken = signedRequest.oauth_token;
            }

            string appUrl = _config.AppUrl;
            string redirectUrl = appUrl + filterContext.HttpContext.Request.Url.PathAndQuery;
            if (signedRequest == null || userId == null || accessToken == null)
            {
                // Request is not coming from facebook, redirect to facebook login.
                Uri loginUrl = client.GetLoginUrl(redirectUrl, _config.AppId, null);
                filterContext.Result = CreateRedirectResult(loginUrl);
            }
            else
            {
                HashSet<string> requiredPermissions = GetRequiredPermissions(authorizeAttributes);
                if (requiredPermissions.Count > 0)
                {
                    IEnumerable<string> currentPermissions = _config.PermissionService.GetUserPermissions(userId, accessToken);

                    // If the current permissions doesn't cover all required permissions,
                    // redirect to facebook login or to the specified redirect path.
                    if (currentPermissions == null || !requiredPermissions.IsSubsetOf(currentPermissions))
                    {
                        string requiredPermissionString = String.Join(",", requiredPermissions);
                        Uri authorizationUrl;
                        if (String.IsNullOrEmpty(_config.AuthorizationRedirectPath))
                        {
                            authorizationUrl = client.GetLoginUrl(redirectUrl, _config.AppId, requiredPermissionString);
                        }
                        else
                        {
                            UriBuilder authorizationUrlBuilder = new UriBuilder(appUrl);
                            authorizationUrlBuilder.Path += "/" + _config.AuthorizationRedirectPath.TrimStart('/');
                            authorizationUrlBuilder.Query = String.Format(CultureInfo.InvariantCulture,
                                "originUrl={0}&permissions={1}",
                                HttpUtility.UrlEncode(redirectUrl),
                                HttpUtility.UrlEncode(requiredPermissionString));
                            authorizationUrl = authorizationUrlBuilder.Uri;
                        }
                        filterContext.Result = CreateRedirectResult(authorizationUrl);
                    }
                }
            }
        }

        public virtual ActionResult CreateRedirectResult(Uri redirectUrl)
        {
            if (redirectUrl == null)
            {
                throw new ArgumentNullException("redirectUrl");
            }

            ContentResult facebookAuthResult = new ContentResult();
            facebookAuthResult.ContentType = "text/html";

            // Even though we're only JavaScript encoding the redirectUrl, the result is guaranteed to be HTML-safe as well
            facebookAuthResult.Content = String.Format(
                "<script>window.top.location = '{0}';</script>",
                HttpUtility.JavaScriptStringEncode(redirectUrl.AbsoluteUri));
            return facebookAuthResult;
        }

        private static HashSet<string> GetRequiredPermissions(IEnumerable<object> facebookAuthorizeAttributes)
        {
            HashSet<string> requiredPermissions = new HashSet<string>();
            foreach (FacebookAuthorizeAttribute facebookAuthorize in facebookAuthorizeAttributes)
            {
                foreach (string permission in facebookAuthorize.Permissions)
                {
                    if (permission.Contains(','))
                    {
                        throw new ArgumentException(
                            String.Format(
                                CultureInfo.CurrentCulture,
                                Resources.PermissionStringShouldNotContainComma,
                                permission));
                    }

                    requiredPermissions.Add(permission);
                }
            }
            return requiredPermissions;
        }
    }
}
