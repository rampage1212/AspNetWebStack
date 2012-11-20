﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Http
{
    /// <summary>
    /// This class contains route constants for OData.
    /// </summary>
    public static class ODataRouteConstants
    {
        /// <summary>
        /// Route name for the OData route
        /// </summary>
        public static readonly string RouteName = "OData";

        /// <summary>
        /// Route variable name for the OData path.
        /// </summary>
        public static readonly string ODataPath = "odataPath";

        /// <summary>
        /// Wildcard route template for the OData path route variable.
        /// </summary>
        public static readonly string ODataPathTemplate = "{*" + ODataRouteConstants.ODataPath + "}";

        /// <summary>
        /// Parameter name to use for the OData path route constraint.
        /// </summary>
        public static readonly string ConstraintName = "ODataConstraint";

        /// <summary>
        /// Route data key for the action name.
        /// </summary>
        public static readonly string Action = "action";

        /// <summary>
        /// Route data key for entity keys.
        /// </summary>
        public static readonly string Key = "key";

        /// <summary>
        /// Route data key for the related key when deleting links.
        /// </summary>
        public static readonly string RelatedKey = "relatedKey";

        /// <summary>
        /// Route data key for the navigation property name when manipulating links.
        /// </summary>
        public static readonly string NavigationProperty = "navigationProperty";
    }
}
