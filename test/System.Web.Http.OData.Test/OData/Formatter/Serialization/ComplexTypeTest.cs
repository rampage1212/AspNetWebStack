﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Web.Http.Hosting;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.TestCommon.Models;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Formatter.Serialization
{
    public class ComplexTypeTest
    {
        private readonly ODataMediaTypeFormatter _formatter;

        public ComplexTypeTest()
        {
            _formatter = new ODataMediaTypeFormatter(new ODataPayloadKind[] { ODataPayloadKind.Property }, GetSampleRequest());
            _formatter.SupportedMediaTypes.Add(ODataMediaTypes.ApplicationJsonODataMinimalMetadata);
            _formatter.SupportedMediaTypes.Add(ODataMediaTypes.ApplicationXml);
        }

        [Fact]
        public void ComplexTypeSerializesAsODataForJsonLight()
        {
            ComplexTypeSerializesAsOData(Resources.PersonComplexTypeInJsonLight, true);
        }

        [Fact]
        public void ComplexTypeSerializesAsODataForAtom()
        {
            ComplexTypeSerializesAsOData(Resources.PersonComplexTypeInAtom, false);
        }

        private void ComplexTypeSerializesAsOData(string expectedContent, bool json)
        {
            ObjectContent<Person> content = new ObjectContent<Person>(new Person(0, new ReferenceDepthContext(7)),
                _formatter, CollectionTest.GetMediaType(json));


            CollectionTest.AssertEqual(json, expectedContent, content.ReadAsStringAsync().Result);
        }

        private static HttpRequestMessage GetSampleRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/property");
            request.SetEdmModel(GetSampleModel());
            HttpConfiguration config = new HttpConfiguration();
            config.AddFakeODataRoute();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            return request;
        }

        private static IEdmModel GetSampleModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.ComplexType<Person>();
            return builder.GetEdmModel();
        }
    }
}
