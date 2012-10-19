﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Formatter.Serialization.Models;
using System.Xml.Linq;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Formatter
{
    public class EdmLibHelpersTests
    {
        [Theory]
        [InlineData(typeof(Customer), "Customer")]
        [InlineData(typeof(int), "Int32")]
        [InlineData(typeof(IEnumerable<int>), "IEnumerable_1OfInt32")]
        [InlineData(typeof(IEnumerable<Func<int, string>>), "IEnumerable_1OfFunc_2OfInt32_String")]
        [InlineData(typeof(List<Func<int, string>>), "List_1OfFunc_2OfInt32_String")]
        public void EdmFullName(Type clrType, string expectedName)
        {
            Assert.Equal(expectedName, clrType.EdmName());
        }

        [Theory]
        [InlineData(typeof(char), typeof(string))]
        [InlineData(typeof(char?), typeof(string))]
        [InlineData(typeof(ushort), typeof(int))]
        [InlineData(typeof(uint), typeof(long))]
        [InlineData(typeof(ulong), typeof(long))]
        [InlineData(typeof(ushort?), typeof(int?))]
        [InlineData(typeof(uint?), typeof(long?))]
        [InlineData(typeof(ulong?), typeof(long?))]
        [InlineData(typeof(char[]), typeof(string))]
        [InlineData(typeof(Binary), typeof(byte[]))]
        [InlineData(typeof(XElement), typeof(string))]
        public void IsNonstandardEdmPrimitive_Returns_True(Type primitiveType, Type mappedType)
        {
            bool isNonstandardEdmPrimtive;
            Type resultMappedType = EdmLibHelpers.IsNonstandardEdmPrimitive(primitiveType, out isNonstandardEdmPrimtive);

            Assert.True(isNonstandardEdmPrimtive);
            Assert.Equal(mappedType, resultMappedType);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(short))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(TimeSpan))]
        public void IsNonstandardEdmPrimitive_Returns_False(Type primitiveType)
        {
            bool isNonstandardEdmPrimtive;
            Type resultMappedType = EdmLibHelpers.IsNonstandardEdmPrimitive(primitiveType, out isNonstandardEdmPrimtive);

            Assert.False(isNonstandardEdmPrimtive);
            Assert.Equal(primitiveType, resultMappedType);
        }

        [Fact]
        public void GetEdmType_ReturnsBaseType()
        {
            IEdmModel model = GetEdmModel();
            Assert.Equal(model.GetEdmType(typeof(BaseType)), model.SchemaElements.OfType<IEdmEntityType>().Where(t => t.Name == "BaseType").Single());
        }

        [Fact]
        public void GetEdmType_ReturnsDerivedType()
        {
            IEdmModel model = GetEdmModel();
            Assert.Equal(model.GetEdmType(typeof(DerivedTypeA)), model.SchemaElements.OfType<IEdmEntityType>().Where(t => t.Name == "DerivedTypeA").Single());
            Assert.Equal(model.GetEdmType(typeof(DerivedTypeB)), model.SchemaElements.OfType<IEdmEntityType>().Where(t => t.Name == "DerivedTypeB").Single());
        }

        [Fact]
        public void GetEdmType_Returns_NearestDerivedType()
        {
            IEdmModel model = GetEdmModel();
            Assert.Equal(model.GetEdmType(typeof(DerivedTypeAA)), model.SchemaElements.OfType<IEdmEntityType>().Where(t => t.Name == "DerivedTypeA").Single());
        }

        [Fact]
        public void GetEdmType_ReturnsNull_ForUnknownType()
        {
            IEdmModel model = GetEdmModel();
            Assert.Null(model.GetEdmType(typeof(TypeNotInModel)));
        }

        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(List<int>), true)]
        [InlineData(typeof(int[]), true)]
        [InlineData(typeof(object), true)]
        [InlineData(typeof(Nullable<int>), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(char), false)]
        public void IsNullable_RecognizesClassesAndNullableOfTs(Type type, bool isNullable)
        {
            Assert.Equal(isNullable, EdmLibHelpers.IsNullable(type));
        }

        private static IEdmModel GetEdmModel()
        {
            ODataModelBuilder modelBuilder = new ODataModelBuilder();
            modelBuilder
                .Entity<DerivedTypeA>()
                .DerivesFrom<BaseType>();

            modelBuilder
                .Entity<DerivedTypeB>()
                .DerivesFrom<BaseType>();

            return modelBuilder.GetEdmModel();
        }

        public class BaseType
        {
        }

        public class DerivedTypeA : BaseType
        {
        }

        public class DerivedTypeB : BaseType
        {
        }

        public class DerivedTypeAA : DerivedTypeA
        {
        }

        public class TypeNotInModel
        {
        }
    }
}
