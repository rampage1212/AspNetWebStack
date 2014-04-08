﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http;
using System.Web.OData.Properties;
using Microsoft.OData.Core;
using Microsoft.OData.Core.UriParser;
using Microsoft.OData.Edm;
using Semantic = Microsoft.OData.Core.UriParser.Semantic;

namespace System.Web.OData.Routing
{
    /// <summary>
    /// Parses an OData path as an <see cref="ODataPath"/> and converts an <see cref="ODataPath"/> into an OData link.
    /// </summary>
    public class DefaultODataPathHandler : IODataPathHandler, IODataPathTemplateHandler
    {
        /// <summary>
        /// Parses the specified OData path as an <see cref="ODataPath"/> that contains additional information about the EDM type and entity set for the path.
        /// </summary>
        /// <param name="model">The model to use for path parsing.</param>
        /// <param name="serviceRoot">The service root of the OData path.</param>
        /// <param name="odataPath">The OData path to parse.</param>
        /// <returns>A parsed representation of the path, or <c>null</c> if the path does not match the model.</returns>
        public virtual ODataPath Parse(IEdmModel model, string serviceRoot, string odataPath)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }
            if (serviceRoot == null)
            {
                throw Error.ArgumentNull("serviceRoot");
            }
            if (odataPath == null)
            {
                throw Error.ArgumentNull("odataPath");
            }

            return Parse(model, serviceRoot, odataPath, enableUriTemplateParsing: false);
        }

        /// <summary>
        /// Parses the specified OData path template as an <see cref="ODataPathTemplate"/> that can be matched to an <see cref="ODataPath"/>.
        /// </summary>
        /// <param name="model">The model to use for path parsing.</param>
        /// <param name="odataPathTemplate">The OData path template to parse.</param>
        /// <returns>A parsed representation of the path template, or <c>null</c> if the path does not match the model.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "odata", Justification = "odata is spelled correctly")]
        public virtual ODataPathTemplate ParseTemplate(IEdmModel model, string odataPathTemplate)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }
            if (odataPathTemplate == null)
            {
                throw Error.ArgumentNull("odataPathTemplate");
            }

            return Templatify(
                Parse(model, serviceRoot: null, odataPath: odataPathTemplate, enableUriTemplateParsing: true),
                odataPathTemplate);
        }

        /// <summary>
        /// Converts an instance of <see cref="ODataPath" /> into an OData link.
        /// </summary>
        /// <param name="path">The OData path to convert into a link.</param>
        /// <returns>
        /// The generated OData link.
        /// </returns>
        public virtual string Link(ODataPath path)
        {
            if (path == null)
            {
                throw Error.ArgumentNull("path");
            }

            return path.ToString();
        }

        private static ODataPath Parse(
            IEdmModel model,
            string serviceRoot,
            string odataPath,
            bool enableUriTemplateParsing)
        {
            ODataUriParser uriParser;

            if (enableUriTemplateParsing)
            {
                uriParser = new ODataUriParser(model, new Uri(odataPath, UriKind.Relative));
                uriParser.EnableUriTemplateParsing = true;
            }
            else
            {
                Contract.Assert(serviceRoot != null);

                Uri serviceRootUri = new Uri(
                    serviceRoot.EndsWith("/", StringComparison.Ordinal) ?
                        serviceRoot :
                        serviceRoot + "/");

                Uri fullUri = new Uri(serviceRootUri, odataPath);
                uriParser = new ODataUriParser(model, serviceRootUri, fullUri);
            }

            Semantic.ODataPath path;
            UnresolvedPathSegment unresolvedPathSegment = null;
            try
            {
                path = uriParser.ParsePath();
            }
            catch (ODataUnrecognizedPathException ex)
            {
                if (ex.ParsedSegments != null &&
                    ex.ParsedSegments.Count() > 0 &&
                    (ex.ParsedSegments.Last().EdmType is IEdmComplexType ||
                     ex.ParsedSegments.Last().EdmType is IEdmEntityType))
                {
                    if (ex.UnparsedSegments.Count() == 0)
                    {
                        path = new Semantic.ODataPath(ex.ParsedSegments);
                        unresolvedPathSegment = new UnresolvedPathSegment(ex.CurrentSegment);
                    }
                    else
                    {
                        // Throw ODataException if there is some segment following the unresolved segment.
                        throw new ODataException(Error.Format(
                            SRResources.InvalidPathSegment, 
                            ex.UnparsedSegments.First(), 
                            ex.CurrentSegment));
                    }
                }
                else
                {
                    throw;
                }
            }

            return ODataPathSegmentTranslator.TranslateODLPathToWebAPIPath(path, model, unresolvedPathSegment, enableUriTemplateParsing);
        }

        private static ODataPathTemplate Templatify(ODataPath path, string pathTemplate)
        {
            if (path == null)
            {
                throw new ODataException(Error.Format(SRResources.InvalidODataPathTemplate, pathTemplate));
            }

            List<ODataPathSegmentTemplate> templateSegments = new List<ODataPathSegmentTemplate>();
            foreach (ODataPathSegment pathSegment in path.Segments)
            {
                switch (pathSegment.SegmentKind)
                {
                    case ODataSegmentKinds._Unresolved:
                        throw new ODataException(
                            Error.Format(SRResources.UnresolvedPathSegmentInTemplate, pathSegment.ToString(), pathTemplate));

                    case ODataSegmentKinds._Key:
                        templateSegments.Add(new KeyValuePathSegmentTemplate((KeyValuePathSegment)pathSegment));
                        break;

                    case ODataSegmentKinds._Function:
                        templateSegments.Add(new BoundFunctionPathSegmentTemplate((BoundFunctionPathSegment)pathSegment));
                        break;

                    case ODataSegmentKinds._UnboundFunction:
                        templateSegments.Add(new UnboundFunctionPathSegmentTemplate((UnboundFunctionPathSegment)pathSegment));
                        break;

                    default:
                        templateSegments.Add(pathSegment);
                        break;
                }
            }

            return new ODataPathTemplate(templateSegments);
        }
    }
}