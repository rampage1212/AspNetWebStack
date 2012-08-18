﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Formatter.Deserialization;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Query
{
    public class QueryCompositionTests
    {
        private static IEdmModel _queryCompositionCustomerModel;

        public static TheoryDataSet<string, bool> ControllerNames
        {
            get
            {
                return new TheoryDataSet<string, bool> 
                {
                    { "QueryCompositionCustomer", true },
                    { "QueryCompositionCustomerLowLevel", true },
                    { "QueryCompositionCustomerQueryable", true },
                    { "QueryCompositionCustomerGlobal", true },
                    { "QueryCompositionCustomer", false },
                    { "QueryCompositionCustomerLowLevel", false },
                    { "QueryCompositionCustomerQueryable", false },
                    { "QueryCompositionCustomerGlobal", false }
                };
            }
        }

        public static TheoryDataSet<string, bool, int[]> Filters
        {
            get
            {
                return new TheoryDataSet<string, bool, int[]>
                {
                    { "Name eq 'Highest'", true, new int[] { 33 } },
                    { "Address/City eq 'redmond'", true, new int[] { 11 } },
                    { "Address/City eq null", true, new int[] { 22 , 3 } },
                    { "RelationshipManager/Name eq null", true, new int[] { 11, 22, 3 } },
                    { "RelationshipManager/Name ne null", true, new int[] { 33 } }
                };
            }
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionWithoutQueryReturnsOriginalList(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // order by Id
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(QueryCompositionCustomerController.CustomerList.ToList(), customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionOrderByIdReturnedOrderedList(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // order by Id
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=Id", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>  
                {   
                    new QueryCompositionCustomer { Id = 3, Name = "NewLow" },
                    new QueryCompositionCustomer { Id = 11, Name = "Lowest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                    new QueryCompositionCustomer { Id = 33, Name = "Highest" },
                },
                customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionOrderByIdTopSkipReturnsCorrectList(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // order by Id
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=Id&$skip=1&$top=2", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>  
                {   
                    new QueryCompositionCustomer { Id = 11, Name = "Lowest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                },
                customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionTopSkipChoosesDefaultOrderReturnsCorrectList(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // order by Id
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$skip=1&$top=2", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>
                {
                    new QueryCompositionCustomer { Id = 11, Name = "Lowest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                },
                customers);
        }

        [Theory]
        [InlineData("QueryCompositionCustomerLowLevelWithoutDefaultOrder", true)]
        [InlineData("QueryCompositionCustomerLowLevelWithoutDefaultOrder", false)]
        public void QueryableOnActionTopSkipFallsBackToBackendOrderIf_canUseDefaultOrderBy_IsFalse(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // order by Id
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$skip=1&$top=2", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>
                {
                    new QueryCompositionCustomer { Id = 33, Name = "Highest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                },
                customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionOrderByNameReturnsCorrectList(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // order by Name
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=Name", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>  
                {   
                    new QueryCompositionCustomer { Id = 33, Name = "Highest" },
                    new QueryCompositionCustomer { Id = 11, Name = "Lowest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                    new QueryCompositionCustomer { Id = 3, Name = "NewLow" },
                },
                customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionUnknownOperatorIsAllowed(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // unsupported operator - ignored
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=Name&unknown=12", controllerName)).Result;
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>  
                {   
                    new QueryCompositionCustomer { Id = 33, Name = "Highest" },
                    new QueryCompositionCustomer { Id = 11, Name = "Lowest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                    new QueryCompositionCustomer { Id = 3, Name = "NewLow" },
                },
                customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionUnknownOperatorStartingDollarSignThrows(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // unsupported operator starting with $ - throws
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=Name&$unknown=12", controllerName)).Result;

            if (controllerName != "QueryCompositionCustomerLowLevel")
            {
                // QueryableAttribute will validate and throws
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("QueryCompositionCustomerLowLevel", true)]
        [InlineData("QueryCompositionCustomerLowLevel", false)]
        public void QueryableOnActionUnknownOperatorStartingDollarSignIsAllowedForLowLevelApi(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // unsupported operator starting with $ - throws
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=Name&$unknown=12", controllerName)).Result;

            // using low level api works fine
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            AreEqual(
                new List<QueryCompositionCustomer>  
                {   
                    new QueryCompositionCustomer { Id = 33, Name = "Highest" },
                    new QueryCompositionCustomer { Id = 11, Name = "Lowest" }, 
                    new QueryCompositionCustomer { Id = 22, Name = "Middle" }, 
                    new QueryCompositionCustomer { Id = 3, Name = "NewLow" },
                },
                customers);
        }

        [Theory]
        [PropertyData("ControllerNames")]
        public void QueryableOnActionOrderByUnknownPropertyThrows(string controllerName, bool useCustomEdmModel)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // invalid operator - 400
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$orderby=UnknownPropertyName", controllerName)).Result;
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [TestDataSet(typeof(QueryCompositionTests), "ControllerNames", typeof(QueryCompositionTests), "Filters")]
        public void QueryableFilter(string controllerName, bool useCustomEdmModel, string filter, bool nullPropagation, int[] customerIds)
        {
            HttpServer server = new HttpServer(InitializeConfiguration(controllerName, useCustomEdmModel));
            HttpClient client = new HttpClient(server);

            // unsupported operator starting with $ - throws
            HttpResponseMessage response = client.GetAsync(string.Format("http://localhost:8080/{0}/?$filter={1}", controllerName, filter)).Result;

            // using low level api works fine
            response.EnsureSuccessStatusCode();
            List<QueryCompositionCustomer> customers = response.Content.ReadAsAsync<List<QueryCompositionCustomer>>().Result;
            Assert.Equal(
                customerIds,
                customers.Select(customer => customer.Id));
        }

        [Fact]
        public void QueryableUsesConfiguredAssembliesResolver()
        {
            ODataModelBuilder modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.EntitySet<QueryCompositionCustomer>(typeof(QueryCompositionCustomer).Name);
            IEdmModel model = modelBuilder.GetEdmModel();
            model.SetAnnotationValue<ClrTypeAnnotation>(model.FindType("System.Web.Http.OData.Query.QueryCompositionCustomer"), null);

            HttpConfiguration configuration = InitializeConfiguration("QueryCompositionCustomer", useCustomEdmModel: false);
            configuration.SetEdmModel(model);

            bool called = false;
            Mock<IAssembliesResolver> assembliesResolver = new Mock<IAssembliesResolver>();
            assembliesResolver
                .Setup(r => r.GetAssemblies())
                .Returns(new DefaultAssembliesResolver().GetAssemblies())
                .Callback(() => { called = true; })
                .Verifiable();
            configuration.Services.Replace(typeof(IAssembliesResolver), assembliesResolver.Object);

            HttpServer server = new HttpServer(configuration);
            HttpClient client = new HttpClient(server);

            HttpResponseMessage response = client.GetAsync("http://localhost:8080/{0}/?$filter=Id eq 2").Result;
            Assert.True(called);
        }

        [Theory]
        [InlineData("Id eq 10")]
        [InlineData("Locations/any(l : l/City eq 'Redmond')")]
        [InlineData("Locations/any(l : l/Zipcode eq '98052')")]
        public void QueryableWorksWithModelsWithPrimitiveCollectionAndComplexCollection(string filter)
        {
            HttpServer server = new HttpServer(InitializeConfiguration("QueryCompositionCategoryController", useCustomEdmModel: false));
            HttpClient client = new HttpClient(server);

            // unsupported operator starting with $ - throws
            HttpResponseMessage response = client.GetAsync("http://localhost:8080/QueryCompositionCategory/?$filter=" + filter).Result;
            response.EnsureSuccessStatusCode();
            Assert.Equal(0, response.Content.ReadAsAsync<List<QueryCompositionCategory>>().Result.Count());
        }

        private static HttpConfiguration InitializeConfiguration(string controllerName, bool useCustomEdmModel)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}/{id}", new { id = RouteParameter.Optional });

            if (controllerName == "QueryCompositionCustomerGlobal")
            {
                config.Filters.Add(new QueryableAttribute());
            }

            if (useCustomEdmModel)
            {
                if (_queryCompositionCustomerModel == null)
                {
                    ODataModelBuilder modelBuilder = new ODataConventionModelBuilder();
                    modelBuilder.EntitySet<QueryCompositionCustomer>(typeof(QueryCompositionCustomer).Name);
                    _queryCompositionCustomerModel = modelBuilder.GetEdmModel();
                }
                config.SetEdmModel(_queryCompositionCustomerModel);
            }

            return config;
        }

        private static void AreEqual(List<QueryCompositionCustomer> expectedList, List<QueryCompositionCustomer> actualList)
        {
            Assert.NotNull(expectedList);
            Assert.NotNull(actualList);
            Assert.Equal(expectedList.Count, actualList.Count);

            for (int i = 0; i < expectedList.Count; i++)
            {
                QueryCompositionCustomer expected = expectedList[i];
                QueryCompositionCustomer actual = actualList[i];
                AreEqual(expected, actual);
            }
        }

        private static void AreEqual(QueryCompositionCustomer expected, QueryCompositionCustomer actual)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            Assert.True(expected.Name == actual.Name && expected.Id == actual.Id);
        }
    }
}
