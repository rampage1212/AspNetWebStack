﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Owin
{
    public class HostAuthenticationFilterTest
    {
        private const string ChallengeKey = "security.Challenge";

        [Fact]
        public void Constructor_Throws_WhenAuthenticationTypeIsNull()
        {
            // Arrange
            string authenticationType = null;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => { var ignore = CreateProductUnderTest(authenticationType); },
                "authenticationType");
        }

        [Fact]
        public void AuthenticationType_ReturnsSpecifiedInstance()
        {
            // Arrange
            string expectedAuthenticationType = "AuthenticationType";
            HostAuthenticationFilter filter = CreateProductUnderTest(expectedAuthenticationType);

            // Act
            string authenticationType = filter.AuthenticationType;

            // Assert
            Assert.Same(expectedAuthenticationType, authenticationType);
        }

        [Fact]
        public void AllowMultiple_ReturnsTrue()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();

            // Act
            bool allowMultiple = filter.AllowMultiple;

            // Assert
            Assert.True(allowMultiple);
        }

        [Fact]
        public void AuthenticateAsync_SetsClaimsPrincipal_WhenOwinAuthenticateReturnsIdentity()
        {
            // Arrange
            string authenticationType = "AuthenticationType";
            IAuthenticationFilter filter = CreateProductUnderTest(authenticationType);
            IIdentity expectedIdentity = CreateDummyIdentity();
            IAuthenticationManager authenticationManager = CreateAuthenticationManager((a) =>
                {
                    AuthenticateResult result;

                    if (a == authenticationType)
                    {
                        result = new AuthenticateResult(expectedIdentity, null, new Dictionary<string, object>());
                    }
                    else
                    {
                        result = null;
                    }

                    return Task.FromResult(result);
                });
            IOwinContext owinContext = CreateOwinContext(authenticationManager);
            HttpAuthenticationContext context;

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                context = CreateAuthenticationContext(request);

                // Act
                filter.AuthenticateAsync(context, CancellationToken.None).Wait();
            }

            // Assert
            Assert.Null(context.ErrorResult);
            IPrincipal principal = context.Principal;
            Assert.IsType<ClaimsPrincipal>(principal);
            ClaimsPrincipal claimsPrincipal = (ClaimsPrincipal)principal;
            IIdentity identity = claimsPrincipal.Identity;
            Assert.Same(expectedIdentity, identity);
        }

        [Fact]
        public void AuthenticateAsync_SetsNoPrincipalOrError_WhenOwinAuthenticateReturnsNullResult()
        {
            // Arrange
            string authenticationType = "AuthenticationType";
            IAuthenticationFilter filter = CreateProductUnderTest(authenticationType);
            IIdentity expectedIdentity = CreateDummyIdentity();
            IAuthenticationManager authenticationManager = CreateAuthenticationManager(
                (ignore1) => Task.FromResult<AuthenticateResult>(null));
            IOwinContext owinContext = CreateOwinContext(authenticationManager);
            IPrincipal expectedPrincipal = CreateDummyPrincipal();

            HttpAuthenticationContext context;

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                context = CreateAuthenticationContext(request, expectedPrincipal);

                // Act
                filter.AuthenticateAsync(context, CancellationToken.None).Wait();
            }

            // Assert
            Assert.Null(context.ErrorResult);
            Assert.Same(expectedPrincipal, context.Principal);
        }

        [Fact]
        public void AuthenticateAsync_SetsNoPrincipalOrError_WhenOwinAuthenticateReturnsNullIdentity()
        {
            // Arrange
            string authenticationType = "AuthenticationType";
            IAuthenticationFilter filter = CreateProductUnderTest(authenticationType);
            IIdentity expectedIdentity = CreateDummyIdentity();
            IAuthenticationManager authenticationManager = CreateAuthenticationManager(
                (ignore1) => Task.FromResult(new AuthenticateResult(null, null, new Dictionary<string, object>())));
            IOwinContext owinContext = CreateOwinContext(authenticationManager);
            IPrincipal expectedPrincipal = CreateDummyPrincipal();

            HttpAuthenticationContext context;

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                context = CreateAuthenticationContext(request, expectedPrincipal);

                // Act
                filter.AuthenticateAsync(context, CancellationToken.None).Wait();
            }

            // Assert
            Assert.Null(context.ErrorResult);
            Assert.Same(expectedPrincipal, context.Principal);
        }

        [Fact]
        public void AuthenticateAsync_Throws_WhenContextIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();

            // Act & Assert
            Assert.ThrowsArgumentNull(() =>
            {
                filter.AuthenticateAsync(null, CancellationToken.None).ThrowIfFaulted();
            }, "context");
        }

        [Fact]
        public void AuthenticateAsync_Throws_WhenRequestIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();
            HttpAuthenticationContext context = CreateAuthenticationContext();
            Assert.Null(context.Request);

            // Act & Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            {
                filter.AuthenticateAsync(context, CancellationToken.None).ThrowIfFaulted();
            });
            Assert.Equal("HttpAuthenticationContext.Request must not be null.", exception.Message);
        }

        [Fact]
        public void AuthenticateAsync_Throws_WhenOwinContextIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();

            using (HttpRequestMessage request = CreateRequest())
            {
                HttpAuthenticationContext context = CreateAuthenticationContext(request);

                // Act & Assert
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                {
                    filter.AuthenticateAsync(context, CancellationToken.None).ThrowIfFaulted();
                });
                Assert.Equal("No OWIN authentication manager is associated with the request.", exception.Message);
            }
        }

        [Fact]
        public void AuthenticateAsync_Throws_WhenAuthenticationManagerIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();
            IOwinContext owinContext = CreateOwinContext(null);

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                HttpAuthenticationContext context = CreateAuthenticationContext(request);

                // Act & Assert
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                {
                    filter.AuthenticateAsync(context, CancellationToken.None).ThrowIfFaulted();
                });
                Assert.Equal("No OWIN authentication manager is associated with the request.", exception.Message);
            }
        }

        [Fact]
        public void AuthenticateAsync_ReturnsCanceledTask_WhenCancellationIsRequested()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();
            IOwinContext owinContext = CreateOwinContext();

            Task task;

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                HttpAuthenticationContext context = CreateAuthenticationContext(request);
                CancellationToken cancellationToken = new CancellationToken(true);

                // Act
                task = filter.AuthenticateAsync(context, cancellationToken);

                // Assert
                task.WaitUntilCompleted();
            }

            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public void ChallengeAsync_AddsAuthenticationType_WhenOwinChallengeAlreadyExists()
        {
            // Arrange
            string expectedAuthenticationType = "AuthenticationType";
            IAuthenticationFilter filter = CreateProductUnderTest(expectedAuthenticationType);
            IHttpActionResult result = CreateDummyActionResult();
            string originalAuthenticationType = "FirstChallenge";
            AuthenticationExtra originalExtra = CreateExtra();
            AuthenticationResponseChallenge originalChallenge = new AuthenticationResponseChallenge(
                new string[] { originalAuthenticationType }, originalExtra);
            IAuthenticationManager authenticationManager = CreateAuthenticationManager(originalChallenge);
            IOwinContext owinContext = CreateOwinContext(authenticationManager);

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                HttpAuthenticationChallengeContext context = CreateChallengeContext(request, result);

                // Act
                filter.ChallengeAsync(context, CancellationToken.None).Wait();
            }

            // Assert
            AuthenticationResponseChallenge challenge = authenticationManager.AuthenticationResponseChallenge;
            Assert.NotNull(challenge);
            string[] authenticationTypes = challenge.AuthenticationTypes;
            Assert.NotNull(authenticationTypes);
            Assert.Equal(2, authenticationTypes.Length);
            Assert.Same(originalAuthenticationType, authenticationTypes[0]);
            Assert.Same(expectedAuthenticationType, authenticationTypes[1]);
            AuthenticationExtra extra = challenge.Extra;
            Assert.Same(originalExtra, extra);
        }

        [Fact]
        public void ChallengeAsync_CreatesAuthenticationTypes_WhenOwinChallengeWithNullTypesAlreadyExists()
        {
            // Arrange
            string expectedAuthenticationType = "AuthenticationType";
            IAuthenticationFilter filter = CreateProductUnderTest(expectedAuthenticationType);
            IHttpActionResult result = CreateDummyActionResult();
            AuthenticationExtra originalExtra = CreateExtra();
            AuthenticationResponseChallenge originalChallenge = new AuthenticationResponseChallenge(null,
                originalExtra);
            IAuthenticationManager authenticationManager = CreateAuthenticationManager(originalChallenge);
            IOwinContext owinContext = CreateOwinContext(authenticationManager);

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                HttpAuthenticationChallengeContext context = CreateChallengeContext(request, result);

                // Act
                filter.ChallengeAsync(context, CancellationToken.None).Wait();
            }

            // Assert
            AuthenticationResponseChallenge challenge = authenticationManager.AuthenticationResponseChallenge;
            Assert.NotNull(challenge);
            string[] authenticationTypes = challenge.AuthenticationTypes;
            Assert.NotNull(authenticationTypes);
            Assert.Equal(1, authenticationTypes.Length);
            Assert.Same(expectedAuthenticationType, authenticationTypes[0]);
            AuthenticationExtra extra = challenge.Extra;
            Assert.Same(originalExtra, extra);
        }

        [Fact]
        public void ChallengeAsync_CreatesOwinChallengeWithAuthenticationType_WhenNoChallengeExists()
        {
            // Arrange
            string expectedAuthenticationType = "AuthenticationType";
            IAuthenticationFilter filter = CreateProductUnderTest(expectedAuthenticationType);
            IHttpActionResult result = CreateDummyActionResult();
            IAuthenticationManager authenticationManager = CreateAuthenticationManager(
                (AuthenticationResponseChallenge)null);
            IOwinContext owinContext = CreateOwinContext(authenticationManager);

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                HttpAuthenticationChallengeContext context = CreateChallengeContext(request, result);

                // Act
                filter.ChallengeAsync(context, CancellationToken.None).Wait();
            }

            // Assert
            AuthenticationResponseChallenge challenge = authenticationManager.AuthenticationResponseChallenge;
            Assert.NotNull(challenge);
            string[] authenticationTypes = challenge.AuthenticationTypes;
            Assert.NotNull(authenticationTypes);
            Assert.Equal(1, authenticationTypes.Length);
            Assert.Same(expectedAuthenticationType, authenticationTypes[0]);
            AuthenticationExtra extra = challenge.Extra;
            Assert.NotNull(extra);
        }

        [Fact]
        public void ChallengeAsync_Throws_WhenContextIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();

            // Act & Assert
            Assert.ThrowsArgumentNull(() =>
            {
                filter.ChallengeAsync(null, CancellationToken.None).ThrowIfFaulted();
            }, "context");
        }

        [Fact]
        public void ChallengeAsync_Throws_WhenRequestIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();
            IHttpActionResult result = CreateDummyActionResult();
            HttpAuthenticationChallengeContext context = new HttpAuthenticationChallengeContext(
                new HttpActionContext(), result);
            Assert.Null(context.Request);

            // Act & Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            {
                filter.ChallengeAsync(context, CancellationToken.None).ThrowIfFaulted();
            });
            Assert.Equal("HttpAuthenticationChallengeContext.Request must not be null.", exception.Message);
        }

        [Fact]
        public void ChallengeAsync_Throws_WhenOwinContextIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();
            IHttpActionResult result = CreateDummyActionResult();

            using (HttpRequestMessage request = CreateRequest())
            {
                HttpAuthenticationChallengeContext context = CreateChallengeContext(request, result);

                // Act & Assert
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                {
                    filter.ChallengeAsync(context, CancellationToken.None).ThrowIfFaulted();
                });
                Assert.Equal("No OWIN authentication manager is associated with the request.", exception.Message);
            }
        }

        [Fact]
        public void ChallengeAsync_Throws_WhenAuthenticationManagerIsNull()
        {
            // Arrange
            IAuthenticationFilter filter = CreateProductUnderTest();
            IHttpActionResult result = CreateDummyActionResult();
            IOwinContext owinContext = CreateOwinContext(null);

            using (HttpRequestMessage request = CreateRequest(owinContext))
            {
                HttpAuthenticationChallengeContext context = CreateChallengeContext(request, result);

                // Act & Assert
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                {
                    filter.ChallengeAsync(context, CancellationToken.None).ThrowIfFaulted();
                });
                Assert.Equal("No OWIN authentication manager is associated with the request.", exception.Message);
            }
        }

        private static HttpActionContext CreateActionContext(HttpRequestMessage request)
        {
            HttpControllerContext controllerContext = new HttpControllerContext();
            controllerContext.Request = request;
            HttpActionDescriptor descriptor = CreateDummyActionDescriptor();
            return new HttpActionContext(controllerContext, descriptor);
        }

        private static HttpAuthenticationContext CreateAuthenticationContext()
        {
            HttpActionContext actionContext = new HttpActionContext();
            IPrincipal principal = CreateDummyPrincipal();
            return new HttpAuthenticationContext(actionContext, principal);
        }

        private static HttpAuthenticationContext CreateAuthenticationContext(HttpRequestMessage request)
        {
            IPrincipal principal = CreateDummyPrincipal();
            return CreateAuthenticationContext(request, principal);
        }

        private static HttpAuthenticationContext CreateAuthenticationContext(HttpRequestMessage request,
            IPrincipal principal)
        {
            HttpActionContext actionContext = CreateActionContext(request);
            return new HttpAuthenticationContext(actionContext, principal);
        }

        private static IAuthenticationManager CreateAuthenticationManager(
            Func<string, Task<AuthenticateResult>> authenticate)
        {
            Mock<IAuthenticationManager> mock = new Mock<IAuthenticationManager>(
                MockBehavior.Strict);
            string authenticationType = null;
            mock.Setup(m => m.AuthenticateAsync(It.IsAny<string>()))
                .Callback<string>((a) => { authenticationType = a; })
                .Returns(() => authenticate.Invoke(authenticationType));
            return mock.Object;
        }

        private static IAuthenticationManager CreateAuthenticationManager(AuthenticationResponseChallenge challenge)
        {
            Mock<IAuthenticationManager> mock = new Mock<IAuthenticationManager>(MockBehavior.Strict);
            mock.SetupProperty(m => m.AuthenticationResponseChallenge);
            IAuthenticationManager authenticationManager = mock.Object;
            authenticationManager.AuthenticationResponseChallenge = challenge;
            return authenticationManager;
        }

        private static HttpAuthenticationChallengeContext CreateChallengeContext(HttpRequestMessage request,
            IHttpActionResult result)
        {
            HttpActionContext actionContext = CreateActionContext(request);
            return new HttpAuthenticationChallengeContext(actionContext, result);
        }

        private static HttpActionDescriptor CreateDummyActionDescriptor()
        {
            return new Mock<HttpActionDescriptor>(MockBehavior.Strict).Object;
        }

        private static IHttpActionResult CreateDummyActionResult()
        {
            return new Mock<IHttpActionResult>(MockBehavior.Strict).Object;
        }

        private static ClaimsIdentity CreateDummyIdentity()
        {
            return new Mock<ClaimsIdentity>(MockBehavior.Strict).Object;
        }

        private static IPrincipal CreateDummyPrincipal()
        {
            return new Mock<IPrincipal>(MockBehavior.Strict).Object;
        }

        private static AuthenticationExtra CreateExtra()
        {
            return new AuthenticationExtra();
        }

        private static IOwinContext CreateOwinContext()
        {
            return new OwinContext();
        }

        private static IOwinContext CreateOwinContext(IAuthenticationManager authenticationManager)
        {
            Mock<IOwinContext> mockContext = new Mock<IOwinContext>(MockBehavior.Strict);
            mockContext.SetupGet(c => c.Authentication).Returns(authenticationManager);
            return mockContext.Object;
        }

        private static HostAuthenticationFilter CreateProductUnderTest()
        {
            return CreateProductUnderTest("IgnoreAuthenticationType");
        }

        private static HostAuthenticationFilter CreateProductUnderTest(string authenticationType)
        {
            return new HostAuthenticationFilter(authenticationType);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private static HttpRequestMessage CreateRequest(IOwinContext owinContext)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.SetOwinContext(owinContext);
            return request;
        }
    }
}
