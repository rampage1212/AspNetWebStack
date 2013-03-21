﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Web.Http.OData.Builder.TestModels;
using System.Web.Http.OData.Formatter.Serialization;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Builder.Conventions
{
    public class ActionLinkGenerationConventionTest
    {
        [Fact]
        public void GenerateActionLink_GeneratesLinkWithoutCast_IfEntitySetTypeDerivesFromActionEntityType()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var cars = builder.EntitySet<Car>("cars");
            var paintAction = builder.Entity<Vehicle>().Action("Paint");

            IEdmModel model = builder.GetEdmModel();
            var carsEdmSet = model.EntityContainers().Single().FindEntitySet("cars");

            HttpConfiguration configuration = new HttpConfiguration();
            string routeName = "Route";
            configuration.Routes.MapODataRoute(routeName, null, model);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.SetConfiguration(configuration);
            request.SetODataRouteName(routeName);

            var serializerContext = new ODataSerializerContext { Model = model, EntitySet = carsEdmSet, Url = request.GetUrlHelper() };
            var entityContext = new EntityInstanceContext(serializerContext, carsEdmSet.ElementType.AsReference(), new Car { Model = 2009, Name = "Accord" });

            Uri link = ActionLinkGenerationConvention.GenerateActionLink(entityContext, paintAction);

            Assert.Equal("http://localhost/cars(Model=2009,Name='Accord')/Paint", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithoutCast_IfEntitySetTypeMatchesActionEntityType()
        {
            // Arrange
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var cars = builder.EntitySet<Car>("cars");
            var paintAction = cars.EntityType.Action("Paint");

            IEdmModel model = builder.GetEdmModel();
            var carsEdmSet = model.EntityContainers().Single().FindEntitySet("cars");

            HttpConfiguration configuration = new HttpConfiguration();
            string routeName = "Route";
            configuration.Routes.MapODataRoute(routeName, null, model);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.SetConfiguration(configuration);
            request.SetODataRouteName(routeName);

            var serializerContext = new ODataSerializerContext { Model = model, EntitySet = carsEdmSet, Url = request.GetUrlHelper() };
            var entityContext = new EntityInstanceContext(serializerContext, carsEdmSet.ElementType.AsReference(), new Car { Model = 2009, Name = "Accord" });

            // Act
            Uri link = ActionLinkGenerationConvention.GenerateActionLink(entityContext, paintAction);

            Assert.Equal("http://localhost/cars(Model=2009,Name='Accord')/Paint", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var vehicles = builder.EntitySet<Vehicle>("vehicles");
            var car = builder.Entity<Car>();
            var paintAction = car.Action("Paint");

            IEdmModel model = builder.GetEdmModel();
            var vehiclesEdmSet = model.EntityContainers().Single().FindEntitySet("vehicles");
            var carEdmType = model.FindDeclaredType("System.Web.Http.OData.Builder.TestModels.Car") as IEdmEntityType;

            HttpConfiguration configuration = new HttpConfiguration();
            string routeName = "Route";
            configuration.Routes.MapODataRoute(routeName, null, model);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.SetConfiguration(configuration);
            request.SetODataRouteName(routeName);

            var serializerContext = new ODataSerializerContext { Model = model, EntitySet = vehiclesEdmSet, Url = request.GetUrlHelper() };
            var entityContext = new EntityInstanceContext(serializerContext, carEdmType.AsReference(), new Car { Model = 2009, Name = "Accord" });

            Uri link = ActionLinkGenerationConvention.GenerateActionLink(entityContext, paintAction);

            Assert.Equal("http://localhost/vehicles(Model=2009,Name='Accord')/System.Web.Http.OData.Builder.TestModels.Car/Paint", link.AbsoluteUri);
        }

        [Fact]
        public void Apply_Doesnot_Override_UserConfiguration()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var vehicles = builder.EntitySet<Vehicle>("vehicles");
            var car = builder.AddEntity(typeof(Car));
            var paintAction = vehicles.EntityType.Action("Paint");
            paintAction.HasActionLink(ctxt => new Uri("http://localhost/ActionTestWorks"), followsConventions: false);
            ActionLinkGenerationConvention convention = new ActionLinkGenerationConvention();

            convention.Apply(paintAction, builder);

            IEdmModel model = builder.GetEdmModel();
            var vehiclesEdmSet = model.EntityContainers().Single().FindEntitySet("vehicles");
            var carEdmType = model.FindDeclaredType("System.Web.Http.OData.Builder.TestModels.Car") as IEdmEntityType;
            var paintEdmAction = model.GetAvailableProcedures(model.FindDeclaredType("System.Web.Http.OData.Builder.TestModels.Car") as IEdmEntityType).Single();

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapODataRoute(model);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.SetConfiguration(configuration);

            ActionLinkBuilder actionLinkBuilder = model.GetActionLinkBuilder(paintEdmAction);

            var serializerContext = new ODataSerializerContext { Model = model, EntitySet = vehiclesEdmSet, Url = request.GetUrlHelper() };
            var entityContext = new EntityInstanceContext(serializerContext, carEdmType.AsReference(), new Car { Model = 2009, Name = "Accord" });

            Uri link = actionLinkBuilder.BuildActionLink(entityContext);
            Assert.Equal("http://localhost/ActionTestWorks", link.AbsoluteUri);
        }

        [Fact]
        public void Apply_SetsActionLinkBuilder_OnlyIfActionIsBindable()
        {
            // Arrange
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            var vehicles = builder.EntitySet<Vehicle>("vehicles");
            var paintAction = builder.Action("Paint");
            ActionLinkGenerationConvention convention = new ActionLinkGenerationConvention();

            // Act
            convention.Apply(paintAction, builder);

            // Assert
            IEdmModel model = builder.GetEdmModel();
            var paintEdmAction = model.EntityContainers().Single().Elements.OfType<IEdmFunctionImport>().Single();

            ActionLinkBuilder actionLinkBuilder = model.GetActionLinkBuilder(paintEdmAction);

            Assert.Null(actionLinkBuilder);
        }

        [Fact]
        public void Apply_FollowsConventions()
        {
            // Arrange
            ODataModelBuilder builder = new ODataModelBuilder();
            ActionConfiguration action = new ActionConfiguration(builder, "IgnoreAction");
            Mock<IEdmTypeConfiguration> mockBindingParameterType = new Mock<IEdmTypeConfiguration>();
            mockBindingParameterType.Setup(o => o.Kind).Returns(EdmTypeKind.Entity);
            action.SetBindingParameter("IgnoreParameter", mockBindingParameterType.Object, alwaysBindable: false);
            ActionLinkGenerationConvention convention = new ActionLinkGenerationConvention();

            // Act
            convention.Apply(action, builder);

            // Assert
            Assert.True(action.FollowsConventions);
        }
    }
}
