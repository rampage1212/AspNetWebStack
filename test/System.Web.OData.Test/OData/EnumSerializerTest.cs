﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Builder.TestModels;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.OData.TestCommon.Models;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.TestCommon;

namespace System.Web.Http.OData
{
    public class EnumSerializerTest
    {
        [Fact]
        public void GetEdmTypeSerializer_ReturnODataEnumSerializer_ForEnumType()
        {
            // Arrange
            IEdmTypeReference edmType = new EdmEnumTypeReference(new EdmEnumType("TestModel", "Color"), isNullable: false);

            // Act
            ODataEdmTypeSerializer serializer = new DefaultODataSerializerProvider().GetEdmTypeSerializer(edmType);

            // Assert
            Assert.NotNull(serializer);
            Assert.IsType<ODataEnumSerializer>(serializer);
        }

        [Fact]
        public void WriteObject_Throws_ForNullMessageWriterParameter()
        {
            // Arrange
            object graph = null;
            Type type = typeof(Color);
            ODataMessageWriter messageWriter = null;
            ODataSerializerContext writeContext = new ODataSerializerContext();

            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => new ODataEnumSerializer().WriteObject(graph, type, messageWriter, writeContext),
                "messageWriter");
        }

        [Fact]
        public void WriteObject_Throws_ForNullWriteContextParameter()
        {
            // Arrange
            object graph = null;
            Type type = typeof(Color);
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            httpRequestMessage.Content = new StringContent("");
            ODataMessageWriter messageWriter = new ODataMessageWriter(new HttpRequestODataMessage(httpRequestMessage));
            ODataSerializerContext writeContext = null;

            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => new ODataEnumSerializer().WriteObject(graph, type, messageWriter, writeContext),
                "writeContext");
        }

        [Fact]
        public void WriteObject_Throws_ForWriteContextWithoutRootElementName()
        {
            // Arrange
            object graph = null;
            Type type = typeof(Color);
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            httpRequestMessage.Content = new StringContent("");
            ODataMessageWriter messageWriter = new ODataMessageWriter(new HttpRequestODataMessage(httpRequestMessage));
            ODataSerializerContext writeContext = new ODataSerializerContext();

            // Act & Assert
            Assert.ThrowsArgument(
                () => new ODataEnumSerializer().WriteObject(graph, type, messageWriter, writeContext),
                "writeContext",
                "The 'RootElementName' property is required on 'ODataSerializerContext'.");
        }

        [Fact]
        public void CreateODataValue_Throws_ForNotEnumTypeon()
        {
            // Arrange
            object graph = null;
            IEdmTypeReference expectedType = EdmCoreModel.Instance.GetInt32(false);
            ODataSerializerContext writeContext = new ODataSerializerContext();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => new ODataEnumSerializer().CreateODataValue(graph, expectedType, writeContext),
                "ODataEnumSerializer cannot write an object of type 'Edm.Int32'.");
        }

        [Fact]
        public void EnumTypeSerializerTestForODataJsonLight()
        {
            EnumTypeSerializerTestForOData(Resources.EnumComplexTypeInJsonLight, true);
        }

        [Fact]
        public void EnumTypeSerializerTestForODataAtom()
        {
            EnumTypeSerializerTestForOData(Resources.EnumComplexTypeInAtom, false);
        }

        private void EnumTypeSerializerTestForOData(string expectedContent, bool isJson)
        {
            ODataMediaTypeFormatter formatter = GetFormatter();
            ObjectContent<EnumComplex> content = new ObjectContent<EnumComplex>(
                new EnumComplex()
                {
                    RequiredColor = Color.Red | Color.Blue,
                    NullableColor = null,
                    UndefinedColor = (Color)123
                },
                formatter,
                GetMediaType(isJson));
            AssertEqual(isJson, expectedContent, content.ReadAsStringAsync().Result);
        }

        private static ODataMediaTypeFormatter GetFormatter()
        {
            var formatter = new ODataMediaTypeFormatter(new ODataPayloadKind[] { ODataPayloadKind.Property })
            {
                Request = GetSampleRequest()
            };
            formatter.SupportedMediaTypes.Add(ODataMediaTypes.ApplicationJsonODataMinimalMetadata);
            formatter.SupportedMediaTypes.Add(ODataMediaTypes.ApplicationXml);
            return formatter;
        }

        private static HttpRequestMessage GetSampleRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/property");
            request.SetEdmModel(GetSampleModel());
            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapFakeODataRoute();
            request.SetConfiguration(configuration);
            request.SetFakeODataRouteName();
            return request;
        }

        private static IEdmModel GetSampleModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.ComplexType<EnumComplex>();
            return builder.GetEdmModel();
        }

        private class EnumComplex
        {
            public Color RequiredColor { get; set; }
            public Color? NullableColor { get; set; }
            public Color UndefinedColor { get; set; }
        }

        private static MediaTypeHeaderValue GetMediaType(bool jsJson)
        {
            return jsJson ? ODataMediaTypes.ApplicationJsonODataMinimalMetadata : ODataMediaTypes.ApplicationXml;
        }

        private static void AssertEqual(bool jsJson, string expected, string actual)
        {
            if (jsJson)
            {
                JsonAssert.Equal(expected, actual);
            }
            else
            {
                Assert.Xml.Equal(expected, actual);
            }
        }
    }
}