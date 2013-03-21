﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Http.Dispatcher;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Formatter.Serialization.Models;
using System.Web.Http.OData.TestCommon;
using System.Web.Http.TestCommon;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Query
{
    public class SelectExpandQueryOptionTest
    {
        private CustomersModelWithInheritance _model = new CustomersModelWithInheritance();

        [Fact]
        public void Ctor_ThrowsArgumentNull_Context()
        {
            Assert.ThrowsArgumentNull(
                () => new SelectExpandQueryOption(select: "select", expand: "expand", context: null),
                "context");
        }

        [Fact]
        public void Ctor_ThrowsArgument_IfBothSelectAndExpandAreNull()
        {
            _model.Model.SetAnnotationValue<ClrTypeAnnotation>(_model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext context = new ODataQueryContext(_model.Model, typeof(Customer));

            Assert.Throws<ArgumentException>(
                () => new SelectExpandQueryOption(select: null, expand: null, context: context),
                "'select' and 'expand' cannot be both null or empty.");
        }

        [Fact]
        public void Ctor_ThrowsArgument_IfContextIsNotForAnEntityType()
        {
            ODataQueryContext context = new ODataQueryContext(_model.Model, typeof(int));

            Assert.ThrowsArgument(
                () => new SelectExpandQueryOption(select: "Name", expand: "Name", context: context),
                "context",
                "The type 'Edm.Int32' is not an entity type. Only entity types support $select and $expand.");
        }

        [Fact]
        public void Ctor_SetsProperty_RawSelect()
        {
            // Arrange
            string selectValue = "select";
            _model.Model.SetAnnotationValue<ClrTypeAnnotation>(_model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext context = new ODataQueryContext(_model.Model, typeof(Customer));

            // Act
            SelectExpandQueryOption result = new SelectExpandQueryOption(selectValue, expand: null, context: context);

            // Assert
            Assert.Equal(selectValue, result.RawSelect);
        }

        [Fact]
        public void Ctor_SetsProperty_RawExpand()
        {
            // Arrange
            string expandValue = "expand";
            _model.Model.SetAnnotationValue<ClrTypeAnnotation>(_model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext context = new ODataQueryContext(_model.Model, typeof(Customer));

            // Act
            SelectExpandQueryOption result = new SelectExpandQueryOption(select: null, expand: expandValue, context: context);

            // Assert
            Assert.Equal(expandValue, result.RawExpand);
        }

        [Fact]
        public void SelectExpandClause_Property_ParsesRawSelectAndRawExpand()
        {
            // Arrange
            IEdmModel model = _model.Model;
            _model.Model.SetAnnotationValue<ClrTypeAnnotation>(_model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext context = new ODataQueryContext(model, typeof(Customer));
            SelectExpandQueryOption option = new SelectExpandQueryOption("ID,Name,Orders", "Orders", context);

            // Act
            SelectExpandClause selectExpandClause = option.SelectExpandClause;

            // Assert
            Assert.NotNull(selectExpandClause.Selection);
            Assert.NotNull(selectExpandClause.Expansion);
        }

        [Theory]
        [InlineData("ID", null)]
        [InlineData("LastName,FirstName", null)]
        [InlineData("LastName,    FirstName", null)]
        [InlineData("LastName,FirstName", "Orders")]
        [InlineData("LastName,FirstName,Orders", "Orders")]
        [InlineData("LastName,FirstName,Orders", "Orders")]
        [InlineData("Orders,Orders/Customer,Orders/Customer/Orders", "Orders,Orders/Customer,Orders/Customer/Orders")]
        public void SelectExpandClause_CanParse_ModelBuiltForQueryable(string select, string expand)
        {
            // Arrange
            HttpConfiguration config = new HttpConfiguration();
            config.Services.Replace(typeof(IAssembliesResolver), new TestAssemblyResolver());
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder(new HttpConfiguration(), isQueryCompositionMode: true);
            builder.Entity<Customer>();
            IEdmModel model = builder.GetEdmModel();

            ODataQueryContext context = new ODataQueryContext(model, typeof(Customer));
            SelectExpandQueryOption option = new SelectExpandQueryOption(select, expand, context);

            // Act & Assert
            Assert.DoesNotThrow(() => option.SelectExpandClause.ToString());
        }

        [Theory]
        [InlineData("IDD", null, "Type 'NS.Customer' does not have a property 'IDD'.")]
        [InlineData("ID, Namee", null, "Type 'NS.Customer' does not have a property 'Namee'.")]
        [InlineData("NSSS.Name", null, "Type 'NS.Customer' does not have a property 'NSSS.Name'.")]
        [InlineData("NS+Name", null, "Syntax error: character '+' is not valid at position 2 in 'NS+Name'.")]
        [InlineData("NS.Customerrr/SpecialCustomerProperty", null, "The type 'NS.Customerrr' is not defined in the model.")]
        public void SelectExpandCaluse_ThrowsODataException_InvalidQuery(string select, string expand, string error)
        {
            // Arrange
            IEdmModel model = _model.Model;
            _model.Model.SetAnnotationValue<ClrTypeAnnotation>(_model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext context = new ODataQueryContext(model, typeof(Customer));
            SelectExpandQueryOption option = new SelectExpandQueryOption(select, expand, context);

            // Act
            Assert.Throws<ODataException>(
                () => option.SelectExpandClause.ToString(),
                error);
        }
    }
}
