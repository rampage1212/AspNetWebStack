﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Mvc.Filters;

namespace System.Web.Mvc
{
    /// <summary>Represents a filter attribute that overrides action filters defined at a higher level.</summary>
    public sealed class OverrideActionFiltersAttribute : FilterAttribute, IOverrideFilter
    {
        /// <inheritdoc />
        public Type FiltersToOverride
        {
            get { return typeof(IActionFilter); }
        }
    }
}
