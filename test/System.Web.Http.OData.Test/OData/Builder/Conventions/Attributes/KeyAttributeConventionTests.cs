﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Builder.Conventions.Attributes
{
    public class KeyAttributeConventionTests
    {
        [Fact]
        public void Empty_Ctor_DoesnotThrow()
        {
            Assert.DoesNotThrow(() => new KeyAttributeConvention());
        }

        [Fact]
        public void Apply_AddsKey_EntityTypeConfiguration()
        {
            // Arrange
            Mock<PropertyInfo> property = new Mock<PropertyInfo>();
            property.Setup(p => p.Name).Returns("Property");
            property.Setup(p => p.PropertyType).Returns(typeof(int));
            property.Setup(p => p.GetCustomAttributes(It.IsAny<bool>())).Returns(new[] { new KeyAttribute() });

            Mock<PrimitivePropertyConfiguration> primitiveProperty = new Mock<PrimitivePropertyConfiguration>(property.Object);
            Mock<IEntityTypeConfiguration> entityType = new Mock<IEntityTypeConfiguration>(MockBehavior.Strict);
            entityType.Setup(e => e.HasKey(property.Object)).Returns(entityType.Object).Verifiable();

            // Act
            new KeyAttributeConvention().Apply(primitiveProperty.Object, entityType.Object);

            // Assert
            entityType.Verify();
        }

        [Fact]
        public void Apply_IgnoresKey_NonEntityTypeConfiguration()
        {
            // Arrange
            Mock<PropertyInfo> property = new Mock<PropertyInfo>();
            property.Setup(p => p.Name).Returns("Property");
            property.Setup(p => p.PropertyType).Returns(typeof(int));
            property.Setup(p => p.GetCustomAttributes(It.IsAny<bool>())).Returns(new[] { new KeyAttribute() });

            Mock<PrimitivePropertyConfiguration> primitiveProperty = new Mock<PrimitivePropertyConfiguration>(property.Object);
            Mock<IComplexTypeConfiguration> complexType = new Mock<IComplexTypeConfiguration>(MockBehavior.Strict);

            // Act
            new KeyAttributeConvention().Apply(primitiveProperty.Object, complexType.Object);

            // Assert
            complexType.Verify();
        }

        [Fact]
        public void Apply_IgnoresKey_ComplexProperty()
        {
            // Arrange
            Mock<PropertyInfo> property = new Mock<PropertyInfo>();
            property.Setup(p => p.Name).Returns("Property");
            property.Setup(p => p.PropertyType).Returns(typeof(int));
            property.Setup(p => p.GetCustomAttributes(It.IsAny<bool>())).Returns(new[] { new KeyAttribute() });

            Mock<ComplexPropertyConfiguration> complexProperty = new Mock<ComplexPropertyConfiguration>(property.Object);
            Mock<IEntityTypeConfiguration> entityType = new Mock<IEntityTypeConfiguration>(MockBehavior.Strict);

            // Act
            new KeyAttributeConvention().Apply(complexProperty.Object, entityType.Object);

            // Assert
            entityType.Verify();
        }

        [Fact]
        public void Apply_IgnoresKey_NavigationProperty()
        {
            // Arrange
            Mock<PropertyInfo> property = new Mock<PropertyInfo>();
            property.Setup(p => p.Name).Returns("Property");
            property.Setup(p => p.PropertyType).Returns(typeof(int));
            property.Setup(p => p.GetCustomAttributes(It.IsAny<bool>())).Returns(new[] { new KeyAttribute() });

            Mock<NavigationPropertyConfiguration> navigationProperty = new Mock<NavigationPropertyConfiguration>(property.Object, EdmMultiplicity.ZeroOrOne);
            Mock<IEntityTypeConfiguration> entityType = new Mock<IEntityTypeConfiguration>(MockBehavior.Strict);

            // Act
            new KeyAttributeConvention().Apply(navigationProperty.Object, entityType.Object);

            // Assert
            entityType.Verify();
        }
    }
}
