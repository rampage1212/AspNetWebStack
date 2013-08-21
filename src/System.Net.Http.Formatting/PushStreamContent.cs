﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Http.Internal;
using System.Threading.Tasks;
using System.Web.Http;

namespace System.Net.Http
{
    /// <summary>
    /// Provides an <see cref="HttpContent"/> implementation that exposes an output <see cref="Stream"/>
    /// which can be written to directly. The ability to push data to the output stream differs from the 
    /// <see cref="StreamContent"/> where data is pulled and not pushed.
    /// </summary>
    public class PushStreamContent : HttpContent
    {
        private readonly Func<Stream, HttpContent, TransportContext, Task> _onStreamAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class. The
        /// <paramref name="onStreamAvailable"/> action is called when an output stream
        /// has become available allowing the action to write to it directly. When the 
        /// stream is closed, it will signal to the content that is has completed and the 
        /// HTTP request or response will be completed.
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable)
            : this(Taskify(onStreamAvailable), (MediaTypeHeaderValue)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class. 
        /// </summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available. The stream is automatically
        /// closed when the return task is completed.</param>
        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable)
            : this(onStreamAvailable, (MediaTypeHeaderValue)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.
        /// </summary>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable, string mediaType)
            : this(Taskify(onStreamAvailable), new MediaTypeHeaderValue(mediaType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.
        /// </summary>
        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable, string mediaType)
            : this(onStreamAvailable, new MediaTypeHeaderValue(mediaType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.
        /// </summary>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable, MediaTypeHeaderValue mediaType)
            : this(Taskify(onStreamAvailable), mediaType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.
        /// </summary>
        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable, MediaTypeHeaderValue mediaType)
        {
            if (onStreamAvailable == null)
            {
                throw Error.ArgumentNull("onStreamAvailable");
            }

            _onStreamAvailable = onStreamAvailable;
            Headers.ContentType = mediaType ?? MediaTypeConstants.ApplicationOctetStreamMediaType;
        }

        private static Func<Stream, HttpContent, TransportContext, Task> Taskify(
            Action<Stream, HttpContent, TransportContext> onStreamAvailable)
        {
            if (onStreamAvailable == null)
            {
                throw Error.ArgumentNull("onStreamAvailable");
            }

            return (Stream stream, HttpContent content, TransportContext transportContext) =>
            {
                onStreamAvailable(stream, content, transportContext);
                return TaskHelpers.Completed();
            };
        }

        /// <summary>
        /// When this method is called, it calls the action provided in the constructor with the output 
        /// stream to write to. Once the action has completed its work it closes the stream which will 
        /// close this content instance and complete the HTTP request or response.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="context">The associated <see cref="TransportContext"/>.</param>
        /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is passed as task result.")]
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            TaskCompletionSource<bool> serializeToStreamTask = new TaskCompletionSource<bool>();

            Stream wrappedStream = new CompleteTaskOnCloseStream(stream, serializeToStreamTask);
            await _onStreamAvailable(wrappedStream, this, context);

            // wait for wrappedStream.Close/Dispose to get called.
            await serializeToStreamTask.Task;
        }

        /// <summary>
        /// Computes the length of the stream if possible.
        /// </summary>
        /// <param name="length">The computed length of the stream.</param>
        /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            // We can't know the length of the content being pushed to the output stream.
            length = -1;
            return false;
        }

        internal class CompleteTaskOnCloseStream : DelegatingStream
        {
            private TaskCompletionSource<bool> _serializeToStreamTask;

            public CompleteTaskOnCloseStream(Stream innerStream, TaskCompletionSource<bool> serializeToStreamTask)
                : base(innerStream)
            {
                Contract.Assert(serializeToStreamTask != null);
                _serializeToStreamTask = serializeToStreamTask;
            }

#if NETFX_CORE
            // In the non portable version, we don't want to close the inner stream as HttpContent will do that for us
            // For the portable library version, we implement dispose in order to signal task completion
            // since there is no Close on Stream. In that case we do want to dispose the inner stream since in
            // client scenarios we can't rely on HttpContent.Dispose to do this for us since the stream is not
            // necessarily owned by HttpContent.
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _serializeToStreamTask.TrySetResult(true);
            }
#else
            public override void Close()
            {
                // Note we don't call close on the inner stream as the stream will get closed when this
                // HttpContent instance is disposed.
                _serializeToStreamTask.TrySetResult(true);
            }
#endif
        }
    }
}
