﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace System.Web.Http.Routing
{
    public interface IInlineRouteConstraintResolver
    {
        IHttpRouteConstraint ResolveConstraint(string constraintKey);
    }
}