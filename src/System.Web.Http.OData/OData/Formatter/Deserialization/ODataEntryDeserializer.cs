﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;

namespace System.Web.Http.OData.Formatter.Deserialization
{
    /// <summary>
    /// Base class for all <see cref="ODataDeserializer" />'s that deserialize into an object backed by <see cref="IEdmType"/>.
    /// </summary>
    public abstract class ODataEntryDeserializer : ODataDeserializer
    {
        protected ODataEntryDeserializer(IEdmTypeReference edmType, ODataPayloadKind payloadKind)
            : base(payloadKind)
        {
            if (edmType == null)
            {
                throw Error.ArgumentNull("edmType");
            }

            EdmType = edmType;
        }

        protected ODataEntryDeserializer(IEdmTypeReference edmType, ODataPayloadKind payloadKind, ODataDeserializerProvider deserializerProvider)
            : this(edmType, payloadKind)
        {
            DeserializerProvider = deserializerProvider;
        }

        /// <summary>
        /// The edm type.
        /// </summary>
        public IEdmTypeReference EdmType { get; private set; }

        public IEdmModel EdmModel
        {
            get
            {
                return DeserializerProvider != null ? DeserializerProvider.EdmModel : null;
            }
        }

        /// <summary>
        /// The <see cref="ODataDeserializerProvider"/> to use for deserializing inner items.
        /// </summary>
        public ODataDeserializerProvider DeserializerProvider { get; private set; }

        /// <summary>
        /// Deserializes the item into a new object of type corresponding to <see cref="EdmType"/>.
        /// </summary>
        /// <param name="item">The item to deserialize.</param>
        /// <param name="readContext">The <see cref="ODataDeserializerContext"/></param>
        /// <returns>The deserialized object.</returns>
        public virtual object ReadInline(object item, ODataDeserializerContext readContext)
        {
            throw Error.NotSupported(SRResources.DoesNotSupportReadInLine, GetType().Name);
        }

        internal static void RecurseEnter(ODataDeserializerContext readContext)
        {
            if (!readContext.IncrementCurrentReferenceDepth())
            {
                throw Error.InvalidOperation(SRResources.RecursionLimitExceeded);
            }
        }

        internal static void RecurseLeave(ODataDeserializerContext readContext)
        {
            readContext.DecrementCurrentReferenceDepth();
        }

        internal static object CreateResource(IEdmComplexType edmComplexType, IEdmModel edmModel)
        {
            Type clrType = EdmLibHelpers.GetClrType(new EdmComplexTypeReference(edmComplexType, isNullable: true), edmModel);
            if (clrType == null)
            {
                throw Error.Argument("edmComplexType", SRResources.MappingDoesNotContainEntityType, edmComplexType.FullName());
            }

            return Activator.CreateInstance(clrType);
        }

        internal static void ApplyProperty(ODataProperty property, IEdmStructuredTypeReference resourceType, object resource, ODataDeserializerProvider deserializerProvider, ODataDeserializerContext readContext)
        {
            IEdmProperty edmProperty = resourceType.FindProperty(property.Name);

            string propertyName = property.Name;
            IEdmTypeReference propertyType = edmProperty != null ? edmProperty.Type : null; // open properties have null values

            // If we are in patch mode and we are deserializing an entity object then we are updating Delta<T> and not T.
            bool isDelta = readContext.IsPatchMode && resourceType.IsEntity();

            if (isDelta && resourceType.AsEntity().Key().Select(key => key.Name).Contains(propertyName))
            {
                // we are patching a key property.
                if (readContext.PatchKeyMode == PatchKeyMode.Ignore)
                {
                    return;
                }
                else if (readContext.PatchKeyMode == PatchKeyMode.Throw)
                {
                    throw Error.InvalidOperation(SRResources.CannotPatchKeyProperty, propertyName, resourceType.FullName(), typeof(PatchKeyMode).Name, PatchKeyMode.Throw);
                }
            }

            EdmTypeKind propertyKind;
            object value = ConvertValue(property.Value, ref propertyType, deserializerProvider, readContext, out propertyKind);

            if (propertyKind == EdmTypeKind.Collection)
            {
                SetCollectionProperty(resource, propertyName, isDelta, value);
            }
            else
            {
                if (propertyKind == EdmTypeKind.Primitive)
                {
                    value = EdmPrimitiveHelpers.ConvertPrimitiveValue(value, GetPropertyType(resource, propertyName, isDelta));
                }

                SetProperty(resource, propertyName, isDelta, value);
            }
        }

        internal static void SetCollectionProperty(object resource, string propertyName, bool isDelta, object value)
        {
            if (value != null)
            {
                IEnumerable collection = value as IEnumerable;
                Contract.Assert(collection != null, "SetCollectionProperty is always passed the result of ODataFeedDeserializer or ODataCollectionDeserializer");

                Type resourceType = resource.GetType();
                Type propertyType = GetPropertyType(resource, propertyName, isDelta);

                Type elementType;
                if (!propertyType.IsCollection(out elementType))
                {
                    throw Error.InvalidOperation(SRResources.PropertyIsNotCollection, propertyType.FullName, propertyName, resourceType.FullName);
                }

                IEnumerable newCollection;
                if (CanSetProperty(resource, propertyName, isDelta) &&
                    CollectionDeserializationHelpers.TryCreateInstance(propertyType, elementType, out newCollection))
                {
                    // settable collections
                    collection.AddToCollection(newCollection, elementType, resourceType, propertyName, propertyType);
                    if (propertyType.IsArray)
                    {
                        newCollection = CollectionDeserializationHelpers.ToArray(newCollection, elementType);
                    }

                    SetProperty(resource, propertyName, isDelta, newCollection);
                }
                else
                {
                    // get-only collections.
                    newCollection = GetProperty(resource, propertyName, isDelta) as IEnumerable;
                    if (newCollection == null)
                    {
                        throw Error.InvalidOperation(SRResources.CannotAddToNullCollection, propertyName, resourceType.FullName);
                    }

                    collection.AddToCollection(newCollection, elementType, resourceType, propertyName, propertyType);
                }
            }
        }

        internal static void SetProperty(object resource, string propertyName, bool isDelta, object value)
        {
            if (!isDelta)
            {
                resource.GetType().GetProperty(propertyName).SetValue(resource, value, index: null);
            }
            else
            {
                // If we are in patch mode and we are deserializing an entity object then we are updating Delta<T> and not T.
                (resource as IDelta).TrySetPropertyValue(propertyName, value);
            }
        }

        internal static object ConvertValue(object oDataValue, ref IEdmTypeReference propertyType, ODataDeserializerProvider deserializerProvider, ODataDeserializerContext readContext, out EdmTypeKind typeKind)
        {
            if (oDataValue == null)
            {
                typeKind = EdmTypeKind.None;
                return null;
            }

            ODataComplexValue complexValue = oDataValue as ODataComplexValue;
            if (complexValue != null)
            {
                typeKind = EdmTypeKind.Complex;
                return ConvertComplexValue(complexValue, ref propertyType, deserializerProvider, readContext);
            }

            ODataCollectionValue collection = oDataValue as ODataCollectionValue;
            if (collection != null)
            {
                typeKind = EdmTypeKind.Collection;
                Contract.Assert(propertyType != null, "Open collection properties are not supported.");
                return ConvertCollectionValue(collection, propertyType, deserializerProvider, readContext);
            }

            typeKind = EdmTypeKind.Primitive;
            return ConvertPrimitiveValue(oDataValue, ref propertyType);
        }

        internal static Type GetPropertyType(object resource, string propertyName, bool isDelta)
        {
            Contract.Assert(resource != null);
            Contract.Assert(propertyName != null);

            if (isDelta)
            {
                IDelta delta = resource as IDelta;
                Contract.Assert(delta != null);

                Type type;
                delta.TryGetPropertyType(propertyName, out type);
                return type;
            }
            else
            {
                PropertyInfo property = resource.GetType().GetProperty(propertyName);
                return property == null ? null : property.PropertyType;
            }
        }

        private static object ConvertComplexValue(ODataComplexValue complexValue, ref IEdmTypeReference propertyType, ODataDeserializerProvider deserializerProvider, ODataDeserializerContext readContext)
        {
            IEdmComplexTypeReference edmComplexType;
            if (propertyType == null)
            {
                // open complex property
                Contract.Assert(!String.IsNullOrEmpty(complexValue.TypeName), "ODataLib should have verified that open complex value has a type name since we provided metadata.");
                IEdmType edmType = deserializerProvider.EdmModel.FindType(complexValue.TypeName);
                Contract.Assert(edmType.TypeKind == EdmTypeKind.Complex, "ODataLib should have verified that complex value has a complex resource type.");
                edmComplexType = new EdmComplexTypeReference(edmType as IEdmComplexType, isNullable: true);
            }
            else
            {
                edmComplexType = propertyType.AsComplex();
            }

            ODataEntryDeserializer deserializer = deserializerProvider.GetODataDeserializer(edmComplexType);
            return deserializer.ReadInline(complexValue, readContext);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "propertyType", Justification = "TODO: remove when implement TODO below")]
        private static object ConvertPrimitiveValue(object oDataValue, ref IEdmTypeReference propertyType)
        {
            // TODO: Bug 467612: check for type conversion issues here
            Contract.Assert(propertyType == null || propertyType.TypeKind() == EdmTypeKind.Primitive, "Only primitive types are supported by this method.");

            return oDataValue;
        }

        private static bool CanSetProperty(object resource, string propertyName, bool isDelta)
        {
            if (isDelta)
            {
                return true;
            }
            else
            {
                PropertyInfo property = resource.GetType().GetProperty(propertyName);
                return property != null && property.GetSetMethod() != null;
            }
        }

        private static object GetProperty(object resource, string propertyName, bool isDelta)
        {
            if (isDelta)
            {
                IDelta delta = resource as IDelta;
                Contract.Assert(delta != null);

                object value;
                delta.TryGetPropertyValue(propertyName, out value);
                return value;
            }
            else
            {
                PropertyInfo property = resource.GetType().GetProperty(propertyName);
                Contract.Assert(property != null, "ODataLib should have already verified that the property exists on the type.");
                return property.GetValue(resource, index: null);
            }
        }

        private static object ConvertCollectionValue(ODataCollectionValue collection, IEdmTypeReference propertyType, ODataDeserializerProvider deserializerProvider, ODataDeserializerContext readContext)
        {
            IEdmCollectionTypeReference collectionType = propertyType as IEdmCollectionTypeReference;
            Contract.Assert(collectionType != null, "The type for collection must be a IEdmCollectionType.");

            ODataEntryDeserializer deserializer = deserializerProvider.GetODataDeserializer(collectionType);
            return deserializer.ReadInline(collection, readContext);
        }
    }
}
