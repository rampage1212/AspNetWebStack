﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using System.Web.Http.Routing;
using Microsoft.TestCommon;
using Moq;
using Moq.Protected;

namespace System.Web.Http
{
    public class HttpServerTest
    {
        [Fact]
        public void IsCorrectType()
        {
            Assert.Type.HasProperties<HttpServer, DelegatingHandler>(TypeAssert.TypeProperties.IsPublicVisibleClass | TypeAssert.TypeProperties.IsDisposable);
        }

        [Fact]
        public void DefaultConstructor()
        {
            Assert.DoesNotThrow(() => new HttpServer());
        }

        [Fact]
        public void ConstructorConfigThrowsOnNull()
        {
            Assert.ThrowsArgumentNull(() => new HttpServer((HttpConfiguration)null), "configuration");
        }

        [Fact]
        public void ConstructorConfigSetsUpProperties()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();

            // Act
            HttpServer server = new HttpServer(config);

            // Assert
            Assert.Same(config, server.Configuration);
        }

        [Fact]
        public void ConstructorDispatcherThrowsOnNull()
        {
            Assert.ThrowsArgumentNull(() => new HttpServer((HttpMessageHandler)null), "dispatcher");
        }

        [Fact]
        public void ConstructorDispatcherSetsUpProperties()
        {
            // Arrange
            Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();

            // Act
            HttpServer server = new HttpServer(mockHandler.Object);

            // Assert
            Assert.Same(mockHandler.Object, server.Dispatcher);
        }

        [Fact]
        public void ConstructorThrowsOnNull()
        {
            Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
            Assert.ThrowsArgumentNull(() => new HttpServer((HttpConfiguration)null, mockHandler.Object), "configuration");
            Assert.ThrowsArgumentNull(() => new HttpServer(new HttpConfiguration(), null), "dispatcher");
        }

        [Fact]
        public void ConstructorSetsUpProperties()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            Mock<HttpControllerDispatcher> controllerDispatcherMock = new Mock<HttpControllerDispatcher>(config);

            // Act
            HttpServer server = new HttpServer(config, controllerDispatcherMock.Object);

            // Assert
            Assert.Same(config, server.Configuration);
            Assert.Same(controllerDispatcherMock.Object, server.Dispatcher);
        }

        [Fact]
        public void ExceptionLoggerGet_ReturnsSpecifiedInstance()
        {
            // Arrange
            IExceptionLogger expectedExceptionLogger = CreateDummyExceptionLogger();
            IExceptionHandler exceptionHandler = CreateDummyExceptionHandler();

            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpMessageHandler dispatcher = CreateDummyMessageHandler())
            using (HttpServer product = CreateProductUnderTest(configuration, dispatcher, expectedExceptionLogger,
                exceptionHandler))
            {
                // Act
                IExceptionLogger exceptionLogger = product.ExceptionLogger;

                // Assert
                Assert.Same(expectedExceptionLogger, exceptionLogger);
            }
        }

        [Fact]
        public void ExceptionHandlerGet_ReturnsSpecifiedInstance()
        {
            // Arrange
            IExceptionLogger exceptionLogger = CreateDummyExceptionLogger();
            IExceptionHandler expectedExceptionHandler = CreateDummyExceptionHandler();

            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpMessageHandler dispatcher = CreateDummyMessageHandler())
            using (HttpServer product = CreateProductUnderTest(configuration, dispatcher, exceptionLogger,
                expectedExceptionHandler))
            {
                // Act
                IExceptionHandler exceptionHandler = product.ExceptionHandler;

                // Assert
                Assert.Same(expectedExceptionHandler, exceptionHandler);
            }
        }

        [Fact]
        public void ExceptionLoggerGet_IfUnset_ReturnsExceptionLoggerFromConfiguration()
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            {
                IExceptionLogger expectedExceptionLogger = CreateDummyExceptionLogger();
                configuration.Services.Add(typeof(IExceptionLogger), expectedExceptionLogger);

                using (HttpMessageHandler dispatcher = CreateDummyMessageHandler())
                using (HttpServer product = new HttpServer(configuration, dispatcher))
                {
                    // Act
                    IExceptionLogger exceptionLogger = product.ExceptionLogger;

                    // Assert
                    Assert.IsType<CompositeExceptionLogger>(exceptionLogger);
                    CompositeExceptionLogger compositeLogger = (CompositeExceptionLogger)exceptionLogger;
                    IEnumerable<IExceptionLogger> loggers = compositeLogger.Loggers;
                    Assert.NotNull(loggers);
                    Assert.Equal(1, loggers.Count());
                    Assert.Same(expectedExceptionLogger, loggers.Single());
                }
            }
        }

        [Fact]
        public void ExceptionHandlerGet_IfUnset_UsesExceptionHandlerFromConfiguration()
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            {
                IExceptionHandler expectedExceptionHandler = CreateDummyExceptionHandler();
                configuration.Services.Replace(typeof(IExceptionHandler), expectedExceptionHandler);

                using (HttpMessageHandler dispatcher = CreateDummyMessageHandler())
                using (HttpServer product = new HttpServer(configuration, dispatcher))
                {
                    // Act
                    IExceptionHandler exceptionHandler = product.ExceptionHandler;

                    // Assert
                    Assert.IsType<LastChanceExceptionHandler>(exceptionHandler);
                    LastChanceExceptionHandler lastChanceHandler = (LastChanceExceptionHandler)exceptionHandler;
                    Assert.Same(expectedExceptionHandler, lastChanceHandler.InnerHandler);
                }
            }
        }

        [Fact]
        public Task<HttpResponseMessage> DisposedReturnsServiceUnavailable()
        {
            // Arrange
            Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
            HttpServer server = new HttpServer(mockHandler.Object);
            HttpMessageInvoker invoker = new HttpMessageInvoker(server);
            server.Dispose();
            HttpRequestMessage request = new HttpRequestMessage();

            // Act
            return invoker.SendAsync(request, CancellationToken.None).ContinueWith(
                (reqTask) =>
                {
                    // Assert
                    mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Never(), request, CancellationToken.None);
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, reqTask.Result.StatusCode);
                    return reqTask.Result;
                }
            );
        }

        [Fact]
        public Task<HttpResponseMessage> RequestGetsConfigurationAsParameter()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();

            HttpConfiguration config = new HttpConfiguration();
            Mock<HttpControllerDispatcher> dispatcherMock = new Mock<HttpControllerDispatcher>(config);
            dispatcherMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", request, CancellationToken.None)
                .Returns(TaskHelpers.FromResult<HttpResponseMessage>(request.CreateResponse()));

            HttpServer server = new HttpServer(config, dispatcherMock.Object);
            HttpMessageInvoker invoker = new HttpMessageInvoker(server);

            // Act
            return invoker.SendAsync(request, CancellationToken.None).ContinueWith(
                (reqTask) =>
                {
                    // Assert
                    dispatcherMock.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), request, CancellationToken.None);
                    Assert.Same(config, request.GetConfiguration());
                    return reqTask.Result;
                }
            );
        }

        [Fact]
        public Task<HttpResponseMessage> RequestGetsSyncContextAsParameter()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();

            HttpConfiguration config = new HttpConfiguration();
            Mock<HttpControllerDispatcher> dispatcherMock = new Mock<HttpControllerDispatcher>(config);
            dispatcherMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", request, CancellationToken.None)
                .Returns(TaskHelpers.FromResult<HttpResponseMessage>(request.CreateResponse()));

            HttpServer server = new HttpServer(config, dispatcherMock.Object);
            HttpMessageInvoker invoker = new HttpMessageInvoker(server);

            SynchronizationContext syncContext = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncContext);

            // Act
            return invoker.SendAsync(request, CancellationToken.None).ContinueWith(
                (reqTask) =>
                {
                    // Assert
                    dispatcherMock.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), request, CancellationToken.None);
                    Assert.Same(syncContext, request.GetSynchronizationContext());
                    return reqTask.Result;
                }
            );
        }

        [Fact, RestoreThreadPrincipal]
        public Task SendAsync_SetsGenericPrincipalWhenThreadPrincipalIsNullAndCleansUpAfterward()
        {
            // Arrange
            var config = new HttpConfiguration();
            var request = new HttpRequestMessage();
            var dispatcherMock = new Mock<HttpControllerDispatcher>(config);
            var server = new HttpServer(config, dispatcherMock.Object);
            var invoker = new HttpMessageInvoker(server);
            IPrincipal callbackPrincipal = null;
            Thread.CurrentPrincipal = null;
            dispatcherMock.Protected()
                          .Setup<Task<HttpResponseMessage>>("SendAsync", request, CancellationToken.None)
                          .Callback(() => callbackPrincipal = Thread.CurrentPrincipal)
                          .Returns(TaskHelpers.FromResult<HttpResponseMessage>(request.CreateResponse()));

            // Act
            return invoker.SendAsync(request, CancellationToken.None)
                          .ContinueWith(req =>
                          {
                              // Assert
                              Assert.NotNull(callbackPrincipal);
                              Assert.False(callbackPrincipal.Identity.IsAuthenticated);
                              Assert.Empty(callbackPrincipal.Identity.Name);
                              Assert.Null(Thread.CurrentPrincipal);
                          });
        }

        [Fact, RestoreThreadPrincipal]
        public Task SendAsync_DoesNotChangeExistingThreadPrincipal()
        {
            // Arrange
            var config = new HttpConfiguration();
            var request = new HttpRequestMessage();
            var dispatcherMock = new Mock<HttpControllerDispatcher>(config);
            var server = new HttpServer(config, dispatcherMock.Object);
            var invoker = new HttpMessageInvoker(server);
            var principal = new GenericPrincipal(new GenericIdentity("joe"), new string[0]);
            Thread.CurrentPrincipal = principal;
            IPrincipal callbackPrincipal = null;
            dispatcherMock.Protected()
                          .Setup<Task<HttpResponseMessage>>("SendAsync", request, CancellationToken.None)
                          .Callback(() => callbackPrincipal = Thread.CurrentPrincipal)
                          .Returns(TaskHelpers.FromResult<HttpResponseMessage>(request.CreateResponse()));

            // Act
            return invoker.SendAsync(request, CancellationToken.None)
                          .ContinueWith(req =>
                          {
                              // Assert
                              Assert.Same(principal, callbackPrincipal);
                              Assert.Same(principal, Thread.CurrentPrincipal);
                          });
        }

        [Fact]
        public void SendAsync_Handles_ExceptionsThrownInMessageHandlers()
        {
            // Arrange
            var config = new HttpConfiguration();
            config.MessageHandlers.Add(new ThrowingMessageHandler(new InvalidOperationException()));
            HttpServer server = new HttpServer(config);
            var invoker = new HttpMessageInvoker(server);

            // Act
            var response = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None).Result;

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public void SendAsync_Handles_HttpResponseExceptionsThrownInMessageHandlers()
        {
            // Arrange
            HttpResponseException exception = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.HttpVersionNotSupported));
            exception.Response.ReasonPhrase = "whatever";
            var config = new HttpConfiguration();
            config.MessageHandlers.Add(new ThrowingMessageHandler(exception));
            HttpServer server = new HttpServer(config);
            var invoker = new HttpMessageInvoker(server);

            // Act
            var response = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None).Result;

            // Assert
            Assert.Equal(exception.Response.StatusCode, response.StatusCode);
            Assert.Equal(exception.Response.ReasonPhrase, response.ReasonPhrase);
        }

        [Fact]
        public void SendAsync_Handles_ExceptionsThrownInCustomRoutes()
        {
            // Arrange
            var config = new HttpConfiguration();
            config.Routes.Add("throwing route", new ThrowingRoute(new InvalidOperationException()));
            HttpServer server = new HttpServer(config);
            var invoker = new HttpMessageInvoker(server);

            // Act
            var response = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None).Result;

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public void SendAsync_Handles_HttpResponseExceptionsThrownInCustomRoutes()
        {
            // Arrange
            HttpResponseException exception = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.HttpVersionNotSupported));
            exception.Response.ReasonPhrase = "whatever";
            var config = new HttpConfiguration();
            config.Routes.Add("throwing route", new ThrowingRoute(exception));
            HttpServer server = new HttpServer(config);
            var invoker = new HttpMessageInvoker(server);

            // Act
            var response = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None).Result;

            // Assert
            Assert.Equal(exception.Response.StatusCode, response.StatusCode);
            Assert.Equal(exception.Response.ReasonPhrase, response.ReasonPhrase);
        }

        [Fact]
        public void SendAsync_IfDispatcherTaskIsFaulted_CallsExceptionServices()
        {
            // Arrange
            Exception expectedException = CreateException();

            HttpMessageHandler dispatcher = CreateFaultingMessageHandler(expectedException);

            Mock<IExceptionLogger> exceptionLoggerMock = CreateStubExceptionLoggerMock();
            IExceptionLogger exceptionLogger = exceptionLoggerMock.Object;

            Mock<IExceptionHandler> exceptionHandlerMock = CreateStubExceptionHandlerMock();
            IExceptionHandler exceptionHandler = exceptionHandlerMock.Object;

            using (HttpRequestMessage expectedRequest = CreateRequest())
            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpServer product = CreateProductUnderTest(configuration, dispatcher, exceptionLogger,
                exceptionHandler))
            {
                CancellationToken cancellationToken = CreateCancellationToken();

                Task<HttpResponseMessage> task = product.SendAsync(expectedRequest, cancellationToken);

                // Act
                task.WaitUntilCompleted();

                // Assert
                Func<ExceptionContext, bool> exceptionContextMatches = (c) =>
                    c != null
                    && c.Exception == expectedException
                    && c.CatchBlock == ExceptionCatchBlocks.HttpServer
                    && c.IsTopLevelCatchBlock == true
                    && c.Request == expectedRequest;

                exceptionLoggerMock.Verify(l => l.LogAsync(
                    It.Is<ExceptionLoggerContext>(c => c.CanBeHandled == true
                        && exceptionContextMatches(c.ExceptionContext)),
                    cancellationToken), Times.Once());

                exceptionHandlerMock.Verify(h => h.HandleAsync(
                    It.Is<ExceptionHandlerContext>((c) => exceptionContextMatches(c.ExceptionContext)),
                    cancellationToken), Times.Once());
            }
        }

        [Fact]
        public void SendAsync_IfExceptionHandlerSetsNullResult_PropogatesFaultedTaskException()
        {
            // Arrange
            Exception expectedException = CreateExceptionWithCallStack();
            string expectedStackTrace = expectedException.StackTrace;

            HttpMessageHandler dispatcher = CreateFaultingMessageHandler(expectedException);

            IExceptionLogger exceptionLogger = CreateStubExceptionLogger();

            Mock<IExceptionHandler> exceptionHandlerMock = new Mock<IExceptionHandler>(MockBehavior.Strict);
            exceptionHandlerMock
                .Setup(h => h.HandleAsync(It.IsAny<ExceptionHandlerContext>(), It.IsAny<CancellationToken>()))
                .Callback<ExceptionHandlerContext, CancellationToken>((c, i) => c.Result = null)
                .Returns(Task.FromResult(0))
                .Verifiable();
            IExceptionHandler exceptionHandler = exceptionHandlerMock.Object;

            using (HttpRequestMessage request = CreateRequest())
            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpServer product = CreateProductUnderTest(configuration, dispatcher, exceptionLogger,
                exceptionHandler))
            {
                CancellationToken cancellationToken = CreateCancellationToken();

                Task<HttpResponseMessage> task = product.SendAsync(request, cancellationToken);

                // Act
                task.WaitUntilCompleted();

                // Assert
                Assert.Equal(TaskStatus.Faulted, task.Status);
                Assert.NotNull(task.Exception);
                Exception exception = task.Exception.GetBaseException();
                Assert.Same(expectedException, exception);
                Assert.NotNull(exception.StackTrace);
                Assert.True(exception.StackTrace.StartsWith(expectedStackTrace));
            }
        }

        [Fact]
        public void SendAsync_IfExceptionHandlerHandlesException_ReturnsResponse()
        {
            // Arrange
            HttpMessageHandler dispatcher = CreateFaultingMessageHandler(CreateException());

            IExceptionLogger exceptionLogger = CreateStubExceptionLogger();

            using (HttpResponseMessage expectedResponse = CreateResponse())
            {
                Mock<IExceptionHandler> exceptionHandlerMock = new Mock<IExceptionHandler>(MockBehavior.Strict);
                exceptionHandlerMock
                    .Setup(h => h.HandleAsync(It.IsAny<ExceptionHandlerContext>(), It.IsAny<CancellationToken>()))
                    .Callback<ExceptionHandlerContext, CancellationToken>((c, i) =>
                        c.Result = new ResponseMessageResult(expectedResponse))
                    .Returns(Task.FromResult(0))
                    .Verifiable();
                IExceptionHandler exceptionHandler = exceptionHandlerMock.Object;

                using (HttpRequestMessage request = CreateRequest())
                using (HttpConfiguration configuration = new HttpConfiguration())
                using (HttpServer product = CreateProductUnderTest(configuration, dispatcher, exceptionLogger,
                    exceptionHandler))
                {
                    CancellationToken cancellationToken = CreateCancellationToken();

                    Task<HttpResponseMessage> task = product.SendAsync(request, cancellationToken);

                    // Act
                    task.WaitUntilCompleted();

                    // Assert
                    Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                    HttpResponseMessage response = task.Result;
                    Assert.Same(expectedResponse, response);
                }
            }
        }

        [Fact]
        public void HttpServerAddsDefaultRequestContext()
        {
            // Arrange
            HttpServer server = new HttpServer();
            var handler = new ThrowIfNoContext();

            server.Configuration.MessageHandlers.Add(handler);
            server.Configuration.MapHttpAttributeRoutes();
            server.Configuration.EnsureInitialized();

            var invoker = new HttpMessageInvoker(server);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Customers");

            // Act
            var response = invoker.SendAsync(request, CancellationToken.None).Result;

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.True(handler.ContextFound);
        }

        [Fact]
        public void HttpServerDoesNotReplaceOriginalRequestContext()
        {
            // Arrange
            HttpServer server = new HttpServer();
            var handler = new ThrowIfNoContext();

            server.Configuration.MessageHandlers.Add(handler);
            server.Configuration.MapHttpAttributeRoutes();
            server.Configuration.EnsureInitialized();

            HttpMessageInvoker invoker = new HttpMessageInvoker(server);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Customers");

            HttpRequestContext context = new HttpRequestContext();

            request.SetRequestContext(context);

            // Act
            var response = invoker.SendAsync(request, CancellationToken.None).Result;

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.True(handler.ContextFound);
            Assert.Equal(context, response.RequestMessage.GetRequestContext());
        }

        private static CancellationToken CreateCancellationToken()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            return source.Token;
        }

        private static HttpConfiguration CreateConfiguration()
        {
            return new HttpConfiguration();
        }

        private static IExceptionHandler CreateDummyExceptionHandler()
        {
            return new Mock<IExceptionHandler>(MockBehavior.Strict).Object;
        }

        private static IExceptionLogger CreateDummyExceptionLogger()
        {
            return new Mock<IExceptionLogger>(MockBehavior.Strict).Object;
        }

        private static HttpMessageHandler CreateDummyMessageHandler()
        {
            Mock<HttpMessageHandler> mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mock.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            return mock.Object;
        }

        private static Exception CreateException()
        {
            return new Exception();
        }

        private static Exception CreateExceptionWithCallStack()
        {
            try
            {
                throw CreateException();
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static Task<TResult> CreateFaultedTask<TResult>(Exception exception)
        {
            TaskCompletionSource<TResult> source = new TaskCompletionSource<TResult>();
            source.SetException(exception);
            return source.Task;
        }

        private static HttpMessageHandler CreateFaultingMessageHandler(Exception exception)
        {
            Mock<HttpMessageHandler> mock = new Mock<HttpMessageHandler>();
            mock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(CreateFaultedTask<HttpResponseMessage>(exception));
            return mock.Object;
        }

        private static HttpServer CreateProductUnderTest(HttpConfiguration configuration,
            HttpMessageHandler dispatcher, IExceptionLogger exceptionLogger, IExceptionHandler exceptionHandler)
        {
            return new HttpServer(configuration, dispatcher)
            {
                ExceptionLogger = exceptionLogger,
                ExceptionHandler = exceptionHandler
            };
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private static HttpResponseMessage CreateResponse()
        {
            return new HttpResponseMessage();
        }

        private static Mock<IExceptionHandler> CreateStubExceptionHandlerMock()
        {
            Mock<IExceptionHandler> mock = new Mock<IExceptionHandler>(MockBehavior.Strict);
            mock
                .Setup(h => h.HandleAsync(It.IsAny<ExceptionHandlerContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0));
            return mock;
        }

        private static IExceptionLogger CreateStubExceptionLogger()
        {
            return CreateStubExceptionLoggerMock().Object;
        }

        private static Mock<IExceptionLogger> CreateStubExceptionLoggerMock()
        {
            Mock<IExceptionLogger> mock = new Mock<IExceptionLogger>(MockBehavior.Strict);
            mock
                .Setup(l => l.LogAsync(It.IsAny<ExceptionLoggerContext>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0));
            return mock;
        }

        private class ThrowIfNoContext : DelegatingHandler
        {
            public bool ContextFound { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpRequestContext incomingContext = request.GetRequestContext();

                if (incomingContext == null)
                {
                    throw new InvalidOperationException("context missing");
                }

                ContextFound = true;

                Task<HttpResponseMessage> result = base.SendAsync(request, cancellationToken);

                HttpRequestContext outgoingContext = result.Result.RequestMessage.GetRequestContext();

                if (outgoingContext != incomingContext)
                {
                    throw new InvalidOperationException("context mismatch");
                }

                return result;
            }
        }

        public class RequestHasContextController : ApiController
        {
            [Route("Customers")]
            public IHttpActionResult Get()
            {
                if (RequestContext == null)
                {
                    return InternalServerError();
                }

                if (Request.GetRequestContext() == null)
                {
                    return BadRequest();
                }

                return Ok();
            }
        }

        private class ThrowingMessageHandler : DelegatingHandler
        {
            private Exception _exception;

            public ThrowingMessageHandler(Exception exception)
            {
                _exception = exception;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // dummy await so that the task doesn't get completed synchronously.
                await Task.FromResult(42);
                throw _exception;
            }
        }

        private class ThrowingRoute : HttpRoute
        {
            private Exception _exception;

            public ThrowingRoute(Exception exception)
            {
                _exception = exception;
            }

            public override IHttpRouteData GetRouteData(string virtualPathRoot, HttpRequestMessage request)
            {
                throw _exception;
            }
        }
    }
}
