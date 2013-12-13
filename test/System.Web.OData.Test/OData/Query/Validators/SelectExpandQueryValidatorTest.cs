// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;
using System.Web.Http.OData.Formatter.Serialization.Models;
using System.Web.Http.TestCommon;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Query.Validators
{
    public class SelectExpandQueryValidatorTest
    {
        private ODataQueryContext _queryContext;

        public SelectExpandQueryValidatorTest()
        {
            CustomersModelWithInheritance model = new CustomersModelWithInheritance();
            model.Model.SetAnnotationValue(model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            _queryContext = new ODataQueryContext(model.Model, typeof(Customer));
        }

        [Theory]
        [InlineData("Orders/Customer", 1)]
        [InlineData("Orders,Orders/Customer", 1)]
        [InlineData("Orders/Customer/Orders", 2)]
        [InlineData("Orders/Customer/Orders/Customer/Orders/Customer", 5)]
        [InlineData("Orders/NS.SpecialOrder/SpecialCustomer", 1)]
        public void Validate_DepthChecks(string expand, int maxExpansionDepth)
        {
            // Arrange
            SelectExpandQueryValidator validator = new SelectExpandQueryValidator();
            SelectExpandQueryOption selectExpandQueryOption = new SelectExpandQueryOption(null, expand, _queryContext);

            // Act & Assert
            Assert.Throws<ODataException>(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings { MaxExpansionDepth = maxExpansionDepth }),
                String.Format(CultureInfo.CurrentCulture, "The request includes a $expand path which is too deep. The maximum depth allowed is {0}. " +
                "To increase the limit, set the 'MaxExpansionDepth' property on QueryableAttribute or ODataValidationSettings.", maxExpansionDepth));

            Assert.DoesNotThrow(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings { MaxExpansionDepth = maxExpansionDepth + 1 }));
        }

        [Fact]
        public void ValidateDoesNotThrow_IfExpansionDepthIsZero()
        {
            string expand = "Orders/Customer/Orders/Customer/Orders/Customer";
            SelectExpandQueryValidator validator = new SelectExpandQueryValidator();
            SelectExpandQueryOption selectExpandQueryOption = new SelectExpandQueryOption(null, expand, _queryContext);

            Assert.DoesNotThrow(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings { MaxExpansionDepth = 0 }));
        }

        [Fact]
        public void ValidateThrowException_IfNotNavigable()
        {
            CustomersModelWithInheritance model = new CustomersModelWithInheritance();
            model.Model.SetAnnotationValue(model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext queryContext = new ODataQueryContext(model.Model, typeof(Customer));
            model.Model.SetAnnotationValue(model.Customer.FindProperty("Orders"), new QueryableRestrictionsAnnotation(new QueryableRestrictions{NotNavigable=true}));

            string select = "Orders";
            SelectExpandQueryValidator validator = new SelectExpandQueryValidator();
            SelectExpandQueryOption selectExpandQueryOption = new SelectExpandQueryOption(select, null, queryContext);
            Assert.Throws<ODataException>(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings()),
                "The property 'Orders' cannot be used for navigation.");
        }

        [Theory]
        [InlineData("Customer", "Orders")]
        [InlineData("SpecialCustomer", "SpecialOrders")]
        public void ValidateThrowException_IfBaseOrDerivedClassPropertyNotNavigable(string className, string propertyName)
        {
            CustomersModelWithInheritance model = new CustomersModelWithInheritance();
            model.Model.SetAnnotationValue(model.SpecialCustomer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext queryContext = new ODataQueryContext(model.Model, typeof(Customer));
            EdmEntityType classType = (className == "Customer") ? model.Customer : model.SpecialCustomer;
            model.Model.SetAnnotationValue(classType.FindProperty(propertyName), new QueryableRestrictionsAnnotation(new QueryableRestrictions { NotNavigable = true }));

            string select = "NS.SpecialCustomer/" + propertyName;
            SelectExpandQueryValidator validator = new SelectExpandQueryValidator();
            SelectExpandQueryOption selectExpandQueryOption = new SelectExpandQueryOption(select, null, queryContext);
            Assert.Throws<ODataException>(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings()),
                String.Format(CultureInfo.InvariantCulture, "The property '{0}' cannot be used for navigation.", propertyName));
        }

        [Fact]
        public void ValidateThrowException_IfNotExpandable()
        {
            CustomersModelWithInheritance model = new CustomersModelWithInheritance();
            model.Model.SetAnnotationValue(model.Customer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext queryContext = new ODataQueryContext(model.Model, typeof(Customer));
            model.Model.SetAnnotationValue(model.Customer.FindProperty("Orders"), new QueryableRestrictionsAnnotation(new QueryableRestrictions { NotExpandable = true }));

            string expand = "Orders";
            SelectExpandQueryValidator validator = new SelectExpandQueryValidator();
            SelectExpandQueryOption selectExpandQueryOption = new SelectExpandQueryOption(null, expand, queryContext);
            Assert.Throws<ODataException>(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings()),
                "The property 'Orders' cannot be used in the $expand query option.");
        }

        [Theory]
        [InlineData("Customer", "Orders")]
        [InlineData("SpecialCustomer", "SpecialOrders")]
        public void ValidateThrowException_IfBaseOrDerivedClassPropertyNotExpandable(string className, string propertyName)
        {
            CustomersModelWithInheritance model = new CustomersModelWithInheritance();
            model.Model.SetAnnotationValue(model.SpecialCustomer, new ClrTypeAnnotation(typeof(Customer)));
            ODataQueryContext queryContext = new ODataQueryContext(model.Model, typeof(Customer));
            EdmEntityType classType = (className == "Customer") ? model.Customer : model.SpecialCustomer;
            model.Model.SetAnnotationValue(classType.FindProperty(propertyName), new QueryableRestrictionsAnnotation(new QueryableRestrictions { NotExpandable = true }));

            string expand = "NS.SpecialCustomer/" + propertyName;
            SelectExpandQueryValidator validator = new SelectExpandQueryValidator();
            SelectExpandQueryOption selectExpandQueryOption = new SelectExpandQueryOption(null, expand, queryContext);
            Assert.Throws<ODataException>(
                () => validator.Validate(selectExpandQueryOption, new ODataValidationSettings()),
                String.Format(CultureInfo.InvariantCulture, "The property '{0}' cannot be used in the $expand query option.", propertyName));
        }
    }
}
