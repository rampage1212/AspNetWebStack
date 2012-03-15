﻿using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;
using Xunit;
using Xunit.Extensions;

namespace System.Web.Http.ApiExplorer
{
    public class RoutesTest
    {
        [Fact]
        public void VerifyDescription_OnEmptyRoute()
        {
            HttpConfiguration config = new HttpConfiguration();
            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();

            Assert.NotNull(explorer);
            Assert.Equal(0, explorer.ApiDescriptions.Count);
        }

        public static IEnumerable<object[]> VerifyDescription_OnDefaultRoute_PropertyData
        {
            get
            {
                object controllerType = typeof(ItemController);
                object expectedApiDescriptions = new List<object>
                {
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Item?name={name}&series={series}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "Item", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Put, RelativePath = "Item", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Item/{id}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 1}
                };
                yield return new[] { controllerType, expectedApiDescriptions };

                controllerType = typeof(OverloadsController);
                expectedApiDescriptions = new List<object>
                {
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/{id}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 0},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads?name={name}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/{id}?name={name}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads?name={name}&age={age}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads?name={name}&age={age}&ssn={ssn}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 3},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/{id}?name={name}&ssn={ssn}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 3},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads?name={name}&ssn={ssn}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "Overloads", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "Overloads?name={name}&age={age}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Overloads/{id}?name={name}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Overloads/{id}?name={name}&age={age}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 3}
                };
                yield return new[] { controllerType, expectedApiDescriptions };
            }
        }

        [Theory]
        [PropertyData("VerifyDescription_OnDefaultRoute_PropertyData")]
        public void VerifyDescription_OnDefaultRoute(Type controllerType, List<object> expectedResults)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { id = RouteParameter.Optional });

            DefaultHttpControllerFactory controllerFactory = ApiExplorerHelper.GetStrictControllerFactory(config, controllerType);
            config.ServiceResolver.SetService(typeof(IHttpControllerFactory), controllerFactory);

            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();
            ApiExplorerHelper.VerifyApiDescriptions(explorer.ApiDescriptions, expectedResults);
        }

        public static IEnumerable<object[]> VerifyDescription_OnRouteWithControllerOnDefaults_PropertyData
        {
            get
            {
                object controllerType = typeof(ItemController);
                object expectedApiDescriptions = new List<object>
                {
                    new { HttpMethod = HttpMethod.Get, RelativePath = "myitem?name={name}&series={series}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "myitem", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Put, RelativePath = "myitem", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "myitem/{id}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 1}
                };
                yield return new[] { controllerType, expectedApiDescriptions };
            }
        }

        [Theory]
        [PropertyData("VerifyDescription_OnRouteWithControllerOnDefaults_PropertyData")]
        public void VerifyDescription_OnRouteWithControllerOnDefaults(Type controllerType, List<object> expectedResults)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "myitem/{id}", new { controller = "Item", id = RouteParameter.Optional });

            DefaultHttpControllerFactory controllerFactory = ApiExplorerHelper.GetStrictControllerFactory(config, controllerType);
            config.ServiceResolver.SetService(typeof(IHttpControllerFactory), controllerFactory);

            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();
            ApiExplorerHelper.VerifyApiDescriptions(explorer.ApiDescriptions, expectedResults);
        }

        public static IEnumerable<object[]> VerifyDescription_OnRouteWithActionVariable_PropertyData
        {
            get
            {
                object controllerType = typeof(ItemController);
                object expectedApiDescriptions = new List<object>
                {
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Item/GetItem?name={name}&series={series}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "Item/PostItem", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Put, RelativePath = "Item/PostItem", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Item/RemoveItem/{id}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 1}
                };
                yield return new[] { controllerType, expectedApiDescriptions };

                controllerType = typeof(OverloadsController);
                expectedApiDescriptions = new List<object>
                {
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/Get/{id}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/Get", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 0},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/Get?name={name}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/GetPersonByNameAndId/{id}?name={name}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/GetPersonByNameAndAge?name={name}&age={age}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/GetPersonByNameAgeAndSsn?name={name}&age={age}&ssn={ssn}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 3},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/GetPersonByNameIdAndSsn/{id}?name={name}&ssn={ssn}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 3},
                    new { HttpMethod = HttpMethod.Get, RelativePath = "Overloads/GetPersonByNameAndSsn?name={name}&ssn={ssn}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "Overloads/Post", HasRequestFormatters = true, HasResponseFormatters = true, NumberOfParameters = 1},
                    new { HttpMethod = HttpMethod.Post, RelativePath = "Overloads/ActionDefaultedToPost?name={name}&age={age}", HasRequestFormatters = false, HasResponseFormatters = true, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Overloads/Delete/{id}?name={name}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 2},
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Overloads/Delete/{id}?name={name}&age={age}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 3}
                };
                yield return new[] { controllerType, expectedApiDescriptions };
            }
        }

        [Theory]
        [PropertyData("VerifyDescription_OnRouteWithActionVariable_PropertyData")]
        public void VerifyDescription_OnRouteWithActionVariable(Type controllerType, List<object> expectedResults)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{action}/{id}", new { id = RouteParameter.Optional });

            DefaultHttpControllerFactory controllerFactory = ApiExplorerHelper.GetStrictControllerFactory(config, controllerType);
            config.ServiceResolver.SetService(typeof(IHttpControllerFactory), controllerFactory);

            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();
            ApiExplorerHelper.VerifyApiDescriptions(explorer.ApiDescriptions, expectedResults);
        }

        public static IEnumerable<object[]> VerifyDescription_On_RouteWithActionOnDefaults_PropertyData
        {
            get
            {
                object controllerType = typeof(ItemController);
                object expectedApiDescriptions = new List<object>
                {
                    new { HttpMethod = HttpMethod.Delete, RelativePath = "Item/{id}", HasRequestFormatters = false, HasResponseFormatters = false, NumberOfParameters = 1}
                };
                yield return new[] { controllerType, expectedApiDescriptions };
            }
        }

        [Theory]
        [PropertyData("VerifyDescription_On_RouteWithActionOnDefaults_PropertyData")]
        public void VerifyDescription_On_RouteWithActionOnDefaults(Type controllerType, List<object> expectedResults)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { action = "RemoveItem", id = RouteParameter.Optional });

            DefaultHttpControllerFactory controllerFactory = ApiExplorerHelper.GetStrictControllerFactory(config, controllerType);
            config.ServiceResolver.SetService(typeof(IHttpControllerFactory), controllerFactory);

            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();
            ApiExplorerHelper.VerifyApiDescriptions(explorer.ApiDescriptions, expectedResults);
        }

        [Fact]
        public void InvalidActionNameOnRoute_DoesNotThrow()
        {
            Type controllerType = typeof(OverloadsController);
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { action = "ActionThatDoesNotExist", id = RouteParameter.Optional });

            DefaultHttpControllerFactory controllerFactory = ApiExplorerHelper.GetStrictControllerFactory(config, controllerType);
            config.ServiceResolver.SetService(typeof(IHttpControllerFactory), controllerFactory);

            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();
            Assert.Empty(explorer.ApiDescriptions);
        }

        [Fact]
        public void InvalidControllerNameOnRoute_DoesNotThrow()
        {
            Type controllerType = typeof(OverloadsController);
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "mycontroller/{id}", new { controller = "ControllerThatDoesNotExist", id = RouteParameter.Optional });

            DefaultHttpControllerFactory controllerFactory = ApiExplorerHelper.GetStrictControllerFactory(config, controllerType);
            config.ServiceResolver.SetService(typeof(IHttpControllerFactory), controllerFactory);

            IApiExplorer explorer = config.ServiceResolver.GetApiExplorer();
            Assert.Empty(explorer.ApiDescriptions);
        }
    }
}
