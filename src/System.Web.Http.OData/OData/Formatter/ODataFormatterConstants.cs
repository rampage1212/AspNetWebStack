﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http.Headers;
using Microsoft.Data.OData;

namespace System.Web.Http.OData.Formatter
{
    /// <summary>
    /// Constants related to OData.
    /// </summary>
    internal static class ODataFormatterConstants
    {
        public const string DefaultNamespace = "http://www.tempuri.org";

        public const string Element = "element";

        public const string Entry = "entry";
        public const string Feed = "feed";

        public const string ODataServiceVersion = "DataServiceVersion";
        public const string ODataMaxServiceVersion = "MaxDataServiceVersion";

        public const ODataVersion DefaultODataVersion = ODataVersion.V3;
        public static readonly ODataFormat DefaultODataFormat = ODataFormat.Atom;
    }
}
