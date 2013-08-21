﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Hosting;
using System.Web.Http.ModelBinding;
using System.Web.Http.ModelBinding.Binders;
using System.Web.Http.Routing;
using Microsoft.TestCommon;
using Moq;

namespace System.Net.Http
{
    public class HttpConfigurationExtensionsTest
    {
        [Fact]
        public void BindParameter_GuardClauses()
        {
            HttpConfiguration config = new HttpConfiguration();
            Type type = typeof(TestParameter);
            IModelBinder binder = new Mock<IModelBinder>().Object;

            Assert.ThrowsArgumentNull(() => HttpConfigurationExtensions.BindParameter(null, type, binder), "configuration");
            Assert.ThrowsArgumentNull(() => HttpConfigurationExtensions.BindParameter(config, null, binder), "type");
            Assert.ThrowsArgumentNull(() => HttpConfigurationExtensions.BindParameter(config, type, null), "binder");
        }

        [Fact]
        public void BindParameter_InsertsModelBinderProviderInPositionZero()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            Type type = typeof(TestParameter);
            IModelBinder binder = new Mock<IModelBinder>().Object;

            // Act
            config.BindParameter(type, binder);

            // Assert
            SimpleModelBinderProvider provider = config.Services.GetServices(typeof(ModelBinderProvider)).OfType<SimpleModelBinderProvider>().First();
            Assert.Equal(type, provider.ModelType);
        }

        [Fact]
        public void MapHttpAttributeRoutes_DoesNotAddRoutesWithoutAttribute()
        {
            // Arrange
            var config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>() { new RoutePrefixAttribute("prefix") };
            var routeProviders = new Collection<IHttpRouteInfoProvider>() { };
            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act
            config.MapHttpAttributeRoutes();

            // Assert
            var routes = config.GetAttributeRoutes();
            Assert.Empty(routes);
        }

        [Fact]
        public void MapHttpAttributeRoutes_DoesNotRegisterRoute_ForActionsWithPrefixButNoRouteTemplate()
        {
            // Arrange
            var config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>() { new RoutePrefixAttribute("prefix") };
            var routeProviders = new Collection<IHttpRouteInfoProvider>() { };
            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act
            config.MapHttpAttributeRoutes();

            // Assert
            var routes = config.GetAttributeRoutes();
            Assert.Empty(routes);
        }

        [Theory]
        [InlineData(null, "", "")]
        [InlineData(null, "   ", "   ")]
        [InlineData(null, "controller/{id}", "controller/{id}")]
        [InlineData("", "", "")]
        [InlineData("", "   ", "   ")]
        [InlineData("", "controller/{id}", "controller/{id}")]
        [InlineData("   ", "", "   ")]
        [InlineData("   ", "   ", "   /   ")]
        [InlineData("   ", "controller/{id}", "   /controller/{id}")]
        [InlineData("prefix/{prefixId}", "", "prefix/{prefixId}")]
        [InlineData("prefix/{prefixId}", "   ", "prefix/{prefixId}/   ")]
        [InlineData("prefix/{prefixId}", "controller/{id}", "prefix/{prefixId}/controller/{id}")]
        [InlineData(null, "~/controller/{id}", "controller/{id}")]
        [InlineData("prefix/{prefixId}", "~/", "")]
        [InlineData("prefix/{prefixId}", "~/controller/{id}", "controller/{id}")]
        public void MapHttpAttributeRoutes_AddsRouteFromAttribute(string prefix, string template, string expectedTemplate)
        {
            // Arrange
            var config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>();
            if (prefix != null)
            {
                routePrefixes.Add(new RoutePrefixAttribute(prefix));
            }

            var routeProviders = new Collection<IHttpRouteInfoProvider>() { new RouteAttribute(template) };

            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act
            config.MapHttpAttributeRoutes();

            // Assert
            HttpSubRouteCollection routes = config.GetAttributeRoutes();
            IHttpRoute route = Assert.Single(routes);
            Assert.Equal(expectedTemplate, route.RouteTemplate);
        }

        [Fact]
        public void MapHttpAttributeRoutes_ThrowsForRoutePrefixThatEndsWithSeparator()
        {
            // Arrange
            var config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>() { new RoutePrefixAttribute("prefix/") };
            var routeProviders = new Collection<IHttpRouteInfoProvider>() { new RouteAttribute("") };
            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => { config.MapHttpAttributeRoutes(); config.EnsureInitialized(); },
                "The route prefix 'prefix/' on the controller named 'Controller' cannot end with a '/' character.");
        }

        [Fact]
        public void MapHttpAttributeRoutes_ThrowsForRouteTemplateThatStartsWithSeparator()
        {
            // Arrange
            var config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>() { };
            var routeProviders = new Collection<IHttpRouteInfoProvider>() { new RouteAttribute("/get") };
            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => { config.MapHttpAttributeRoutes(); config.EnsureInitialized(); },
                "The route template '/get' on the action named 'Action' cannot start with a '/' character.");
        }

        [Fact]
        public void MapHttpAttributeRoutes_AddsMultipleRoutesFromAttributes()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>();
            var routeProviders = new Collection<IHttpRouteInfoProvider>() { new RouteAttribute("controller/get1"), new RouteAttribute("controller/get2") };
            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act
            config.MapHttpAttributeRoutes();

            // Assert
            HttpSubRouteCollection routes = config.GetAttributeRoutes();
            Assert.Equal(2, routes.Count);
            Assert.Single(routes.Where(route => route.RouteTemplate == "controller/get1"));
            Assert.Single(routes.Where(route => route.RouteTemplate == "controller/get2"));
        }

        [Fact]
        public void MapHttpAttributeRoutes_IsDeferred()
        {
            bool called = false;
            HttpConfiguration config = new HttpConfiguration();
            
            config.Initializer = _ => called = true;
            config.Services.Clear(typeof(IHttpControllerSelector));
            config.Services.Clear(typeof(IHttpActionSelector));
            config.Services.Clear(typeof(IActionValueBinder));

            // Call Map, ensure that it's not touching any services yet since all work is deferred. 
            // This is important since these services aren't ready to be used until after config is finalized. 
            // Else we may end up caching objects prematurely.
            config.MapHttpAttributeRoutes();

            Assert.False(called);
        }

        [Fact]
        public void MapHttpAttributeRoutes_AddsGenerationRoutes()
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            var routePrefixes = new Collection<RoutePrefixAttribute>() { };
            var routeProviders = new Collection<IHttpRouteInfoProvider>()
                {
                    new RouteAttribute("get1") { Name = "one" },
                    new RouteAttribute("get2") { Name = "two" },
                    new RouteAttribute("get3") { Name = "three" }
                };
            SetUpConfiguration(config, routePrefixes, routeProviders);

            // Act
            config.MapHttpAttributeRoutes();
            config.Initializer(config);

            // Assert
            HttpRouteCollection routes = config.Routes;
            Assert.Equal(4, routes.Count); // 1 attr route, plus 3 generation routes
            Assert.IsType<RouteCollectionRoute>(routes.ElementAt(0));
            for (int i = 1; i < 4; i++)
            {
                Assert.IsType<GenerateRoute>(routes.ElementAt(i));
            }

            Assert.IsType<GenerateRoute>(routes["one"]);
            Assert.IsType<GenerateRoute>(routes["two"]);
            Assert.IsType<GenerateRoute>(routes["three"]);
        }

        [Fact]
        public void MapHttpAttributeRoutes_RespectsPerControllerActionSelectors()
        {
            // Arrange
            var globalConfiguration = new HttpConfiguration();
            var _controllerDescriptor = new HttpControllerDescriptor(globalConfiguration, "PerControllerActionSelector", typeof(PerControllerActionSelectorController));

            // Set up the global action selector and controller selector
            var controllerSelector = CreateControllerSelector(new HttpControllerDescriptor[] { _controllerDescriptor });
            globalConfiguration.Services.Replace(typeof(IHttpControllerSelector), controllerSelector);

            var globalAction = CreateActionDescriptor("Global", new Collection<IHttpRouteInfoProvider>() { new RouteAttribute("Global") });
            var globalActionSelector = CreateActionSelector(
                new Dictionary<HttpControllerDescriptor, IEnumerable<HttpActionDescriptor>>()
                    {
                        { _controllerDescriptor, new HttpActionDescriptor[] { globalAction } }
                    });
            globalConfiguration.Services.Replace(typeof(IHttpActionSelector), globalActionSelector);

            // Configure the per controller action selector to return the action with route "PerController"
            var perControllerAction = CreateActionDescriptor(
                "PerController",
                new Collection<IHttpRouteInfoProvider>() { new RouteAttribute("PerController") });
            ActionSelectorConfigurationAttribute.PerControllerActionSelectorMock
                .Setup(a => a.GetActionMapping(_controllerDescriptor))
                .Returns(new HttpActionDescriptor[] { perControllerAction }.ToLookup(ad => ad.ActionName));

            // Act
            globalConfiguration.MapHttpAttributeRoutes();

            // Assert
            HttpSubRouteCollection routes = globalConfiguration.GetAttributeRoutes();
            Assert.Equal("PerController", Assert.Single(routes).RouteTemplate);
        }

        [Fact]
        public void MapHttpAttributeRoutes_AddsOnlyOneActionToRoute_ForMultipleAttributesOnASingleAction()
        {
            // Arrange
            var config = new HttpConfiguration();
            string routeTemplate = "api/values";
            HttpControllerDescriptor controllerDescriptor = CreateControllerDescriptor(config, "Controller", new Collection<RoutePrefixAttribute>());
            HttpActionDescriptor actionDescriptor = CreateActionDescriptor(
                "Action",
                new Collection<IHttpRouteInfoProvider>() { new RouteAttribute(routeTemplate), new RouteAttribute(routeTemplate) });

            var controllerSelector = CreateControllerSelector(new[] { controllerDescriptor });
            config.Services.Replace(typeof(IHttpControllerSelector), controllerSelector);
            var actionSelector = CreateActionSelector(
                new Dictionary<HttpControllerDescriptor, IEnumerable<HttpActionDescriptor>>()
                {
                    { controllerDescriptor, new HttpActionDescriptor[] { actionDescriptor } }
                });
            config.Services.Replace(typeof(IHttpActionSelector), actionSelector);

            // Act
            config.MapHttpAttributeRoutes();

            // Assert
            HttpSubRouteCollection routes = config.GetAttributeRoutes();
            IHttpRoute route = Assert.Single(routes);
            Assert.Equal(routeTemplate, route.RouteTemplate);
            Assert.Equal(actionDescriptor, Assert.Single(route.DataTokens["actions"] as ReflectedHttpActionDescriptor[]));
        }

        [Fact]
        public void SuppressHostPrincipal_InsertsSuppressHostPrincipalMessageHandler()
        {
            // Arrange
            IHostPrincipalService expectedPrincipalService = new Mock<IHostPrincipalService>(
                MockBehavior.Strict).Object;
            DelegatingHandler existingHandler = new Mock<DelegatingHandler>(MockBehavior.Strict).Object;

            using (HttpConfiguration configuration = new HttpConfiguration())
            {
                configuration.Services.Replace(typeof(IHostPrincipalService), expectedPrincipalService);
                configuration.MessageHandlers.Add(existingHandler);

                // Act
                configuration.SuppressHostPrincipal();

                // Assert
                Assert.Equal(2, configuration.MessageHandlers.Count);
                DelegatingHandler firstHandler = configuration.MessageHandlers[0];
                Assert.IsType<SuppressHostPrincipalMessageHandler>(firstHandler);
                SuppressHostPrincipalMessageHandler suppressPrincipalHandler =
                    (SuppressHostPrincipalMessageHandler)firstHandler;
                IHostPrincipalService principalService = suppressPrincipalHandler.HostPrincipalService;
                Assert.Same(expectedPrincipalService, principalService);
            }
        }

        [Fact]
        public void SuppressHostPrincipal_Throws_WhenConfigurationIsNull()
        {
            // Act & Assert
            Assert.ThrowsArgumentNull(() => { HttpConfigurationExtensions.SuppressHostPrincipal(null); },
                "configuration");
        }

        private static void SetUpConfiguration(HttpConfiguration config, Collection<RoutePrefixAttribute> routePrefixes, Collection<IHttpRouteInfoProvider> routeProviders)
        {
            HttpControllerDescriptor controllerDescriptor = CreateControllerDescriptor(config, "Controller", routePrefixes);
            HttpActionDescriptor actionDescriptor = CreateActionDescriptor("Action", routeProviders);

            var controllerSelector = CreateControllerSelector(new[] { controllerDescriptor });
            config.Services.Replace(typeof(IHttpControllerSelector), controllerSelector);
            var actionSelector = CreateActionSelector(
                new Dictionary<HttpControllerDescriptor, IEnumerable<HttpActionDescriptor>>()
                {
                    { controllerDescriptor, new HttpActionDescriptor[] { actionDescriptor } }
                });
            config.Services.Replace(typeof(IHttpActionSelector), actionSelector);
        }

        private static HttpControllerDescriptor CreateControllerDescriptor(HttpConfiguration configuration, string controllerName,
            Collection<RoutePrefixAttribute> routePrefixes)
        {
            Mock<HttpControllerDescriptor> controllerDescriptor = new Mock<HttpControllerDescriptor>();
            controllerDescriptor.Object.Configuration = configuration;
            controllerDescriptor.Object.ControllerName = controllerName;
            controllerDescriptor.Setup(cd => cd.GetCustomAttributes<RoutePrefixAttribute>(false)).Returns(routePrefixes);
            return controllerDescriptor.Object;
        }

        private static HttpActionDescriptor CreateActionDescriptor(string actionName, Collection<IHttpRouteInfoProvider> routeProviders)
        {
            Mock<ReflectedHttpActionDescriptor> actionDescriptor = new Mock<ReflectedHttpActionDescriptor>();
            actionDescriptor.Setup(ad => ad.ActionName).Returns(actionName);
            actionDescriptor.Setup(ad => ad.GetCustomAttributes<IHttpRouteInfoProvider>(false)).Returns(routeProviders);
            actionDescriptor.Setup(ad => ad.SupportedHttpMethods).Returns(new Collection<HttpMethod>());
            actionDescriptor.CallBase = true;
            return actionDescriptor.Object;
        }

        private static IHttpControllerSelector CreateControllerSelector(IEnumerable<HttpControllerDescriptor> controllerDescriptors)
        {
            Mock<IHttpControllerSelector> controllerSelector = new Mock<IHttpControllerSelector>();
            controllerSelector.Setup(c => c.GetControllerMapping()).Returns(controllerDescriptors.ToDictionary(cd => cd.ControllerName));
            return controllerSelector.Object;
        }

        private static IHttpActionSelector CreateActionSelector(Dictionary<HttpControllerDescriptor, IEnumerable<HttpActionDescriptor>> actionMap)
        {
            Mock<IHttpActionSelector> actionSelector = new Mock<IHttpActionSelector>();
            foreach (var mapEntry in actionMap)
            {
                actionSelector.Setup(a => a.GetActionMapping(mapEntry.Key)).Returns(mapEntry.Value.ToLookup(ad => ad.ActionName));
            }
            return actionSelector.Object;
        }

        public class TestParameter
        {
        }
        
        [ActionSelectorConfiguration]
        public class PerControllerActionSelectorController : ApiController { }

        public class ActionSelectorConfigurationAttribute : Attribute, IControllerConfiguration
        {
            public static Mock<IHttpActionSelector> PerControllerActionSelectorMock = new Mock<IHttpActionSelector>();

            public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
            {
                controllerSettings.Services.Replace(typeof(IHttpActionSelector), PerControllerActionSelectorMock.Object);
            }
        }
    }
}
