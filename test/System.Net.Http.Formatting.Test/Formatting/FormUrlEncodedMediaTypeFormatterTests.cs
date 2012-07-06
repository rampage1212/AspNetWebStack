﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Net.Http.Formatting.DataSets;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.TestCommon;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;
using Assert = Microsoft.TestCommon.AssertEx;

namespace System.Net.Http.Formatting
{
    public class FormUrlEncodedMediaTypeFormatterTests
    {
        private const int MinBufferSize = 256;
        private const int DefaultBufferSize = 32 * 1024;
        private const int DefaultMaxDepth = 1024;

        [Fact]
        public void TypeIsCorrect()
        {
            Assert.Type.HasProperties(typeof(FormUrlEncodedMediaTypeFormatter), TypeAssert.TypeProperties.IsPublicVisibleClass);
        }

        [Fact]
        public void SupportedMediaTypes_HeaderValuesAreNotSharedBetweenInstances()
        {
            var formatter1 = new FormUrlEncodedMediaTypeFormatter();
            var formatter2 = new FormUrlEncodedMediaTypeFormatter();

            foreach (MediaTypeHeaderValue mediaType1 in formatter1.SupportedMediaTypes)
            {
                MediaTypeHeaderValue mediaType2 = formatter2.SupportedMediaTypes.Single(m => m.Equals(mediaType1));
                Assert.NotSame(mediaType1, mediaType2);
            }
        }

        [Fact]
        public void SupportEncodings_ValuesAreNotSharedBetweenInstances()
        {
            var formatter1 = new FormUrlEncodedMediaTypeFormatter();
            var formatter2 = new FormUrlEncodedMediaTypeFormatter();

            foreach (Encoding encoding1 in formatter1.SupportedEncodings)
            {
                Encoding encoding2 = formatter2.SupportedEncodings.Single(e => e.Equals(encoding1));
                Assert.NotSame(encoding1, encoding2);
            }
        }

        [Theory]
        [TestDataSet(typeof(HttpTestData), "StandardFormUrlEncodedMediaTypes")]
        public void Constructor(MediaTypeHeaderValue mediaType)
        {
            FormUrlEncodedMediaTypeFormatter formatter = new FormUrlEncodedMediaTypeFormatter();
            Assert.True(formatter.SupportedMediaTypes.Contains(mediaType), String.Format("SupportedMediaTypes should have included {0}.", mediaType.ToString()));
        }

        [Fact]
        public void DefaultMediaTypeReturnsApplicationJson()
        {
            MediaTypeHeaderValue mediaType = FormUrlEncodedMediaTypeFormatter.DefaultMediaType;
            Assert.NotNull(mediaType);
            Assert.Equal("application/x-www-form-urlencoded", mediaType.MediaType);
        }

        [Fact]
        public void ReadBufferSize_RoundTrips()
        {
            Assert.Reflection.IntegerProperty(
                new FormUrlEncodedMediaTypeFormatter(),
                c => c.ReadBufferSize,
                expectedDefaultValue: 32 * 1024,
                minLegalValue: 256,
                illegalLowerValue: 255,
                maxLegalValue: null,
                illegalUpperValue: null,
                roundTripTestValue: 1024);
        }

        [Fact]
        public void MaxDepthReturnsCorrectValue()
        {
            Assert.Reflection.IntegerProperty(
                 new FormUrlEncodedMediaTypeFormatter(),
                 f => f.MaxDepth,
                 expectedDefaultValue: 256,
                 minLegalValue: 1,
                 illegalLowerValue: 0,
                 maxLegalValue: null,
                 illegalUpperValue: null,
                 roundTripTestValue: 10);
        }

        [Fact]
        public void ReadDeeplyNestedObjectThrows()
        {
            FormUrlEncodedMediaTypeFormatter formatter = new FormUrlEncodedMediaTypeFormatter() { MaxDepth = 100 };

            StringContent content = new StringContent(GetDeeplyNestedObject(125));

            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            Assert.ThrowsArgument(
                () => formatter.ReadFromStreamAsync(typeof(JToken), content.ReadAsStreamAsync().Result, content, null).Result,
                null);
        }

        [Fact]
        public void ReadDeeplyNestedObjectWithBigDepthQuotaWorks()
        {
            FormUrlEncodedMediaTypeFormatter formatter = new FormUrlEncodedMediaTypeFormatter() { MaxDepth = 150 };

            StringContent content = new StringContent(GetDeeplyNestedObject(125));

            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            JToken result = (JToken)formatter.ReadFromStreamAsync(typeof(JToken), content.ReadAsStreamAsync().Result, content, null).Result;
            Assert.NotNull(result);
        }

        static string GetDeeplyNestedObject(int depth)
        {
            StringBuilder sb = new StringBuilder("a");
            for (int i = 0; i < depth; i++)
            {
                sb.Append("[a]");
            }
            sb.Append("=1");
            return sb.ToString();
        }

        [Fact]
        public void CanReadTypeThrowsOnNull()
        {
            TestFormUrlEncodedMediaTypeFormatter formatter = new TestFormUrlEncodedMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.CanReadType(null); }, "type");
        }

        [Theory]
        [InlineData(typeof(FormDataCollection))]
        [InlineData(typeof(JToken))]
        public void CanReadTypeTrue(Type type)
        {
            TestFormUrlEncodedMediaTypeFormatter formatter = new TestFormUrlEncodedMediaTypeFormatter();

            Assert.True(formatter.CanReadType(type));
        }


        [Theory]
        [TestDataSet(typeof(CommonUnitTestDataSets), "RepresentativeValueAndRefTypeTestDataCollection")]
        public void CanReadTypeReturnsFalse(Type variationType, object testData)
        {
            TestFormUrlEncodedMediaTypeFormatter formatter = new TestFormUrlEncodedMediaTypeFormatter();

            Assert.False(formatter.CanReadType(variationType));

            // Ask a 2nd time to probe whether the cached result is treated the same
            Assert.False(formatter.CanReadType(variationType));
        }


        [Fact]
        public void CanWriteTypeThrowsOnNull()
        {
            TestFormUrlEncodedMediaTypeFormatter formatter = new TestFormUrlEncodedMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.CanWriteType(null); }, "type");
        }

        [Theory]
        [TestDataSet(typeof(CommonUnitTestDataSets), "RepresentativeValueAndRefTypeTestDataCollection")]
        public void CanWriteTypeReturnsFalse(Type variationType, object testData)
        {
            TestFormUrlEncodedMediaTypeFormatter formatter = new TestFormUrlEncodedMediaTypeFormatter();

            Assert.False(formatter.CanWriteType(variationType), "formatter should have returned false.");

            // Ask a 2nd time to probe whether the cached result is treated the same
            Assert.False(formatter.CanWriteType(variationType), "formatter should have returned false on 2nd try as well.");
        }

        [Fact]
        public void ReadFromStreamThrowsOnNull()
        {
            TestFormUrlEncodedMediaTypeFormatter formatter = new TestFormUrlEncodedMediaTypeFormatter();
            Assert.ThrowsArgumentNull(() => { formatter.ReadFromStreamAsync(null, Stream.Null, null, null); }, "type");
            Assert.ThrowsArgumentNull(() => { formatter.ReadFromStreamAsync(typeof(object), null, null, null); }, "readStream");
        }

        [Fact]
        public void WriteToStreamAsyncThrowsNotImplemented()
        {
            FormUrlEncodedMediaTypeFormatter formatter = new FormUrlEncodedMediaTypeFormatter();
            Assert.Throws<NotSupportedException>(
                () => formatter.WriteToStreamAsync(typeof(object), new object(), Stream.Null, null, null),
                "The media type formatter of type 'FormUrlEncodedMediaTypeFormatter' does not support writing because it does not implement the WriteToStreamAsync method.");
        }

        public class TestFormUrlEncodedMediaTypeFormatter : FormUrlEncodedMediaTypeFormatter
        {
            public new bool CanReadType(Type type)
            {
                return base.CanReadType(type);
            }

            public new bool CanWriteType(Type type)
            {
                return base.CanWriteType(type);
            }
        }
    }
}
