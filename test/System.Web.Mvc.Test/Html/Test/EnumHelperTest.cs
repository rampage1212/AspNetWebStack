﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Script.Serialization;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Mvc.Html.Test
{
    public class EnumHelperTest
    {
        private static JavaScriptSerializer _serializer = new JavaScriptSerializer();

        // IsValidForEnumHelper()

        public static TheoryDataSet<Type, bool> IsValidForEnumHelperData
        {
            get
            {
                return new TheoryDataSet<Type, bool>
                {
                    { typeof(int), false },
                    { typeof(int?), false },
                    { typeof(EnumHelperTest), false },
                    { typeof(Enum), false },
                    { typeof(EnumWithDisplay), true },
                    { typeof(EnumWithoutAnything), true },
                    { typeof(EnumWithoutZero?), true },
                    { typeof(EnumWithFlags), false },
                    { typeof(EnumWithFlags?), false },
                };
            }
        }

        [Theory]
        [InlineData(null, false)]
        [PropertyData("IsValidForEnumHelperData")]
        public void IsValidForEnumHelperWithTypeIsSuccessful(Type type, bool expected)
        {
            // Arrange & Act
            bool isValid = EnumHelper.IsValidForEnumHelper(type);

            // Assert
            Assert.Equal(expected, isValid);
        }

        [Fact]
        public void IsValidForEnumHelperWithNullMetadataIsSuccessful()
        {
            // Arrange & Act
            bool isValid = EnumHelper.IsValidForEnumHelper((ModelMetadata)null);

            // Assert
            Assert.False(isValid);
        }

        // No way to run a test with non-null ModelMetadata and null ModelType
        [Theory]
        [PropertyData("IsValidForEnumHelperData")]
        public void IsValidForEnumHelperWithMetadataIsSuccessful(Type type, bool expected)
        {
            // Arrange
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            ModelMetadata metadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Act
            bool isValid = EnumHelper.IsValidForEnumHelper(metadata);

            // Assert
            Assert.Equal(expected, isValid);
        }

        // GetSelectList()

        public static TheoryDataSet<Type, string> GetSelectListIsSuccessfulData
        {
            get
            {
                return new TheoryDataSet<Type, string>
                {
                    { typeof(EnumWithDisplay),
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"3\"}]" },
                    { typeof(EnumWithDuplicates),
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"2\"}]" },
                    { typeof(EnumWithGaps),
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"4\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"6\"}]" },
                    { typeof(EnumWithoutAnything), "[]" },
                    { typeof(EnumWithoutZero),
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"12\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"13\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"14\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"15\"}]" },
                    { typeof(EnumWithReversedValues),
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"3\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"0\"}]" },
                    { typeof(EnumWithReversedValues?),
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"\",\"Value\":\"\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"3\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"0\"}]" },
                };
            }
        }

        [Theory]
        [PropertyData("GetSelectListIsSuccessfulData")]
        public void GetSelectListWithTypeIsSuccessful(Type type, string expected)
        {
            // Arrange & Act
            IList<SelectListItem> selectList = EnumHelper.GetSelectList(type);

            // Assert
            Assert.Equal(expected, SelectListToString(selectList));
        }

        [Theory]
        [PropertyData("GetSelectListIsSuccessfulData")]
        public void GetSelectListWithMetadataIsSuccessful(Type type, string expected)
        {
            // Arrange
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            ModelMetadata metadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Act
            IList<SelectListItem> selectList = EnumHelper.GetSelectList(metadata);

            // Assert
            Assert.Equal(expected, SelectListToString(selectList));
        }

        public static TheoryDataSet<Type, Enum, string> GetSelectListWithEnumIsSuccessfulData
        {
            get
            {
                return new TheoryDataSet<Type, Enum, string>
                {
                    { typeof(EnumWithDisplay?), EnumWithDisplay.Zero,
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"\",\"Value\":\"\"}," +
                        "{\"Group\":null,\"Selected\":true,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"3\"}]" },
                    { typeof(EnumWithDuplicates), EnumWithDuplicates.Second,
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":true,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"2\"}]" },
                    { typeof(EnumWithDuplicates), EnumWithDuplicates.Third,
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":true,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"2\"}]" },
                    { typeof(EnumWithGaps?), EnumWithGaps.Third,
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"\",\"Value\":\"\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":true,\"Text\":\"Third\",\"Value\":\"4\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"6\"}]" },
                    { typeof(EnumWithoutAnything), (EnumWithoutAnything)0,
                        "[{\"Group\":null,\"Selected\":true,\"Text\":\"\",\"Value\":\"0\"}]" },
                    { typeof(EnumWithoutZero), EnumWithoutZero.Fourth,
                        "[{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"12\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"13\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"14\"}," +
                        "{\"Group\":null,\"Selected\":true,\"Text\":\"Fourth\",\"Value\":\"15\"}]" },
                    { typeof(EnumWithoutZero), (EnumWithoutZero)0,
                        "[{\"Group\":null,\"Selected\":true,\"Text\":\"\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"12\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"13\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"14\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"15\"}]" },
                    { typeof(EnumWithReversedValues?), (EnumWithReversedValues)32,
                        "[{\"Group\":null,\"Selected\":true,\"Text\":\"\",\"Value\":\"32\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"3\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"0\"}]" },
                    { typeof(EnumWithDisplay?), null,
                        "[{\"Group\":null,\"Selected\":true,\"Text\":\"\",\"Value\":\"\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"2\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"3\"}]" },
                    { typeof(EnumWithDuplicates), null,
                        "[{\"Group\":null,\"Selected\":true,\"Text\":\"First\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"1\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"2\"}]" },
                    { typeof(EnumWithoutZero), null,
                        "[{\"Group\":null,\"Selected\":true,\"Text\":\"\",\"Value\":\"0\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"First\",\"Value\":\"12\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Second\",\"Value\":\"13\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Third\",\"Value\":\"14\"}," +
                        "{\"Group\":null,\"Selected\":false,\"Text\":\"Fourth\",\"Value\":\"15\"}]" },
                };
            }
        }

        [Theory]
        [PropertyData("GetSelectListWithEnumIsSuccessfulData")]
        public void GetSelectListWithTypeAndEnumIsSuccessful(Type type, Enum value, string expected)
        {
            // Arrange & Act
            IList<SelectListItem> selectList = EnumHelper.GetSelectList(type, value);

            // Assert
            Assert.Equal(expected, SelectListToString(selectList));
        }

        [Theory]
        [PropertyData("GetSelectListWithEnumIsSuccessfulData")]
        public void GetSelectListWithMetadataAndEnumIsSuccessful(Type type, Enum value, string expected)
        {
            // Arrange
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            ModelMetadata metadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Act
            IList<SelectListItem> selectList = EnumHelper.GetSelectList(metadata, value);

            // Assert
            Assert.Equal(expected, SelectListToString(selectList));
        }

        [Fact]
        public void GetSelectListWithTypeThrowsArgumentNull()
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgumentNull(() => EnumHelper.GetSelectList((Type)null), "type");
        }

        [Fact]
        public void GetSelectListWithMetadataThrowsArgumentNull()
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgumentNull(() => EnumHelper.GetSelectList((ModelMetadata)null), "metadata");
        }

        [Fact]
        public void GetSelectListWithTypeAndEnumThrowsArgumentNull()
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgumentNull(() => EnumHelper.GetSelectList((Type)null, EnumWithDisplay.Two), "type");
        }

        [Fact]
        public void GetSelectListWithMetadataAndEnumThrowsArgumentNull()
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgumentNull(() => EnumHelper.GetSelectList((ModelMetadata)null, EnumWithDisplay.Two), "metadata");
        }

        public static TheoryDataSet<Type> GetSelectListThrowsArgumentData
        {
            get
            {
                return new TheoryDataSet<Type>
                {
                    { typeof(int) },
                    { typeof(int?) },
                    { typeof(EnumHelperTest) },
                    { typeof(Enum) },
                    { typeof(EnumWithFlags) },
                    { typeof(EnumWithFlags?) },
                };
            }
        }

        [Theory]
        [PropertyData("GetSelectListThrowsArgumentData")]
        public void GetSelectListWithTypeThrowsArgument(Type type)
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgument(() => EnumHelper.GetSelectList(type), "type");
        }

        // No way to run a test with non-null ModelMetadata and null ModelType
        [Theory]
        [PropertyData("GetSelectListThrowsArgumentData")]
        public void GetSelectListWithMetadataThrowsArgument(Type type)
        {
            // Arrange
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            ModelMetadata metadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Act & Assert
            Assert.ThrowsArgument(() => EnumHelper.GetSelectList(metadata), "metadata");
        }

        [Theory]
        [PropertyData("GetSelectListThrowsArgumentData")]
        public void GetSelectListWithTypeAndEnumThrowsArgument(Type type)
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgument(() => EnumHelper.GetSelectList(type, EnumWithDisplay.Two), "type");
        }

        // No way to run a test with non-null ModelMetadata and null ModelType
        [Theory]
        [PropertyData("GetSelectListThrowsArgumentData")]
        public void GetSelectListWithMetadataAndEnumThrowsArgument(Type type)
        {
            // Arrange
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            ModelMetadata metadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Act & Assert
            Assert.ThrowsArgument(() => EnumHelper.GetSelectList(metadata, EnumWithDisplay.Two), "metadata");
        }

        [Fact]
        public void GetSelectListWithTypeAndEnumThrowsArgumentOnMismatch()
        {
            // Arrange & Act & Assert
            Assert.ThrowsArgument(() => EnumHelper.GetSelectList(typeof(EnumWithDuplicates), EnumWithDisplay.Two),
                paramName: "value",
                exceptionMessage: "Invalid value parameter type " +
                "'System.Web.Mvc.Html.Test.EnumHelperTest+EnumWithDisplay'. Must match type parameter " +
                "'System.Web.Mvc.Html.Test.EnumHelperTest+EnumWithDuplicates'." + Environment.NewLine +
                "Parameter name: value");
        }

        [Fact]
        public void GetSelectListWithMetadataAndEnumThrowsArgumentOnMismatch()
        {
            // Arrange
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            ModelMetadata metadata = new ModelMetadata(provider.Object, null, null, typeof(EnumWithDuplicates), null);

            // Act & Assert
            Assert.ThrowsArgument(() => EnumHelper.GetSelectList(metadata, EnumWithDisplay.Two), paramName: "value",
                exceptionMessage: "Invalid value parameter type " +
                "'System.Web.Mvc.Html.Test.EnumHelperTest+EnumWithDisplay'. Must match type parameter " +
                "'System.Web.Mvc.Html.Test.EnumHelperTest+EnumWithDuplicates'." + Environment.NewLine +
                "Parameter name: value");
        }

        // helpers

        private static string SelectListToString(IList<SelectListItem> selectList)
        {
            return _serializer.Serialize(selectList);
        }

        // enum definitions

        private enum EnumWithDisplay : byte
        {
            [Display(Name="First")]
            Zero,
            [Display(Name = "Second")]
            One,
            [Display(Name = "Third")]
            Two,
            [Display(Name = "Fourth")]
            Three,
        }

        private enum EnumWithoutAnything : byte
        {
        }

        private enum EnumWithoutZero : byte
        {
            First = 12,
            Second,
            Third,
            Fourth,
        }

        private enum EnumWithDuplicates : byte
        {
            First,
            Second,
            Third = 1,
            Fourth,
        }

        private enum EnumWithGaps : byte
        {
            First,
            Second = 2,
            Third = 4,
            Fourth = 6,
        }

        private enum EnumWithReversedValues : byte
        {
            First = 3,
            Second = 2,
            Third = 1,
            Fourth = 0,
        }

        [Flags]
        private enum EnumWithFlags : byte
        {
            First = 1,
            Second = 2,
            Third = 4,
            Fourth = 8,
        }
    }
}
