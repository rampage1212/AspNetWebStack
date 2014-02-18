﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.Description
{
    public class ApiExplorerTest
    {
        [Fact]
        public void Descriptions_RecognizesDirectRoutes()
        {
            var config = new HttpConfiguration();
            var routeTemplate = "api/values";
            var controllerDescriptor = new HttpControllerDescriptor(config, "ApiExplorerValues", typeof(ApiExplorerValuesController));
            var action = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(ApiExplorerValuesController).GetMethod("Get"));
            var actions = new ReflectedHttpActionDescriptor[] { action };
            config.Routes.Add("Route", CreateDirectRoute(routeTemplate, actions));

            var descriptions = new ApiExplorer(config).ApiDescriptions;

            ApiDescription description = Assert.Single(descriptions);
            Assert.Equal(HttpMethod.Get, description.HttpMethod);
            Assert.Equal(routeTemplate, description.RelativePath);
            Assert.Equal(action, description.ActionDescriptor);
        }

        [Fact]
        public void Descriptions_RecognizesIgnoreApiForDirectRoutes_Action()
        {
            var config = new HttpConfiguration();
            var routeTemplate = "api/values";
            var controllerDescriptor = new HttpControllerDescriptor(config, "ApiExplorerValues", typeof(ApiExplorerValuesController));
            var actions = new ReflectedHttpActionDescriptor[] 
            {
                new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(ApiExplorerValuesController).GetMethod("Get")),
                new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(ApiExplorerValuesController).GetMethod("Post")),
            };
            config.Routes.Add("Route", CreateDirectRoute(routeTemplate, actions));

            var descriptions = new ApiExplorer(config).ApiDescriptions;

            ApiDescription description = Assert.Single(descriptions);
            Assert.Equal(HttpMethod.Get, description.HttpMethod);
            Assert.Equal(routeTemplate, description.RelativePath);
        }

        [Fact]
        public void Descriptions_RecognizesIgnoreApiForDirectRoutes_Controller()
        {
            var config = new HttpConfiguration();
            var routeTemplate = "api/values";
            var controllerDescriptor = new HttpControllerDescriptor(config, "IgnoreApiValues", typeof(IgnoreApiValuesController));
            var actions = new ReflectedHttpActionDescriptor[] 
            {
                new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(IgnoreApiValuesController).GetMethod("Get")),
                new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(IgnoreApiValuesController).GetMethod("Post")),
            };
            config.Routes.Add("Route", CreateDirectRoute(routeTemplate, actions));

            var descriptions = new ApiExplorer(config).ApiDescriptions;

            Assert.Empty(descriptions);
        }

        public class ApiExplorerValuesController : ApiController
        {
            public void Get() { }

            [ApiExplorerSettings(IgnoreApi = true)]
            public void Post() { }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public class IgnoreApiValuesController : ApiController
        {
            public void Get() { }
            public void Post() { }
        }

        [Fact]
        public void Descriptions_RecognizesCompositeRoutes()
        {
            var config = new HttpConfiguration();
            var routeTemplate = "api/values";
            var controllerDescriptor = new HttpControllerDescriptor(config, "AttributeApiExplorerValues", typeof(AttributeApiExplorerValuesController));
            var action = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(AttributeApiExplorerValuesController).GetMethod("Action"));
            var actions = new ReflectedHttpActionDescriptor[] { action };

            var routeCollection = new List<IHttpRoute>();
            routeCollection.Add(CreateDirectRoute(routeTemplate, actions));

            RouteCollectionRoute route = new RouteCollectionRoute();
            route.EnsureInitialized(() => routeCollection);

            config.Routes.Add("Route", route);

            var descriptions = new ApiExplorer(config).ApiDescriptions;

            ApiDescription description = Assert.Single(descriptions);
            Assert.Equal(HttpMethod.Get, description.HttpMethod);
            Assert.Equal(routeTemplate, description.RelativePath);
            Assert.Equal(action, description.ActionDescriptor);
        }

        [Fact]
        public void TryExpandUriParameters_EnsureNoKeyConflicts()
        {
            // This test ensures that keys adding to parameterValuesForRoute are case-insensitive
            // and would not cause any exeception if it already has the key. So set up two
            // ApiParameterDescription instances, one with "id" and another with "Id". Act the
            // method and assert that no exception occurs and the output is correct.
            // Arrange
            string expectedExpandedRouteTemplate = "?id={id}";
            string expandedRouteTemplate;
            Mock<HttpParameterDescriptor> parameterDescriptorMock = new Mock<HttpParameterDescriptor>();
            parameterDescriptorMock.SetupGet(p => p.ParameterType).Returns(typeof(ClassWithId));
            List<ApiParameterDescription> descriptions = new List<ApiParameterDescription>()
            {
                new ApiParameterDescription()
                {
                    Source = ApiParameterSource.FromUri,
                    Name = "id"
                },
                new ApiParameterDescription()
                {
                    Source = ApiParameterSource.FromUri,
                    ParameterDescriptor = parameterDescriptorMock.Object
                },
            };

            // Act
            bool isExpanded = ApiExplorer.TryExpandUriParameters(new HttpRoute(),
                                                     new HttpParsedRoute(new List<PathSegment>()),
                                                     descriptions,
                                                     out expandedRouteTemplate);

            // Assert
            Assert.True(isExpanded);
            Assert.Equal(expectedExpandedRouteTemplate, expandedRouteTemplate);
        }

        [Fact]
        public void Descriptions_RecognizesMixedCaseParameters()
        {
            // Ensure that two "Id"s, one from "api/values/{id}" and another "Id" from ClassWithId,
            // would not cause any exception and only one of them is added.
            var config = new HttpConfiguration();
            var routeTemplate = "api/values/{id}";
            var controllerDescriptor = new HttpControllerDescriptor(config, "ApiExplorerValues", typeof(DuplicatedIdController));
            var action = new ReflectedHttpActionDescriptor(controllerDescriptor, typeof(DuplicatedIdController).GetMethod("Get"));
            var actions = new ReflectedHttpActionDescriptor[] { action };
            config.Routes.Add("Route", CreateDirectRoute(routeTemplate, actions));

            var descriptions = new ApiExplorer(config).ApiDescriptions;

            ApiDescription description = Assert.Single(descriptions);
            Assert.Equal(HttpMethod.Get, description.HttpMethod);
            Assert.Equal(routeTemplate, description.RelativePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(action, description.ActionDescriptor);
        }

        private class ClassWithId
        {
            public int Id { get; set; }
        }

        private class DuplicatedIdController : ApiController
        {
            public void Get([FromUri] ClassWithId objectWithId) { }
        }

        public class AttributeApiExplorerValuesController : ApiController
        {
            [Route("")]
            [HttpGet]
            public void Action() { }
        }

        private static IHttpRoute CreateDirectRoute(string template,
            IReadOnlyCollection<ReflectedHttpActionDescriptor> actions)
        {
            DirectRouteBuilder builder = new DirectRouteBuilder(actions, targetIsAction: true);
            builder.Template = template;
            return builder.Build().Route;
        }
    }
}
