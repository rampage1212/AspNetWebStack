﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.Metadata.Providers;
using System.Web.Http.ModelBinding;
using System.Web.Http.ValueProviders;
using Moq;
using Xunit;
using Assert = Microsoft.TestCommon.AssertEx;

namespace System.Web.Http.Tracing.Tracers
{
    public class HttpParameterBindingTracerTest
    {
        [Fact]
        public void ValueProviderFactories_Delegates_To_Inner()
        {
            // Arrange
            Mock<HttpParameterDescriptor> mockParamDescriptor = new Mock<HttpParameterDescriptor>() { CallBase = true };
            mockParamDescriptor.Setup(d => d.ParameterName).Returns("paramName");
            mockParamDescriptor.Setup(d => d.ParameterType).Returns(typeof(string));
            Mock<IModelBinder> mockModelBinder = new Mock<IModelBinder>() { CallBase = true };
            Mock<ValueProviderFactory> mockValueProviderFactory = new Mock<ValueProviderFactory>() { CallBase = true };

            List<ValueProviderFactory> expectedFactories = new List<ValueProviderFactory>
            {
                mockValueProviderFactory.Object
            };

            ModelBinderParameterBinding binding = new ModelBinderParameterBinding(mockParamDescriptor.Object, mockModelBinder.Object, expectedFactories);
            HttpParameterBindingTracer tracer = new HttpParameterBindingTracer(binding, new TestTraceWriter());

            // Act
            List<ValueProviderFactory> actualFactories = tracer.ValueProviderFactories.ToList();

            // Assert
            Assert.Equal(expectedFactories, actualFactories);
        }

        [Fact]
        public void ValueProviderFactories_Returns_Empty_Enumerable_When_Not_IValueProviderParameterBinding()
        {
            // Arrange
            Mock<HttpParameterDescriptor> mockParamDescriptor = new Mock<HttpParameterDescriptor>() { CallBase = true };
            mockParamDescriptor.Setup(d => d.ParameterName).Returns("paramName");
            mockParamDescriptor.Setup(d => d.ParameterType).Returns(typeof(string));
            Mock<HttpParameterBinding> mockBinding = new Mock<HttpParameterBinding>(mockParamDescriptor.Object);
            HttpParameterBindingTracer tracer = new HttpParameterBindingTracer(mockBinding.Object, new TestTraceWriter());

            // Act
            ValueProviderFactory[] actualFactories = tracer.ValueProviderFactories.ToArray();

            // Assert
            Assert.Equal(0, actualFactories.Length);
        }

        [Fact]
        public void ExecuteBindingAsync_Traces_And_Invokes_Inner()
        {
            // Arrange
            Mock<HttpParameterDescriptor> mockParamDescriptor = new Mock<HttpParameterDescriptor>() { CallBase = true };
            mockParamDescriptor.Setup(d => d.ParameterName).Returns("paramName");
            mockParamDescriptor.Setup(d => d.ParameterType).Returns(typeof(string));
            Mock<HttpParameterBinding> mockBinding = new Mock<HttpParameterBinding>(mockParamDescriptor.Object) { CallBase = true };
            bool innerInvoked = false;
            mockBinding.Setup(
                b =>
                b.ExecuteBindingAsync(It.IsAny<ModelMetadataProvider>(), It.IsAny<HttpActionContext>(),
                                      It.IsAny<CancellationToken>())).Returns(TaskHelpers.Completed()).Callback(() => innerInvoked = true);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpParameterBindingTracer tracer = new HttpParameterBindingTracer(mockBinding.Object, traceWriter);
            HttpActionContext actionContext = ContextUtil.CreateActionContext();
            ModelMetadataProvider metadataProvider = new EmptyModelMetadataProvider();

            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(actionContext.Request, TraceCategories.ModelBindingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "ExecuteBindingAsync" },
                new TraceRecord(actionContext.Request, TraceCategories.ModelBindingCategory, TraceLevel.Info) { Kind = TraceKind.End, Operation = "ExecuteBindingAsync" }
            };

            // Act
            Task task = tracer.ExecuteBindingAsync(metadataProvider, actionContext, CancellationToken.None);
            task.Wait();

            // Assert
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
            Assert.True(innerInvoked);
        }

        [Fact]
        public void ExecuteBindingAsync_Traces_And_Throws_When_Inner_Throws()
        {
            // Arrange
            Mock<HttpParameterDescriptor> mockParamDescriptor = new Mock<HttpParameterDescriptor>() { CallBase = true };
            mockParamDescriptor.Setup(d => d.ParameterName).Returns("paramName");
            mockParamDescriptor.Setup(d => d.ParameterType).Returns(typeof(string));
            Mock<HttpParameterBinding> mockBinding = new Mock<HttpParameterBinding>(mockParamDescriptor.Object) { CallBase = true };
            InvalidOperationException exception = new InvalidOperationException("test");
            mockBinding.Setup(
                b =>
                b.ExecuteBindingAsync(It.IsAny<ModelMetadataProvider>(), It.IsAny<HttpActionContext>(),
                                      It.IsAny<CancellationToken>())).Throws(exception);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpParameterBindingTracer tracer = new HttpParameterBindingTracer(mockBinding.Object, traceWriter);
            HttpActionContext actionContext = ContextUtil.CreateActionContext();
            ModelMetadataProvider metadataProvider = new EmptyModelMetadataProvider();

            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(actionContext.Request, TraceCategories.ModelBindingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "ExecuteBindingAsync" },
                new TraceRecord(actionContext.Request, TraceCategories.ModelBindingCategory, TraceLevel.Error) { Kind = TraceKind.End, Operation = "ExecuteBindingAsync" }
            };

            // Act & Assert
            Exception thrown = Assert.Throws<InvalidOperationException>(() => tracer.ExecuteBindingAsync(metadataProvider, actionContext, CancellationToken.None));

            // Assert
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
        }

        [Fact]
        public void ExecuteBindingAsync_Traces_And_Faults_When_Inner_Faults()
        {
            // Arrange

            Mock<HttpParameterDescriptor> mockParamDescriptor = new Mock<HttpParameterDescriptor>() { CallBase = true };
            mockParamDescriptor.Setup(d => d.ParameterName).Returns("paramName");
            mockParamDescriptor.Setup(d => d.ParameterType).Returns(typeof(string));
            Mock<HttpParameterBinding> mockBinding = new Mock<HttpParameterBinding>(mockParamDescriptor.Object) { CallBase = true };
            InvalidOperationException exception = new InvalidOperationException("test");
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(exception);

            mockBinding.Setup(
                b =>
                b.ExecuteBindingAsync(It.IsAny<ModelMetadataProvider>(), It.IsAny<HttpActionContext>(),
                                      It.IsAny<CancellationToken>())).Returns(tcs.Task);

            TestTraceWriter traceWriter = new TestTraceWriter();
            HttpParameterBindingTracer tracer = new HttpParameterBindingTracer(mockBinding.Object, traceWriter);
            HttpActionContext actionContext = ContextUtil.CreateActionContext();
            ModelMetadataProvider metadataProvider = new EmptyModelMetadataProvider();

            TraceRecord[] expectedTraces = new TraceRecord[]
            {
                new TraceRecord(actionContext.Request, TraceCategories.ModelBindingCategory, TraceLevel.Info) { Kind = TraceKind.Begin, Operation = "ExecuteBindingAsync" },
                new TraceRecord(actionContext.Request, TraceCategories.ModelBindingCategory, TraceLevel.Error) { Kind = TraceKind.End, Operation = "ExecuteBindingAsync" }
            };

            // Act & Assert
            Task task = tracer.ExecuteBindingAsync(metadataProvider, actionContext, CancellationToken.None);

            // Assert
            Exception thrown = Assert.Throws<InvalidOperationException>(() => task.Wait());
            Assert.Same(exception, thrown);
            Assert.Same(exception, traceWriter.Traces[1].Exception);
            Assert.Equal<TraceRecord>(expectedTraces, traceWriter.Traces, new TraceRecordComparer());
        }
    }
}
