﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Hosting;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.TestCommon.Models;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Formatter.Deserialization
{
    public class ODataActionPayloadDeserializerTest
    {
        private IEdmModel _model;

        [Fact]
        public void Can_deserialize_payload_with_primitive_parameters()
        {
            string actionName = "Primitive";
            int quantity = 1;
            string productCode = "PCode";
            string body = "{" + string.Format(@" ""Quantity"": {0} , ""ProductCode"": ""{1}"" ", quantity, productCode) + "}";

            ODataMessageWrapper message = new ODataMessageWrapper(GetStringAsStream(body));
            message.SetHeader("Content-Type", "application/json;odata=verbose");

            IEdmModel model = GetModel();
            ODataMessageReader reader = new ODataMessageReader(message as IODataRequestMessage, new ODataMessageReaderSettings(), model);
            ODataActionPayloadDeserializer deserializer = new ODataActionPayloadDeserializer(typeof(ODataActionParameters), new DefaultODataDeserializerProvider(model));
            string url = "http://server/service/EntitySet(key)/" + actionName;
            HttpRequestMessage request = GetPostRequest(url);

            ODataDeserializerContext context = new ODataDeserializerContext { Request = request, Model = model };
            ODataActionParameters payload = deserializer.Read(reader, context) as ODataActionParameters;

            Assert.NotNull(payload);
            Assert.Same(model.EntityContainers().Single().FunctionImports().SingleOrDefault(f => f.Name == "Primitive"), payload.GetFunctionImport(context));
            Assert.True(payload.ContainsKey("Quantity"));
            Assert.Equal(quantity, payload["Quantity"]);
            Assert.True(payload.ContainsKey("ProductCode"));
            Assert.Equal(productCode, payload["ProductCode"]);
        }

        [Fact]
        public void Can_deserialize_payload_with_complex_parameters()
        {
            string actionName = "Complex";
            string body = @"{ ""Quantity"": 1 , ""Address"": { ""StreetAddress"":""1 Microsoft Way"", ""City"": ""Redmond"", ""State"": ""WA"", ""ZipCode"": 98052 } }";

            ODataMessageWrapper message = new ODataMessageWrapper(GetStringAsStream(body));
            message.SetHeader("Content-Type", "application/json;odata=verbose");
            IEdmModel model = GetModel();
            ODataMessageReader reader = new ODataMessageReader(message as IODataRequestMessage, new ODataMessageReaderSettings(), model);

            ODataActionPayloadDeserializer deserializer = new ODataActionPayloadDeserializer(typeof(ODataActionParameters), new DefaultODataDeserializerProvider(model));
            string url = "http://server/service/EntitySet(key)/" + actionName;
            HttpRequestMessage request = GetPostRequest(url);
            ODataDeserializerContext context = new ODataDeserializerContext { Request = request, Model = model };
            ODataActionParameters payload = deserializer.Read(reader, context) as ODataActionParameters;

            Assert.NotNull(payload);
            Assert.Same(model.EntityContainers().Single().FunctionImports().SingleOrDefault(f => f.Name == "Complex"), payload.GetFunctionImport(context));
            Assert.True(payload.ContainsKey("Quantity"));
            Assert.Equal(1, payload["Quantity"]);
            Assert.True(payload.ContainsKey("Address"));
            MyAddress address = payload["Address"] as MyAddress;
            Assert.NotNull(address);
            Assert.Equal("1 Microsoft Way", address.StreetAddress);
            Assert.Equal("Redmond", address.City);
            Assert.Equal("WA", address.State);
            Assert.Equal(98052, address.ZipCode);
        }

        [Fact]
        public void Can_deserialize_payload_with_primitive_collection_parameters()
        {
            string actionName = "PrimitiveCollection";
            string body = @"{ ""Name"": ""Avatar"", ""Ratings"": [ 5, 5, 3, 4, 5, 5, 4, 5, 5, 4 ] }";
            int[] expectedRatings = new int[] { 5, 5, 3, 4, 5, 5, 4, 5, 5, 4 };
            ODataMessageWrapper message = new ODataMessageWrapper(GetStringAsStream(body));
            message.SetHeader("Content-Type", "application/json;odata=verbose");
            IEdmModel model = GetModel();
            ODataMessageReader reader = new ODataMessageReader(message as IODataRequestMessage, new ODataMessageReaderSettings(), model);

            ODataActionPayloadDeserializer deserializer = new ODataActionPayloadDeserializer(typeof(ODataActionParameters), new DefaultODataDeserializerProvider(model));
            string url = "http://server/service/EntitySet(key)/" + actionName;
            HttpRequestMessage request = GetPostRequest(url);
            ODataDeserializerContext context = new ODataDeserializerContext { Request = request, Model = model };
            ODataActionParameters payload = deserializer.Read(reader, context) as ODataActionParameters;

            Assert.NotNull(payload);
            Assert.Same(model.EntityContainers().Single().FunctionImports().SingleOrDefault(f => f.Name == "PrimitiveCollection"), payload.GetFunctionImport(context));
            Assert.True(payload.ContainsKey("Name"));
            Assert.Equal("Avatar", payload["Name"]);
            Assert.True(payload.ContainsKey("Ratings"));
            IList<int> ratings = payload["Ratings"] as IList<int>;
            Assert.Equal(10, ratings.Count);
            Assert.True(expectedRatings.Zip(ratings, (expected, actual) => expected - actual).All(diff => diff == 0));
        }

        [Fact]
        public void Can_deserialize_payload_with_complex_collection_parameters()
        {
            string actionName = "ComplexCollection";
            string body = @"{ ""Name"": ""Microsoft"", ""Addresses"": [ { ""StreetAddress"":""1 Microsoft Way"", ""City"": ""Redmond"", ""State"": ""WA"", ""ZipCode"": 98052 } ] }";
            ODataMessageWrapper message = new ODataMessageWrapper(GetStringAsStream(body));
            message.SetHeader("Content-Type", "application/json;odata=verbose");
            IEdmModel model = GetModel();
            ODataMessageReader reader = new ODataMessageReader(message as IODataRequestMessage, new ODataMessageReaderSettings(), model);

            ODataActionPayloadDeserializer deserializer = new ODataActionPayloadDeserializer(typeof(ODataActionParameters), new DefaultODataDeserializerProvider(model));
            string url = "http://server/service/EntitySet(key)/" + actionName;
            HttpRequestMessage request = GetPostRequest(url);
            ODataDeserializerContext context = new ODataDeserializerContext { Request = request, Model = model };
            ODataActionParameters payload = deserializer.Read(reader, context) as ODataActionParameters;

            Assert.NotNull(payload);
            Assert.True(payload.ContainsKey("Name"));
            Assert.Equal("Microsoft", payload["Name"]);
            Assert.True(payload.ContainsKey("Addresses"));
            IList<MyAddress> addresses = payload["Addresses"] as IList<MyAddress>;
            Assert.NotNull(addresses);
            Assert.Equal(1, addresses.Count);
            MyAddress address = addresses[0];
            Assert.NotNull(address);
            Assert.Equal("1 Microsoft Way", address.StreetAddress);
            Assert.Equal("Redmond", address.City);
            Assert.Equal("WA", address.State);
            Assert.Equal(98052, address.ZipCode);
        }

        [Fact]
        public void Throws_ODataException_when_parameter_not_found()
        {
            string body = @"{ ""Quantity"": 1 , ""ProductCode"": ""PCode"", ""MissingParameter"": 1 }";

            ODataMessageWrapper message = new ODataMessageWrapper(GetStringAsStream(body));
            message.SetHeader("Content-Type", "application/json;odata=verbose");
            IEdmModel model = GetModel();
            ODataMessageReader reader = new ODataMessageReader(message as IODataRequestMessage, new ODataMessageReaderSettings(), model);

            ODataActionPayloadDeserializer deserializer = new ODataActionPayloadDeserializer(typeof(ODataActionParameters), new DefaultODataDeserializerProvider(model));
            string url = "http://server/service/EntitySet(key)/Primitive";
            HttpRequestMessage request = GetPostRequest(url);
            ODataDeserializerContext context = new ODataDeserializerContext { Request = request, Model = model };
            Assert.Throws<ODataException>(() =>
            {
                ODataActionParameters payload = deserializer.Read(reader, context) as ODataActionParameters;
            }, "The parameter 'MissingParameter' in the request payload is not a valid parameter for the function import 'Primitive'.");
        }

        private IEdmModel GetModel()
        {
            if (_model == null)
            {
                ODataModelBuilder builder = new ODataConventionModelBuilder();
                builder.ContainerName = "C";
                builder.Namespace = "A.B";
                EntityTypeConfiguration<Customer> customer = builder.EntitySet<Customer>("Customers").EntityType;

                ActionConfiguration primitive = customer.Action("Primitive");
                primitive.Parameter<int>("Quantity");
                primitive.Parameter<string>("ProductCode");

                ActionConfiguration complex = customer.Action("Complex");
                complex.Parameter<int>("Quantity");
                complex.Parameter<MyAddress>("Address");

                ActionConfiguration primitiveCollection = customer.Action("PrimitiveCollection");
                primitiveCollection.Parameter<string>("Name");
                primitiveCollection.CollectionParameter<int>("Ratings");

                ActionConfiguration complexCollection = customer.Action("ComplexCollection");
                complexCollection.Parameter<string>("Name");
                complexCollection.CollectionParameter<MyAddress>("Addresses");

                _model = builder.GetEdmModel();
            }
            return _model;
        }

        private static HttpRequestMessage GetPostRequest(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
            return request;
        }

        private static Stream GetStringAsStream(string body)
        {
            Stream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    public class MyAddress
    {
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int ZipCode { get; set; }
    }
}
