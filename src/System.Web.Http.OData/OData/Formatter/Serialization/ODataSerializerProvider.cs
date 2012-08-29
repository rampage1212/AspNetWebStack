﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Formatter.Serialization
{
    /// <summary>
    /// An ODataSerializerProvider is a factory for creating ODataSerializers.
    /// </summary>
    public abstract class ODataSerializerProvider
    {
        private ConcurrentDictionary<IEdmTypeReference, ODataSerializer> _serializerCache;

        protected ODataSerializerProvider(IEdmModel edmModel)
        {
            if (edmModel == null)
            {
                throw Error.ArgumentNull("edmModel");
            }

            EdmModel = edmModel;
            _serializerCache = new ConcurrentDictionary<IEdmTypeReference, ODataSerializer>(new EdmTypeReferenceEqualityComparer());
        }

        /// <summary>
        /// Gets or Sets the <see cref="IEdmModel" />
        /// </summary>
        public IEdmModel EdmModel { get; private set; }

        /// <summary>
        /// Gets an <see cref="ODataSerializer" /> for the given edmType.
        /// </summary>
        /// <param name="edmType">The <see cref="IEdmTypeReference" /></param>
        /// <returns>The <see cref="ODataSerializer" /></returns>
        public ODataSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType == null)
            {
                throw Error.ArgumentNull("edmType");
            }
            return _serializerCache.GetOrAdd(edmType, CreateEdmTypeSerializer);
        }

        /// <summary>
        /// Gets an <see cref="ODataSerializer" /> for the given clr <see cref="Type" />.
        /// </summary>
        /// <param name="type">The <see cref="Type" /></param>
        /// <returns>The <see cref="ODataSerializer" /></returns>
        public abstract ODataSerializer GetODataPayloadSerializer(Type type);

        /// <summary>
        /// Sets the ODataSerializer for the given edmType in the serializer cache.
        /// </summary>
        /// <param name="edmType"></param>
        /// <param name="serializer"></param>
        public void SetEdmTypeSerializer(IEdmTypeReference edmType, ODataSerializer serializer)
        {
            if (edmType == null)
            {
                throw Error.ArgumentNull("edmType");
            }

            if (serializer == null)
            {
                throw Error.ArgumentNull("serializer");
            }

            _serializerCache.AddOrUpdate(edmType, serializer, (t, s) => serializer);
        }

        /// <summary>
        /// Creates a new instance of the ODataSerializer for the given edm type.
        /// </summary>
        /// <param name="type">The <see cref="IEdmTypeReference" /></param>
        /// <returns>The constructed <see cref="ODataSerializer" /></returns>
        public abstract ODataSerializer CreateEdmTypeSerializer(IEdmTypeReference type);
    }
}
