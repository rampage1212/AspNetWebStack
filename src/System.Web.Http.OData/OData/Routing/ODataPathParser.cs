﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;

namespace System.Web.Http.OData.Routing
{
    /// <summary>
    /// Parses an OData URI as an <see cref="ODataPath"/> that contains additional information about the EDM type and entity set for the path.
    /// </summary>
    public class ODataPathParser : IODataPathParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataPathParser" /> class.
        /// </summary>
        /// <param name="model">The model to use for segment parsing.</param>
        public ODataPathParser(IEdmModel model)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }

            IEnumerable<IEdmEntityContainer> containers = model.EntityContainers();
            int containerCount = containers.Count();
            if (containerCount != 1)
            {
                throw Error.InvalidOperation(SRResources.ParserModelMustHaveOneContainer, containerCount);
            }
            Model = model;
            Container = containers.Single();
        }

        /// <summary>
        /// Gets the model used for segment parsing.
        /// </summary>
        public IEdmModel Model
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the container used to resolve procedures and entity sets.
        /// </summary>
        public IEdmEntityContainer Container
        {
            get;
            private set;
        }

        /// <summary>
        /// Parses the specified OData URI as an <see cref="ODataPath"/> that contains additional information about the EDM type and entity set for the path.
        /// </summary>
        /// <param name="uri">The OData URI to parse.</param>
        /// <param name="baseUri">The base URI of the service.</param>
        /// <returns>A parsed representation of the URI, or <c>null</c> if the URI does not match the model.</returns>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Implementations shouldn't need to subclass ODataPath")]
        public virtual ODataPath Parse(Uri uri, Uri baseUri)
        {
            if (uri == null)
            {
                throw Error.ArgumentNull("uri");
            }
            if (baseUri == null)
            {
                throw Error.ArgumentNull("baseUri");
            }

            string uriPath = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            string basePath = baseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (!uriPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            string relativePath = uriPath.Substring(basePath.Length);

            ODataPath path = new ODataPath();
            ODataPathSegment pathSegment = new ServiceBasePathSegment(baseUri);
            path.Segments.AddLast(pathSegment);
            foreach (string segment in ParseSegments(relativePath))
            {
                pathSegment = ParseNextSegment(pathSegment, segment);

                // If the Uri stops matching the model at any point, return null
                if (pathSegment == null)
                {
                    return null;
                }

                path.Segments.AddLast(pathSegment);
            }
            return path;
        }

        /// <summary>
        /// Parses the OData path into segments.
        /// </summary>
        /// <param name="relativePath">The path relative to the service base URI to parse.</param>
        /// <returns>The segments of the OData URI.</returns>
        protected internal virtual IEnumerable<string> ParseSegments(string relativePath)
        {
            if (relativePath == null)
            {
                throw Error.ArgumentNull("relativePath");
            }

            string[] segments = relativePath.Split('/');

            foreach (string segment in segments)
            {
                int startIndex = 0;
                int openParensIndex = 0;
                bool insideParens = false;
                for (int i = 0; i < segment.Length; i++)
                {
                    switch (segment[i])
                    {
                        case '(':
                            openParensIndex = i;
                            insideParens = true;
                            break;
                        case ')':
                            if (insideParens)
                            {
                                if (openParensIndex > startIndex)
                                {
                                    yield return segment.Substring(startIndex, openParensIndex - startIndex);
                                }
                                if (i > openParensIndex + 1)
                                {
                                    // yield parentheses substring if there are any characters inside the parentheses
                                    yield return segment.Substring(openParensIndex, (i + 1) - openParensIndex);
                                }
                                startIndex = i + 1;
                                insideParens = false;
                            }
                            break;
                    }
                }

                if (startIndex < segment.Length)
                {
                    yield return segment.Substring(startIndex);
                }
            }
        }

        /// <summary>
        /// Parses the next OData path segment.
        /// </summary>
        /// <param name="previous">The previous path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseNextSegment(ODataPathSegment previous, string segment)
        {
            if (previous == null)
            {
                throw Error.ArgumentNull("previous");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }

            if (previous is ServiceBasePathSegment)
            {
                // Parse entry node
                return ParseEntrySegment(previous, segment);
            }
            else
            {
                // Parse non-entry node
                if (previous.EdmType == null)
                {
                    throw new ODataException(Error.Format(SRResources.InvalidPathSegment, segment, previous));
                }

                switch (previous.EdmType.TypeKind)
                {
                    case EdmTypeKind.Collection:
                        return ParseAtCollection(previous, segment);

                    case EdmTypeKind.Entity:
                        return ParseAtEntity(previous, segment);

                    case EdmTypeKind.Complex:
                        return ParseAtComplex(previous, segment);

                    case EdmTypeKind.Primitive:
                        return ParseAtPrimitiveProperty(previous, segment);

                    default:
                        throw new ODataException(Error.Format(SRResources.InvalidPathSegment, segment, previous));
                }
            }
        }

        /// <summary>
        /// Parses the first OData segment following the service base URI.
        /// </summary>
        /// <param name="root">The service base path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseEntrySegment(ODataPathSegment root, string segment)
        {
            if (root == null)
            {
                throw Error.ArgumentNull("root");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }

            if (segment == ODataSegmentKinds.Metadata)
            {
                return new MetadataPathSegment(root);
            }
            if (segment == ODataSegmentKinds.Batch)
            {
                return new BatchPathSegment(root);
            }

            IEdmEntitySet entitySet = Container.FindEntitySet(segment);
            if (entitySet != null)
            {
                return new EntitySetPathSegment(root, entitySet);
            }

            IEdmFunctionImport function = Container.FunctionImports().SingleOrDefault(fi => fi.Name == segment && fi.IsBindable == false);
            if (function != null)
            {
                return new ActionPathSegment(root, function);
            }

            // segment does not match the model
            return null;
        }

        /// <summary>
        /// Parses the next OData path segment following a collection.
        /// </summary>
        /// <param name="previous">The previous path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseAtCollection(ODataPathSegment previous, string segment)
        {
            if (previous == null)
            {
                throw Error.ArgumentNull("previous");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }

            if (previous.EdmType == null)
            {
                throw Error.InvalidOperation(SRResources.PreviousSegmentEdmTypeCannotBeNull);
            }

            IEdmCollectionType collection = previous.EdmType as IEdmCollectionType;
            if (collection == null)
            {
                throw Error.Argument(SRResources.PreviousSegmentMustBeCollectionType, previous.EdmType);
            }

            switch (collection.ElementType.Definition.TypeKind)
            {
                case EdmTypeKind.Entity:
                    return ParseAtEntityCollection(previous, segment);

                default:
                    throw new ODataException(Error.Format(SRResources.InvalidPathSegment, segment, previous));
            }
        }

        /// <summary>
        /// Parses the next OData path segment following a complex-typed segment.
        /// </summary>
        /// <param name="previous">The previous path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseAtComplex(ODataPathSegment previous, string segment)
        {
            if (previous == null)
            {
                throw Error.ArgumentNull("previous");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }

            IEdmComplexType previousType = previous.EdmType as IEdmComplexType;
            if (previousType == null)
            {
                throw Error.Argument(SRResources.PreviousSegmentMustBeComplexType, previous.EdmType);
            }

            // look for properties
            IEdmProperty property = previousType.Properties().SingleOrDefault(p => p.Name == segment);
            if (property != null)
            {
                return new PropertyAccessPathSegment(previous, property);
            }

            // Treating as an open property
            return new UnresolvedPathSegment(previous, segment);
        }

        /// <summary>
        /// Parses the next OData path segment following an entity collection.
        /// </summary>
        /// <param name="previous">The previous path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseAtEntityCollection(ODataPathSegment previous, string segment)
        {
            if (previous == null)
            {
                throw Error.ArgumentNull("previous");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }

            if (previous.EdmType == null)
            {
                throw Error.InvalidOperation(SRResources.PreviousSegmentEdmTypeCannotBeNull);
            }
            IEdmCollectionType collectionType = previous.EdmType as IEdmCollectionType;
            if (collectionType == null)
            {
                throw Error.Argument(SRResources.PreviousSegmentMustBeEntityCollectionType, previous.EdmType);
            }
            IEdmEntityType elementType = collectionType.ElementType.Definition as IEdmEntityType;
            if (elementType == null)
            {
                throw Error.Argument(SRResources.PreviousSegmentMustBeEntityCollectionType, previous.EdmType);
            }

            // look for keys first.
            if (segment.StartsWith("(", StringComparison.Ordinal) && segment.EndsWith(")", StringComparison.Ordinal))
            {
                Contract.Assert(segment.Length >= 2);
                string value = segment.Substring(1, segment.Length - 2);
                return new KeyValuePathSegment(previous, value);
            }

            // next look for casts
            IEdmEntityType castType = Model.FindDeclaredType(segment) as IEdmEntityType;
            if (castType != null)
            {
                IEdmType previousElementType = collectionType.ElementType.Definition;
                if (!castType.IsOrInheritsFrom(previousElementType))
                {
                    throw new ODataException(Error.Format(SRResources.InvalidCastInPath, castType, previousElementType));
                }
                return new CastPathSegment(previous, castType);
            }

            // now look for bindable actions
            IEdmFunctionImport procedure = Container.FunctionImports().FindBindableAction(collectionType, segment);
            if (procedure != null)
            {
                return new ActionPathSegment(previous, procedure);
            }

            throw new ODataException(Error.Format(SRResources.NoActionFoundForCollection, segment, collectionType.ElementType));
        }

        /// <summary>
        /// Parses the next OData path segment following a primitive property.
        /// </summary>
        /// <param name="previous">The previous path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseAtPrimitiveProperty(ODataPathSegment previous, string segment)
        {
            if (previous == null)
            {
                throw Error.ArgumentNull("previous");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }

            if (segment == ODataSegmentKinds.Value)
            {
                return new ValuePathSegment(previous);
            }

            throw new ODataException(Error.Format(SRResources.InvalidPathSegment, segment, previous));
        }

        /// <summary>
        /// Parses the next OData path segment following an entity.
        /// </summary>
        /// <param name="previous">The previous path segment.</param>
        /// <param name="segment">The value of the segment to parse.</param>
        /// <returns>A parsed representation of the segment.</returns>
        protected virtual ODataPathSegment ParseAtEntity(ODataPathSegment previous, string segment)
        {
            if (previous == null)
            {
                throw Error.ArgumentNull("previous");
            }
            if (String.IsNullOrEmpty(segment))
            {
                throw Error.Argument(SRResources.SegmentNullOrEmpty);
            }
            IEdmEntityType previousType = previous.EdmType as IEdmEntityType;
            if (previousType == null)
            {
                throw Error.Argument(SRResources.PreviousSegmentMustBeEntityType, previous.EdmType);
            }

            if (segment == ODataSegmentKinds.Links)
            {
                return new LinksPathSegment(previous);
            }

            // first look for navigation properties
            IEdmNavigationProperty navigation = previousType.NavigationProperties().SingleOrDefault(np => np.Name == segment);
            if (navigation != null)
            {
                return new NavigationPathSegment(previous, navigation);
            }

            // next look for properties
            IEdmProperty property = previousType.Properties().SingleOrDefault(p => p.Name == segment);
            if (property != null)
            {
                return new PropertyAccessPathSegment(previous, property);
            }

            // next look for type casts
            IEdmEntityType castType = Model.FindDeclaredType(segment) as IEdmEntityType;
            if (castType != null)
            {
                if (!castType.IsOrInheritsFrom(previousType))
                {
                    throw new ODataException(Error.Format(SRResources.InvalidCastInPath, castType, previousType));
                }
                return new CastPathSegment(previous, castType);
            }

            // finally look for bindable procedures
            IEdmFunctionImport procedure = Container.FunctionImports().FindBindableAction(previousType, segment);
            if (procedure != null)
            {
                return new ActionPathSegment(previous, procedure);
            }

            // Treating as an open property
            return new UnresolvedPathSegment(previous, segment);
        }
    }
}