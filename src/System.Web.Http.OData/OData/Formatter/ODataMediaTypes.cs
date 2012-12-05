﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http.Headers;

namespace System.Web.Http.OData.Formatter
{
    /// <summary>
    /// Contains media types used by the OData formatter.
    /// </summary>
    internal static class ODataMediaTypes
    {
        private static readonly MediaTypeHeaderValue _applicationAtomSvcXml =
            new MediaTypeHeaderValue("application/atomsvc+xml");
        private static readonly MediaTypeHeaderValue _applicationAtomXml =
            new MediaTypeHeaderValue("application/atom+xml");
        private static readonly MediaTypeHeaderValue _applicationAtomXmlTypeEntry =
            MediaTypeHeaderValue.Parse("application/atom+xml;type=entry");
        private static readonly MediaTypeHeaderValue _applicationAtomXmlTypeFeed =
            MediaTypeHeaderValue.Parse("application/atom+xml;type=feed");
        private static readonly MediaTypeHeaderValue _applicationJson = new MediaTypeHeaderValue("application/json");
        private static readonly MediaTypeHeaderValue _applicationJsonODataFullMetadata =
            MediaTypeHeaderValue.Parse("application/json;odata=fullmetadata");
        private static readonly MediaTypeHeaderValue _applicationJsonODataFullMetadataStreamingFalse =
            MediaTypeHeaderValue.Parse("application/json;odata=fullmetadata;streaming=false");
        private static readonly MediaTypeHeaderValue _applicationJsonODataMinimalMetadata =
            MediaTypeHeaderValue.Parse("application/json;odata=minimalmetadata");
        private static readonly MediaTypeHeaderValue _applicationJsonODataMinimalMetadataStreamingFalse =
            MediaTypeHeaderValue.Parse("application/json;odata=minimalmetadata;streaming=false");
        private static readonly MediaTypeHeaderValue _applicationJsonODataVerbose =
            MediaTypeHeaderValue.Parse("application/json;odata=verbose");
        private static readonly MediaTypeHeaderValue _applicationJsonStreamingFalse =
            MediaTypeHeaderValue.Parse("application/json;streaming=false");
        private static readonly MediaTypeHeaderValue _applicationXml = new MediaTypeHeaderValue("application/xml");
        private static readonly MediaTypeHeaderValue _textXml = new MediaTypeHeaderValue("text/xml");

        public static MediaTypeHeaderValue ApplicationAtomSvcXml
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationAtomSvcXml).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationAtomXml
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationAtomXml).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationAtomXmlTypeEntry
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationAtomXmlTypeEntry).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationAtomXmlTypeFeed
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationAtomXmlTypeFeed).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationJson
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationJson).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationJsonODataFullMetadata
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationJsonODataFullMetadata).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationJsonODataFullMetadataStreamingFalse
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationJsonODataFullMetadataStreamingFalse).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationJsonODataMinimalMetadata
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationJsonODataMinimalMetadata).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationJsonODataMinimalMetadataStreamingFalse
        {
            get
            {
                return (MediaTypeHeaderValue)((ICloneable)_applicationJsonODataMinimalMetadataStreamingFalse).Clone();
            }
        }

        public static MediaTypeHeaderValue ApplicationJsonODataVerbose
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationJsonODataVerbose).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationJsonStreamingFalse
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationJsonStreamingFalse).Clone(); }
        }

        public static MediaTypeHeaderValue ApplicationXml
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_applicationXml).Clone(); }
        }

        public static MediaTypeHeaderValue TextXml
        {
            get { return (MediaTypeHeaderValue)((ICloneable)_textXml).Clone(); }
        }
    }
}
