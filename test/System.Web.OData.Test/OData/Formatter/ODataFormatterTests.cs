﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Tracing;
using System.Web.OData.Builder;
using System.Web.OData.Builder.TestModels;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.OData.Core;
using Microsoft.OData.Core.Atom;
using Microsoft.OData.Edm;
using Microsoft.TestCommon;
using Moq;
using Newtonsoft.Json.Linq;

namespace System.Web.OData.Formatter
{
    public class ODataFormatterTests
    {
        private const string baseAddress = "http://localhost:8081/";

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetEntryInODataAtomFormat(bool tracingEnabled)
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration(tracingEnabled))
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = CreateRequestWithDataServiceVersionHeaders("People(10)",
                ODataTestUtil.ApplicationAtomMediaTypeWithQuality))
            // Act
            using (HttpResponseMessage response = client.SendAsync(request).Result)
            {
                // Assert
                AssertODataVersion4AtomResponse(Resources.PersonEntryInAtom, response);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PostEntryInODataAtomFormat(bool tracingEnabled)
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration(tracingEnabled))
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, baseAddress + "People"))
            {
                request.Content = new StringContent(Resources.PersonEntryInAtom);
                request.Content.Headers.ContentType = ODataTestUtil.ApplicationAtomMediaTypeWithQuality;

                // Act
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    // Assert
                    AssertODataVersion4AtomResponse(Resources.PersonEntryInAtom, response, HttpStatusCode.Created);
                }
            }
        }

        [Theory]
        [InlineData("application/json;odata.metadata=none", "PersonEntryInJsonLightNoMetadata.json")]
        [InlineData("application/json;odata.metadata=minimal", "PersonEntryInJsonLightMinimalMetadata.json")]
        [InlineData("application/json;odata.metadata=full", "PersonEntryInJsonLightFullMetadata.json")]
        public void GetEntryInODataJsonLightFormat(string metadata, string expect)
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = CreateRequestWithDataServiceVersionHeaders("People(10)",
                MediaTypeWithQualityHeaderValue.Parse(metadata)))
            // Act
            using (HttpResponseMessage response = client.SendAsync(request).Result)
            {
                // Assert
                AssertODataVersion4JsonResponse(Resources.GetString(expect), response);
            }
        }

        [Fact]
        public void GetEntry_UsesRouteModel_ForMultipleModels()
        {
            // Model 1 only has Name, Model 2 only has Age
            ODataModelBuilder builder1 = new ODataModelBuilder();
            var personType1 = builder1.Entity<FormatterPerson>().Property(p => p.Name);
            builder1.EntitySet<FormatterPerson>("People").HasIdLink(p => "http://link/", false);
            var model1 = builder1.GetEdmModel();

            ODataModelBuilder builder2 = new ODataModelBuilder();
            builder2.Entity<FormatterPerson>().Property(p => p.Age);
            builder2.EntitySet<FormatterPerson>("People").HasIdLink(p => "http://link/", false);
            var model2 = builder2.GetEdmModel();

            var config = new HttpConfiguration();
            config.Routes.MapODataRoute("OData1", "v1", model1);
            config.Routes.MapODataRoute("OData2", "v2", model2);

            using (HttpServer host = new HttpServer(config))
            using (HttpClient client = new HttpClient(host))
            {
                using (HttpResponseMessage response = client.GetAsync("http://localhost/v1/People(10)").Result)
                {
                    Assert.True(response.IsSuccessStatusCode);
                    JToken json = JToken.Parse(response.Content.ReadAsStringAsync().Result);

                    // Model 1 has the Name property but not the Age property
                    Assert.NotNull(json["Name"]);
                    Assert.Null(json["Age"]);
                }

                using (HttpResponseMessage response = client.GetAsync("http://localhost/v2/People(10)").Result)
                {
                    Assert.True(response.IsSuccessStatusCode);
                    JToken json = JToken.Parse(response.Content.ReadAsStringAsync().Result);

                    // Model 2 has the Age property but not the Name property
                    Assert.Null(json["Name"]);
                    Assert.NotNull(json["Age"]);
                }
            }
        }

        [Fact]
        public void GetFeedInODataJsonFullMetadataFormat()
        {
            // Arrange
            IEdmModel model = CreateModelForFullMetadata(sameLinksForIdAndEdit: false, sameLinksForEditAndRead: false);

            using (HttpConfiguration configuration = CreateConfiguration(model))
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = CreateRequestWithDataServiceVersionHeaders("MainEntity",
                MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full")))
            // Act
            using (HttpResponseMessage response = client.SendAsync(request).Result)
            {
                // Assert
                AssertODataVersion4JsonResponse(
                    Resources.MainEntryFeedInJsonFullMetadata, response);
            }
        }

        [Fact]
        public void GetFeedInODataJsonNoMetadataFormat()
        {
            // Arrange
            IEdmModel model = CreateModelForFullMetadata(sameLinksForIdAndEdit: false, sameLinksForEditAndRead: false);

            using (HttpConfiguration configuration = CreateConfiguration(model))
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = CreateRequestWithDataServiceVersionHeaders("MainEntity",
                MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=none")))
            // Act
            using (HttpResponseMessage response = client.SendAsync(request).Result)
            {
                // Assert
                AssertODataVersion4JsonResponse(Resources.MainEntryFeedInJsonNoMetadata, response);
            }
        }

        [Fact]
        public void SupportOnlyODataAtomFormat()
        {
            // Arrange #1 and #2
            using (HttpConfiguration configuration = CreateConfiguration())
            {
                foreach (ODataMediaTypeFormatter odataFormatter in
                    configuration.Formatters.OfType<ODataMediaTypeFormatter>())
                {
                    odataFormatter.SupportedMediaTypes.Remove(ODataMediaTypes.ApplicationJson);
                }

                using (HttpServer host = new HttpServer(configuration))
                using (HttpClient client = new HttpClient(host))
                {
                    // Arrange #1
                    using (HttpRequestMessage request = CreateRequestWithDataServiceVersionHeaders("People(10)",
                        ODataTestUtil.ApplicationAtomMediaTypeWithQuality))
                    // Act #1
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        // Assert #1
                        AssertODataVersion4AtomResponse(Resources.PersonEntryInAtom, response);
                    }

                    // Arrange #2
                    using (HttpRequestMessage request = CreateRequestWithDataServiceVersionHeaders("People(10)",
                        ODataTestUtil.ApplicationJsonMediaTypeWithQuality))
                    // Act #2
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        // Assert #2
                        Assert.NotNull(response);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(ODataTestUtil.ApplicationJsonMediaTypeWithQuality.MediaType,
                            response.Content.Headers.ContentType.MediaType);
                        ODataTestUtil.VerifyJsonResponse(response.Content, Resources.PersonEntryInPlainOldJson);
                    }
                }
            }
        }

        [Fact]
        public void ConditionallySupportODataIfQueryStringPresent()
        {
            // Arrange #1, #2 and #3
            using (HttpConfiguration configuration = CreateConfiguration())
            {
                foreach (ODataMediaTypeFormatter odataFormatter in
                    configuration.Formatters.OfType<ODataMediaTypeFormatter>())
                {
                    odataFormatter.SupportedMediaTypes.Clear();
                    odataFormatter.MediaTypeMappings.Add(new ODataMediaTypeMapping(ODataTestUtil.ApplicationAtomMediaTypeWithQuality));
                    odataFormatter.MediaTypeMappings.Add(new ODataMediaTypeMapping(ODataTestUtil.ApplicationJsonMediaTypeWithQuality));
                }

                using (HttpServer host = new HttpServer(configuration))
                using (HttpClient client = new HttpClient(host))
                {
                    // Arrange #1 this request should return response in OData atom format
                    using (HttpRequestMessage request = ODataTestUtil.GenerateRequestMessage(
                        CreateAbsoluteUri("People(10)?$format=atom"), isAtom: true))
                    // Act #1
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        // Assert #1
                        AssertODataVersion4AtomResponse(Resources.PersonEntryInAtom, response);
                    }

                    // Arrange #2: this request should return response in OData json format
                    using (HttpRequestMessage requestWithJsonHeader = ODataTestUtil.GenerateRequestMessage(
                        CreateAbsoluteUri("People(10)?$format=application/json"), isAtom: false))
                    // Act #2
                    using (HttpResponseMessage response = client.SendAsync(requestWithJsonHeader).Result)
                    {
                        // Assert #2
                        AssertODataVersion4JsonResponse(Resources.PersonEntryInJsonLight, response);
                    }

                    // Arrange #3: when the query string is not present, request should be handled by the regular Json
                    // Formatter
                    using (HttpRequestMessage requestWithNonODataJsonHeader = ODataTestUtil.GenerateRequestMessage(
                        CreateAbsoluteUri("People(10)"), isAtom: false))
                    // Act #3
                    using (HttpResponseMessage response = client.SendAsync(requestWithNonODataJsonHeader).Result)
                    {
                        // Assert #3
                        Assert.NotNull(response);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(ODataTestUtil.ApplicationJsonMediaTypeWithQuality.MediaType,
                            response.Content.Headers.ContentType.MediaType);
                        Assert.Null(ODataTestUtil.GetDataServiceVersion(response.Content.Headers));

                        ODataTestUtil.VerifyJsonResponse(response.Content, Resources.PersonEntryInPlainOldJson);
                    }
                }
            }
        }

        [Fact]
        public void GetFeedInODataAtomFormat_HasSelfLink()
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = CreateRequest("People",
                ODataTestUtil.ApplicationAtomMediaTypeWithQuality))
            // Act
            using (HttpResponseMessage response = client.SendAsync(request).Result)
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                XElement xml = XElement.Load(response.Content.ReadAsStreamAsync().Result);
                XElement[] links = xml.Elements(XName.Get("link", "http://www.w3.org/2005/Atom")).ToArray();
                Assert.Equal("self", links.First().Attribute("rel").Value);
                Assert.Equal(baseAddress + "People", links.First().Attribute("href").Value);
            }
        }

        [Fact]
        public void GetFeedInODataAtomFormat_LimitsResults()
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            using (HttpServer host = new HttpServer(configuration))
            using (HttpClient client = new HttpClient(host))
            using (HttpRequestMessage request = CreateRequest("People?$orderby=Name&$count=true",
                    ODataTestUtil.ApplicationAtomMediaTypeWithQuality))
            // Act
            using (HttpResponseMessage response = client.SendAsync(request).Result)
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                XElement xml = XElement.Load(response.Content.ReadAsStreamAsync().Result);
                XElement[] entries = xml.Elements(XName.Get("entry", "http://www.w3.org/2005/Atom")).ToArray();
                XElement nextPageLink = xml.Elements(XName.Get("link", "http://www.w3.org/2005/Atom"))
                    .Where(link => link.Attribute(XName.Get("rel")).Value == "next")
                    .SingleOrDefault();
                XElement count = xml.Element(XName.Get("count", "http://docs.oasis-open.org/odata/ns/metadata"));

                // Assert the PageSize correctly limits three results to two
                Assert.Equal(2, entries.Length);
                // Assert there is a next page link
                Assert.NotNull(nextPageLink);
                // Assert the count is included with the number of entities (3)
                Assert.Equal("3", count.Value);
            }
        }

        [Fact]
        [ReplaceCulture]
        public void HttpErrorInODataFormat_GetsSerializedCorrectly()
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            {
                configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
                using (HttpServer host = new HttpServer(configuration))
                using (HttpClient client = new HttpClient(host))
                using (HttpRequestMessage request = CreateRequest("People?$filter=abc+eq+null",
                    MediaTypeWithQualityHeaderValue.Parse("application/xml")))
                // Act
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                    XElement xml = XElement.Load(response.Content.ReadAsStreamAsync().Result);

                    Assert.Equal("error", xml.Name.LocalName);
                    Assert.Equal("The query specified in the URI is not valid. Could not find a property named 'abc' on type 'System.Web.OData.Formatter.FormatterPerson'.",
                        xml.Element(XName.Get("{http://docs.oasis-open.org/odata/ns/metadata}message")).Value);
                    XElement innerErrorXml = xml.Element(XName.Get("{http://docs.oasis-open.org/odata/ns/metadata}innererror"));
                    Assert.NotNull(innerErrorXml);
                    Assert.Equal("Could not find a property named 'abc' on type 'System.Web.OData.Formatter.FormatterPerson'.",
                        innerErrorXml.Element(XName.Get("{http://docs.oasis-open.org/odata/ns/metadata}message")).Value);
                    Assert.Equal("Microsoft.OData.Core.ODataException",
                        innerErrorXml.Element(XName.Get("{http://docs.oasis-open.org/odata/ns/metadata}type")).Value);
                }
            }
        }

        [Fact]
        public void CustomSerializerWorks()
        {
            // Arrange
            using (HttpConfiguration configuration = CreateConfiguration())
            {
                configuration.Formatters.InsertRange(
                    0,
                    ODataMediaTypeFormatters.Create(new CustomSerializerProvider(), new DefaultODataDeserializerProvider()));
                using (HttpServer host = new HttpServer(configuration))
                using (HttpClient client = new HttpClient(host))
                using (HttpRequestMessage request = CreateRequest("People", MediaTypeWithQualityHeaderValue.Parse("application/atom+xml")))
                // Act
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    XElement xml = XElement.Load(response.Content.ReadAsStreamAsync().Result);

                    Assert.Equal("My amazing feed", xml.Elements().Single(e => e.Name.LocalName == "title").Value);
                }
            }
        }

        [Fact]
        public void EnumTypeRoundTripTest()
        {
            // Arrange
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<EnumCustomer>("EnumCustomers");
            IEdmModel model = builder.GetEdmModel();

            using (HttpConfiguration configuration = new HttpConfiguration())
            {
                configuration.Routes.MapODataRoute("odata", routePrefix: null, model: model);
                using (HttpServer host = new HttpServer(configuration))
                using (HttpClient client = new HttpClient(host))
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/EnumCustomers"))
                {
                    request.Content = new StringContent(
                        string.Format(@"{{'@odata.type':'#System.Web.OData.Formatter.EnumCustomer',
                            'ID':0,'Color':'Green, Blue','Colors':['Red','Red, Blue']}}"));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    request.Headers.Accept.ParseAdd("application/json");

                    // Act
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        // Assert
                        response.EnsureSuccessStatusCode();
                        var customer = response.Content.ReadAsAsync<JObject>().Result;
                        Assert.Equal(0, customer["ID"]);
                        Assert.Equal(Color.Green | Color.Blue, Enum.Parse(typeof(Color), customer["Color"].ToString()));
                        var colors = customer["Colors"].Select(c => Enum.Parse(typeof(Color), c.ToString()));
                        Assert.Equal(2, colors.Count());
                        Assert.Contains(Color.Red, colors);
                        Assert.Contains(Color.Red | Color.Blue, colors);
                    }
                }
            }
        }

        [Fact]
        public void EnumSerializer_HasODataType_ForFullMetadata()
        {
            // Arrange & Act
            string acceptHeader = "application/json;odata.metadata=full";
            HttpResponseMessage response = GetEnumResponse(acceptHeader);

            // Assert
            response.EnsureSuccessStatusCode();
            JObject customer = response.Content.ReadAsAsync<JObject>().Result;
            Assert.Equal("#System.Web.OData.Builder.TestModels.Color",
                customer.GetValue("Color@odata.type"));
            Assert.Equal("#Collection(System.Web.OData.Builder.TestModels.Color)",
                customer.GetValue("Colors@odata.type"));
        }

        [Theory]
        [InlineData("application/json;odata.metadata=minimal")]
        [InlineData("application/json;odata.metadata=none")]
        public void EnumSerializer_HasNoODataType_ForNonFullMetadata(string acceptHeader)
        {
            // Arrange & Act
            HttpResponseMessage response = GetEnumResponse(acceptHeader);

            // Assert
            response.EnsureSuccessStatusCode();
            JObject customer = response.Content.ReadAsAsync<JObject>().Result;
            Assert.False(customer.Values().Contains("Color@odata.type"));
            Assert.False(customer.Values().Contains("Colors@odata.type"));
        }

        private HttpResponseMessage GetEnumResponse(string acceptHeader)
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<EnumCustomer>("EnumCustomers");
            IEdmModel model = builder.GetEdmModel();

            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapODataRoute("odata", routePrefix: null, model: model);
            HttpServer host = new HttpServer(configuration);
            HttpClient client = new HttpClient(host);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/EnumCustomers");
            request.Content = new StringContent(
                string.Format(@"{{'@odata.type':'#System.Web.OData.Formatter.EnumCustomer',
                            'ID':0,'Color':'Green, Blue','Colors':['Red','Red, Blue']}}"));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Headers.Accept.ParseAdd(acceptHeader);

            HttpResponseMessage response = client.SendAsync(request).Result;
            return response;
        }

        [Fact]
        public void EnumSerializer_HasMetadataType_InAtom()
        {
            // Arrange
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<EnumCustomer>("EnumCustomers");
            IEdmModel model = builder.GetEdmModel();

            using (HttpConfiguration configuration = new HttpConfiguration())
            {
                configuration.Routes.MapODataRoute("odata", routePrefix: null, model: model);
                using (HttpServer host = new HttpServer(configuration))
                using (HttpClient client = new HttpClient(host))
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/EnumCustomers"))
                {
                    request.Content = new StringContent(
                        string.Format(@"{{'@odata.type':'#System.Web.OData.Formatter.EnumCustomer',
                            'ID':0,'Color':'Green, Blue','Colors':['Red','Red, Blue']}}"));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    request.Headers.Accept.ParseAdd("application/atom+xml");

                    // Act
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        // Assert
                        response.EnsureSuccessStatusCode();
                        var atomResult = response.Content.ReadAsStreamAsync().Result;
                        var atomXmlDocument = new XmlDocument();
                        atomXmlDocument.Load(atomResult);

                        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(atomXmlDocument.NameTable);
                        namespaceManager.AddNamespace("ns", atomXmlDocument.DocumentElement.NamespaceURI);
                        namespaceManager.AddNamespace("m", atomXmlDocument.DocumentElement.GetNamespaceOfPrefix("m"));
                        namespaceManager.AddNamespace("d", atomXmlDocument.DocumentElement.GetNamespaceOfPrefix("d"));

                        var colorMetadataType = atomXmlDocument.DocumentElement.SelectNodes(
                            "ns:content/m:properties/d:Color/attribute::m:type", namespaceManager).Cast<XmlNode>().Select(e => e.Value);
                        var colorsMetadataType = atomXmlDocument.DocumentElement.SelectNodes(
                            "ns:content/m:properties/d:Colors/attribute::m:type", namespaceManager).Cast<XmlNode>().Select(e => e.Value);
                        Assert.Equal("#System.Web.OData.Builder.TestModels.Color", colorMetadataType.Single());
                        Assert.Equal("#Collection(System.Web.OData.Builder.TestModels.Color)", colorsMetadataType.Single());
                    }
                }
            }
        }

        public class EnumCustomer
        {
            public int ID { get; set; }
            public Color Color { get; set; }
            public List<Color> Colors { get; set; }
        }

        public class EnumCustomersController : ODataController
        {
            public IHttpActionResult Post(EnumCustomer customer)
            {
                return Ok(customer);
            }
        }

        private static void AddDataServiceVersionHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("OData-Version", "4.0");
            request.Headers.Add("OData-MaxVersion", "4.0");
        }

        private static void AssertODataVersion4AtomResponse(string expectedContent, HttpResponseMessage actual)
        {
            AssertODataVersion4AtomResponse(expectedContent, actual, HttpStatusCode.OK);
        }

        private static void AssertODataVersion4AtomResponse(string expectedContent, HttpResponseMessage actual, HttpStatusCode statusCode)
        {
            Assert.NotNull(actual);
            Assert.Equal(statusCode, actual.StatusCode);
            Assert.Equal(ODataTestUtil.ApplicationAtomMediaTypeWithQuality.MediaType,
                actual.Content.Headers.ContentType.MediaType);
            Assert.Equal(ODataTestUtil.Version4NumberString,
                ODataTestUtil.GetDataServiceVersion(actual.Content.Headers));
            ODataTestUtil.VerifyResponse(actual.Content, expectedContent);
        }

        private static void AssertODataVersion4JsonResponse(string expectedContent, HttpResponseMessage actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
            Assert.Equal(ODataTestUtil.ApplicationJsonMediaTypeWithQuality.MediaType,
                actual.Content.Headers.ContentType.MediaType);
            Assert.Equal(ODataTestUtil.Version4NumberString,
                ODataTestUtil.GetDataServiceVersion(actual.Content.Headers));
            ODataTestUtil.VerifyJsonResponse(actual.Content, expectedContent);
        }

        private static string CreateAbsoluteLink(string relativeUri)
        {
            return CreateAbsoluteUri(relativeUri).AbsoluteUri;
        }

        private static Uri CreateAbsoluteUri(string relativeUri)
        {
            return new Uri(new Uri(baseAddress), relativeUri);
        }

        private static HttpConfiguration CreateConfiguration(bool tracingEnabled = false)
        {
            IEdmModel model = ODataTestUtil.GetEdmModel();
            HttpConfiguration configuration = CreateConfiguration(model);

            if (tracingEnabled)
            {
                configuration.Services.Replace(typeof(ITraceWriter), new Mock<ITraceWriter>().Object);
            }

            return configuration;
        }

        private static HttpConfiguration CreateConfiguration(IEdmModel model)
        {
            HttpConfiguration configuration = new HttpConfiguration();
            configuration.Routes.MapODataRoute(model);
            configuration.Formatters.InsertRange(0, ODataMediaTypeFormatters.Create());
            return configuration;
        }

        private static IEdmModel CreateModelForFullMetadata(bool sameLinksForIdAndEdit, bool sameLinksForEditAndRead)
        {
            ODataModelBuilder builder = new ODataModelBuilder();

            EntitySetConfiguration<MainEntity> mainSet = builder.EntitySet<MainEntity>("MainEntity");

            Func<EntityInstanceContext<MainEntity>, string> idLinkFactory = (e) =>
                CreateAbsoluteLink("/MainEntity/id/" + e.GetPropertyValue("Id").ToString());
            mainSet.HasIdLink(idLinkFactory, followsConventions: true);

            Func<EntityInstanceContext<MainEntity>, string> editLinkFactory;

            if (!sameLinksForIdAndEdit)
            {
                editLinkFactory = (e) => CreateAbsoluteLink("/MainEntity/edit/" + e.GetPropertyValue("Id").ToString());
                mainSet.HasEditLink(editLinkFactory, followsConventions: false);
            }

            Func<EntityInstanceContext<MainEntity>, string> readLinkFactory;

            if (!sameLinksForEditAndRead)
            {
                readLinkFactory = (e) => CreateAbsoluteLink("/MainEntity/read/" + e.GetPropertyValue("Id").ToString());
                mainSet.HasReadLink(readLinkFactory, followsConventions: false);
            }

            EntityTypeConfiguration<MainEntity> main = mainSet.EntityType;

            main.HasKey<int>((e) => e.Id);
            main.Property<short>((e) => e.Int16);
            NavigationPropertyConfiguration mainToRelated = mainSet.EntityType.HasRequired((e) => e.Related);

            main.Action("DoAlways").ReturnsCollectionFromEntitySet<MainEntity>("MainEntity").HasActionLink((c) =>
                CreateAbsoluteUri("/MainEntity/DoAlways/" + c.GetPropertyValue("Id")),
                followsConventions: true);
            main.TransientAction("DoSometimes").ReturnsCollectionFromEntitySet<MainEntity>(
                "MainEntity").HasActionLink((c) =>
                    CreateAbsoluteUri("/MainEntity/DoSometimes/" + c.GetPropertyValue("Id")),
                    followsConventions: false);

            mainSet.HasNavigationPropertyLink(mainToRelated, (c, p) => new Uri("/MainEntity/RelatedEntity/" +
                c.GetPropertyValue("Id"), UriKind.Relative), followsConventions: true);

            EntitySetConfiguration<RelatedEntity> related = builder.EntitySet<RelatedEntity>("RelatedEntity");

            return builder.GetEdmModel();
        }

        private static HttpRequestMessage CreateRequest(string pathAndQuery, MediaTypeWithQualityHeaderValue accept)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, CreateAbsoluteUri(pathAndQuery));
            request.Headers.Accept.Add(accept);
            return request;
        }

        private static HttpRequestMessage CreateRequestWithDataServiceVersionHeaders(string pathAndQuery,
            MediaTypeWithQualityHeaderValue accept)
        {
            HttpRequestMessage request = CreateRequest(pathAndQuery, accept);
            AddDataServiceVersionHeaders(request);
            return request;
        }

        private class CustomFeedSerializer : ODataFeedSerializer
        {
            public CustomFeedSerializer(ODataSerializerProvider serializerProvider)
                : base(serializerProvider)
            {
            }

            public override ODataFeed CreateODataFeed(IEnumerable feedInstance, IEdmCollectionTypeReference feedType,
                ODataSerializerContext writeContext)
            {
                ODataFeed feed = base.CreateODataFeed(feedInstance, feedType, writeContext);
                feed.Atom().Title = new AtomTextConstruct { Kind = AtomTextConstructKind.Text, Text = "My amazing feed" };
                return feed;
            }
        }

        private class CustomSerializerProvider : DefaultODataSerializerProvider
        {
            public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
            {
                if (edmType.IsCollection() && edmType.AsCollection().ElementType().IsEntity())
                {
                    return new CustomFeedSerializer(this);
                }

                return base.GetEdmTypeSerializer(edmType);
            }
        }
    }

    public class MainEntity
    {
        public int Id { get; set; }

        public short Int16 { get; set; }

        public RelatedEntity Related { get; set; }
    }

    public class RelatedEntity
    {
        public int Id { get; set; }
    }

    public class MainEntityController : ODataController
    {
        public IEnumerable<MainEntity> Get()
        {
            MainEntity[] entities = new MainEntity[]
            {
                new MainEntity
                {
                    Id = 1,
                    Int16 = -1,
                    Related = new RelatedEntity
                    {
                        Id = 101
                    }
                },
                new MainEntity
                {
                    Id = 2,
                    Int16 = -2,
                    Related = new RelatedEntity
                    {
                        Id = 102
                    }
                }
            };

            return new PageResult<MainEntity>(entities, new Uri("aa:b"), 3);
        }
    }
}
