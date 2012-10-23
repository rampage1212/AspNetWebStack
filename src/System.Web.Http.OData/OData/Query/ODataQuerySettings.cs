﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Http.OData.Query
{
    /// <summary>
    /// This class describes the settings to use during query composition.
    /// </summary>
    public class ODataQuerySettings
    {
        private HandleNullPropagationOption _handleNullPropagationOption = HandleNullPropagationOption.Default;
        private int _lambdaNestingLimit = 1;
        private int? _resultLimit;

        /// <summary>
        /// Instantiates a new instance of the <see cref="ODataQuerySettings"/> class
        /// and initializes the default settings.
        /// </summary>
        public ODataQuerySettings()
        {
            EnsureStableOrdering = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether query composition should
        /// alter the original query when necessary to ensure a stable sort order.
        /// </summary>
        /// <value>A <c>true</c> value indicates the original query should
        /// be modified when necessary to guarantee a stable sort order.
        /// A <c>false</c> value indicates the sort order can be considered
        /// stable without modifying the query.  Query providers that ensure
        /// a stable sort order should set this value to <c>false</c>.
        /// The default value is <c>true</c>.</value>
        public bool EnsureStableOrdering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how null propagation should
        /// be handled during query composition. 
        /// </summary>
        /// <value>
        /// The default is <see cref="HandleNullPropagationOption.Default"/>.
        /// </value>
        public HandleNullPropagationOption HandleNullPropagation
        {
            get
            {
                return _handleNullPropagationOption;
            }
            set
            {
                HandleNullPropagationOptionHelper.Validate(value, "value");
                _handleNullPropagationOption = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth of the Any or All elements nested inside the query.
        /// </summary>
        /// <remarks>
        /// This limit helps prevent Denial of Service attacks. The default value is 1.
        /// </remarks>
        /// <value>
        /// The maxiumum depth of the Any or All elements nested inside the query.
        /// </value>
        public int LambdaNestingLimit
        {
            get
            {
                return _lambdaNestingLimit;
            }
            set
            {
                if (value <= 0)
                {
                    throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, 1);
                }

                _lambdaNestingLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of query results to return.
        /// </summary>
        /// <value>
        /// The maximum number of query results to return, or <c>null</c> if there is no limit.
        /// </value>
        public int? ResultLimit
        {
            get
            {
                return _resultLimit;
            }
            set
            {
                if (value.HasValue && value <= 0)
                {
                    throw Error.ArgumentMustBeGreaterThanOrEqualTo("value", value, 1);
                }

                _resultLimit = value;
            }
        }
    }
}
