﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Builder.TestModels;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.OData.Routing;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Formatter
{
    public class ODataTestUtil
    {
        private static IEdmModel _model;

        public const string Version4NumberString = "4.0";
        public static MediaTypeHeaderValue ApplicationJsonMediaType = MediaTypeHeaderValue.Parse("application/json");
        public static MediaTypeHeaderValue ApplicationAtomMediaType = MediaTypeHeaderValue.Parse("application/atom+xml");
        public static MediaTypeWithQualityHeaderValue ApplicationJsonMediaTypeWithQuality = MediaTypeWithQualityHeaderValue.Parse("application/json");
        public static MediaTypeWithQualityHeaderValue ApplicationAtomMediaTypeWithQuality = MediaTypeWithQualityHeaderValue.Parse("application/atom+xml");

        public static void VerifyResponse(HttpContent responseContent, string expected)
        {
            string response = responseContent.ReadAsStringAsync().Result;
            Regex updatedRegEx = new Regex("<updated>*.*</updated>");
            response = updatedRegEx.Replace(response, "<updated>UpdatedTime</updated>");
            Assert.Xml.Equal(expected, response);
        }

        public static void VerifyJsonResponse(HttpContent actualContent, string expected)
        {
            string actual = actualContent.ReadAsStringAsync().Result;
            JsonAssert.Equal(expected, actual);
        }

        public static HttpRequestMessage GenerateRequestMessage(Uri address, bool isAtom)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, address);
            MediaTypeWithQualityHeaderValue mediaType = isAtom ? ApplicationAtomMediaTypeWithQuality : ApplicationJsonMediaTypeWithQuality;
            requestMessage.Headers.Accept.Add(mediaType);
            requestMessage.Headers.Add("OData-Version", "4.0");
            requestMessage.Headers.Add("OData-MaxVersion", "4.0");
            return requestMessage;
        }

        public static string GetDataServiceVersion(HttpContentHeaders headers)
        {
            string dataServiceVersion = null;
            IEnumerable<string> values;
            if (headers.TryGetValues("OData-Version", out values))
            {
                dataServiceVersion = values.FirstOrDefault();
            }
            return dataServiceVersion;
        }

        public static IEdmModel GetEdmModel()
        {
            if (_model == null)
            {
                ODataModelBuilder model = new ODataModelBuilder();

                var color = model.EnumType<Color>();
                color.Member(Color.Red);
                color.Member(Color.Green);
                color.Member(Color.Blue);

                var people = model.EntitySet<FormatterPerson>("People");
                people.HasFeedSelfLink(context => new Uri(context.Url.ODataLink(new EntitySetPathSegment(
                    context.EntitySet))));
                people.HasIdLink(context =>
                    {
                        return context.Url.ODataLink(
                            new EntitySetPathSegment(context.EntitySet),
                            new KeyValuePathSegment(context.GetPropertyValue("PerId").ToString()));
                    },
                    followsConventions: false);

                var person = people.EntityType;
                person.HasKey(p => p.PerId);
                person.Property(p => p.Age);
                person.Property(p => p.MyGuid);
                person.Property(p => p.Name);
                person.EnumProperty(p => p.FavoriteColor);
                person.ComplexProperty<FormatterOrder>(p => p.Order);

                var order = model.ComplexType<FormatterOrder>();
                order.Property(o => o.OrderAmount);
                order.Property(o => o.OrderName);

                _model = model.GetEdmModel();
            }

            return _model;
        }

        public static ODataMessageWriter GetMockODataMessageWriter()
        {
            MockODataRequestMessage requestMessage = new MockODataRequestMessage();
            return new ODataMessageWriter(requestMessage);
        }

        public static ODataMessageReader GetMockODataMessageReader()
        {
            MockODataRequestMessage requestMessage = new MockODataRequestMessage();
            return new ODataMessageReader(requestMessage);
        }

        public static ODataSerializerProvider GetMockODataSerializerProvider(ODataEdmTypeSerializer serializer)
        {
            Mock<ODataSerializerProvider> serializerProvider = new Mock<ODataSerializerProvider>();
            serializerProvider.Setup(sp => sp.GetEdmTypeSerializer(It.IsAny<IEdmTypeReference>())).Returns(serializer);
            return serializerProvider.Object;
        }
    }

    public class FormatterPerson
    {
        public int Age { get; set; }
        public Guid MyGuid { get; set; }
        public string Name { get; set; }
        public FormatterOrder Order { get; set; }
        public Color FavoriteColor { get; set; }
        [Key]
        public int PerId { get; set; }
    }

    public class FormatterOrder
    {
        public int OrderAmount { get; set; }
        public string OrderName { get; set; }
    }
}
