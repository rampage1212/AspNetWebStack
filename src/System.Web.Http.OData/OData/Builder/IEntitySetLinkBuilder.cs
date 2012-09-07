﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Builder
{
    public interface IEntitySetLinkBuilder
    {
        /// <summary>
        /// Builds the feed self link.
        /// </summary>
        /// <param name="context">The entity set instance context.</param>
        /// <returns>A URI </returns>
        Uri BuildFeedSelfLink(FeedContext context);

        string BuildIdLink(EntityInstanceContext context);

        Uri BuildEditLink(EntityInstanceContext context);

        Uri BuildReadLink(EntityInstanceContext context);

        Uri BuildNavigationLink(EntityInstanceContext context, IEdmNavigationProperty navigationProperty);
    }
}
