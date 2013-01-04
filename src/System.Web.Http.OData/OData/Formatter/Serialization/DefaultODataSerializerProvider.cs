﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;

namespace System.Web.Http.OData.Formatter.Serialization
{
    /// <summary>
    /// The default <see cref="ODataSerializerProvider" />
    /// </summary>
    internal class DefaultODataSerializerProvider : ODataSerializerProvider
    {
        // cache the clrtype to ODataSerializer mappings as we might have to crawl the 
        // inheritance hierarchy to find the mapping.
        private readonly ConcurrentDictionary<Tuple<IEdmModel, Type>, ODataSerializer> _clrTypeMappingCache =
            new ConcurrentDictionary<Tuple<IEdmModel, Type>, ODataSerializer>();

        public override ODataSerializer CreateEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType == null)
            {
                throw Error.ArgumentNull("edmType");
            }

            switch (edmType.TypeKind())
            {
                case EdmTypeKind.Primitive:
                    return new ODataPrimitiveSerializer(edmType.AsPrimitive());

                case EdmTypeKind.Collection:
                    IEdmCollectionTypeReference collectionType = edmType.AsCollection();
                    if (collectionType.ElementType().IsEntity())
                    {
                        return new ODataFeedSerializer(collectionType, this);
                    }
                    else
                    {
                        return new ODataCollectionSerializer(collectionType, this);
                    }

                case EdmTypeKind.Complex:
                    return new ODataComplexTypeSerializer(edmType.AsComplex(), this);

                case EdmTypeKind.Entity:
                    return new ODataEntityTypeSerializer(edmType.AsEntity(), this);

                default:
                    string message = Error.Format(SRResources.TypeCannotBeSerialized, edmType.ToTraceString(),
                        typeof(ODataMediaTypeFormatter).Name);
                    throw new SerializationException(message);
            }
        }

        public override ODataSerializer GetODataPayloadSerializer(IEdmModel model, Type type)
        {
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }

            // handle the special types.
            if (type == typeof(ODataWorkspace))
            {
                return new ODataWorkspaceSerializer();
            }
            else if (type == typeof(Uri))
            {
                return new ODataEntityReferenceLinkSerializer();
            }
            else if (type == typeof(ODataError) || type == typeof(HttpError))
            {
                return new ODataErrorSerializer();
            }
            else if (typeof(IEdmModel).IsAssignableFrom(type))
            {
                return new ODataMetadataSerializer();
            }

            // TODO: Feature #694 - support Uri[] => EntityReferenceLinks

            // if it is not a special type, assume it has a corresponding EdmType.
            Tuple<IEdmModel, Type> cacheKey = Tuple.Create(model, type);
            return _clrTypeMappingCache.GetOrAdd(cacheKey, (key) =>
            {
                IEdmModel m = key.Item1;
                Type t = key.Item2;
                IEdmTypeReference edmType = m.GetEdmTypeReference(t);
                if (edmType != null)
                {
                    return GetEdmTypeSerializer(edmType);
                }
                else
                {
                    return null;
                }
            });
        }
    }
}
