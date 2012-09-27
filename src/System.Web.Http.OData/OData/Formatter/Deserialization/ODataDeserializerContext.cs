﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Net.Http;
using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Formatter.Deserialization
{
    /// <summary>
    /// This class encapsulates the state and settings that get passed to <see cref="ODataDeserializer"/>
    /// from the <see cref="ODataMediaTypeFormatter"/>.
    /// </summary>
    public class ODataDeserializerContext
    {
        private const int MaxReferenceDepth = 200;
        private int _currentReferenceDepth = 0;
        private PatchKeyMode _patchKeyMode;

        /// <summary>
        /// Gets or sets whether the <see cref="ODataMediaTypeFormatter"/> is reading a 
        /// PATCH request.
        /// </summary>
        public bool IsPatchMode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PatchKeyMode"/> to be used when reading a PATCH request.
        /// </summary>
        public PatchKeyMode PatchKeyMode
        {
            get
            {
                return _patchKeyMode;
            }

            set
            {
                PatchKeyModeHelper.Validate(value, "value");
                _patchKeyMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the HttpRequestMessage. 
        /// The HttpRequestMessage can then be used by ODataDeserializers to learn more about the Request that triggered the deserialization
        /// </summary>
        public HttpRequestMessage Request
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or set the EdmModel associated with the Request.
        /// </summary>
        public IEdmModel Model
        {
            get;
            set;
        }

        /// <summary>
        /// Increments the current reference depth.
        /// </summary>
        /// <returns><c>false</c> if the current reference depth is greater than the maximum allowed and <c>false</c> otherwise.</returns>
        public bool IncrementCurrentReferenceDepth()
        {
            if (++_currentReferenceDepth > MaxReferenceDepth)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Decrements the current reference depth.
        /// </summary>
        public void DecrementCurrentReferenceDepth()
        {
            _currentReferenceDepth--;
            Contract.Assert(_currentReferenceDepth >= 0);
        }
    }
}
