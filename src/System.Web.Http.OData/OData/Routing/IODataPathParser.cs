﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace System.Web.Http.OData.Routing
{
    /// <summary>
    /// Exposes the ability to parse an OData path as an <see cref="ODataPath"/> that contains additional information about the EDM type and entity set for the path.
    /// </summary>
    public interface IODataPathParser
    {
        /// <summary>
        /// Parses the specified OData path as an <see cref="ODataPath"/> that contains additional information about the EDM type and entity set for the path.
        /// </summary>
        /// <param name="odataPath">The OData path to parse.</param>
        /// <returns>A parsed representation of the URI, or <c>null</c> if the URI does not match the model.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "odata", Justification = "odata is spelled correctly")]
        ODataPath Parse(string odataPath);
    }
}