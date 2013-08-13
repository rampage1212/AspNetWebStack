﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace System.Web.Http
{
    /// <summary>
    /// Specifies that an action supports the PUT HTTP method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class HttpPutAttribute : AcceptVerbsAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpPutAttribute" /> class.
        /// </summary>
        public HttpPutAttribute()
            : base(HttpMethod.Put)
        {
        }
    }
}
