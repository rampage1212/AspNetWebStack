﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Formatter.Serialization.Models;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Atom;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Formatter.Serialization
{
    public class ODataFeedSerializerTests
    {
        IEdmModel _model;
        IEdmEntitySet _customerSet;
        Customer[] _customers;
        ODataFeedSerializer _serializer;
        IEdmCollectionTypeReference _customersType;
        ODataSerializerContext _writeContext;

        public ODataFeedSerializerTests()
        {
            _model = SerializationTestsHelpers.SimpleCustomerOrderModel();
            _customerSet = _model.FindDeclaredEntityContainer("Default.Container").FindEntitySet("Customers");
            _customers = new[] {
                new Customer()
                {
                    FirstName = "Foo",
                    LastName = "Bar",
                    ID = 10,
                },
                new Customer()
                {
                    FirstName = "Foo",
                    LastName = "Bar",
                    ID = 42,
                }
            };

            _customersType = new EdmCollectionTypeReference(
                    new EdmCollectionType(
                        new EdmEntityTypeReference(
                            _customerSet.ElementType,
                            isNullable: false)),
                    isNullable: false);

            _writeContext = new ODataSerializerContext() { EntitySet = _customerSet, Model = _model };
        }

        [Fact]
        public void Ctor_ThrowsArgumentNull_EdmType()
        {
            Assert.ThrowsArgumentNull(
                () => new ODataFeedSerializer(edmType: null, serializerProvider: new DefaultODataSerializerProvider()),
                "edmType");
        }

        [Fact]
        public void Ctor_ThrowsArgumentNull_SerializerProvider()
        {
            Assert.ThrowsArgumentNull(
                () => new ODataFeedSerializer(edmType: _customersType, serializerProvider: null),
                "serializerProvider");
        }

        [Fact]
        public void Ctor_Throws_TypeMustBeEntityCollection()
        {
            EdmComplexType complexType = new EdmComplexType("namespace", "name");
            EdmCollectionType collectionType = new EdmCollectionType(new EdmComplexTypeReference(complexType, isNullable: true));

            Assert.Throws<NotSupportedException>(
                () => new ODataFeedSerializer(new EdmCollectionTypeReference(collectionType, isNullable: false), new DefaultODataSerializerProvider()),
                "namespace.name is not a collection of type IEdmEntityType. The ODataFeedSerializer can serialize only entity collections.");
        }

        [Fact]
        public void Ctor_SetsProperty_EntityCollectionType()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.Equal(_customersType, serializer.EntityCollectionType);
        }

        [Fact]
        public void Ctor_SetsProperty_EntityType()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.Equal(_customersType.ElementType(), serializer.EntityType);
        }

        [Fact]
        public void WriteObject_ThrowsArgumentNull_MessageWriter()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.ThrowsArgumentNull(
                () => serializer.WriteObject(graph: null, messageWriter: null, writeContext: new ODataSerializerContext()),
                "messageWriter");
        }

        [Fact]
        public void WriteObject_ThrowsArgumentNull_WriteContext()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.ThrowsArgumentNull(
                () => serializer.WriteObject(graph: null, messageWriter: ODataTestUtil.GetMockODataMessageWriter(), writeContext: null),
                "writeContext");
        }

        [Fact]
        public void WriteObject_ThrowsEntitySetMissingDuringSerialization()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.Throws<SerializationException>(
                () => serializer.WriteObject(graph: null, messageWriter: ODataTestUtil.GetMockODataMessageWriter(), writeContext: new ODataSerializerContext()),
                "The related entity set could not be found from the OData path. The related entity set is required to serialize the payload.");
        }

        [Fact]
        public void WriteObjectInline_ThrowsArgumentNull_Writer()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.ThrowsArgumentNull(
                () => serializer.WriteObjectInline(graph: null, writer: null, writeContext: new ODataSerializerContext()),
                "writer");
        }

        [Fact]
        public void WriteObjectInline_ThrowsArgumentNull_WriteContext()
        {
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Assert.ThrowsArgumentNull(
                () => serializer.WriteObjectInline(graph: null, writer: new Mock<ODataWriter>().Object, writeContext: null),
                "writeContext");
        }

        [Fact]
        public void WriteObjectInline_Calls_CreateODataFeed()
        {
            // Arrange
            IEnumerable instance = new object[0];
            Mock<ODataFeedSerializer> serializer = new Mock<ODataFeedSerializer>(_customersType, new DefaultODataSerializerProvider());
            serializer.CallBase = true;
            serializer.Setup(s => s.CreateODataFeed(instance, _writeContext)).Returns(new ODataFeed()).Verifiable();

            // Act
            serializer.Object.WriteObjectInline(instance, new Mock<ODataWriter>().Object, _writeContext);

            // Assert
            serializer.Verify();
        }

        [Fact]
        public void WriteObjectInline_Writes_CreateODataFeedOutput()
        {
            // Arrange
            IEnumerable instance = new object[0];
            ODataFeed feed = new ODataFeed();
            Mock<ODataFeedSerializer> serializer = new Mock<ODataFeedSerializer>(_customersType, new DefaultODataSerializerProvider());
            serializer.CallBase = true;
            serializer.Setup(s => s.CreateODataFeed(instance, _writeContext)).Returns(feed);
            Mock<ODataWriter> writer = new Mock<ODataWriter>();
            writer.Setup(s => s.WriteStart(feed)).Verifiable();

            // Act
            serializer.Object.WriteObjectInline(instance, writer.Object, _writeContext);

            // Assert
            writer.Verify();
        }

        [Fact]
        public void WriteObjectInline_Throws_CannotSerializerNull_IfCreateODataFeedReturnsNull()
        {
            // Arrange
            IEnumerable instance = new object[0];
            Mock<ODataFeedSerializer> serializer = new Mock<ODataFeedSerializer>(_customersType, new DefaultODataSerializerProvider());
            serializer.CallBase = true;
            serializer.Setup(s => s.CreateODataFeed(instance, _writeContext)).Returns<ODataFeed>(null);

            // Act
            Assert.Throws<SerializationException>(
                () => serializer.Object.WriteObjectInline(instance, new Mock<ODataWriter>().Object, _writeContext),
                "Cannot serialize a null 'feed'.");
        }

        [Fact]
        public void WriteObjectInline_WritesEachEntityInstance()
        {
            // Arrange
            Mock<ODataEntrySerializer> customerSerializer = new Mock<ODataEntrySerializer>(_customersType.ElementType(), ODataPayloadKind.Entry);
            ODataSerializerProvider provider = ODataTestUtil.GetMockODataSerializerProvider(customerSerializer.Object);
            var mockWriter = new Mock<ODataWriter>();

            customerSerializer.Setup(s => s.WriteObjectInline(_customers[0], mockWriter.Object, _writeContext)).Verifiable();
            customerSerializer.Setup(s => s.WriteObjectInline(_customers[1], mockWriter.Object, _writeContext)).Verifiable();

            _serializer = new ODataFeedSerializer(_customersType, provider);

            // Act
            _serializer.WriteObjectInline(_customers, mockWriter.Object, _writeContext);

            // Assert
            customerSerializer.Verify();
        }

        [Fact]
        public void WriteObjectInline_Sets_InlineCount_OnWriteStart()
        {
            // Arrange
            IEnumerable instance = new object[0];
            ODataFeed feed = new ODataFeed { Count = 1000 };
            Mock<ODataFeedSerializer> serializer = new Mock<ODataFeedSerializer>(_customersType, new DefaultODataSerializerProvider());
            serializer.CallBase = true;
            serializer.Setup(s => s.CreateODataFeed(instance, _writeContext)).Returns(feed);
            var mockWriter = new Mock<ODataWriter>();

            mockWriter.Setup(m => m.WriteStart(It.Is<ODataFeed>(f => f.Count == 1000))).Verifiable();

            // Act
            serializer.Object.WriteObjectInline(instance, mockWriter.Object, _writeContext);

            // Assert
            mockWriter.Verify();
        }

        [Fact]
        public void WriteObjectInline_Sets_NextPageLink_OnWriteEnd()
        {
            // Arrange
            IEnumerable instance = new object[0];
            ODataFeed feed = new ODataFeed { NextPageLink = new Uri("http://nextlink.com/") };
            Mock<ODataFeedSerializer> serializer = new Mock<ODataFeedSerializer>(_customersType, new DefaultODataSerializerProvider());
            serializer.CallBase = true;
            serializer.Setup(s => s.CreateODataFeed(instance, _writeContext)).Returns(feed);
            var mockWriter = new Mock<ODataWriter>();

            mockWriter.Setup(m => m.WriteStart(It.Is<ODataFeed>(f => f.NextPageLink == null))).Verifiable();
            mockWriter
                .Setup(m => m.WriteEnd())
                .Callback(() =>
                {
                    Assert.Equal("http://nextlink.com/", feed.NextPageLink.AbsoluteUri);
                })
                .Verifiable();

            // Act
            serializer.Object.WriteObjectInline(instance, mockWriter.Object, _writeContext);

            // Assert
            mockWriter.Verify();
        }

        [Fact]
        public void CreateODataFeed_Sets_InlineCountForPageResult()
        {
            // Arrange
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Uri expectedNextLink = new Uri("http://nextlink.com");
            long expectedInlineCount = 1000;

            var result = new PageResult<Customer>(_customers, expectedNextLink, expectedInlineCount);

            // Act
            ODataFeed feed = serializer.CreateODataFeed(result, new ODataSerializerContext());

            // Assert
            Assert.Equal(1000, feed.Count);
        }

        [Fact]
        public void CreateODataFeed_Sets_NextPageLinkForPageResult()
        {
            // Arrange
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Uri expectedNextLink = new Uri("http://nextlink.com");
            long expectedInlineCount = 1000;

            var result = new PageResult<Customer>(_customers, expectedNextLink, expectedInlineCount);

            // Act
            ODataFeed feed = serializer.CreateODataFeed(result, new ODataSerializerContext());

            // Assert
            Assert.Equal(expectedNextLink, feed.NextPageLink);
        }

        [Fact]
        public void CreateODataFeed_Sets_InlineCountFromContext()
        {
            // Arrange
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            long expectedInlineCount = 1000;

            var result = new object[0];

            // Act
            ODataFeed feed = serializer.CreateODataFeed(result, new ODataSerializerContext { InlineCount = expectedInlineCount });

            // Assert
            Assert.Equal(expectedInlineCount, feed.Count);
        }

        [Fact]
        public void CreateODataFeed_Sets_NextPageLinkFromContext()
        {
            // Arrange
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Uri expectedNextLink = new Uri("http://nextlink.com");

            var result = new object[0];

            // Act
            ODataFeed feed = serializer.CreateODataFeed(result, new ODataSerializerContext { NextPageLink = expectedNextLink });

            // Assert
            Assert.Equal(expectedNextLink, feed.NextPageLink);
        }

        [Fact]
        public void CreateODataFeed_Sets_FeedSelfLink()
        {
            // Arrange
            var feedInstance = new object[0];
            ODataSerializerContext writeContext = new ODataSerializerContext { EntitySet = _customerSet, Model = _model, Request = new HttpRequestMessage() };
            writeContext.Url = new UrlHelper(writeContext.Request);
            ODataFeedSerializer serializer = new ODataFeedSerializer(_customersType, new DefaultODataSerializerProvider());
            Uri feedSelfLink = new Uri("http://feed_self_link/");
            EntitySetLinkBuilderAnnotation linkBuilder = new MockEntitySetLinkBuilderAnnotation
            {
                FeedSelfLinkBuilder = (context) =>
                    {
                        Assert.Equal(_customerSet, context.EntitySet);
                        Assert.Equal(feedInstance, context.FeedInstance);
                        Assert.Equal(writeContext.Request, context.Request);
                        Assert.Equal(writeContext.Url, context.Url);
                        return feedSelfLink;
                    }
            };
            _model.SetEntitySetLinkBuilderAnnotation(_customerSet, linkBuilder);

            // Act
            ODataFeed feed = serializer.CreateODataFeed(feedInstance, writeContext);

            // Assert
            AtomFeedMetadata feedMetadata = feed.GetAnnotation<AtomFeedMetadata>();
            Assert.Equal(feedSelfLink, feedMetadata.SelfLink.Href);
            Assert.Equal("self", feedMetadata.SelfLink.Relation);
        }
    }
}
