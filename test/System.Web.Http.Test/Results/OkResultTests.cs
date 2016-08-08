﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TestCommon;

namespace System.Web.Http.Results
{
    public class OkResultTests
    {
        [Fact]
        public void Constructor_Throws_WhenRequestIsNull()
        {
            // Arrange
            HttpRequestMessage request = null;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => { CreateProductUnderTest(request); }, "request");
        }

        [Fact]
        public void Request_Returns_InstanceProvided()
        {
            // Arrange
            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                OkResult result = CreateProductUnderTest(expectedRequest);

                // Act
                HttpRequestMessage request = result.Request;

                // Assert
                Assert.Same(expectedRequest, request);
            }
        }

        [Fact]
        public void ExecuteAsync_Returns_CorrectResponse()
        {
            // Arrange
            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                IHttpActionResult result = CreateProductUnderTest(expectedRequest);

                // Act
                Task<HttpResponseMessage> task = result.ExecuteAsync(CancellationToken.None);

                // Assert
                Assert.NotNull(task);
                task.WaitUntilCompleted();

                using (HttpResponseMessage response = task.Result)
                {
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Same(expectedRequest, response.RequestMessage);
                }
            }
        }

        [Fact]
        public void Constructor_ForApiController_Throws_WhenControllerIsNull()
        {
            // Arrange
            ApiController controller = null;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => { CreateProductUnderTest(controller); }, "controller");
        }

        [Fact]
        public void ExecuteAsync_ForApiController_ReturnsCorrectResponse()
        {
            // Arrange
            ApiController controller = CreateController();

            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                controller.Request = expectedRequest;
                IHttpActionResult result = CreateProductUnderTest(controller);

                // Act
                Task<HttpResponseMessage> task = result.ExecuteAsync(CancellationToken.None);

                // Assert
                Assert.NotNull(task);
                task.WaitUntilCompleted();

                using (HttpResponseMessage response = task.Result)
                {
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Same(expectedRequest, response.RequestMessage);
                }
            }
        }

        [Fact]
        public void Request_ForApiController_EvaluatesLazily()
        {
            // Arrange
            ApiController controller = CreateController();
            OkResult result = CreateProductUnderTest(controller);

            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                controller.Request = expectedRequest;

                // Act
                HttpRequestMessage request = result.Request;

                // Assert
                Assert.Same(expectedRequest, request);
            }
        }

        [Fact]
        public void Request_ForApiController_EvaluatesOnce()
        {
            // Arrange
            ApiController controller = CreateController();
            OkResult result = CreateProductUnderTest(controller);

            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                controller.Request = expectedRequest;
                HttpRequestMessage ignore = result.Request;

                using (HttpRequestMessage otherRequest = CreateRequest())
                {
                    controller.Request = otherRequest;

                    // Act
                    HttpRequestMessage request = result.Request;

                    // Assert
                    Assert.Same(expectedRequest, request);
                }
            }
        }

        [Fact]
        public void Request_ForApiController_Throws_WhenControllerRequestIsNull()
        {
            // Arrange
            ApiController controller = CreateController();
            Assert.Null(controller.Request);
            OkResult result = CreateProductUnderTest(controller);

            // Act & Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                { HttpRequestMessage ignore = result.Request; }, "ApiController.Request must not be null.");
        }

        [Fact]
        public void ApiControllerOk_CreatesCorrectResult()
        {
            // Arrange
            ApiController controller = CreateController();

            // Act
            OkResult result = controller.Ok();

            // Assert
            Assert.NotNull(result);

            using (HttpRequestMessage expectedRequest = CreateRequest())
            {
                controller.Request = expectedRequest;
                Assert.Same(expectedRequest, result.Request);
            }
        }

        private static ApiController CreateController()
        {
            return new FakeController();
        }

        private static OkResult CreateProductUnderTest(HttpRequestMessage request)
        {
            return new OkResult(request);
        }

        private static OkResult CreateProductUnderTest(ApiController controller)
        {
            return new OkResult(controller);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private class FakeController : ApiController
        {
        }
    }
}
