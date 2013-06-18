﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Owin.Properties;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace System.Web.Http
{
    /// <summary>Represents an authentication filter that authenticates via OWIN middleware.</summary>
    public class HostAuthenticationFilter : IAuthenticationFilter
    {
        private readonly string _authenticationType;

        /// <summary>Initializes a new instance of the <see cref="HostAuthenticationFilter"/> class.</summary>
        /// <param name="authenticationType">The authentication type of the OWIN middleware to use.</param>
        public HostAuthenticationFilter(string authenticationType)
        {
            if (authenticationType == null)
            {
                throw new ArgumentNullException("authenticationType");
            }

            _authenticationType = authenticationType;
        }

        /// <summary>Gets the authentication type of the OWIN middleware to use.</summary>
        public string AuthenticationType
        {
            get { return _authenticationType; }
        }

        /// <inheritdoc />
        public async Task<IAuthenticationResult> AuthenticateAsync(HttpAuthenticationContext context,
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpRequestMessage request = context.Request;

            if (request == null)
            {
                throw new InvalidOperationException(OwinResources.HttpAuthenticationContext_RequestMustNotBeNull);
            }

            OwinRequest owinRequest = request.GetOwinRequest();

            cancellationToken.ThrowIfCancellationRequested();
            IIdentity identity = await owinRequest.AuthenticateAsync(_authenticationType);

            if (identity == null)
            {
                return null;
            }

            return new SucceededAuthenticationResult(new ClaimsPrincipal(identity));
        }

        /// <inheritdoc />
        public Task<IHttpActionResult> ChallengeAsync(HttpActionContext context, IHttpActionResult innerResult,
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpRequestMessage request = context.Request;

            if (request == null)
            {
                throw new InvalidOperationException(OwinResources.HttpAuthenticationContext_RequestMustNotBeNull);
            }

            OwinResponse response = request.GetOwinResponse();

            // Control the challenges that OWIN middleware adds later.
            response.AuthenticationResponseChallenge = AddChallengeAuthenticationType(
                response.AuthenticationResponseChallenge, _authenticationType);

            // Otherwise, return the provided result as-is.
            return Task.FromResult(innerResult);
        }

        /// <inheritdoc />
        public bool AllowMultiple
        {
            get { return true; }
        }

        private static AuthenticationResponseChallenge AddChallengeAuthenticationType(
            AuthenticationResponseChallenge challenge, string authenticationType)
        {
            Contract.Assert(authenticationType != null);

            List<string> authenticationTypes = new List<string>();
            AuthenticationExtra extra;

            if (challenge != null)
            {
                string[] currentAuthenticationTypes = challenge.AuthenticationTypes;

                if (currentAuthenticationTypes != null)
                {
                    authenticationTypes.AddRange(currentAuthenticationTypes);
                }

                extra = challenge.Extra;
            }
            else
            {
                extra = new AuthenticationExtra();
            }

            authenticationTypes.Add(authenticationType);

            return new AuthenticationResponseChallenge(authenticationTypes.ToArray(), extra);
        }
    }
}
