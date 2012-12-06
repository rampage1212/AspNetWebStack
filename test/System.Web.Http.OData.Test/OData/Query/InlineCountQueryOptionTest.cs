﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Builder.TestModels;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Query
{
    public class InlineCountQueryOptionTest
    {
        private static IEdmModel _model = new ODataModelBuilder().Add_Customer_EntityType().Add_Customers_EntitySet().GetEdmModel();
        private static ODataQueryContext _context = new ODataQueryContext(_model, typeof(Customer), "Customers");
        private static IQueryable _customers = new List<Customer>()
            {
                new Customer { CustomerId = 1, Name = "Andy" },
                new Customer { CustomerId = 2, Name = "Aaron" },
                new Customer { CustomerId = 3, Name = "Alex" }
            }.AsQueryable();

        [Fact]
        public void ConstructorNullContextThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new InlineCountQueryOption("none", null));
        }

        [Fact]
        public void ConstructorNullRawValueThrows()
        {
            Assert.Throws<ArgumentException>(() => new InlineCountQueryOption(null, _context));
        }

        [Fact]
        public void ConstructorEmptyRawValueThrows()
        {
            Assert.Throws<ArgumentException>(() => new InlineCountQueryOption(string.Empty, _context));
        }

        [Theory]
        [InlineData("none", InlineCountValue.None)]
        [InlineData("NonE", InlineCountValue.None)]
        [InlineData("allpages", InlineCountValue.AllPages)]
        [InlineData("aLLpaGeS", InlineCountValue.AllPages)]
        public void Value_Returns_ParsedInlineCountValue(string inlineCountValue, InlineCountValue expectedValue)
        {
            var inlineCount = new InlineCountQueryOption(inlineCountValue, _context);

            Assert.Equal(expectedValue, inlineCount.Value);
        }

        [Theory]
        [InlineData("onions")]
        [InlineData(" ")]
        public void Value_ThrowsODataException_ForInvalidValues(string inlineCountValue)
        {
            var inlineCount = new InlineCountQueryOption(inlineCountValue, _context);

            Assert.Throws<ODataException>(() => inlineCount.Value);
        }

        [Fact]
        public void GetEntityCount_ReturnsCount_IfValueIsAllPages()
        {
            var inlineCount = new InlineCountQueryOption("allpages", _context);

            Assert.Equal(3, inlineCount.GetEntityCount(_customers));
        }

        [Fact]
        public void GetEntityCount_ReturnsNull_IfValueIsNone()
        {
            var inlineCount = new InlineCountQueryOption("none", _context);

            Assert.Null(inlineCount.GetEntityCount(_customers));
        }
    }
}
