﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Hosting;
using Microsoft.TestCommon;
using Moq;
using Owin;

namespace System.Web.Http.Owin
{
    public class WebApiAppBuilderExtensionsTest
    {
        [Fact]
        public void UseWebApiWithConfiguration_IfBuilderIsNull_Throws()
        {
            // Arrange
            IAppBuilder builder = null;

            using (HttpConfiguration configuration = CreateConfiguration())
            {
                // Act & Assert
                Assert.ThrowsArgumentNull(() => WebApiAppBuilderExtensions.UseWebApi(builder, configuration),
                    "builder");
            }
        }

        [Fact]
        public void UseWebApiWithConfiguration_IfConfigurationIsNull_Throws()
        {
            // Arrange
            IAppBuilder builder = CreateDummyAppBuilder();
            HttpConfiguration configuration = null;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => WebApiAppBuilderExtensions.UseWebApi(builder, configuration),
                "configuration");
        }

        [Fact]
        public void UseWebApiWithServer_IfBuilderIsNull_Throws()
        {
            // Arrange
            IAppBuilder builder = null;

            using (HttpServer httpServer = CreateServer())
            {
                // Act & Assert
                Assert.ThrowsArgumentNull(() => WebApiAppBuilderExtensions.UseWebApi(builder, httpServer), "builder");
            }
        }

        [Fact]
        public void UseWebApiWithServer_IfServerIsNull_Throws()
        {
            // Arrange
            IAppBuilder builder = CreateDummyAppBuilder();
            HttpServer httpServer = null;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => WebApiAppBuilderExtensions.UseWebApi(builder, httpServer), "httpServer");
        }

        [Fact]
        public void UseWebApi_UsesAdapter()
        {
            var config = new HttpConfiguration();
            var appBuilder = new Mock<IAppBuilder>();
            appBuilder
                .Setup(ab => ab.Use(
                    typeof(HttpMessageHandlerAdapter),
                    It.Is<HttpMessageHandlerOptions>((o) => ((HttpServer)o.MessageHandler).Configuration == config)))
                .Returns(appBuilder.Object)
                .Verifiable();

            IAppBuilder returnedAppBuilder = appBuilder.Object.UseWebApi(config);

            Assert.Equal(appBuilder.Object, returnedAppBuilder);
            appBuilder.Verify();
        }

        [Fact]
        public void UseWebApi_UsesAdapterAndConfigServices()
        {
            var config = new HttpConfiguration();
            var bufferPolicySelector = new Mock<IHostBufferPolicySelector>().Object;
            Mock<IExceptionLogger> loggerMock = new Mock<IExceptionLogger>();
            Mock<IExceptionHandler> handlerMock = new Mock<IExceptionHandler>();
            config.Services.Replace(typeof(IHostBufferPolicySelector), bufferPolicySelector);
            config.Services.Replace(typeof(IExceptionLogger), loggerMock.Object);
            config.Services.Replace(typeof(IExceptionHandler), handlerMock.Object);
            IExceptionLogger exceptionLogger = null;
            IExceptionHandler exceptionHandler = null;
            var appBuilder = new Mock<IAppBuilder>();
            appBuilder
                .Setup(ab => ab.Use(
                    typeof(HttpMessageHandlerAdapter),
                    It.Is<HttpMessageHandlerOptions>((o) => ((HttpServer)o.MessageHandler).Configuration == config
                        && o.BufferPolicySelector == bufferPolicySelector)))
                .Callback<object, object[]>((i, args) =>
                {
                    HttpMessageHandlerOptions options = (HttpMessageHandlerOptions)args[0];
                    exceptionLogger = options.ExceptionLogger;
                    exceptionHandler = options.ExceptionHandler;
                })
                .Returns(appBuilder.Object)
                .Verifiable();

            IAppBuilder returnedAppBuilder = appBuilder.Object.UseWebApi(config);

            Assert.Equal(appBuilder.Object, returnedAppBuilder);
            appBuilder.Verify();
            AssertDelegatesTo(loggerMock, exceptionLogger);
            AssertDelegatesTo(handlerMock, exceptionHandler);
        }

        [Fact]
        public void UseWebApiWithHttpServer_UsesAdapter()
        {
            // Arrange
            HttpServer httpServer = new Mock<HttpServer>().Object;
            Mock<IAppBuilder> appBuilderMock = new Mock<IAppBuilder>();
            appBuilderMock
                .Setup(ab => ab.Use(
                    typeof(HttpMessageHandlerAdapter),
                    It.Is<HttpMessageHandlerOptions>((o) => o.MessageHandler == httpServer)))
                .Returns(appBuilderMock.Object)
                .Verifiable();

            // Act
            IAppBuilder returnedAppBuilder = appBuilderMock.Object.UseWebApi(httpServer);

            // Assert
            Assert.Equal(appBuilderMock.Object, returnedAppBuilder);
            appBuilderMock.Verify();
        }

        [Fact]
        public void UseWebApiWithHttpServer_UsesAdapterAndConfigServices()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            IHostBufferPolicySelector bufferPolicySelector = new Mock<IHostBufferPolicySelector>().Object;
            Mock<IExceptionLogger> loggerMock = new Mock<IExceptionLogger>();
            Mock<IExceptionHandler> handlerMock = new Mock<IExceptionHandler>();
            config.Services.Replace(typeof(IHostBufferPolicySelector), bufferPolicySelector);
            config.Services.Replace(typeof(IExceptionLogger), loggerMock.Object);
            config.Services.Replace(typeof(IExceptionHandler), handlerMock.Object);
            HttpServer httpServer = new Mock<HttpServer>(config).Object;
            IExceptionLogger exceptionLogger = null;
            IExceptionHandler exceptionHandler = null;
            Mock<IAppBuilder> appBuilderMock = new Mock<IAppBuilder>();
            appBuilderMock
                .Setup(ab => ab.Use(
                    typeof(HttpMessageHandlerAdapter),
                    It.Is<HttpMessageHandlerOptions>((o) => o.MessageHandler == httpServer
                        && o.BufferPolicySelector == bufferPolicySelector)))
                .Callback<object, object[]>((i, args) =>
                {
                    HttpMessageHandlerOptions options = (HttpMessageHandlerOptions)args[0];
                    exceptionLogger = options.ExceptionLogger;
                    exceptionHandler = options.ExceptionHandler;
                })
                .Returns(appBuilderMock.Object)
                .Verifiable();

            // Act
            IAppBuilder returnedAppBuilder = appBuilderMock.Object.UseWebApi(httpServer);

            // Assert
            Assert.Equal(appBuilderMock.Object, returnedAppBuilder);
            appBuilderMock.Verify();
            AssertDelegatesTo(loggerMock, exceptionLogger);
            AssertDelegatesTo(handlerMock, exceptionHandler);
        }

        private static void AssertDelegatesTo(Mock<IExceptionHandler> expected, IExceptionHandler actual)
        {
            Assert.NotNull(actual);

            ExceptionHandlerContext context = new ExceptionHandlerContext(new ExceptionContext()
            {
                Exception = new Exception()
            });
            CancellationToken cancellationToken = CancellationToken.None;

            expected
                .Setup((l) => l.HandleAsync(It.IsAny<ExceptionHandlerContext>(), It.IsAny<CancellationToken>()))
                .Returns(CreateCanceledTask());

            Task task = actual.HandleAsync(context, cancellationToken);

            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Canceled, task.Status);

            expected.Verify((l) => l.HandleAsync(context, cancellationToken), Times.Once());
        }

        private static void AssertDelegatesTo(Mock<IExceptionLogger> expected, IExceptionLogger actual)
        {
            Assert.NotNull(actual);

            ExceptionLoggerContext context = new ExceptionLoggerContext(new ExceptionContext()
            {
                Exception = new Exception()
            });
            CancellationToken cancellationToken = CancellationToken.None;

            expected
                .Setup((l) => l.LogAsync(It.IsAny<ExceptionLoggerContext>(), It.IsAny<CancellationToken>()))
                .Returns(CreateCanceledTask());

            Task task = actual.LogAsync(context, cancellationToken);

            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Canceled, task.Status);

            expected.Verify((l) => l.LogAsync(context, cancellationToken), Times.Once());
        }

        private static Task CreateCanceledTask()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            source.SetCanceled();
            return source.Task;
        }

        private static HttpConfiguration CreateConfiguration()
        {
            return new HttpConfiguration();
        }

        private static IAppBuilder CreateDummyAppBuilder()
        {
            return new Mock<IAppBuilder>(MockBehavior.Strict).Object;
        }

        private static HttpServer CreateServer()
        {
            return new HttpServer();
        }
    }
}
