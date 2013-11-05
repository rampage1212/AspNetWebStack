﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting.DataSets;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TestCommon;
using Microsoft.TestCommon.Types;
using Newtonsoft.Json;

namespace System.Net.Http.Formatting
{
    public class BsonMediaTypeFormatterTests : MediaTypeFormatterTestBase<BsonMediaTypeFormatter>
    {
        // Exclude IEnumerable<T> and IQueryable<T> to avoid attempts to round trip values that are known to cause
        // trouble in deserialization e.g. base IEnumerable<T>.  BSON reader won't know how to construct such types.
        private const TestDataVariations RoundTripVariations = (TestDataVariations.All | TestDataVariations.WithNull) &
            ~(TestDataVariations.AsIEnumerable | TestDataVariations.AsIQueryable);

        // Copied from TaskAssert.cs
        private static int timeOutMs =
            Debugger.IsAttached ? TimeoutConstant.DefaultTimeout : TimeoutConstant.DefaultTimeout * 10;

        /// <summary>
        /// Provide test data for round trip tests.  Avoid types BSON does not support.
        /// <remarks>
        /// BSON does not support some unsigned integers as well as having issues with <see cref="decimal"/>.
        /// <list type="bullet">
        /// <item><description>
        /// BSON writer attempts to write an unsigned int or long as a signed integer of the same size e.g. it writes
        /// an <see cref="uint"/> as an <see cref="int"/> and thus can only write values less than
        /// <c>Int32.MaxValue</c>.  BSON writer fortunately uses an <see cref="int"/> for <see cref="sbyte"/>,
        /// <see cref="byte"/>, <see cref="short"/>, and <see cref="ushort"/> values.
        /// </description></item>
        /// <item><description>
        /// BSON successfully writes all <see cref="decimal"/> values as <see cref="double"/>.  But BSON reader may not
        /// be able to be convert the <see cref="double"/> value back e.g. <c>Decimal.MaxValue</c> loses precision when
        /// written and is rounded up -- to an invalid <see cref="decimal"/> value.
        /// </description></item>
        /// <item><description>
        /// BSON (as well as JSON and default <c>ToString()</c> in the <see cref="DateTime"/> case) loses information
        /// when writing <see cref="DateTime"/> and <see cref="DateTimeOffset"/> values.  BSON writer uses a UTC
        /// datetime value in both cases -- losing <c>Kind</c> and <c>Offset</c> property values, respectively.
        /// (<see cref="DateTime"/> values are not currently included in
        /// <see cref="CommonUnitTestDataSets.ValueAndRefTypeTestDataCollection"/> but exclude
        /// <see cref="CommonUnitTestDataSets.DateTimes"/> to be safe.)
        /// </description></item>
        /// <item><description>
        /// BSON readers and writers appear to round trip <see cref="ISerializableType"/> values successfully.  However
        /// <see cref="ISerializableType"/> does not implement <see cref="IEquatable{T}"/> or
        /// <see cref="IComparable{T}"/> and thus <see cref="Assert.Equals()"/> fails.
        /// </description></item>
        /// </list>
        /// </remarks>
        /// </summary>
        public static IEnumerable<TestData> ValueAndRefTypeTestDataCollection
        {
            get
            {
                return CommonUnitTestDataSets.ValueAndRefTypeTestDataCollection.Except(
                    new TestData[] {
                        CommonUnitTestDataSets.Uints,
                        CommonUnitTestDataSets.Ulongs,
                        CommonUnitTestDataSets.DateTimeOffsets,
                        CommonUnitTestDataSets.DateTimes,
                        CommonUnitTestDataSets.Decimals,
                        CommonUnitTestDataSets.ISerializableTypes,
                    });
            }
        }


        public override IEnumerable<MediaTypeHeaderValue> ExpectedSupportedMediaTypes
        {
            get { return HttpTestData.StandardBsonMediaTypes; }
        }

        public override IEnumerable<Encoding> ExpectedSupportedEncodings
        {
            get { return HttpTestData.StandardEncodings; }
        }

        public override byte[] ExpectedSampleTypeByteRepresentation
        {
            get
            {
                return new byte[17]
                {
                    // Little-endian length
                    17, 0, 0, 0,
                    // Opcode indicating a 32bit integer
                    0x10,
                    // Field name as a C string
                    (byte)'N', (byte)'u', (byte)'m', (byte)'b', (byte)'e', (byte)'r', 0,
                    // Little-endian value
                    42, 0, 0, 0,
                    // BSON document terminator
                    0,
                };
            }
        }

        [Theory]
        [TestDataSet(typeof(HttpTestData), "ReadAndWriteCorrectCharacterEncoding")]
        public override Task ReadFromStreamAsync_UsesCorrectCharacterEncoding(string content, string encoding, bool isDefaultEncoding)
        {
            // Arrange
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();

            string mediaType = string.Format("application/bson; charset={0}", encoding);
            Encoding enc = CreateOrGetSupportedEncoding(formatter, encoding, isDefaultEncoding);

            // Test roundtrip in this case, not against expectations
            byte[] sourceData;
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.WriteToStream(typeof(string), content, stream, enc);
                sourceData = stream.ToArray();
            }

            // Further Arrange, Act & Assert
            return ReadContentUsingCorrectCharacterEncodingHelper(formatter, content, sourceData, mediaType);
        }

        [Theory]
        [TestDataSet(typeof(HttpTestData), "ReadAndWriteCorrectCharacterEncoding")]
        public override Task WriteToStreamAsync_UsesCorrectCharacterEncoding(string content, string encoding, bool isDefaultEncoding)
        {
            // Arrange
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();

            string mediaType = string.Format("application/bson; charset={0}", encoding);
            Encoding enc = Encoding.GetEncoding(encoding);

            // Test sync and async approaches match, not against expectations
            // See ReadFromStreamAsync_UsesCorrectCharacterEncoding for roundtrip test
            byte[] expectedData;
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.WriteToStream(typeof(string), content, stream, enc);
                expectedData = stream.ToArray();
            }

            // Further Arrange, Act & Assert
            return WriteContentUsingCorrectCharacterEncodingHelper(formatter, content, expectedData, mediaType);
        }

        [Fact]
        void CopyConstructor()
        {
            // Arrange
            TestBsonMediaTypeFormatter formatter = new TestBsonMediaTypeFormatter()
            {
#if !NETFX_CORE // MaxDepth is not supported in portable library
                MaxDepth = 42,
#endif
            };

            // Replace serializable settings and switch one property's value
            JsonSerializerSettings oldSettings = formatter.SerializerSettings;
            formatter.SerializerSettings = formatter.CreateDefaultSerializerSettings();
            formatter.SerializerSettings.CheckAdditionalContent = !formatter.SerializerSettings.CheckAdditionalContent;

            // Act
            TestBsonMediaTypeFormatter derivedFormatter = new TestBsonMediaTypeFormatter(formatter);

            // Assert
#if !NETFX_CORE // MaxDepth is not supported in portable library
            Assert.Equal(formatter.MaxDepth, derivedFormatter.MaxDepth);
#endif
            Assert.NotSame(oldSettings, formatter.SerializerSettings);
            Assert.NotEqual(oldSettings.CheckAdditionalContent, formatter.SerializerSettings.CheckAdditionalContent);
            Assert.Same(formatter.SerializerSettings, derivedFormatter.SerializerSettings);
            Assert.Same(formatter.SerializerSettings.ContractResolver, derivedFormatter.SerializerSettings.ContractResolver);
        }

#if !NETFX_CORE // MaxDepth is not supported in portable library
        [Fact]
        public void MaxDepth_RoundTrips()
        {
            // Arrange & Act & Assert
            Assert.Reflection.IntegerProperty(
                new BsonMediaTypeFormatter(),
                c => c.MaxDepth,
                expectedDefaultValue: 256,
                minLegalValue: 1,
                illegalLowerValue: 0,
                maxLegalValue: null,
                illegalUpperValue: null,
                roundTripTestValue: 256);
        }
#endif

        [Theory]
        [TestDataSet(typeof(CommonUnitTestDataSets), "RepresentativeValueAndRefTypeTestDataCollection")]
        public void CanReadType_ReturnsExpectedValues(Type variationType, object testData)
        {
            // Arrange
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();

            // Act & Assert
            Assert.True(formatter.CanReadType(variationType));
        }

        [Theory]
        [TestDataSet(typeof(CommonUnitTestDataSets), "RepresentativeValueAndRefTypeTestDataCollection")]
        public void CanWriteType_ReturnsExpectedValues(Type variationType, object testData)
        {
            // Arrange
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();

            // Act & Assert
            Assert.True(formatter.CanWriteType(variationType));
        }

        [Fact]
        public void FormatterThrowsOnWriteWhenOverridenCreateFails()
        {
            // Arrange
            TestBsonMediaTypeFormatter formatter = new TestBsonMediaTypeFormatter
            {
                ThrowExceptionOnCreate = true,
            };

            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Action action = () => formatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null).Wait(timeOutMs);
            Assert.Throws<Exception>(action, "Throwing exception directly, since it needs to get caught by a catch all");

            Assert.False(formatter.WasCreateJsonReaderCalled);
            Assert.True(formatter.WasCreateJsonWriterCalled);
        }

        [Fact]
        public void FormatterThrowsOnWriteWhenOverridenCreateReturnsNull()
        {
            // Arrange
            TestBsonMediaTypeFormatter formatter = new TestBsonMediaTypeFormatter
            {
                ReturnNullOncreate = true,
            };

            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Action action = () => formatter.WriteToStreamAsync(typeof(SampleType), new SampleType(), memoryStream, content, transportContext: null).Wait(timeOutMs);
            Assert.Throws<InvalidOperationException>(action, "The 'CreateJsonWriter' method returned null. It must return a JSON writer instance.");

            Assert.False(formatter.WasCreateJsonReaderCalled);
            Assert.True(formatter.WasCreateJsonWriterCalled);
        }

        [Fact]
        public void FormatterThrowsOnWriteWithInvalidContent()
        {
            // Arrange (set up to serialize Int32.MaxValue + 1 as an UInt32; can't be done since serialization uses an Int32
            Type variationType = typeof(uint);
            uint testData = (uint)Int32.MaxValue + 1u;
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();
            HttpContent content = new StringContent(String.Empty);
            MemoryStream stream = new MemoryStream();

            // Act & Assert
            // Note error message is not quite correct: BSON supports byte, ushort, and smaller uint / ulong values.
            Assert.Throws<JsonWriterException>(
                () => formatter.WriteToStreamAsync(variationType, testData, stream, content, transportContext: null).Wait(timeOutMs),
                "Value is too large to fit in a signed 32 bit integer. BSON does not support unsigned values. Path ''.");
        }

        [Fact]
        public void FormatterThrowsOnReadWhenOverridenCreateFails()
        {
            // Arrange
            TestBsonMediaTypeFormatter formatter = new TestBsonMediaTypeFormatter
            {
                ThrowExceptionOnCreate = true,
            };

            byte[] array = Encoding.UTF8.GetBytes("foo");
            MemoryStream memoryStream = new MemoryStream(array);

            HttpContent content = new StringContent("foo");

            // Act & Assert
            Action action = () => formatter.ReadFromStreamAsync(typeof(SampleType), memoryStream, content, null).Wait(timeOutMs);
            Assert.Throws<Exception>(action, "Throwing exception directly, since it needs to get caught by a catch all");

            Assert.True(formatter.WasCreateJsonReaderCalled);
            Assert.False(formatter.WasCreateJsonWriterCalled);
        }

        [Fact]
        public void FormatterThrowsOnReadWhenOverridenCreateReturnsNull()
        {
            // Arrange
            TestBsonMediaTypeFormatter formatter = new TestBsonMediaTypeFormatter
            {
                ReturnNullOncreate = true,
            };

            byte[] array = Encoding.UTF8.GetBytes("foo");
            MemoryStream memoryStream = new MemoryStream(array);

            HttpContent content = new StringContent("foo");

            // Act & Assert
            Action action = () => formatter.ReadFromStreamAsync(typeof(SampleType), memoryStream, content, null).Wait(timeOutMs);
            Assert.Throws<InvalidOperationException>(action, "The 'CreateJsonReader' method returned null. It must return a JSON reader instance.");

            Assert.True(formatter.WasCreateJsonReaderCalled);
            Assert.False(formatter.WasCreateJsonWriterCalled);
        }

        [Fact]
        public void FormatterThrowsOnReadWithInvalidContent()
        {
            // Arrange (serialize Decimal.MaxValue; can't be read back since serialization uses rounded Double)
            Type variationType = typeof(decimal);
            decimal testData = Decimal.MaxValue;
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();
            HttpContent content = new StringContent(String.Empty);
            HttpContentHeaders contentHeaders = content.Headers;
            MemoryStream stream = new MemoryStream();

            formatter.WriteToStreamAsync(variationType, testData, stream, content, transportContext: null).Wait(timeOutMs);

            contentHeaders.ContentLength = stream.Length;
            stream.Flush();
            stream.Seek(0L, SeekOrigin.Begin);

            // Act & Assert
            Assert.Throws<OverflowException>(
                () => formatter.ReadFromStreamAsync(variationType, stream, content, null).Wait(timeOutMs),
                "Value was either too large or too small for a Decimal.");
        }

        [Theory]
        [InlineData(typeof(IList<string>))]
        [InlineData(typeof(IDictionary<string, object>))]
        public void UseBsonFormatterWithNullCollections(Type type)
        {
            // Arrange
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();
            MemoryStream memoryStream = new MemoryStream();
            HttpContent content = new StringContent(String.Empty);

            // Act & Assert
            Assert.Task.Succeeds(formatter.WriteToStreamAsync(type, null, memoryStream, content, transportContext: null));
            memoryStream.Position = 0;
            string serializedString = new StreamReader(memoryStream).ReadToEnd();
            Assert.Empty(serializedString);
        }

        [Theory]
        [TestDataSet(typeof(BsonMediaTypeFormatterTests), "ValueAndRefTypeTestDataCollection", RoundTripVariations)]
        public void ReadFromStreamAsync_RoundTripsWriteToStreamAsync(Type variationType, object testData)
        {
            // Arrange
            BsonMediaTypeFormatter formatter = new BsonMediaTypeFormatter();
            HttpContent content = new StringContent(String.Empty);
            HttpContentHeaders contentHeaders = content.Headers;
            object readObj = null;

            // Act & Assert
            Assert.Stream.WriteAndRead(
                stream =>
                {
                    Assert.Task.Succeeds(formatter.WriteToStreamAsync(variationType, testData, stream, content, transportContext: null));
                    contentHeaders.ContentLength = stream.Length;
                },
                stream => readObj = Assert.Task.SucceedsWithResult(formatter.ReadFromStreamAsync(variationType, stream, content, null)));
            Assert.Equal(testData, readObj);
        }

        private class TestBsonMediaTypeFormatter : BsonMediaTypeFormatter
        {
            public TestBsonMediaTypeFormatter() : base()
            {
            }

            public TestBsonMediaTypeFormatter(TestBsonMediaTypeFormatter formatter) : base(formatter)
            {
            }

            public bool ReturnNullOncreate { get; set; }
            public bool ThrowExceptionOnCreate { get; set; }
            public bool WasCreateJsonReaderCalled { get; private set; }
            public bool WasCreateJsonWriterCalled { get; private set; }

            public override JsonReader CreateJsonReader(Type type, Stream readStream, Encoding effectiveEncoding)
            {
                WasCreateJsonReaderCalled = true;
                if (ReturnNullOncreate)
                {
                    return null;
                }

                if (ThrowExceptionOnCreate)
                {
                    throw new Exception("Throwing exception directly, since it needs to get caught by a catch all");
                }

                return base.CreateJsonReader(type, readStream, effectiveEncoding);
            }

            public override JsonWriter CreateJsonWriter(Type type, Stream writeStream, Encoding effectiveEncoding)
            {
                WasCreateJsonWriterCalled = true;
                if (ReturnNullOncreate)
                {
                    return null;
                }

                if (ThrowExceptionOnCreate)
                {
                    throw new Exception("Throwing exception directly, since it needs to get caught by a catch all");
                }

                return base.CreateJsonWriter(type, writeStream, effectiveEncoding);
            }
        }
    }
}
