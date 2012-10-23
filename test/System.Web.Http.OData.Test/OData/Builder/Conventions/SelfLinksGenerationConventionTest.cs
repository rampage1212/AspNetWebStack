﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Web.Http.Hosting;
using System.Web.Http.OData.Builder.TestModels;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Builder.Conventions
{
    public class SelfLinksGenerationConventionTest
    {
        [Fact]
        public void Apply_AddsFeedSelfLink()
        {
            // Arrange
            var mockEntityType = new Mock<IEntityTypeConfiguration>();
            var mockEntitySet = new Mock<IEntitySetConfiguration>();
            mockEntitySet.Setup(entitySet => entitySet.GetFeedSelfLink()).Returns((Func<FeedContext, Uri>)null).Verifiable();
            mockEntitySet.Setup(entitySet => entitySet.HasFeedSelfLink(It.IsAny<Func<FeedContext, Uri>>())).Returns(mockEntitySet.Object).Verifiable();
            mockEntitySet.Setup(entitySet => entitySet.EntityType).Returns(mockEntityType.Object);

            var mockModelBuilder = new Mock<ODataModelBuilder>();

            // Act
            new SelfLinksGenerationConvention().Apply(mockEntitySet.Object, mockModelBuilder.Object);

            // Assert
            mockEntitySet.Verify();
        }

        [Fact]
        public void Apply_AddsFeedSelfLink_ThatThrowsForMissingRoute()
        {
            // Arrange
            Func<FeedContext, Uri> feedSelfLink = null;
            var mockEntityType = new Mock<IEntityTypeConfiguration>();
            var mockEntitySet = new Mock<IEntitySetConfiguration>();
            mockEntitySet.Setup(entitySet => entitySet.EntityType).Returns(mockEntityType.Object);
            mockEntitySet.Setup(entitySet => entitySet.HasFeedSelfLink(It.IsAny<Func<FeedContext, Uri>>()))
                .Returns(mockEntitySet.Object)
                .Callback<Func<FeedContext, Uri>>(selfLink => { feedSelfLink = selfLink; });

            var mockModelBuilder = new Mock<ODataModelBuilder>();

            HttpConfiguration configuration = new HttpConfiguration();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties["MS_HttpConfiguration"] = configuration;
            FeedContext context = new FeedContext(new Mock<IEdmEntitySet>().Object, new UrlHelper(request), new Product[0]);

            // Act
            new SelfLinksGenerationConvention().Apply(mockEntitySet.Object, mockModelBuilder.Object);

            // Assert
            Assert.NotNull(feedSelfLink);
            Assert.ThrowsArgument(() => feedSelfLink(context), "name",
                "A route named 'OData.Default' could not be found in the route collection");
        }

        [Fact]
        public void Apply_DoesNotAddFeedSelfLink_IfOneIsPresent()
        {
            // Arrange
            var mockEntitySet = new Mock<IEntitySetConfiguration>();
            var mockEntityType = new Mock<IEntityTypeConfiguration>();
            mockEntitySet.Setup(entitySet => entitySet.GetFeedSelfLink()).Returns(feedContext => new Uri("http://www.cool.com")).Verifiable();
            mockEntitySet.Setup(entitySet => entitySet.EntityType).Returns(mockEntityType.Object);

            var mockModelBuilder = new Mock<ODataModelBuilder>();

            // Act
            new SelfLinksGenerationConvention().Apply(mockEntitySet.Object, mockModelBuilder.Object);

            // Assert
            mockEntitySet.Verify();
            mockEntitySet.Verify(entitySet => entitySet.HasFeedSelfLink(It.IsAny<Func<FeedContext, Uri>>()), Times.Never());
        }

        [Fact]
        public void GenerateSelfLinkWithoutCast_Works()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var vehicles = builder.AddEntitySet("cars", builder.AddEntity(typeof(Car)));

            IEdmModel model = builder.GetEdmModel();
            IEdmEntitySet carsEdmEntitySet = model.EntityContainers().Single().EntitySets().Single();

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(ODataRouteNames.GetById, "{controller}({id})");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());

            Uri uri =
                SelfLinksGenerationConvention.GenerateSelfLink(
                vehicles,
                new EntityInstanceContext(model, carsEdmEntitySet, carsEdmEntitySet.ElementType, request.GetUrlHelper(), new Car { Model = 2009, Name = "Accord" }),
                includeCast: false);

            Assert.Equal("http://localhost/cars(Model=2009,Name='Accord')", uri.AbsoluteUri);
        }

        [Fact]
        public void GenerateSelfLinkWithCast_Works()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var vehicles = builder.AddEntitySet("cars", builder.AddEntity(typeof(Car)));

            IEdmModel model = builder.GetEdmModel();
            IEdmEntitySet carsEdmEntitySet = model.EntityContainers().Single().EntitySets().Single();

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(ODataRouteNames.GetByIdWithCast, "{controller}({id})/{entitytype}");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());

            Uri uri =
                SelfLinksGenerationConvention.GenerateSelfLink(
                vehicles,
                new EntityInstanceContext(model, carsEdmEntitySet, carsEdmEntitySet.ElementType, request.GetUrlHelper(), new Car { Model = 2009, Name = "Accord" }),
                includeCast: true);

            Assert.Equal("http://localhost/cars(Model=2009,Name='Accord')/System.Web.Http.OData.Builder.TestModels.Car", uri.AbsoluteUri);
        }

        [Fact]
        public void SelfLinksGenerationConvention_Uses_GetByIdWithCast_IfDerivedTypeHasNavigationProperty()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var vehicles = builder.EntitySet<Vehicle>("vehicles");

            IEdmModel model = builder.GetEdmModel();
            IEdmEntitySet vehiclesEdmEntitySet = model.EntityContainers().Single().EntitySets().Single();
            IEdmEntityType carType = model.AssertHasEntityType(typeof(Car));

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(ODataRouteNames.GetByIdWithCast, "{controller}({id})/{entitytype}");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());

            IEntitySetLinkBuilder linkBuilder = model.GetEntitySetLinkBuilder(vehiclesEdmEntitySet);

            Uri uri = linkBuilder.BuildEditLink(
                new EntityInstanceContext(model, vehiclesEdmEntitySet, carType, request.GetUrlHelper(), new Car { Model = 2009, Name = "Accord" }));

            Assert.Equal("http://localhost/vehicles(Model=2009,Name='Accord')/System.Web.Http.OData.Builder.TestModels.Car", uri.AbsoluteUri);
        }

        [Fact]
        public void SelfLinksGenerationConvention_Uses_GetByIdWithoutCast_IfDerivedTypeDoesnotHaveNavigationProperty()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var motorcycles = builder.EntitySet<Motorcycle>("motorcycles");

            IEdmModel model = builder.GetEdmModel();
            IEdmEntitySet vehiclesEdmEntitySet = model.EntityContainers().Single().EntitySets().Single();
            IEdmEntityType sportbikeType = model.AssertHasEntityType(typeof(SportBike));

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(ODataRouteNames.GetById, "{controller}({id})");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());

            IEntitySetLinkBuilder linkBuilder = model.GetEntitySetLinkBuilder(vehiclesEdmEntitySet);

            Uri uri = linkBuilder.BuildEditLink(
                new EntityInstanceContext(model, vehiclesEdmEntitySet, sportbikeType, request.GetUrlHelper(), new SportBike { Model = 2009, Name = "Ninja" }));

            Assert.Equal("http://localhost/motorcycles(Model=2009,Name='Ninja')", uri.AbsoluteUri);
        }

        [Fact]
        public void SelfLinksGenerationConvention_Throws_If_RouteInCorrect()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var motorcycles = builder.EntitySet<Motorcycle>("motorcycles");

            IEdmModel model = builder.GetEdmModel();
            IEdmEntitySet vehiclesEdmEntitySet = model.EntityContainers().Single().EntitySets().Single();
            IEdmEntityType sportbikeType = model.AssertHasEntityType(typeof(SportBike));

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(ODataRouteNames.GetById, "{Cntroller}({id})");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = configuration;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());

            IEntitySetLinkBuilder linkBuilder = model.GetEntitySetLinkBuilder(vehiclesEdmEntitySet);

            Assert.Throws<InvalidOperationException>(
                () => linkBuilder.BuildEditLink(new EntityInstanceContext(model, vehiclesEdmEntitySet, sportbikeType, request.GetUrlHelper(), new SportBike { Model = 2009, Name = "Ninja" })),
                "EditLink generation failed. Check that you have the 'OData.GetById' route correctly registered.");
        }

        class SelfLinkConventionTests_EntityType
        {
            public string ID { get; set; }
        }
    }
}
