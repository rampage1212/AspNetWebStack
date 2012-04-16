﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Xunit.Extensions;
using Assert = Microsoft.TestCommon.AssertEx;

namespace System.Web.Http.Tracing.Tracers
{
    public class MediaTypeFormatterTracerTest
    {
        [Fact]
        public void CanReadType_Calls_Inner()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            Type randomType = typeof(string);
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>();
            mockFormatter.Setup(f => f.CanReadType(randomType)).Returns(true).Verifiable();
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, new TestTraceWriter(), request);

            // Act
            bool valueReturned = tracer.CanReadType(randomType);

            // Assert
            Assert.True(valueReturned);
            mockFormatter.Verify();
        }

        [Fact]
        public void CanWriteType_Calls_Inner()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            Type randomType = typeof(string);
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>();
            mockFormatter.Setup(f => f.CanWriteType(randomType)).Returns(true).Verifiable();
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, new TestTraceWriter(), request);

            // Act
            bool valueReturned = tracer.CanWriteType(randomType);

            // Assert
            Assert.True(valueReturned);
            mockFormatter.Verify();
        }

        [Fact]
        public void GetPerRequestFormatterInstance_Calls_Inner_And_Wraps_Tracer_Around_It()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            Type randomType = typeof(string);
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("plain/text");
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>();
            MediaTypeFormatter formatterObject = mockFormatter.Object;

            mockFormatter.Setup(f => f.GetPerRequestFormatterInstance(randomType, request, mediaType)).Returns(formatterObject).Verifiable();
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(formatterObject, new TestTraceWriter(), request);

            // Act
            MediaTypeFormatter valueReturned = tracer.GetPerRequestFormatterInstance(randomType, request, mediaType);

            // Assert
            MediaTypeFormatterTracer tracerReturned = Assert.IsType<MediaTypeFormatterTracer>(valueReturned);
            Assert.Same(formatterObject, tracerReturned.InnerFormatter);
            mockFormatter.Verify();
        }

        [Fact]
        public void SetDefaultContentHeaders_Calls_Inner()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            Type randomType = typeof(string);
            HttpContentHeaders contentHeaders = new StringContent("").Headers;
            string mediaType = "plain/text";
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>();
            MediaTypeFormatter formatterObject = mockFormatter.Object;

            mockFormatter.Setup(f => f.SetDefaultContentHeaders(randomType, contentHeaders, mediaType)).Verifiable();
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(formatterObject, new TestTraceWriter(), request);

            // Act
            tracer.SetDefaultContentHeaders(randomType, contentHeaders, mediaType);

            // Assert
            mockFormatter.Verify();
        }

        [Fact]
        public void SupportedMediaTypes_Uses_Inners()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/fake");          
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            MediaTypeFormatter innerFormatter = mockFormatter.Object;
            innerFormatter.SupportedMediaTypes.Clear();
            innerFormatter.SupportedMediaTypes.Add(mediaType);
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(innerFormatter, new TestTraceWriter(), request);

            // Act & Assert
            Assert.Equal(innerFormatter.SupportedMediaTypes, tracer.SupportedMediaTypes);
        }

        [Fact]
        public void MediaTypeMappings_Uses_Inners()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/fake");
            Mock<MediaTypeMapping> mockMapping = new Mock<MediaTypeMapping>(mediaType);
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            MediaTypeFormatter innerFormatter = mockFormatter.Object;
            innerFormatter.MediaTypeMappings.Clear();
            innerFormatter.MediaTypeMappings.Add(mockMapping.Object);
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(innerFormatter, new TestTraceWriter(), request);

            // Act & Assert
            Assert.Equal(innerFormatter.MediaTypeMappings, tracer.MediaTypeMappings);
        }

        [Fact]
        public void SupportedEncodings_Uses_Inners()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            Mock<Encoding> mockEncoding = new Mock<Encoding>();
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            MediaTypeFormatter innerFormatter = mockFormatter.Object;
            innerFormatter.SupportedEncodings.Clear();
            innerFormatter.SupportedEncodings.Add(mockEncoding.Object);
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(innerFormatter, new TestTraceWriter(), request);

            // Act & Assert
            Assert.Equal(innerFormatter.SupportedEncodings, tracer.SupportedEncodings);
        }

        [Fact]
        public void RequiredMemberSelector_Uses_Inners()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            Mock<IRequiredMemberSelector> mockSelector = new Mock<IRequiredMemberSelector>();
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            MediaTypeFormatter innerFormatter = mockFormatter.Object;
            innerFormatter.RequiredMemberSelector = mockSelector.Object;
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(innerFormatter, new TestTraceWriter(), request);

            // Act & Assert
            Assert.Equal(innerFormatter.RequiredMemberSelector, tracer.RequiredMemberSelector);
        }

        [Fact]
        public void ReadFromStreamAsync_Traces()
        {
            // Arrange
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            mockFormatter.Setup(
                f => f.ReadFromStreamAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<HttpContentHeaders>(), It.IsAny<IFormatterLogger>())).
                Returns(TaskHelpers.FromResult<object>("sampleValue"));
            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, traceWriter, request);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "ReadFromStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.End, Operation = "ReadFromStreamAsync" }
            };

            // Act
            Task<object> task = tracer.ReadFromStreamAsync(typeof(string), new MemoryStream(), request.Content.Headers, null);
            string result = task.Result as string;

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Equal("sampleValue", result);
        }

        [Fact]
        public void ReadFromStreamAsync_Traces_And_Throws_When_Inner_Throws()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            mockFormatter.Setup(
                f => f.ReadFromStreamAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<HttpContentHeaders>(), It.IsAny<IFormatterLogger>())).Throws(exception);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, traceWriter, request);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "ReadFromStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error) { Kind = TraceKind.End, Operation = "ReadFromStreamAsync" }
            };

            // Act
            Exception thrown = Assert.Throws<InvalidOperationException>(() => tracer.ReadFromStreamAsync(typeof(string), new MemoryStream(), request.Content.Headers, null));

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Fact]
        public void ReadFromStreamAsync_Traces_And_Faults_When_Inner_Faults()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(exception);

            mockFormatter.Setup(
                f => f.ReadFromStreamAsync(It.IsAny<Type>(), It.IsAny<Stream>(), It.IsAny<HttpContentHeaders>(), It.IsAny<IFormatterLogger>())).
                Returns(tcs.Task);
            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, traceWriter, request);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "ReadFromStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error) { Kind = TraceKind.End, Operation = "ReadFromStreamAsync" }
            };

            // Act
            Task<object> task = tracer.ReadFromStreamAsync(typeof(string), new MemoryStream(), request.Content.Headers, null);

            // Assert
            Exception thrown = Assert.Throws<InvalidOperationException>(() => task.Wait());
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Fact]
        public void WriteToStreamAsync_Traces()
        {
            // Arrange
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            mockFormatter.Setup(
                f => f.WriteToStreamAsync(It.IsAny<Type>(), It.IsAny<Object>(), It.IsAny<Stream>(), It.IsAny<HttpContentHeaders>(), It.IsAny<TransportContext>())).
                Returns(TaskHelpers.Completed());
            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, traceWriter, request);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "WriteToStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.End, Operation = "WriteToStreamAsync" }
            };

            // Act
            Task task = tracer.WriteToStreamAsync(typeof(string), "sampleValue", new MemoryStream(), request.Content.Headers, transportContext: null);
            task.Wait();

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
        }

        [Fact]
        public void WriteToStreamAsync_Traces_And_Throws_When_Inner_Throws()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            mockFormatter.Setup(
                f =>
                f.WriteToStreamAsync(It.IsAny<Type>(), It.IsAny<Object>(), It.IsAny<Stream>(),
                                     It.IsAny<HttpContentHeaders>(), It.IsAny<TransportContext>())).
                Throws(exception);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, traceWriter, request);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "WriteToStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error) { Kind = TraceKind.End, Operation = "WriteToStreamAsync" }
            };

            // Act
            Exception thrown = Assert.Throws<InvalidOperationException>(() => tracer.WriteToStreamAsync(typeof(string), "sampleValue", new MemoryStream(), request.Content.Headers, transportContext: null));

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Fact]
        public void WriteToStreamAsync_Traces_And_Faults_When_Inner_Faults()
        {
            // Arrange
            InvalidOperationException exception = new InvalidOperationException("test");
            Mock<MediaTypeFormatter> mockFormatter = new Mock<MediaTypeFormatter>() { CallBase = true };
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(exception);

            mockFormatter.Setup(
                f => f.WriteToStreamAsync(It.IsAny<Type>(), It.IsAny<Object>(), It.IsAny<Stream>(), It.IsAny<HttpContentHeaders>(), It.IsAny<TransportContext>())).
                Returns(tcs.Task);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new StringContent("");
            MediaTypeFormatterTracer tracer = new MediaTypeFormatterTracer(mockFormatter.Object, traceWriter, request);
            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "WriteToStreamAsync" },
                new TraceRecord(request, TraceCategories.FormattingCategory, TraceLevel.Error) { Kind = TraceKind.End, Operation = "WriteToStreamAsync" }
            };

            // Act
            Task task = tracer.WriteToStreamAsync(typeof(string), "sampleValue", new MemoryStream(), request.Content.Headers, transportContext: null);

            // Assert
            Exception thrown = Assert.Throws<InvalidOperationException>(() => task.Wait());
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
        }

        [Theory]
        [InlineDataAttribute(typeof(XmlMediaTypeFormatter))]
        [InlineDataAttribute(typeof(JsonMediaTypeFormatter))]
        [InlineDataAttribute(typeof(FormUrlEncodedMediaTypeFormatter))]
        public void CreateTracer_Returns_Tracing_Formatter(Type formatterType)
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeFormatter formatter = (MediaTypeFormatter)Activator.CreateInstance(formatterType);

            // Act
            MediaTypeFormatter tracingFormatter = MediaTypeFormatterTracer.CreateTracer(formatter, new TestTraceWriter(), request);

            // Assert
            Assert.IsAssignableFrom<IFormatterTracer>(tracingFormatter);
            Assert.IsAssignableFrom(formatterType, tracingFormatter);
        }

        [Fact]
        public void CreateTracer_Returns_Tracing_BufferedFormatter()
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeFormatter formatter = new Mock<BufferedMediaTypeFormatter>() { CallBase = true }.Object;

            // Act
            MediaTypeFormatter tracingFormatter = MediaTypeFormatterTracer.CreateTracer(formatter, new TestTraceWriter(), request);

            // Assert
            Assert.IsAssignableFrom<IFormatterTracer>(tracingFormatter);
            Assert.IsAssignableFrom<BufferedMediaTypeFormatter>(tracingFormatter);
        }

        [Theory]
        [InlineDataAttribute(typeof(XmlMediaTypeFormatter))]
        [InlineDataAttribute(typeof(JsonMediaTypeFormatter))]
        [InlineDataAttribute(typeof(FormUrlEncodedMediaTypeFormatter))]
        public void CreateTracer_Loads_SupportedEncodings_From_InnerFormatter(Type formatterType)
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeFormatter formatter = (MediaTypeFormatter)Activator.CreateInstance(formatterType);
            Mock<Encoding> encoding = new Mock<Encoding>();
            formatter.SupportedEncodings.Clear();
            formatter.SupportedEncodings.Add(encoding.Object);

            // Act
            MediaTypeFormatter tracingFormatter = MediaTypeFormatterTracer.CreateTracer(formatter, new TestTraceWriter(), request);

            // Assert
            Assert.Equal(formatter.SupportedEncodings, tracingFormatter.SupportedEncodings);
        }

        [Theory]
        [InlineDataAttribute(typeof(XmlMediaTypeFormatter))]
        [InlineDataAttribute(typeof(JsonMediaTypeFormatter))]
        [InlineDataAttribute(typeof(FormUrlEncodedMediaTypeFormatter))]
        public void CreateTracer_Loads_SupportedMediaTypes_From_InnerFormatter(Type formatterType)
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeFormatter formatter = (MediaTypeFormatter)Activator.CreateInstance(formatterType);

            MediaTypeHeaderValue mediaTypeHeaderValue = new MediaTypeHeaderValue("application/dummy");
            formatter.SupportedMediaTypes.Clear();
            formatter.SupportedMediaTypes.Add(mediaTypeHeaderValue);

            // Act
            MediaTypeFormatter tracingFormatter = MediaTypeFormatterTracer.CreateTracer(formatter, new TestTraceWriter(), request);

            // Assert
            Assert.Equal(formatter.SupportedMediaTypes, tracingFormatter.SupportedMediaTypes);
        }

        [Theory]
        [InlineDataAttribute(typeof(XmlMediaTypeFormatter))]
        [InlineDataAttribute(typeof(JsonMediaTypeFormatter))]
        [InlineDataAttribute(typeof(FormUrlEncodedMediaTypeFormatter))]
        public void CreateTracer_Loads_MediaTypeMappings_From_InnerFormatter(Type formatterType)
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeFormatter formatter = (MediaTypeFormatter)Activator.CreateInstance(formatterType);

            Mock<MediaTypeMapping> mediaTypeMapping = new Mock<MediaTypeMapping>(new MediaTypeHeaderValue("application/dummy"));
            formatter.MediaTypeMappings.Clear();
            formatter.MediaTypeMappings.Add(mediaTypeMapping.Object);

            // Act
            MediaTypeFormatter tracingFormatter = MediaTypeFormatterTracer.CreateTracer(formatter, new TestTraceWriter(), request);

            // Assert
            Assert.Equal(formatter.MediaTypeMappings, tracingFormatter.MediaTypeMappings);
        }

        [Theory]
        [InlineDataAttribute(typeof(XmlMediaTypeFormatter))]
        [InlineDataAttribute(typeof(JsonMediaTypeFormatter))]
        [InlineDataAttribute(typeof(FormUrlEncodedMediaTypeFormatter))]
        public void CreateTracer_Loads_RequiredMemberSelector_From_InnerFormatter(Type formatterType)
        {
            // Arrange
            HttpRequestMessage request = new HttpRequestMessage();
            MediaTypeFormatter formatter = (MediaTypeFormatter)Activator.CreateInstance(formatterType);

            Mock<IRequiredMemberSelector> requiredMemberSelector = new Mock<IRequiredMemberSelector>();
            formatter.RequiredMemberSelector = requiredMemberSelector.Object;

            // Act
            MediaTypeFormatter tracingFormatter = MediaTypeFormatterTracer.CreateTracer(formatter, new TestTraceWriter(), request);

            // Assert
            Assert.Equal(formatter.RequiredMemberSelector, tracingFormatter.RequiredMemberSelector);
        }
    }
}
