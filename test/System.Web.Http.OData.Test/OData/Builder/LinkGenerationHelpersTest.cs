﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.TestCommon;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Builder
{
    public class LinkGenerationHelpersTest
    {
        private CustomersModelWithInheritance _model = new CustomersModelWithInheritance();

        [Theory]
        [InlineData(false, "http://localhost/Customers(42)")]
        [InlineData(true, "http://localhost/Customers(42)/NS.SpecialCustomer")]
        public void GenerateSelfLink_GeneratesExpectedSelfLink(bool includeCast, string expectedIdLink)
        {
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, EntitySet = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new EntityInstanceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            string idLink = entityContext.GenerateSelfLink(includeCast);

            Assert.Equal(expectedIdLink, idLink);
        }

        [Theory]
        [InlineData(false, "http://localhost/Customers(42)/Orders")]
        [InlineData(true, "http://localhost/Customers(42)/NS.SpecialCustomer/Orders")]
        public void GenerateNavigationLink_GeneratesExpectedNavigationLink(bool includeCast, string expectedNavigationLink)
        {
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, EntitySet = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new EntityInstanceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });
            IEdmNavigationProperty ordersProperty = _model.Customer.NavigationProperties().Single();

            Uri uri = entityContext.GenerateNavigationPropertyLink(ordersProperty, includeCast);

            Assert.Equal(expectedNavigationLink, uri.AbsoluteUri);
        }

        [Fact]
        public void GenerateSelfLink_ThrowsArgumentNull_EntityContext()
        {
            Assert.ThrowsArgumentNull(
                ()=>LinkGenerationHelpers.GenerateSelfLink(entityContext: null, includeCast: false),
                "entityContext");
        }

        [Fact]
        public void GenerateSelfLink_ThrowsArgument_IfUrlHelperIsNull()
        {
            EntityInstanceContext context = new EntityInstanceContext();

            Assert.ThrowsArgument(
                () => LinkGenerationHelpers.GenerateSelfLink(context, includeCast: false),
                "entityContext",
                "The property 'Url' of EntityInstanceContext cannot be null.");
        }

        [Fact]
        public void GenerateNavigationPropertyLink_ThrowsArgumentNull_EntityContext()
        {
            IEdmNavigationProperty navigationProperty = new Mock<IEdmNavigationProperty>().Object;

            Assert.ThrowsArgumentNull(
                () => LinkGenerationHelpers.GenerateNavigationPropertyLink(entityContext: null, navigationProperty: navigationProperty,includeCast: false),
                "entityContext");
        }

        [Fact]
        public void GenerateNavigationPropertyLink_ThrowsArgument_IfUrlHelperIsNull()
        {
            IEdmNavigationProperty navigationProperty = new Mock<IEdmNavigationProperty>().Object;
            EntityInstanceContext context = new EntityInstanceContext();

            Assert.ThrowsArgument(
                () => LinkGenerationHelpers.GenerateNavigationPropertyLink(context, navigationProperty, includeCast: false),
                "entityContext",
                "The property 'Url' of EntityInstanceContext cannot be null.");
        }

        private static HttpRequestMessage GetODataRequest(IEdmModel model)
        {
            HttpConfiguration configuration = new HttpConfiguration();
            string routeName = "Route";
            configuration.Routes.MapODataRoute(routeName, null, model);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.SetConfiguration(configuration);
            request.SetODataRouteName(routeName);
            return request;
        }
    }
}
