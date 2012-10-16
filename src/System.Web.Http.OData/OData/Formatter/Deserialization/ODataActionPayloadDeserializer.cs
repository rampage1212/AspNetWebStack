﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;

namespace System.Web.Http.OData.Formatter.Deserialization
{
    internal class ODataActionPayloadDeserializer : ODataDeserializer
    {
        private ODataDeserializerProvider _provider;
        private Type _payloadType;

        public ODataActionPayloadDeserializer(Type payloadType, ODataDeserializerProvider provider)
            : base(ODataPayloadKind.Parameter)
        {
            Contract.Assert(payloadType != null);
            Contract.Assert(provider != null);
            _payloadType = payloadType;
            _provider = provider;
        }

        public override object Read(ODataMessageReader messageReader, ODataDeserializerContext readContext)
        {
            // Create the correct resource type;
            ODataActionParameters payload = CreateNewPayload();

            IEdmFunctionImport action = payload.GetFunctionImport(readContext);
            ODataParameterReader reader = messageReader.CreateODataParameterReader(action);

            while (reader.Read())
            {
                string parameterName = null;
                IEdmFunctionParameter parameter = null;

                switch (reader.State)
                {
                    case ODataParameterReaderState.Value:
                        parameterName = reader.Name;
                        parameter = action.Parameters.SingleOrDefault(p => p.Name == parameterName);
                        // ODataLib protects against this but asserting just in case.
                        Contract.Assert(parameter != null, String.Format(CultureInfo.InvariantCulture, "Parameter '{0}' not found.", parameterName));
                        payload[parameterName] = Convert(reader.Value, parameter.Type, readContext);
                        break;

                    case ODataParameterReaderState.Collection:
                        parameterName = reader.Name;
                        parameter = action.Parameters.SingleOrDefault(p => p.Name == parameterName);
                        // ODataLib protects against this but asserting just in case.
                        Contract.Assert(parameter != null, String.Format(CultureInfo.InvariantCulture, "Parameter '{0}' not found.", parameterName));
                        IEdmCollectionTypeReference collectionType = parameter.Type as IEdmCollectionTypeReference;
                        Contract.Assert(collectionType != null);

                        payload[parameterName] = Convert(reader.CreateCollectionReader(), collectionType, readContext);
                        break;

                    default:
                        break;
                }
            }

            return payload;
        }

        private ODataActionParameters CreateNewPayload()
        {
            if (_payloadType == typeof(ODataActionParameters))
            {
                return new ODataActionParameters();
            }
            else
            {
                return Activator.CreateInstance(_payloadType, false) as ODataActionParameters;
            }
        }

        private object Convert(object value, IEdmTypeReference parameterType, ODataDeserializerContext readContext)
        {
            if (parameterType.IsPrimitive())
            {
                return value;
            }
            else
            {
                ODataEntryDeserializer deserializer = _provider.GetODataDeserializer(parameterType);
                return deserializer.ReadInline(value, readContext);
            }
        }

        private object Convert(ODataCollectionReader reader, IEdmCollectionTypeReference collectionType, ODataDeserializerContext readContext)
        {
            IEdmTypeReference elementType = collectionType.ElementType();
            Type clrElementType = EdmLibHelpers.GetClrType(elementType, readContext.Model);
            IList list = Activator.CreateInstance(typeof(List<>).MakeGenericType(clrElementType)) as IList;

            while (reader.Read())
            {
                switch (reader.State)
                {
                    case ODataCollectionReaderState.Value:
                        object element = Convert(reader.Item, elementType, readContext);
                        list.Add(element);
                        break;

                    default:
                        break;
                }
            }
            return list;
        }
    }
}