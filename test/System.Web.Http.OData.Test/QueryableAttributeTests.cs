﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Hosting;
using System.Web.Http.OData.Query.Controllers;
using System.Web.Http.OData.TestCommon.Models;
using System.Web.Http.Routing;
using Microsoft.TestCommon;
using Xunit;
using Xunit.Extensions;

namespace System.Web.Http.OData
{
    public class QueryableAttributeTests
    {
        public static List<Customer> CustomerList = new List<Customer>()
        {
            new Customer(){ Name = "B" },
            new Customer(){ Name = "C" },
            new Customer(){ Name = "A" },
        };

        public static TheoryDataSet<string, object, bool> DifferentReturnTypeWorksTestData
        {
            get
            {
                return new TheoryDataSet<string, object, bool>
                {
                    { "GetObject", new List<Customer>(CustomerList), false },
                    { "GetObject", new Collection<Customer>(CustomerList), false },
                    { "GetObject", new CustomerCollection(), false },
                    { "GetObject", new Customer(), true },
                    { "GetObject", new TwoGenericsCollection(), true }
                };
            }
        }

        [Theory]
        [PropertyData("DifferentReturnTypeWorksTestData")]
        public void DifferentReturnTypeWorks(string methodName, object responseObject, bool isNoOp)
        {
            // Arrange
            QueryableAttribute attribute = new QueryableAttribute();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Customer/?$orderby=Name");
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            HttpControllerContext controllerContext = new HttpControllerContext(config, new HttpRouteData(new HttpRoute()), request);
            HttpControllerDescriptor controllerDescriptor = new HttpControllerDescriptor(new HttpConfiguration(), "CustomerHighLevel", typeof(CustomerHighLevelController));
            HttpActionDescriptor actionDescriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(CustomerHighLevelController).GetMethod(methodName));
            HttpActionContext actionContext = new HttpActionContext(controllerContext, actionDescriptor);
            HttpActionExecutedContext context = new HttpActionExecutedContext(actionContext, null);
            context.Response = new HttpResponseMessage(HttpStatusCode.OK);
            context.Response.Content = new ObjectContent(typeof(object), responseObject, new JsonMediaTypeFormatter());

            // Act and Assert
            attribute.OnActionExecuted(context);

            Assert.Equal(HttpStatusCode.OK, context.Response.StatusCode);
            Assert.True(context.Response.Content is ObjectContent);
            Assert.Equal(isNoOp, ((ObjectContent)context.Response.Content).Value == responseObject);
        }

        [Fact]
        public void UnknownQueryNotStartingWithDollarSignWorks()
        {
            // Arrange
            QueryableAttribute attribute = new QueryableAttribute();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Customer/?select");
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            HttpControllerContext controllerContext = new HttpControllerContext(config, new HttpRouteData(new HttpRoute()), request);
            HttpControllerDescriptor controllerDescriptor = new HttpControllerDescriptor(new HttpConfiguration(), "CustomerHighLevel", typeof(CustomerHighLevelController));
            HttpActionDescriptor actionDescriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(CustomerHighLevelController).GetMethod("Get"));
            HttpActionContext actionContext = new HttpActionContext(controllerContext, actionDescriptor);
            HttpActionExecutedContext context = new HttpActionExecutedContext(actionContext, null);
            context.Response = new HttpResponseMessage(HttpStatusCode.OK);
            context.Response.Content = new ObjectContent(typeof(object), new List<Customer>(), new JsonMediaTypeFormatter());

            // Act and Assert
            attribute.OnActionExecuted(context);

            Assert.Equal(HttpStatusCode.OK, context.Response.StatusCode);
        }

        [Fact]
        public void UnknownQueryStartingWithDollarSignThrows()
        {
            // Arrange
            QueryableAttribute attribute = new QueryableAttribute();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Customer/?$select");
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            HttpControllerContext controllerContext = new HttpControllerContext(config, new HttpRouteData(new HttpRoute()), request);
            HttpControllerDescriptor controllerDescriptor = new HttpControllerDescriptor(new HttpConfiguration(), "CustomerHighLevel", typeof(CustomerHighLevelController));
            HttpActionDescriptor actionDescriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(CustomerHighLevelController).GetMethod("Get"));
            HttpActionContext actionContext = new HttpActionContext(controllerContext, actionDescriptor);
            HttpActionExecutedContext context = new HttpActionExecutedContext(actionContext, null);
            context.Response = new HttpResponseMessage(HttpStatusCode.OK);
            context.Response.Content = new ObjectContent(typeof(object), new List<Customer>(), new JsonMediaTypeFormatter());

            // Act and Assert
            HttpResponseException errorResponse = Assert.Throws<HttpResponseException>(() =>
                attribute.OnActionExecuted(context));

            Assert.Equal(HttpStatusCode.BadRequest, errorResponse.Response.StatusCode);
        }

        [Theory]
        [InlineData("$top=1")]
        [InlineData("$skip=1")]
        public void Primitives_Can_Be_Used_For_Top_And_Skip(string filter)
        {
            // Arrange
            QueryableAttribute attribute = new QueryableAttribute();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Primitive/?" + filter);
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            HttpControllerContext controllerContext = new HttpControllerContext(config, new HttpRouteData(new HttpRoute()), request);
            HttpControllerDescriptor controllerDescriptor = new HttpControllerDescriptor(new HttpConfiguration(), "Primitive", typeof(PrimitiveController));
            HttpActionDescriptor actionDescriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(PrimitiveController).GetMethod("GetIEnumerableOfInt"));
            HttpActionContext actionContext = new HttpActionContext(controllerContext, actionDescriptor);
            HttpActionExecutedContext context = new HttpActionExecutedContext(actionContext, null);
            context.Response = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent expectedResponse = new ObjectContent(typeof(object), new List<int>(), new JsonMediaTypeFormatter());
            context.Response.Content = expectedResponse;

            // Act and Assert
            attribute.OnActionExecuted(context);
            HttpResponseMessage response = context.Response;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedResponse, response.Content);
        }

        [Theory]
        [InlineData("$filter=2 eq 1")]
        [InlineData("$orderby=1")]
        public void Primitives_Cannot_Be_Used_By_Filter_And_OrderBy(string filter)
        {
            // Arrange
            QueryableAttribute attribute = new QueryableAttribute();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Primitive/?" + filter);
            HttpConfiguration config = new HttpConfiguration();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            HttpControllerContext controllerContext = new HttpControllerContext(config, new HttpRouteData(new HttpRoute()), request);
            HttpControllerDescriptor controllerDescriptor = new HttpControllerDescriptor(new HttpConfiguration(), "Primitive", typeof(PrimitiveController));
            HttpActionDescriptor actionDescriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(PrimitiveController).GetMethod("GetIEnumerableOfInt"));
            HttpActionContext actionContext = new HttpActionContext(controllerContext, actionDescriptor);
            HttpActionExecutedContext context = new HttpActionExecutedContext(actionContext, null);
            context.Response = new HttpResponseMessage(HttpStatusCode.OK);
            context.Response.Content = new ObjectContent(typeof(object), new List<int>(), new JsonMediaTypeFormatter());

            // Act and Assert
            attribute.OnActionExecuted(context);
            HttpResponseMessage response = context.Response;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsAssignableFrom(typeof(ObjectContent), response.Content);
            Assert.IsType(typeof(HttpError), ((ObjectContent)response.Content).Value);
            Assert.Equal("Only $skip and $top OData query options are supported for this type.",
                         ((HttpError)((ObjectContent)response.Content).Value).Message);
        }
    }
}
