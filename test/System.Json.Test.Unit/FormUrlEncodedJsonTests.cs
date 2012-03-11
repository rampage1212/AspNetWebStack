﻿using System.Collections.Generic;
using Microsoft.TestCommon;
using Xunit;
using Assert = Microsoft.TestCommon.AssertEx;

namespace System.Json
{
    public class FormUrlEncodedJsonTests
    {
        [Fact]
        public void TypeIsCorrect()
        {
            Assert.Type.HasProperties(typeof(FormUrlEncodedJson), TypeAssert.TypeProperties.IsPublicVisibleClass | TypeAssert.TypeProperties.IsStatic);
        }

        [Fact]
        public void ParseThrowsOnNull()
        {
            Assert.ThrowsArgumentNull(() => FormUrlEncodedJson.Parse(null), null);
        }

        [Fact]
        public void ParseThrowsInvalidMaxDepth()
        {
            Assert.ThrowsArgument(() => FormUrlEncodedJson.Parse(CreateQuery(), -1), "maxDepth");
            Assert.ThrowsArgument(() => FormUrlEncodedJson.Parse(CreateQuery(), 0), "maxDepth");
        }

        [Fact]
        public void ParseThrowsMaxDepthExceeded()
        {
            // Depth of 'a[b]=1' is 3
            IEnumerable<KeyValuePair<string, string>> query = CreateQuery(new KeyValuePair<string, string>("a[b]", "1"));
            Assert.ThrowsArgument(() => { FormUrlEncodedJson.Parse(query, 2); }, null);

            // This should succeed
            Assert.NotNull(FormUrlEncodedJson.Parse(query, 3));
        }

        [Fact]
        public void TryParseThrowsOnNull()
        {
            JsonObject value;
            Assert.ThrowsArgumentNull(() => FormUrlEncodedJson.TryParse(null, out value), null);
        }

        [Fact]
        public void TryParseThrowsInvalidMaxDepth()
        {
            JsonObject value;
            Assert.ThrowsArgument(() => FormUrlEncodedJson.TryParse(CreateQuery(), -1, out value), "maxDepth");
            Assert.ThrowsArgument(() => FormUrlEncodedJson.TryParse(CreateQuery(), 0, out value), "maxDepth");
        }

        [Fact]
        public void TryParseReturnsFalseMaxDepthExceeded()
        {
            JsonObject value;

            // Depth of 'a[b]=1' is 3
            IEnumerable<KeyValuePair<string, string>> query = CreateQuery(new KeyValuePair<string, string>("a[b]", "1"));
            Assert.False(FormUrlEncodedJson.TryParse(query, 2, out value), "Parse should have failed due to too high depth.");

            // This should succeed
            Assert.True(FormUrlEncodedJson.TryParse(query, 3, out value), "Expected non-null JsonObject instance");
            Assert.NotNull(value);
        }

        private static IEnumerable<KeyValuePair<string, string>> CreateQuery(params KeyValuePair<string, string>[] namevaluepairs)
        {
            return new List<KeyValuePair<string, string>>(namevaluepairs);
        }
    }
}