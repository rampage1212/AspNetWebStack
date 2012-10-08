﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Formatter.Serialization
{
    /// <summary>
    /// Context information used by the <see cref="ODataSerializer"/> when serializing objects in OData message format.
    /// </summary>
    public class ODataSerializerContext
    {
        /// <summary>
        /// Gets or sets the URL helper.
        /// </summary>
        public UrlHelper UrlHelper { get; set; }

        /// <summary>
        /// Gets or sets the entity set.
        /// </summary>
        public IEdmEntitySet EntitySet { get; set; }

        /// <summary>
        /// Gets or sets the ServiceOperationName which is used when writing primitive types
        /// and complex types.
        /// </summary>
        public string ServiceOperationName { get; set; }

        /// <summary>
        /// Gets or sets the HttpRequestMessage. 
        /// The HttpRequestMessage can then be used by ODataSerializers to learn more about the Request that triggered the serialization
        /// </summary>
        public HttpRequestMessage Request { get; set; }

        /// <summary>
        /// Get or sets whether expensive links should be calculated.
        /// </summary>
        public bool SkipExpensiveAvailabilityChecks { get; set; }
    }
}
