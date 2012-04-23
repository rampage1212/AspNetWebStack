﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using Microsoft.TestCommon;
using Moq;
using Xunit;
using Xunit.Extensions;
using Assert = Microsoft.TestCommon.AssertEx;

namespace System.Web.Http.WebHost
{
    public class WebHostBufferPolicySelectorTest
    {
        private const string testString = "testString";

        public static TheoryDataSet<HttpContent, bool> OutputBufferingTestData
        {
            get
            {
                Mock<Stream> mockStream = new Mock<Stream>() { CallBase = true };
                return new TheoryDataSet<HttpContent, bool>()
                {
                    // Known length HttpContents other than OC should not buffer
                    { new StringContent(testString), false },
                    { new ByteArrayContent(Encoding.UTF8.GetBytes(testString)), false },
                    { new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(testString))), false },

                    // StreamContent (unknown length) should not buffer
                    { new StreamContent(mockStream.Object), false },

                    // PushStreamContent (unknown length) should not buffer
                    { new PushStreamContent((stream, headers, context) => {}), false },

                    // ObjectContent (unknown length) should buffer
                    { new ObjectContent<string>(testString, new XmlMediaTypeFormatter()), true }
                };
            }
        }

        [Fact]
        public void UseBufferedOutputStream_ThrowsOnNull()
        {
            WebHostBufferPolicySelector selector = new WebHostBufferPolicySelector();
            Assert.ThrowsArgumentNull(() => selector.UseBufferedOutputStream(null), "response");
        }

        [Theory]
        [PropertyData("OutputBufferingTestData")]
        public void UseBufferedOutputStream_ReturnsCorrectValue(HttpContent content, bool expectedResult)
        {
            // Arrange
            WebHostBufferPolicySelector selector = new WebHostBufferPolicySelector();
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = content;

            // Act
            bool actualResult = selector.UseBufferedOutputStream(response);

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [PropertyData("OutputBufferingTestData")]
        public void UseBufferedOutputStream_CausesContentLengthHeaderToBeSet(HttpContent content, bool expectedResult)
        {
            // Arrange & Act
            WebHostBufferPolicySelector selector = new WebHostBufferPolicySelector();
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = content;

            selector.UseBufferedOutputStream(response);

            IEnumerable<string> contentLengthEnumerable;
            bool isContentLengthInHeaders = content.Headers.TryGetValues("Content-Length", out contentLengthEnumerable);
            string[] contentLengthStrings = isContentLengthInHeaders ? contentLengthEnumerable.ToArray() : new string[0];
            long? contentLength = content.Headers.ContentLength;

            // Assert
            if (contentLength.HasValue && contentLength.Value >= 0)
            {
                // Setting the header is HttpContentHeader's responsibility, but we assert
                // it has happened here because it is UseBufferedOutputStream's responsibility
                // to cause that to happen. HttpControllerHandler relies on this.
                Assert.True(isContentLengthInHeaders);
                Assert.Equal(contentLength.Value, long.Parse(contentLengthStrings[0]));
            }
        }
    }
}
