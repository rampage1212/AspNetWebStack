﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Http.Metadata;
using System.Web.Http.Metadata.Providers;
using Microsoft.TestCommon;
using Moq;
using Moq.Protected;

namespace System.Web.Http.Validation.Validators
{
    public class DataAnnotationsModelValidatorTest
    {
        private static DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();
        private static IEnumerable<ModelValidatorProvider> _noValidatorProviders = Enumerable.Empty<ModelValidatorProvider>();

        [Fact]
        public void ConstructorGuards()
        {
            // Arrange
            ModelMetadata metadata = _metadataProvider.GetMetadataForType(null, typeof(object));
            var attribute = new RequiredAttribute();

            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => new DataAnnotationsModelValidator(null, attribute),
                "validatorProviders");
            Assert.ThrowsArgumentNull(
                () => new DataAnnotationsModelValidator(_noValidatorProviders, null),
                "attribute");
        }

        [Fact]
        public void ValuesSet()
        {
            // Arrange
            ModelMetadata metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            var attribute = new RequiredAttribute();

            // Act
            var validator = new DataAnnotationsModelValidator(_noValidatorProviders, attribute);

            // Assert
            Assert.Same(attribute, validator.Attribute);
        }

        [Fact]
        public void ValidateWithIsValidTrue()
        {
            // Arrange
            ModelMetadata metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            Mock<ValidationAttribute> attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(metadata.Model)).Returns(true);
            DataAnnotationsModelValidator validator = new DataAnnotationsModelValidator(_noValidatorProviders, attribute.Object);

            // Act
            IEnumerable<ModelValidationResult> result = validator.Validate(metadata, null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateWithIsValidFalse()
        {
            // Arrange
            ModelMetadata metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            Mock<ValidationAttribute> attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Setup(a => a.IsValid(metadata.Model)).Returns(false);
            DataAnnotationsModelValidator validator = new DataAnnotationsModelValidator(_noValidatorProviders, attribute.Object);

            // Act
            IEnumerable<ModelValidationResult> result = validator.Validate(metadata, null);

            // Assert
            var validationResult = result.Single();
            Assert.Equal("", validationResult.MemberName);
            Assert.Equal(attribute.Object.FormatErrorMessage("Length"), validationResult.Message);
        }

        [Fact]
        public void ValidateReturnsSingleValidationResultIfMemberNameSequenceIsNull()
        {
            // Arrange
            const string errorMessage = "Some error message";
            ModelMetadata metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            Mock<ValidationAttribute> attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, memberNames: null));
            var validator = new DataAnnotationsModelValidator(_noValidatorProviders, attribute.Object);

            // Act
            IEnumerable<ModelValidationResult> results = validator.Validate(metadata, container: null);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal(errorMessage, validationResult.Message);
        }

        [Fact]
        public void ValidateReturnsSingleValidationResultIfOneMemberNameIsSpecified()
        {
            // Arrange
            const string errorMessage = "A different error message";
            ModelMetadata metadata = _metadataProvider.GetMetadataForType(() => new object(), typeof(object));
            Mock<ValidationAttribute> attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(new ValidationResult(errorMessage, new[] { "FirstName" }));
            var validator = new DataAnnotationsModelValidator(_noValidatorProviders, attribute.Object);

            // Act
            IEnumerable<ModelValidationResult> results = validator.Validate(metadata, container: null);

            // Assert
            ModelValidationResult validationResult = Assert.Single(results);
            Assert.Equal(errorMessage, validationResult.Message);
            Assert.Equal("FirstName", validationResult.MemberName);
        }

        [Fact]
        public void ValidatateWithValidationResultSuccess()
        {
            // Arrange
            ModelMetadata metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");
            Mock<ValidationAttribute> attribute = new Mock<ValidationAttribute> { CallBase = true };
            attribute.Protected()
                     .Setup<ValidationResult>("IsValid", ItExpr.IsAny<object>(), ItExpr.IsAny<ValidationContext>())
                     .Returns(ValidationResult.Success);
            DataAnnotationsModelValidator validator = new DataAnnotationsModelValidator(_noValidatorProviders, attribute.Object);

            // Act
            IEnumerable<ModelValidationResult> result = validator.Validate(metadata, null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void IsRequiredTests()
        {
            // Arrange
            ModelMetadata metadata = _metadataProvider.GetMetadataForProperty(() => 15, typeof(string), "Length");

            // Act & Assert
            Assert.False(new DataAnnotationsModelValidator(_noValidatorProviders, new RangeAttribute(10, 20)).IsRequired);
            Assert.True(new DataAnnotationsModelValidator(_noValidatorProviders, new RequiredAttribute()).IsRequired);
            Assert.True(new DataAnnotationsModelValidator(_noValidatorProviders, new DerivedRequiredAttribute()).IsRequired);
        }

        class DerivedRequiredAttribute : RequiredAttribute
        {
        }
    }
}
