﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Http.OData.Builder.Conventions
{
    /// <summary>
    /// <see cref="EntityTypeConvention"/> to figure out if an entity is abstract or not.
    /// <remarks>This convention configures all entity types backed by an abstract clr type as abstract entities.</remarks>
    /// </summary>
    public class AbstractEntityTypeDiscoveryConvention : EntityTypeConvention
    {
        public override void Apply(IEntityTypeConfiguration entity, ODataModelBuilder model)
        {
            if (entity.IsAbstract == null)
            {
                entity.IsAbstract = entity.ClrType.IsAbstract;
            }
        }
    }
}
