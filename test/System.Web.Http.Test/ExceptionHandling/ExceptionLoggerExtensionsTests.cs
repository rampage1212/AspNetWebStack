﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.ExceptionHandling
{
    public class ExceptionLoggerExtensionsTests
    {
        [Fact]
        public void LogAsync_DelegatesToInterfaceLogAsync()
        {
            // Arrange
            Task expectedTask = CreateCompletedTask();
            Mock<IExceptionLogger> mock = new Mock<IExceptionLogger>(MockBehavior.Strict);
            mock
                .Setup(h => h.LogAsync(It.IsAny<ExceptionLoggerContext>(), It.IsAny<CancellationToken>()))
                .Returns(expectedTask);

            IExceptionLogger logger = mock.Object;

            using (CancellationTokenSource tokenSource = CreateCancellationTokenSource())
            {
                ExceptionContext expectedContext = CreateContext();
                CancellationToken expectedCancellationToken = tokenSource.Token;

                // Act
                Task task = ExceptionLoggerExtensions.LogAsync(logger, expectedContext, expectedCancellationToken);

                // Assert
                Assert.Same(expectedTask, task);
                task.WaitUntilCompleted();

                mock.Verify(h => h.LogAsync(It.Is<ExceptionLoggerContext>(c => c.ExceptionContext == expectedContext),
                    expectedCancellationToken), Times.Once());
            }
        }

        [Fact]
        public void LogAsync_IfLoggerIsNull_Throws()
        {
            // Arrange
            IExceptionLogger logger = null;
            ExceptionContext context = CreateContext();
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.ThrowsArgumentNull(() =>
                ExceptionLoggerExtensions.LogAsync(logger, context, cancellationToken), "logger");
        }

        [Fact]
        public void LogAsync_IfContextIsNull_Throws()
        {
            // Arrange
            IExceptionLogger logger = CreateDummyLogger();
            ExceptionContext context = null;
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            Assert.ThrowsArgumentNull(() =>
                ExceptionLoggerExtensions.LogAsync(logger, context, cancellationToken), "context");
        }

        private static CancellationTokenSource CreateCancellationTokenSource()
        {
            return new CancellationTokenSource();
        }

        private static Task CreateCompletedTask()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            source.SetResult(null);
            return source.Task;
        }

        private static ExceptionContext CreateContext()
        {
            return new ExceptionContext();
        }

        private static IExceptionLogger CreateDummyLogger()
        {
            return new Mock<IExceptionLogger>(MockBehavior.Strict).Object;
        }
    }
}
